namespace ReciteHelper.Core.ValueObjects;

public class Chunk(int index, string content) : IEquatable<Chunk>
{
    public int Index { get; set; } = index;
    public string Content { get; set; } = content;
    public bool IsSuccess { get; set; } = false;

    public bool Equals(Chunk? other)
    {
        if (other is null) return false;
        return Index == other.Index && Content == other.Content;
    }

    public override bool Equals(object? obj) => Equals(obj as Chunk);
    public override int GetHashCode() => HashCode.Combine(Index, Content);

    public override string ToString()
    {
        return $"Index:{Index}\nContent:{Content}\nStatus:{IsSuccess}";
    }
}