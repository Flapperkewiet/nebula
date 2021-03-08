using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NebulaWorld.Console.Exceptions
{
    public class NotFoundException : NebulaConsoleException
    {
        public NotFoundException() : base("The requested thing could not be found.")
        {
        }

        public NotFoundException(string typeName, string nameOrId) : base($"The {typeName} corresponding with \"{nameOrId}\" could not be found.")
        {
        }
    }
}
