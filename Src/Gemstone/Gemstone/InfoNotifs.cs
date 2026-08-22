using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Gemstone.Gemstone
{
    public class InfoNotifs : MonoBehaviourPunCallbacks
    {
        public static Color FindPlayerColor(Player player)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && rig.creator != null && player.UserId == rig.creator.UserId)
                {
                    return rig.playerColor;
                }
            }
            return Color.black;
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            StartCoroutine(WaitForPlayerRigAndNotify(newPlayer));
        }

        private IEnumerator WaitForPlayerRigAndNotify(Player player)
        {
            yield return new WaitForSeconds(0.1f);

            Color playerColor = FindPlayerColor(player);
            string hexColor = ColorUtility.ToHtmlStringRGB(playerColor);
            NotiLib.SendNotification($"<color=green>[JOIN] </color><color=#{hexColor}>{player.NickName}</color>", 2000);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
            StartCoroutine(WaitAndNotifyLeave(otherPlayer));
        }

        private IEnumerator WaitAndNotifyLeave(Player player)
        {
            yield return new WaitForSeconds(0.1f);

            Color playerColor = FindPlayerColor(player);
            string hexColor = ColorUtility.ToHtmlStringRGB(playerColor);
            NotiLib.SendNotification($"<color=red>[LEAVE] </color><color=#{hexColor}>{player.NickName}</color>", 2000);
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            base.OnMasterClientSwitched(newMasterClient);
            if (newMasterClient == PhotonNetwork.LocalPlayer)
            {
                NotiLib.SendNotification("You are now masterclient!", 2000);
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            NotiLib.SendNotification($"You have joined room {PhotonNetwork.CurrentRoom.name}!", 2000);
        }

        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            NotiLib.SendNotification($"You have left your current room!", 2000);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            bool DontShow = message.ToLower().Contains("game does not") || message.ToLower().Contains("full");
            if (!DontShow)
            {
                NotiLib.SendNotification($"Join failure! Reason: {returnCode} {message}", 2000);
            }
            if (message.ToLower().Contains("full"))
            {
                NotiLib.SendNotification($"That room is full!", 2000);
            }
        }
    }
}