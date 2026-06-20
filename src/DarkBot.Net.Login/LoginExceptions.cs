namespace DarkBot.Net.Login;

public class LoginException : Exception
{
    public LoginException(string message) : base(message)
    {
    }

    public LoginException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class WrongCredentialsException : LoginException
{
    public WrongCredentialsException(string message = "Wrong username or password.") : base(message)
    {
    }
}

public sealed class CaptchaException : LoginException
{
    public CaptchaException(string message) : base(message)
    {
    }

    public CaptchaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
