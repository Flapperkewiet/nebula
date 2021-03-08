using HarmonyLib;
using NebulaModel.Logger;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(DSPGame))]
    class DSPGame_Patch
    {
        [HarmonyPatch("Awake")]
        public static void Postfix(DSPGame ___instance)
        {

        }
        [HarmonyPatch("Update")]
        public static void Postfix()
        {
            //Use this function if you need to check/do something every frame
        }
    }
}
