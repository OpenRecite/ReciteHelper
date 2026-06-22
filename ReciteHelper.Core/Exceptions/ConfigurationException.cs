namespace ReciteHelper.Core.Exceptions;

public class ConfigurationException : DomainException
{
    public ConfigurationException() { }

    public ConfigurationException(string message) : base(message) { }

    public ConfigurationException(string message, Exception inner)
        : base(message, inner) { }

    public override string ErrorCode => "CONFIG_ERROR";
}
