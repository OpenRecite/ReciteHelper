namespace ReciteHelper.Core.Configuration;

public enum ModelAccessMode
{
    None,
    DeepSeekAndQwen,
    OpenRouter,
    Hosted
}

public static class ModelAccess
{
    public static ModelAccessMode Resolve(ConfigOptions? config)
    {
        if (config is null)
            return ModelAccessMode.None;

        if (!string.IsNullOrWhiteSpace(config.DeepSeekKey) &&
            !string.IsNullOrWhiteSpace(config.QwenKey))
            return ModelAccessMode.DeepSeekAndQwen;

        if (!string.IsNullOrWhiteSpace(config.OpenRouterKey))
            return ModelAccessMode.OpenRouter;

        if (!string.IsNullOrWhiteSpace(config.HostedLicenseId) ||
            !string.IsNullOrWhiteSpace(config.HostedLicenseCode))
            return ModelAccessMode.Hosted;

        return ModelAccessMode.None;
    }

    public static bool HasTextGeneration(ConfigOptions? config) => Resolve(config) != ModelAccessMode.None;
}
