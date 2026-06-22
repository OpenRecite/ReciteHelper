using ReciteHelper.Core.Configuration;

namespace ReciteHelper.Core.Interfaces.Configuration;

public interface IConfigService
{
    Task<ConfigOptions> LoadAsync();
    Task SaveAsync(ConfigOptions config);
}
