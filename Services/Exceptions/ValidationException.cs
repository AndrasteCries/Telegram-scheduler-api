namespace TelegramScheduler.Services.Exeptions;

public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}