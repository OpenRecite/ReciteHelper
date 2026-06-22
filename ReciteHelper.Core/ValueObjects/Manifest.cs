using ReciteHelper.Core.Enums;
using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public class Manifest : ValueObject
{
    [JsonPropertyName("bankfile")]
    public string? BankFile { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("projectfile")]
    public string? ProjectFile { get; set; }

    private Manifest(string? bankFile, string? version, string? projectFile)
    {
        BankFile = bankFile;
        Version = version;
        ProjectFile = projectFile;
    }

    public static Manifest Create(string? bankFile, string? version, string? projectFile)
    {
        return Create(() =>
        {
            return new Manifest(bankFile, version, projectFile);
        });
    }

    [JsonConstructor]
    public Manifest() { }

    public override T Clone<T>()
    {
        throw new NotImplementedException();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield break;
    }

    protected override void Validate()
    {
        return;
    }
}
