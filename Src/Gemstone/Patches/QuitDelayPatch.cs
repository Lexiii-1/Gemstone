using System;
using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "QuitDelay", MethodType.Enumerator)]
    public class QuitDelayPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
