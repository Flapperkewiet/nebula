using System;

namespace NebulaWorld.Console.Exceptions
{

    public class NebulaConsoleException : Exception
    {
        public NebulaConsoleException()
        {
        }

        public NebulaConsoleException(string message) : base(message)
        {
        }
    }
}
