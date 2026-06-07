using Photon.Pun;
using Photon.Realtime;

namespace Gemstone.Gemstone
{
    public class JoinNotifs : MonoBehaviourPunCallbacks // Deez, this doesnt show your own local player. - Lexi (@_.lex1._)
    {
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            NotiLib.SendNotification("<color=green>[JOIN] </color>" + newPlayer.NickName, 2000);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            NotiLib.SendNotification("<color=red>[LEAVE] </color>" + otherPlayer.NickName, 2000); // this is an ai comment i id skider
        }
    }
}