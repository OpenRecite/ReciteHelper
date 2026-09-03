using ReciteHelper.Core.Interfaces.Configuration;
using ReciteHelper.Core.Configuration;
using ReciteHelper.Core.Exceptions;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ReciteHelper.Infrastructure.Configuration;

public class ConfigService : IConfigService
{
    private readonly string _configPath;

    public ConfigService() 
    {
        _configPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Config.xml");
    }

    public async Task<ConfigOptions> LoadAsync()
    {
        if (!File.Exists(_configPath))
            return new ConfigOptions();

        try
        {
            var serializer = new XmlSerializer(typeof(ConfigOptions));
            await using var stream = File.OpenRead(_configPath);
            var config = (ConfigOptions?)serializer.Deserialize(stream);

            if (config is null)
                return new ConfigOptions();

            config.DeepSeekKey = ResolveConfigText(config.DeepSeekKey);
            config.QwenKey = ResolveConfigText(config.QwenKey);
            config.OpenRouterKey = ResolveConfigText(config.OpenRouterKey);
            return config;
        }
        catch (Exception ex)
        {

            throw new ConfigurationException($"Failed to load configuration: {ex.Message}.");
        }
    }

    public async Task SaveAsync(ConfigOptions config)
    {
        var serializer = new XmlSerializer(typeof(ConfigOptions));
        await using var stream = File.Create(_configPath);
        serializer.Serialize(stream, config);
    }

    private static string? ResolveConfigText(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('%'))
            return text;

        // This damn AI has completely lost its mind; as long as it runs here,
        // that’s good enough—I can't be bothered to do anything else
        var match = Regex.Match(
            text,
            "^%\\s*Environment\\.GetEnvironmentVariable\\(\"(?<name>[A-Za-z_][A-Za-z0-9_]*)\"\\)\\s*%$");

        if (!match.Success)
            return text;

        return Environment.GetEnvironmentVariable(match.Groups["name"].Value);
    }
}
