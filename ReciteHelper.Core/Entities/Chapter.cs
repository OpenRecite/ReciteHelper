using ReciteHelper.Core.ValueObjects;
using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

public class Chapter : Entity
{
    [JsonConstructor]
    public Chapter() { }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("bank")]
    public List<Question>? Questions { get; set; }

    [JsonPropertyName("know")]
    public List<KnowledgePoint>? KnowledgePoints { get; set; }
}
