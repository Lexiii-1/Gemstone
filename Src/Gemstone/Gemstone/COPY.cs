using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ARS
{
    internal class ARS : MonoBehaviourPunCallbacks
    {
        #region PhotonOverrides

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            try
            {
                StartCoroutine(DelayedCheckServer());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        IEnumerator DelayedCheckServer()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f, 10f));
            CheckServer();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            try
            {
                CheckUser(newPlayer);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        #endregion

        #region Main

        void Start()
        {
            if (!Directory.Exists("ARS"))
                Directory.CreateDirectory("ARS");

            _ = AsyncInitializeLists();

            EasierLog("ARS fully initialized, thank you for helping the gorilla tag modding community!");
        }

        public static List<Player> PlayersChecked = new List<Player>();
        public static HashSet<string> PlayersToReport = new HashSet<string>();
        static bool HasChecked = false;
        static string LastRoomChecked = string.Empty;

        void Update()
        {
            if (PhotonNetwork.InRoom)
                if (HasChecked && PhotonNetwork.CurrentRoom.Name != LastRoomChecked)
                {
                    HasChecked = false;
                    PlayersChecked.Clear();
                }
        }

        static void CheckServer()
        {
            if (PlayersToReport.Count == 0) return;

            if (PhotonNetwork.InRoom)
                foreach (Player plr in PhotonNetwork.PlayerListOthers)
                    CheckUser(plr);

            if (!HasChecked && PhotonNetwork.InRoom)
            {
                LastRoomChecked = PhotonNetwork.CurrentRoom.Name;
                HasChecked = true;
            }
        }

        static void CheckUser(Player plrToCheck)
        {
            if (!PlayersChecked.Contains(plrToCheck) && NeedToReport(plrToCheck))
            {
                foreach (GorillaPlayerScoreboardLine scoreboardLine in
                         GorillaScoreboardTotalUpdater.allScoreboardLines.Where(scoreboardLine =>
                             scoreboardLine.linePlayer ==
                             NetworkSystem.Instance.GetNetPlayerByID(plrToCheck.ActorNumber)))
                {
                    scoreboardLine.reportedToxicity = true;
                    scoreboardLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);
                }

                EasierLog($"Reported user {plrToCheck.NickName}.");
                PlayersChecked.Add(plrToCheck);
            }
        }

        private static readonly HttpClient Client = new HttpClient();

        static async Task AsyncInitializeLists()
        {
            string[] urls = new string[]
            {
                "https://raw.githubusercontent.com/AutoReportSystem/ARSPlayerIDs/refs/heads/main/Player%20Ids.txt",
                "https://raw.githubusercontent.com/Lexiii-1/Gemstone/refs/heads/main/GemstoneARS.txt"
            };

            foreach (string url in urls)
            {
                try
                {
                    string content = await Client.GetStringAsync(url);
                    var ids = content.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id));

                    foreach (var id in ids)
                    {
                        PlayersToReport.Add(id);
                    }
                }
                catch (Exception e)
                {
                    EasierLog($"Failed to fetch list from {url}: {e.Message}");
                }
            }

            EasierLog($"Received player ids to report. Total count: {PlayersToReport.Count}");
        }

        public static bool NeedToReport(Player plr)
        {
            if (plr == null || string.IsNullOrEmpty(plr.UserId))
                return false;

            return PlayersToReport.Contains(plr.UserId);
        }

        static void EasierLog(string message)
        {
            Debug.Log($"[ARS LOGGING] {message}");
        }

        #endregion
    }
}