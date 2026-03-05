using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public class ChapterCluster : ValueObject
{
    [JsonPropertyName("names")]
    public List<string>? Chapters { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("uname")]
    public string? UnifiedName { get; set; }

    [JsonConstructor]
    public ChapterCluster() { }

    public override T Clone<T>()
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        throw new NotImplementedException();
    }

    protected override void Validate()
    {
        return;
    }
}
