using System;
using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "GetRPCCallTracker")]
    internal class RPC
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
