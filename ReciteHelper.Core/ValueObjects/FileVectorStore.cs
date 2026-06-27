using ReciteHelper.Core.Entities;
using System.Text.Json;

namespace ReciteHelper.Core.ValueObjects;

public class FileVectorStore
{
    private readonly string _filePath;
    private List<VectorEntry> _entries = [];

    public FileVectorStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public IReadOnlyList<VectorEntry> Entries => _entries;

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

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_entries, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
            return;

        _entries = JsonSerializer.Deserialize<List<VectorEntry>>(json) ?? [];
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0;

        float dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
            return 0;

        return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
