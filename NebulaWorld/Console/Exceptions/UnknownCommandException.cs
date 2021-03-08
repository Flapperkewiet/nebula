using System;

namespace NebulaWorld.Console.Exceptions
{
    [Serializable]
    public class UnknownCommandException : NebulaConsoleException
    {
        public UnknownCommandException() : base("The command you passed could not be found, did you misspell something?.\n Use \"help\" to get a list of valid commands. ") { }
    }
}
