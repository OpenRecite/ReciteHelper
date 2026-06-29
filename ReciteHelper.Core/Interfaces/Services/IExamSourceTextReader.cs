namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamSourceTextReader
{
    Task<string> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
