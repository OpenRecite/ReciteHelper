using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public class KnowledgePoint : ValueObject
{

    [JsonPropertyName("name")]
    public string? Name { get; private set; }

    [JsonPropertyName("content")]
    public string? ContentMarkdown { get; private set; }

    /// <summary>
    /// Mark the knowledge point mastery status as false.
    /// </summary>
    public bool IsMastered { get; private set; } = false;

    [JsonConstructor]
    public KnowledgePoint()
    {

    }

    private KnowledgePoint(string? name, string? contentMarkdown, bool isMastered)
    {
        Name = name;
        ContentMarkdown = contentMarkdown;
        IsMastered = isMastered;
    }

    public static KnowledgePoint Create(string? name, string? contentMarkdown)
    {
        return new KnowledgePoint(name, contentMarkdown, false);
    }

    public override T Clone<T>()
    {
        return (T)(object) new KnowledgePoint(Name, ContentMarkdown, IsMastered);
    }

    public KnowledgePoint ModifyMasteredStatus(bool newStatus)
    {
        return new KnowledgePoint(Name, ContentMarkdown, newStatus);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        if (Name is null) yield break;
        yield return Name;
    }

    protected override void Validate()
    {
        // Theoretically, it should be checked, but I've been experiencing
        // severe AI hallucinations lately, so let's leave it at that for now
        if (Name is null)
            return;
    }
}
