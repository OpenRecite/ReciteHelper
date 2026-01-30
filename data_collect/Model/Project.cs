using System.Text.Json.Serialization;

namespace ReciteHelper.DataCollect.Model;

public class Project
{
    [JsonPropertyName("basic_info")]
    public UserBasic? UserBasic { get; set; }

    [JsonPropertyName("questions")]
    public List<Question> Questions { get; set; } = new();

    [JsonPropertyName("r0")]
    public double Speed { get; set; } = .0d;

}