using ReciteHelper.Core.Enums;
using System.Xml.Serialization;

namespace ReciteHelper.Core.Configuration;

[XmlRoot("Config")]
public class ConfigOptions
{
    public const string SectionName = "ReciteHelper";

    public string Version { get; set; } = "v3";
    public string? DeepSeekKey { get; set; }
    public string? OCRAccess { get; set; }
    public string? OCRSecret { get; set; }
    public int RStandard { get; set; } = 60;
    public PhonkOptions PhonkOptions { get; set; } = new();
    [XmlElement("MissingStrategy")]
    public MissingStrategy Strategy { get; set; } = MissingStrategy.Ignore;
}
