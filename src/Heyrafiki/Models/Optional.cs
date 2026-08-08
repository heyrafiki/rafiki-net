using System.Text.Json;
using System.Text.Json.Serialization;

namespace Heyrafiki.Models;

/// <summary>Represents an optional request property and preserves the difference between omitted and null.</summary>
[JsonConverter(typeof(RequestValueJsonConverterFactory))]
public readonly struct RequestValue<T>
{
    /// <summary>Initializes a supplied request property.</summary>
    public RequestValue(T value)
    {
        Value = value;
        HasValue = true;
    }

    /// <summary>Gets whether the property was supplied.</summary>
    public bool HasValue { get; }

    /// <summary>Gets the supplied value.</summary>
    public T? Value { get; }

    /// <summary>Converts a value to a supplied request property.</summary>
    public static implicit operator RequestValue<T>(T value) => new(value);
}

internal sealed class RequestValueJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(RequestValue<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(RequestValueJsonConverter<>).MakeGenericType(valueType),
            nonPublic: true)!;
    }

    private sealed class RequestValueJsonConverter<TValue> : JsonConverter<RequestValue<TValue>>
    {
        public override RequestValue<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<TValue>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, RequestValue<TValue> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
