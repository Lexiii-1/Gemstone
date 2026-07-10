using Gemstone.Gemstone;
using GorillaNetworking;
using HarmonyLib;
using MonoMod.Utils;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace Gemstone.Console;

public class ServerData : MonoBehaviour
{
    #region Configuration

    public static readonly bool ServerDataEnabled = true;
    public static bool DisableTelemetry = false;

    public const string ServerEndpoint = "https://menu.seralyth.software";

    public static readonly string[] ServerDataEndpoints = new string[]
    {
        "https://raw.githubusercontent.com/Lexiii-1/Feather/refs/heads/main/ServerData.json",
        "https://raw.githubusercontent.com/ChipLikesCereal/Gemstone/refs/heads/main/Console.json",
        "https://menu.seralyth.software/serverdata",
    };

    public static readonly string ServerWebsocket = "wss://menu.seralyth.software";

    public const string AssetsURL = "https://raw.githubusercontent.com/Seralyth/Console/refs/heads/master/ServerData";

    public static readonly Dictionary<string, string> LocalAdmins = new()
    {
    };

    public static ClientWebSocket Websocket;

    public static void SetupAdminPanel(string playerName, string userId)
    {
        if (Main.instance != null)
        {
            Main.instance.EnableAdminMenu();
            NotiLib.SendNotification("Welcome, " + playerName + "!", 5000);
        }
    }

    #endregion

    #region Server Data Code

    private static ServerData instance;

    private static float DataLoadTime = -1f;
    private static float ReloadTime = -1f;

    private static int LoadAttempts;

    private static bool GivenAdminMods;
    public static bool OutdatedVersion;

    public void Awake()
    {
        instance = this;
        DataLoadTime = Time.time + 5f;

        NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinRoom;

        NetworkSystem.Instance.OnPlayerJoined += UpdatePlayerCount;
        NetworkSystem.Instance.OnPlayerLeft += UpdatePlayerCount;
    }

