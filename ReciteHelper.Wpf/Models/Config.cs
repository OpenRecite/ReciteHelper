using ReciteHelper.Core.Configuration;

namespace ReciteHelper.Wpf.Models;

public static class Config
{
    public static ConfigOptions Configure { get; private set; } = new();

    public static void Use(ConfigOptions config)
    {
        Configure = config;
    }
}
