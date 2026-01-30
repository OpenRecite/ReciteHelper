using System.Text.Json.Serialization;

namespace ReciteHelper.DataCollect.Model;

public class AnswerRecord
{
    [JsonPropertyName("datetime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("speed")]
    public int Speed { get; set; }

    [JsonPropertyName("s")]
    public int Similarity {  get; set; }

    [JsonPropertyName("q")]
    public int QMark {  get; set; }

    [JsonPropertyName("q_pred")]
    public int QPredict {  get; set; }
}
