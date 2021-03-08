using System;

namespace NebulaWorld.Console.Exceptions
{
    [Serializable]
    public class InvalidSyntaxException : NebulaConsoleException
    {
        public InvalidSyntaxException() : base() { }
        public InvalidSyntaxException(string correctSyntax) : base("Invalid syntax. The correct syntax is: " + correctSyntax) { }
    }
}
