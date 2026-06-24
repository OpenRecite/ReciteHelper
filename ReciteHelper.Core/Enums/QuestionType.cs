using System.Text.Json.Serialization;
using ReciteHelper.Core.Serialization;

namespace ReciteHelper.Core.Enums;

[JsonConverter(typeof(QuestionTypeJsonConverter))]
public enum QuestionType
{
    ShortAnswer = 0,
    SingleChoice = 1
}
