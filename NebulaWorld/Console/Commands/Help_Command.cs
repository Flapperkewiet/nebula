using System;
using NebulaWorld.Console.Exceptions;

namespace NebulaWorld.Console.Commands
{
    public class Help_Command : ICommand
    {
        public string Command => "Help";

        public string ShortDescription => "Lists all commands and their description";

        public string LongDescription => "Gives a list of all available commands with their short description. \n " +
            "If another command is passed as argument, a longer description and syntax will be given. ";

        public string Syntax => "Help [command]";

        public void Execute(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                NebulaConsole.WriteLine("Write \"help [command]\" to get more info about a specific command. ");
                ICommand[] commands = NebulaConsole.GetAllCommands();
                foreach (ICommand command in commands)
                {
                    try
                    {
                        NebulaConsole.WriteLine(command.Command + ": " + command.ShortDescription);
                    }
                    catch (Exception)
                    {
                        NebulaConsole.WriteLine(command.Command + ": This command has no description");
                    }
                }
            }
            else
            {
                //Commands cannot contain spaces, so we have more than 1 command
                if (param.Contains(" "))
                {
                    throw new InvalidSyntaxException();
                }

                string commandString = param.ToLower().Trim();
                ICommand command = NebulaConsole.GetCommand(commandString);
                if (command == null)
                {
                    throw new UnknownCommandException();
                }
                NebulaConsole.WriteLine($"Syntax: \"{command.Syntax}\"");
                try
                {
                    NebulaConsole.WriteLine(command.LongDescription);
                }
                catch (NotImplementedException)
                {
                    try
                    {
                        NebulaConsole.WriteLine(command.ShortDescription);
                    }
                    catch (NotImplementedException)
                    {
                        NebulaConsole.WriteLine("This command has no description");
                    }
                }
            }
        }
    }
}
