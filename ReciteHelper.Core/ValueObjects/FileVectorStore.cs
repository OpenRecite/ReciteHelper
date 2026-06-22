using ReciteHelper.Core.Entities;
using System.Text.Json;

namespace ReciteHelper.Core.ValueObjects;

public class FileVectorStore(string filePath)
{
    private List<VectorEntry> _entries = new();

    public void Add(VectorEntry entry)
    {
        _entries.Add(entry);
        Save();
    }

    public void AddRange(List<VectorEntry> entries)
    {
        _entries.AddRange(entries);
        Save();
    }

    public List<(VectorEntry Entry, float Score)> Search(float[] queryVector, int topK = 5)
    {
        return _entries
            .Select(e => (e, CosineSimilarity(queryVector, e.Vector)))
            .OrderByDescending(x => x.Item2)
            .Take(topK)
            .ToList();
    }

    private void Save() => File.WriteAllText(filePath, JsonSerializer.Serialize(_entries));
    private void Load()
    {
        if (File.Exists(filePath))
            _entries = JsonSerializer.Deserialize<List<VectorEntry>>(File.ReadAllText(filePath)) ?? new();
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
