using ReciteHelper.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReciteHelper.Core.Configuration;

public class ConfigOptions
{
    public const string SectionName = "ReciteHelper";

    public string Version { get; set; } = "v3";
    public string? DeepSeekKey { get; set; }
    public string? OCRAccess { get; set; }
    public string? OCRSecret { get; set; }
    public int RStandard { get; set; } = 60;
    public PhonkOptions PhonkOptions { get; set; } = new();
    public MissingStrategy Strategy { get; set; } = MissingStrategy.Ignore;
}