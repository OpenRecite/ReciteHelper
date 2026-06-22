namespace ReciteHelper.Core.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }

    public override string ErrorCode => "VALIDATION_ERROR";

    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
}
