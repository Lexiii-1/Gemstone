using System.Collections.Generic;
using Gemstone.Gemstone;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Gemstone.patches
{
    [HarmonyPatch(typeof(MonkeAgent), nameof(MonkeAgent.SendReport))]
    public class ShowReportPatch
    {
        private const float PlayerReportLogCooldown = 0.5f;
        private static readonly Dictionary<string, float> LastLoggedReport = [];

        private static bool Prefix(string susReason, string susId, string susNick)
        {
            if (!ModConfig.instance.ShowAntiCheatReport.Value)
                return true;

            if (LastLoggedReport.ContainsKey(susId) && LastLoggedReport[susId] > Time.time)
                return susId != PhotonNetwork.LocalPlayer.UserId;

            if (susId == PhotonNetwork.LocalPlayer.UserId)
            {
                NotiLib.SendNotification($"You got reported for {susReason}.", 2000, "MonkeAgent");
            }
            else
            {
                NotiLib.SendNotification($"{susNick} was reported for {susReason}.", 2000, "MonkeAgent");
            }

            LastLoggedReport[susId] = Time.time + PlayerReportLogCooldown;

            return susId != PhotonNetwork.LocalPlayer.UserId;
        }
    }
}