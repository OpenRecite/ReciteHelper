using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IKnowledgeBaseService
{
    public Task<FileVectorStore> Build(string projectPath, string content);
}
