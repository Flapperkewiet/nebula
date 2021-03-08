using System;
using NebulaWorld.Console.Exceptions;


namespace NebulaWorld.Console.Commands
{
    class List_Command : ICommand
    {
        public string Command => "List";

        public string ShortDescription => "Lists things such as tech, items, recipies, ...";

        public string LongDescription => "Can list tech, items, recipies and plantets with their name and ID";

        public string Syntax => "List {tech | items | recipies | planets}";

        public void Execute(string param)
        {
            if (string.IsNullOrEmpty(param))
                throw new InvalidSyntaxException();

            string listItem = param.Trim().ToLower();

            switch (listItem)
            {
                case "tech":
                case "technology":
                case "research":
                    listTech();
                    break;
                case "item":
                case "items":
                    listItems();
                    break;
                case "recipe":
                case "recipies":
                    listRecipies();
                    break;
                case "planet":
                case "planets":
                    listPlanets();
                    break;

                default:
                    throw new InvalidSyntaxException();
            }
        }

        private void listProtoArray(Proto[] protos)
        {
            foreach (Proto proto in protos)
            {
                if (proto != null)
                    NebulaConsole.WriteLine($"{proto.ID} {StringTranslate.Translate(proto.name)}");
            }
        }

        private void listTech()
        {
            NebulaConsole.WriteLine("Listing all tech with id");
            listProtoArray(LDB.techs.dataArray);
        }

        private void listItems()
        {
            NebulaConsole.WriteLine("Listing all items with id");
            listProtoArray(LDB.items.dataArray);
        }
        private void listRecipies()
        {
            NebulaConsole.WriteLine("Listing all recipies with id");
            listProtoArray(LDB.recipes.dataArray);
        }
        private void listPlanets()
        {
            NebulaConsole.WriteLine("Listing all planets with id");
            if (GameMain.mainPlayer == null)
                throw new NotInGameException();

            foreach (StarData star in GameMain.galaxy.stars)
            {
                NebulaConsole.WriteLine(star.name);
                foreach (PlanetData planet in star.planets)
                {
                    NebulaConsole.WriteLine($"\t{planet.id} {planet.name}");
                }
            }
        }
    }
}
