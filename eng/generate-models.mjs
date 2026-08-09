import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";
import yaml from "js-yaml";

const [contractPath, outputPath] = process.argv.slice(2);
if (!contractPath || !outputPath) {
  throw new Error("Usage: node eng/generate-models.mjs <openapi.yaml> <Models.g.cs>");
}

const contractLock = JSON.parse(
  fs.readFileSync(new URL("./openapi.lock.json", import.meta.url), "utf8"),
);
const contractSource = fs.readFileSync(contractPath);
const contractDigest = crypto.createHash("sha256").update(contractSource).digest("hex");
if (contractDigest !== contractLock.sha256) {
  throw new Error(`OpenAPI SHA-256 mismatch: expected ${contractLock.sha256}, received ${contractDigest}`);
}

const contract = yaml.load(contractSource.toString("utf8"));
const schemas = contract.components.schemas;
const declarations = [];
const emitted = new Set();

const pascal = (value) => value
  .split(/[^A-Za-z0-9]+/)
  .filter(Boolean)
  .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
  .join("");

function resolveReference(reference) {
  return schemas[reference.split("/").at(-1)];
}

function isNullable(schema) {
  if (Array.isArray(schema?.type)) return schema.type.includes("null");
  return schema?.type === "null" || schema?.oneOf?.some((item) => item.type === "null");
}

function withoutNull(schema) {
  if (schema?.oneOf) {
    return schema.oneOf.find((item) => item.type !== "null") ?? schema;
  }
  if (Array.isArray(schema?.type)) {
    return { ...schema, type: schema.type.find((item) => item !== "null") };
  }
  return schema;
}

function typeFor(schema, ownerName, propertyName) {
  const nullable = isNullable(schema);
  const core = withoutNull(schema);

  if (core.allOf?.length) {
    return typeFor(core.allOf.find((item) => item.$ref) ?? core.allOf[0], ownerName, propertyName);
  }

  if (core.$ref) {
    const name = core.$ref.split("/").at(-1);
    const referenced = resolveReference(core.$ref);
    const result = referenced.type === "array"
      ? `IReadOnlyList<${typeFor(referenced.items, ownerName, propertyName)}>`
      : name;
    return nullable && !result.endsWith("?") ? `${result}?` : result;
  }

  if (core.type === "array") {
    const itemName = `${ownerName}${pascal(propertyName)}Item`;
    let itemType;
    if (core.items.type === "object" || core.items.properties) {
      emitObject(itemName, core.items);
      itemType = itemName;
    } else {
      itemType = typeFor(core.items, itemName, "value");
    }
    const result = `IReadOnlyList<${itemType}>`;
    return nullable ? `${result}?` : result;
  }

  if (core.type === "object" || core.properties) {
    const nestedName = `${ownerName}${pascal(propertyName)}`;
    emitObject(nestedName, core);
    return nullable ? `${nestedName}?` : nestedName;
  }

  let result;
  if (core.type === "integer") result = "long";
  else if (core.type === "number") result = "decimal";
  else if (core.type === "boolean") result = "bool";
  else if (core.type === "string" && core.format === "date-time") result = "DateTimeOffset";
  else if (core.type === "string" && core.format === "uri") result = "Uri";
  else result = "string";
  return nullable ? `${result}?` : result;
}

function defaultFor(type, schema, required) {
  if (!required || type.endsWith("?")) return "";
  if (type === "string") {
    if (schema.const !== undefined) return ` = ${JSON.stringify(schema.const)};`;
    return " = string.Empty;";
  }
  if (type.startsWith("IReadOnlyList<")) return ` = Array.Empty<${type.slice(14, -1)}>();`;
  if (!["long", "decimal", "bool", "DateTimeOffset", "Uri"].includes(type)) return ` = new ${type}();`;
  if (type === "Uri") return " = null!;";
  return "";
}

function emitObject(name, schema) {
  if (emitted.has(name)) return;
  emitted.add(name);
  const properties = Object.entries(schema.properties ?? {});
  const required = new Set(schema.required ?? []);
  const lines = [];
  lines.push(`public sealed class ${name}`);
  lines.push("{");
  for (const [jsonName, propertySchema] of properties) {
    let propertyType = typeFor(propertySchema, name, jsonName);
    const optionalInput = !required.has(jsonName) && name.includes("Input");
    if (optionalInput) propertyType = `RequestValue<${propertyType}>`;
    else if (!required.has(jsonName) && !propertyType.endsWith("?")) propertyType += "?";
    const propertyName = pascal(jsonName);
    lines.push(`    [JsonPropertyName(${JSON.stringify(jsonName)})]`);
    if (optionalInput) lines.push("    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]");
    lines.push(`    public ${propertyType} ${propertyName} { get; set; }${defaultFor(propertyType, propertySchema, required.has(jsonName))}`);
    lines.push("");
  }
  if (properties.length) lines.pop();
  lines.push("}");
  declarations.push(lines.join("\n"));
}

for (const [name, schema] of Object.entries(schemas)) {
  if (schema.type === "array" || name === "ErrorEnvelope" || name === "PreauthorizationDecisionInput") continue;
  emitObject(name, schema);
}

const preauthorizationDecisionInput = `/// <summary>Base type for a pre-authorization decision.</summary>
public abstract class PreauthorizationDecisionInput
{
    [JsonPropertyName("outcome")]
    public abstract string Outcome { get; }

    [JsonPropertyName("reason_codes")]
    public IReadOnlyList<string> ReasonCodes { get; set; } = Array.Empty<string>();

    [JsonPropertyName("policy_reference")]
    public string PolicyReference { get; set; } = string.Empty;

    [JsonPropertyName("policy_version")]
    public string PolicyVersion { get; set; } = string.Empty;

    [JsonPropertyName("evidence_references")]
    public IReadOnlyList<string> EvidenceReferences { get; set; } = Array.Empty<string>();
}

/// <summary>Approves a pre-authorization.</summary>
public sealed class ApprovePreauthorizationInput : PreauthorizationDecisionInput
{
    public override string Outcome => "approved";

    [JsonPropertyName("approved_amount")]
    public long ApprovedAmount { get; set; }

    [JsonPropertyName("valid_until")]
    public DateTimeOffset ValidUntil { get; set; }
}

/// <summary>Denies a pre-authorization.</summary>
public sealed class DenyPreauthorizationInput : PreauthorizationDecisionInput
{
    public override string Outcome => "denied";
}`;

const header = `// <auto-generated />
// Source: heyrafiki/openapi@${contractLock.commit}
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Heyrafiki.Models;
`;

const output = `${header}\n${declarations.join("\n\n")}\n\n${preauthorizationDecisionInput}\n`;
fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(outputPath, output, "utf8");
