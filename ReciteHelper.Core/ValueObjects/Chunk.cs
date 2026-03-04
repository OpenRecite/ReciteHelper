using ReciteHelper.Core.Entities;
using ReciteHelper.SharedKernel;

namespace ReciteHelper.Core.ValueObjects;

public class Chunk : ValueObject
{
    public int Index { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsSuccess { get; private set; } = false;

    private Chunk(string content, bool isSuccess, int index)
    {
        Content = content;
        IsSuccess = isSuccess;
        Index = index;

        Validate();
    }

    public static Chunk Create(string content, bool isSuccess, int index)
    {
        return Create(() => 
        { 
            return new Chunk(content, isSuccess, index); 
        });
    }

    public override T Clone<T>()
    {
        return (T)(object)new Chunk(Content, IsSuccess, Index);
    }

    public Chunk MarkAsSucceed()
    {
        return new Chunk(Content, true, Index);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Content;
        yield return IsSuccess;
    }

    protected override void Validate()
    {
        if (Content.Length > 10000)
            throw new ArgumentException("Chunk is too long");
    }

    public override string ToString()
    {
        var status = IsSuccess ? "✓" : "✗";
        var preview = Content.Length > 30
            ? Content[..30] + "..."
            : Content;

        return $"Chunk[{Index}] {status}: {preview}";
    }
}