    public void Update()
    {
        if (DataLoadTime > 0f && Time.time > DataLoadTime && GorillaComputer.instance.isConnectedToMaster)
        {
            DataLoadTime = Time.time + 5f;

            LoadAttempts++;
            if (LoadAttempts >= 3)
            {
                Console.Log("Server data could not be loaded");
                DataLoadTime = -1f;

                return;
            }

            Console.Log("Attempting to load web data");
            instance.StartCoroutine(LoadAllServerData());
        }

        if (ReloadTime > 0f)
        {
            if (Time.time > ReloadTime)
            {
                ReloadTime = Time.time + 60f;
                instance.StartCoroutine(LoadAllServerData());
                Task.Run(async () =>
                {
                    if (Websocket != null && Websocket.State is WebSocketState.Closed or WebSocketState.Aborted)
                        Websocket?.Dispose();

                    Websocket ??= new ClientWebSocket();
                    await Websocket.ConnectAsync(
                            new Uri($"{ServerWebsocket}?mod={Console.MenuName}"),
                            CancellationToken.None
                    );
                });
            }
        }
        else
        {
            if (GorillaComputer.instance.isConnectedToMaster)
                ReloadTime = Time.time + 5f;
        }

        if (Time.time > DataSyncDelay || !PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.PlayerList.Length != PlayerCount)
                instance.StartCoroutine(PlayerDataSync(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CloudRegion));

            PlayerCount = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList.Length : -1;
        }
    }

    public static void OnJoinRoom() =>
            instance.StartCoroutine(TelementryRequest(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.NickName,
                    PhotonNetwork.CloudRegion, PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.CurrentRoom.IsVisible,
                    PhotonNetwork.PlayerList.Length, NetworkSystem.Instance.GameModeString));

    public static string CleanString(string input, int maxLength = 12)
    {
        input = new string(Array.FindAll(input.ToCharArray(), c => Utils.IsASCIILetterOrDigit(c)));

        if (input.Length > maxLength)
            input = input[..(maxLength - 1)];

        input = input.ToUpper();

        return input;
    }

    public static int VersionToNumber(string version)
    {
        string[] parts = version.Split('.');

        if (parts.Length != 3)
            return -1;

        return int.Parse(parts[0]) * 100 + int.Parse(parts[1]) * 10 + int.Parse(parts[2]);
    }

    public static readonly Dictionary<string, string> Administrators = new();
    public static readonly List<string> SuperAdministrators = new();

    public static IEnumerator LoadAllServerData()
    {
        Administrators.Clear();
        Administrators.AddRange(LocalAdmins);
        SuperAdministrators.Clear();

        foreach (string endpoint in ServerDataEndpoints)
        {
            yield return instance.StartCoroutine(LoadServerData(endpoint));
        }

        if (!GivenAdminMods && PhotonNetwork.LocalPlayer.UserId != null &&
            Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string? administrator))
        {
            GivenAdminMods = true;
            SetupAdminPanel(administrator, PhotonNetwork.LocalPlayer.UserId);
        }
    }

    private static IEnumerator LoadServerData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            try
            {
                string json = request.downloadHandler.text;
                JObject data = JObject.Parse(json);

                string minConsoleVersion = (string)data["min-console-version"];
                if (VersionToNumber(Console.ConsoleVersion) >= VersionToNumber(minConsoleVersion))
                {
                    JArray admins = (JArray)data["admins"];
                    foreach (JToken? admin in admins)
                    {
                        Administrators[admin["user-id"].ToString()] = admin["name"].ToString();
                    }

                    JArray superAdmins = (JArray)data["super-admins"];
                    foreach (JToken? superAdmin in superAdmins)
                        SuperAdministrators.Add(superAdmin.ToString());
                }
            }
            catch { }
        }
    }

    public static IEnumerator TelementryRequest(string directory, string identity, string region, string userid,
                                                bool isPrivate, int playerCount, string gameMode)
    {
        if (DisableTelemetry)
            yield break;

        UnityWebRequest request = new(ServerEndpoint + "/telemetry", "POST");

        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory),
            identity = CleanString(identity),
            region = CleanString(region, 3),
            userid = CleanString(userid, 20),
            isPrivate,
            playerCount,
            gameMode = CleanString(gameMode, 128),
            consoleVersion = Console.ConsoleVersion,
            menuName = Console.MenuName,
            menuVersion = Console.MenuVersion,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(raw);
        request.SetRequestHeader("Content-Type", "application/json");

        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();
    }

    private static float DataSyncDelay;
    public static int PlayerCount;

    public static void UpdatePlayerCount(NetPlayer Player) =>
            PlayerCount = -1;

    public static bool IsPlayerSteam(VRRig Player)
    {
        string concat =
                string.Concat((HashSet<string>)AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics")
                                                          .GetValue(Player));

        int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

        if (concat.Contains("S. FIRST LOGIN")) return true;
        if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2) return true;
        if (concat.Contains("LMAKT.")) return false;

        return false;
    }

    public static IEnumerator PlayerDataSync(string directory, string region)
    {
        if (DisableTelemetry)
            yield break;

        DataSyncDelay = Time.time + 3f;

        yield return new WaitForSeconds(3f);

        if (!PhotonNetwork.InRoom)
            yield break;

        Dictionary<string, Dictionary<string, string>> data = new();

        foreach (Player identification in PhotonNetwork.PlayerList)
        {
            VRRig rig = Console.GetVRRigFromPlayer(identification);
            data.Add(identification.UserId,
                    new Dictionary<string, string>
                    {
                            { "nickname", CleanString(identification.NickName) },
                            {
                                    "cosmetics",
                                    string.Concat((HashSet<string>)AccessTools
                                                                  .Field(rig.GetType(), "_playerOwnedCosmetics")
                                                                  .GetValue(rig))
                            },
                            {
                                    "color",
                                    $"{Math.Round(rig.playerColor.r * 255)} {Math.Round(rig.playerColor.g * 255)} {Math.Round(rig.playerColor.b * 255)}"
                            },
                            { "platform", IsPlayerSteam(rig) ? "STEAM" : "QUEST" },
                    });
        }

        UnityWebRequest request = new(ServerEndpoint + "/syncdata", "POST");

        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory),
            region = CleanString(region, 3),
            data,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(raw);
        request.SetRequestHeader("Content-Type", "application/json");

        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();
    }

    #endregion
}