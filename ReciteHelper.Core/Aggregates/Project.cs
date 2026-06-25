using ReciteHelper.Core.Entities;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Aggregates;

public class Project : AggregateRoot
{
    [JsonConstructor]
    public Project() { }

    [JsonPropertyName("name")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("path")]
    public string? StoragePath { get; set; }

    [JsonPropertyName("bankfile")]
    public string? QuestionBankPath { get; set; }

    [JsonPropertyName("chapter")]
    public List<Chapter>? Chapters { get; set; }

    [JsonPropertyName("knowledge_base")]
    public string? KnowledgeBasePath { get; set; }

    [JsonPropertyName("knowledge_base_error")]
    public string? KnowledgeBaseBuildError { get; set; }

    [JsonIgnore]
    public FileVectorStore? KnowledgeBase { get; private set; }

    [JsonPropertyName("last_accessed")]
    public DateTime LastAccessed { get; private set; }

    public List<Question> ExportQuestions()
    {
        List<Question> questions = [];

        foreach (var chapter in Chapters!)
            questions.AddRange(chapter.Questions!);
        return questions;
    }

    public void UpdateLastAccessed()
    {
        LastAccessed = DateTime.Now;
    }

    public void AttachKnowledgeBase(string relativePath, FileVectorStore store)
    {
        KnowledgeBasePath = relativePath;
        KnowledgeBase = store;
        KnowledgeBaseBuildError = null;
    }

    public void LoadKnowledgeBase(FileVectorStore store)
    {
        KnowledgeBase = store;
    }

    public void MarkKnowledgeBaseBuildFailed(string error)
    {
        KnowledgeBaseBuildError = error;
    }
}
