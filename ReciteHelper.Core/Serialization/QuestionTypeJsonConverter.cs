using System.Text.Json;
using System.Text.Json.Serialization;
using ReciteHelper.Core.Enums;

namespace ReciteHelper.Core.Serialization;

public sealed class QuestionTypeJsonConverter : JsonConverter<QuestionType>
{
    public override QuestionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
            return Enum.IsDefined(typeof(QuestionType), number) ? (QuestionType)number : QuestionType.ShortAnswer;

        if (reader.TokenType != JsonTokenType.String)
            return QuestionType.ShortAnswer;

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return QuestionType.ShortAnswer;

        return Enum.TryParse<QuestionType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : QuestionType.ShortAnswer;
    }

    public override void Write(Utf8JsonWriter writer, QuestionType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
