using ReciteHelper.Core.Configuration;

namespace ReciteHelper.Infrastructure.Configuration;

public interface IConfigService
{
    Task<ConfigOptions> LoadAsync();
    Task SaveAsync(ConfigOptions config);
}
