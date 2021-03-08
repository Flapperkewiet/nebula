using NebulaWorld.Console.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using NebulaModel.Logger;
using System.Reflection;

namespace NebulaWorld.Console
{
    public static class NebulaConsole
    {
        private static Dictionary<string, ICommand> commands;
        public static void Init()
        {
            commands = new Dictionary<string, ICommand>();
            RegisterAllCommands();

            try
            {
                ICommand helpCommand = GetCommand("Help");
                helpCommand.Execute();
                helpCommand.Execute("List");

                ICommand listCommand = GetCommand("List");
                listCommand.Execute("items");
                listCommand.Execute("recipies");
                listCommand.Execute("planets");
                listCommand.Execute("tech");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                return;
            }
          
        }

        public static ICommand GetCommand(string commandString)
        {
            commandString = commandString.ToLower().Trim();
            ICommand command;
            if (commands.TryGetValue(commandString, out command))
                return command;
            return null;
        }
        public static ICommand[] GetAllCommands()
        {
            return commands.Values.ToArray();
        }
        /// <summary>
        /// This function registers a command that can be executed through the console
        /// </summary>
        /// <param name="command">The command that needs to be registerd</param>
        public static void RegisterCommand(ICommand command)
        {
            if (commands == null)
            {
                Log.Warn($"Tried to add command \"{command.Command}\" before NebulaConsole was initialized.");
                return;
            }

            if (commands.ContainsKey(command.Command.ToLower()))
            {
                Log.Warn($"NebulaConsole is already aware of command \"{command.Command}\".");
                return;
            }

            commands[command.Command.ToLower()] = command;
            //TODO remove this logline
            Log.Info($"Succesfully registerd command \"{command.Command}\"");
        }

        /// <summary>
        /// Remove a command so it can no longer be executed through the console
        /// </summary>
        /// <param name="command"></param>
        public static void UnregisterCommand(ICommand command)
        {
            if (commands?.ContainsKey(command.Command) ?? false)
            {
                commands.Remove(command.Command);
            }
        }

        /// <summary>
        /// Finds all classes implementing ICommand and registers them
        /// </summary>
        private static void RegisterAllCommands()
        {
            //Loop all assemblies and find all types that implement ICommand, then register those commands
            var ICommandType = typeof(ICommand);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies)
            {
                try
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (ICommandType.IsAssignableFrom(t) && !t.IsInterface)
                        {
                            RegisterCommand((ICommand)Activator.CreateInstance(t));
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.GetType() != typeof(ReflectionTypeLoadException))
                    {
                        Log.Error(e.Message);
                        Log.Error(e.StackTrace);
                    }
                }
            }
        }

        public static void WriteLine(string message)
        {
            Log.Info("Console: " + message);
        }
    }
}
