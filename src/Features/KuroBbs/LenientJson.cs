using System;
using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arietta.Features.KuroBbs;

public static class LenientJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new LenientInt32Converter());
        options.Converters.Add(new LenientInt64Converter());
        options.Converters.Add(new LenientBooleanConverter());
        options.Converters.Add(new LenientStringConverter());
        return options;
    }

    private static bool TryReadNumber(ref Utf8JsonReader reader, out long value)
    {
        value = 0;
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return false;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out value);
            case JsonTokenType.String:
            {
                var raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return false;
                }

                var span = raw!.Trim().TrimEnd('%');

                var comma = span.IndexOf(',');
                if (comma >= 0)
                {
                    span = span.Replace(",", "");
                }

                var dot = span.IndexOf('.');
                if (dot >= 0)
                {
                    span = span[..dot];
                }

                return long.TryParse(span, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
            }
            default:

                reader.Skip();
                return false;
        }
    }

    private sealed class LenientInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TryReadNumber(ref reader, out var value) ? (int)value : 0;

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    private sealed class LenientInt64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => TryReadNumber(ref reader, out var value) ? value : 0;

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    private sealed class LenientBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                case JsonTokenType.False:
                    return reader.GetBoolean();
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out var n) && n != 0;
                case JsonTokenType.String:
                {
                    var raw = reader.GetString()?.Trim();
                    if (bool.TryParse(raw, out var b))
                    {
                        return b;
                    }

                    return long.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var num) && num != 0;
                }
                default:
                    reader.Skip();
                    return false;
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteBooleanValue(value);
    }

    private sealed class LenientStringConverter : JsonConverter<string>
    {

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null!;
                case JsonTokenType.String:
                    return reader.GetString()!;
                case JsonTokenType.Number:
                {

                    var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                    return System.Text.Encoding.UTF8.GetString(span);
                }
                default:
                    reader.Skip();
                    return null!;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
