using System.Text.Json.Serialization;

namespace ReciteHelper.DataCollect.Model;

public class UserBasic
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("ad")]
    public string AcademicDiscipline {  get; set; } = string.Empty;

    [JsonPropertyName("major")]
    public string Major { get; set; } = string.Empty;

    [JsonPropertyName("workplace")]

    public string Workplace { get; set;  } = string.Empty;
}
