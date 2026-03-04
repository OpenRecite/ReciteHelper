using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

public class ReviewTag : Entity
{
    [JsonPropertyName("similarity")]
    public double Similarity { get; set; }

    [JsonPropertyName("rate")]
    public double Rate { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("q_value")]
    public int QValue { get; set; }
}
