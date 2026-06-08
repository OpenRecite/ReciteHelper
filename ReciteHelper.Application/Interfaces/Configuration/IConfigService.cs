using ReciteHelper.Core.Configuration;

namespace ReciteHelper.Application.Interfaces.Configuration;

public interface IConfigService
{
    Task<ConfigOptions> LoadAsync();
    Task SaveAsync(ConfigOptions config);
}
