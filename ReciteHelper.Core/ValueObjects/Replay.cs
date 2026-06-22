using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.SharedKernel;
using System.Collections.Concurrent;

namespace ReciteHelper.Core.ValueObjects;

public class Replay : ValueObject
{
    public List<Chunk> Chunks { get; private set; } 
    public ConcurrentBag<List<Chapter>> Chapters { get; private set; }

    private Replay(List<Chunk> chunks, ConcurrentBag<List<Chapter>> chapters)
    {
        Chunks = chunks;
        Chapters = chapters;
    }

    protected override void Validate()
    {
        // If anything goes wrong here, I'll go eat shit
        return;
    }

    public static Replay Create(List<Chunk> chunks, ConcurrentBag<List<Chapter>> chapters)
    {
        return Create(() =>
        {
            return new Replay(chunks, chapters);
        });
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Chunks;
        yield return Chapters;
    }

    public override T Clone<T>()
    {
        // Still too lazy to write
        throw new NotImplementedException();
    }
}
