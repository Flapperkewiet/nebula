using HarmonyLib;
using NebulaModel.Logger;
using System.Reflection;
using System.Collections.Generic;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(XConsole))]
    class XConsole_Patch
    {
        [HarmonyPatch("Awake")]
        public static void Postfix(XConsole __instance, ref int ___num)
        {
            Log.Info("XConsole started");
            Log.Info($"{__instance.password} {___num}");

            ___num = __instance.password;
            XConsole.InitCommands();

            MethodInfo dynMethod = typeof(XConsole).GetMethod("RegisterCommands",
            BindingFlags.NonPublic | BindingFlags.Instance);
            dynMethod.Invoke(__instance, new object[] { });

            ItemProto[] items = LDB.items.dataArray;
            foreach (ItemProto item in items)
            {
                if (item != null) ;
                    //Log.Info(StringTranslate.Translate(item.name));
            }

        }
        [HarmonyPatch("RegisterCommands")]
        public static void Postfix(XConsole __instance, Dictionary<string, XConsole.DCommandFunc> ___Commands)
        {
            Log.Info("XConsole registerd commands");
            Log.Info(___Commands.Count);
        }
    }
}
