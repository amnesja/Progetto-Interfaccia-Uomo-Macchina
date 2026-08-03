using System;

namespace Ordo.Infrastructure
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message) { }
    }
}
