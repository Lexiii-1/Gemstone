using System;
using HarmonyLib;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), "CheckReports")]
    public class CheckReportsPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
