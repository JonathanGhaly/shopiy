namespace Shopiy.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("Access denied to this resource.")
    {
    }
}
