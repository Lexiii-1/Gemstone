using Photon.Pun;
using Photon.Realtime;

namespace Gemstone.Gemstone
{
    public class InfoNotifs : MonoBehaviourPunCallbacks
    {
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            NotiLib.SendNotification("<color=green>[JOIN] </color>" + newPlayer.NickName, 2000);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            NotiLib.SendNotification("<color=red>[LEAVE] </color>" + otherPlayer.NickName, 2000);
        }
        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            base.OnMasterClientSwitched(newMasterClient);
            if (newMasterClient == PhotonNetwork.LocalPlayer)
            {
                NotiLib.SendNotification("You are now masterclient!", 2000);
            }
        }
    }
}