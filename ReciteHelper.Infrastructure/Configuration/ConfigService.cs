using Microsoft.Extensions.Options;
using ReciteHelper.Core.Configuration;
using ReciteHelper.Core.Exceptions;
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
        // This exclamation mark is not suppressing null checks
        // It's just emphasizing that this part hasn't been reconstructed yet
        return new()!;

        if (!File.Exists(_configPath))
            return new ConfigOptions();

        try
        {
            var serializer = new XmlSerializer(typeof(ConfigOptions));
            await using var stream = File.OpenRead(_configPath);
            var config = (ConfigOptions?)serializer.Deserialize(stream);

            return config ?? new ConfigOptions();
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
}
