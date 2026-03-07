namespace ReciteHelper.Core.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException() { }

    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception inner)
        : base(message, inner) { }

    public virtual string ErrorCode => "DOMAIN_ERROR";
}
