using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IKnowledgeBaseService
{
    Task<FileVectorStore> Build(string projectPath, string content);

    Task<IReadOnlyList<KnowledgeBaseMatch>> SearchAsync(
        FileVectorStore store,
        string query,
        int topK,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
