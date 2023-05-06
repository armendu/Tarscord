using System;

namespace Tarscord.Core.Persistence.Exceptions;

public class OperationFailedException : Exception
{
    public OperationFailedException() : base()
    {
    }

    public OperationFailedException(string message)
        : base(message)
    {
    }

    public OperationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}