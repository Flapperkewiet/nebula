using System;

namespace NebulaWorld.Console.Exceptions
{
    [Serializable]
    public class NotInGameException : NebulaConsoleException
    {
        public NotInGameException() : base("This command only works if you are in a game.") { }
    }
}
