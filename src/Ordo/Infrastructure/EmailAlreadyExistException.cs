using System;

namespace Ordo.Infrastructure;

public class EmailAlreadyExistException : Exception
{
    public EmailAlreadyExistException(String message) : base(message) { }
}