using ReciteHelper.SharedKernel;

namespace ReciteHelper.Core.Entities;

public class Semantics : Entity
{
    public int Id { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Summary { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is Semantics other && other.Summary == this.Summary;
    }

    public override int GetHashCode()
    {
        return Summary.GetHashCode();
    }
}

