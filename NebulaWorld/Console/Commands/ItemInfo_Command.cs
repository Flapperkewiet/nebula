using System;
using NebulaWorld.Console.Exceptions;


namespace NebulaWorld.Console.Commands
{
    class ItemInfo_Command : ICommand
    {
        public string Command => "Item-info";

        public string ShortDescription => "Gives more detailed information about an item.";

        public string LongDescription => throw new NotImplementedException();

        public string Syntax => "Item-info {itemID | \"item name\"}";

        public void Execute(string param)
        {
            if (string.IsNullOrEmpty(param))
                throw new InvalidSyntaxException(Syntax);

            ItemProto item = Helper.GetProtoByIdOrName(LDB.items, param);

            NebulaConsole.WriteLine(item.name);
            NebulaConsole.WriteLine(item.);
        }
    }
}
