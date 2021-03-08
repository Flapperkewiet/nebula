using System;

namespace NebulaWorld.Console.Commands
{
    public interface ICommand
    {
        string Command { get; }
        string ShortDescription { get; }
        string LongDescription { get; }
        string Syntax { get; }

        void Execute(string param = "");
    }


}
