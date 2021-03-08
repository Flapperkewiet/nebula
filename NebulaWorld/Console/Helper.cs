using System;
using System.Collections.Generic;
using NebulaWorld.Console.Exceptions;

namespace NebulaWorld.Console.Commands
{
    public static class Helper
    {

        /// <summary>
        /// Parses out a proto based on a name or id
        /// </summary>
        public static T GetProtoByIdOrName<T>(ProtoSet<T> protoSet, string nameOrId) where T : Proto
        {
            int protoId;
            T proto = null;
            if (int.TryParse(nameOrId, out protoId))
            {
                try
                {
                    proto = protoSet.Select(protoId);
                }
                catch (Exception) { }
            }
            else if (nameOrId.StartsWith("\"") && nameOrId.EndsWith("\""))
            {
                string protoName = nameOrId.Substring(1, nameOrId.Length - 2);
                T[] allProtos = protoSet.dataArray;
                foreach (T p in allProtos)
                {
                    if (p.name == protoName)
                    {
                        proto = p;
                        break;
                    }
                }
            }
            else
            {
                throw new InvalidSyntaxException();
            }
            if (proto == null)
                throw new NotFoundException(protoSet.TableName, nameOrId);
            return proto;
        }
    }
}
