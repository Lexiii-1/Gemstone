using BepInEx;
using BepInEx.Configuration;
using Console;
using Gemstone.Mods.Cosmetx;
using Gemstone.patches;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace Gemstone.Gemstone
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Main : BaseUnityPlugin
    {
        public static Main instance { get; private set; }
        public static int Pages = 0;
        public bool isMenuCreated;
        private GameObject menuObj;
        private GameObject menuPrefab;
        private AssetBundle menuBundle;

        public int selectedButtonIndex = 0;
        private float joystickScrollCooldown = 0f;
        private bool leftJoystickReset = true;
        private bool rightJoystickReset = true;


        private bool menuOpen = false;
        private bool buttonWasPressed = false;
        private bool lastRigState = false;
        private Vector3 menuForwardOffset = new Vector3(0.08f, 0f, 0f);
        private void LoadMenuAssetBundle()
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = "Gemstone.EmbedResources.menuobject";

            if (menuBundle == null)
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Logger.LogError($"rsrc not found");
                        return;
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        menuBundle = AssetBundle.LoadFromMemory(ms.ToArray());
                    }
                }
            }

            if (menuBundle == null)
            {
                Logger.LogError("asset bundle failed to load");
                return;
            }

            menuPrefab = menuBundle.LoadAsset<GameObject>("Assets/MenuObject.prefab");

            if (menuPrefab == null)
                Logger.LogError("prefab not found");
        }
        private void LoadSound()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                string resourceName = "Gemstone.EmbedResources.buttonpress.ogg";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    Logger.LogError("sound rsrc not found");
                    return;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                var tempClip = WavOrOggToAudioClip(data);

                if (tempClip != null)
                {
                    cachedClip = tempClip;
                    soundReady = true;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("failed loading embedded sound " + e);
            }
        }
        private AudioClip WavOrOggToAudioClip(byte[] data)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "Gemstone_click.ogg");
            File.WriteAllBytes(tempPath, data);

            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.OGGVORBIS);
            var op = www.SendWebRequest();

            while (!op.isDone) { }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.LogError(www.error);
                return null;
            }

            return DownloadHandlerAudioClip.GetContent(www);
        }

        public GameObject HandMenuCollider;
        public GameObject HandMenuCollider2;
        public List<GameObject> btnObjs = new List<GameObject>();
        public float globalClickCooldown = 0f;
        public int currentCategoryIndex = -1;
        public int currentPageIndex = 0;

        public AudioSource audioSource;
        private AudioClip cachedClip;
        private bool soundReady;
        private Coroutine rgbCoroutine;
        public bool IsAdmin = false;

        private List<AudioClip> soundboardClips = new List<AudioClip>();
        private List<string> soundboardFileNames = new List<string>();

        private static Coroutine activeSoundCoroutine;
        private static AudioClip currentlyPlayingClip;

        void Awake()
        {
            LoadMenuAssetBundle();
            instance = this;
            string dirPath = Path.Combine(Paths.GameRootPath, "Gemstone");
            Directory.CreateDirectory(dirPath);

            Harmony harmony = new Harmony(Constants.GUID);
            harmony.PatchAll();

            audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            LoadSound();
            StartCoroutine(LoadSoundboardFiles());
        }

        void Start()
        {
            gameObject.AddComponent<GunLib>();
            gameObject.AddComponent<ModConfig>();
            gameObject.AddComponent<RunMods>();
            gameObject.AddComponent<Cosmetx>();
            gameObject.AddComponent<JoinNotifs>();
            gameObject.AddComponent<Gui>();
            gameObject.AddComponent<ColoredBoards>();
            gameObject.AddComponent<EmoteManager>();
            if (NotiLib.Instance == null)
            {
                var notiObj = new GameObject("NotiLib");
                DontDestroyOnLoad(notiObj);
                notiObj.AddComponent<NotiLib>();
            }
            Console.Console.LoadConsole();
        }

        public void EnableAdminMenu()
        {
            IsAdmin = true;
            if (isMenuCreated) RefreshMenu();
        }
        public static Coroutine beesCoroutine;
        public static void ToggleSoundboard(AudioClip sound)
        {
            if (currentlyPlayingClip == sound)
            {
                StopActiveSound();
                return;
            }

            if (activeSoundCoroutine != null)
            {
                StopActiveSound();
            }

            activeSoundCoroutine = instance.StartCoroutine(PlaySoundMicrophone(sound));
        }

        private static void StopActiveSound()
        {
            if (activeSoundCoroutine != null)
            {
                instance.StopCoroutine(activeSoundCoroutine);
                activeSoundCoroutine = null;
            }

            currentlyPlayingClip = null;

            var recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.AudioClip = null;
            recorder.RestartRecording(true);
            recorder.DebugEchoMode = false;
        }
        public static IEnumerator PlaySoundMicrophone(AudioClip sound)
        {
            if (sound == null) yield break;

            currentlyPlayingClip = sound;

            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.SourceType = Recorder.InputSourceType.AudioClip;
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.AudioClip = sound;
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.RestartRecording(true);
            NetworkSystem.Instance.VoiceConnection.PrimaryRecorder.DebugEchoMode = true;

            yield return new WaitForSeconds(sound.length + 0.1f);

            StopActiveSound();

            if (instance.menuOpen && instance.isMenuCreated)
            {
                instance.RefreshMenu();
            }
        }

        private IEnumerator LoadSoundboardFiles()
        {
            soundboardClips.Clear();
            soundboardFileNames.Clear();

            string soundPath = Path.Combine(Paths.GameRootPath, "Gemstone", "Sounds");
            if (!Directory.Exists(soundPath))
            {
                Directory.CreateDirectory(soundPath);
                yield break;
            }

            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(soundPath, "*.ogg"));
            files.AddRange(Directory.GetFiles(soundPath, "*.mp3"));

            foreach (string file in files)
            {
                AudioType type = file.EndsWith(".mp3") ? AudioType.MPEG : AudioType.OGGVORBIS;

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + file, type))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        clip.name = Path.GetFileNameWithoutExtension(file);
                        soundboardClips.Add(clip);
                        soundboardFileNames.Add(clip.name);
                    }
                    else
                    {
                        Logger.LogError($"Failed to load sound {file}: {www.error}");
                    }
                }
            }
        }
        public static string GetPlatform(VRRig Player)
        {
            if (Player == null || Player.Creator == null || Player.Creator.GetPlayerRef() == null)
            {
                return "?";
            }

            int customPropsCount = 0;
            if (Player.Creator.GetPlayerRef().CustomProperties != null)
            {
                customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;
            }

            var cosmeticsField = AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics");
            if (cosmeticsField == null)
            {
                return "?";
            }

            var cosmeticsObj = cosmeticsField.GetValue(Player);
            string concat = "";

            if (cosmeticsObj is HashSet<string> cosmeticSet)
            {
                concat = string.Join("", cosmeticSet);
            }

            if (concat.Contains("S. FIRST LOGIN"))
            {
                return "Steam";
            }

            if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2)
            {
                return "Oculus Quest Link";
            }

            return "Standalone";
        }
        public static string HasSpecialCosmetic(VRRig Player)
        {

            var cosmeticsField = AccessTools.Field(Player.GetType(), "_playerOwnedCosmetics");

            var cosmeticsObj = cosmeticsField.GetValue(Player);
            string concat = "";

            if (cosmeticsObj is HashSet<string> cosmeticSet)
            {
                concat = string.Join("", cosmeticSet);
            }
            if (concat.Contains("LMAPY."))
                return "Forest Guide";
            if (concat.Contains("LBANI."))
                return "Another Axiom Creator";
            if (concat.Contains("LBADE."))
                return "Finger Painter";
            if (concat.Contains("LBAAD."))
                return "Administrator Badge";
            return "False";
        }
        void Update()
        {
            if (menuOpen && ModConfig.instance.IsJoystickNavigation.Value)
            {
                Vector2 joyl = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
                Vector2 joyr = ControllerInputPoller.instance.rightControllerPrimary2DAxis;
                bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
                bool B = ControllerInputPoller.instance.rightControllerSecondaryButton;

                if (joystickScrollCooldown > 0f)
                {
                    joystickScrollCooldown -= Time.deltaTime;
                }

                if (Mathf.Abs(joyl.y) < 0.5f)
                {
                    leftJoystickReset = true;
                }
                if (Mathf.Abs(joyr.x) < 0.5f)
                {
                    rightJoystickReset = true;
                }

                if (joystickScrollCooldown <= 0f && btnObjs.Count > 0)
                {
                    if (joyl.y > 0.5f && leftJoystickReset)
                    {
                        selectedButtonIndex--;
                        if (selectedButtonIndex < 0) selectedButtonIndex = btnObjs.Count - 1;
                        joystickScrollCooldown = 0.1f;
                        leftJoystickReset = false;
                        RefreshMenu();
                    }
                    else if (joyl.y < -0.5f && leftJoystickReset)
                    {
                        selectedButtonIndex++;
                        if (selectedButtonIndex >= btnObjs.Count) selectedButtonIndex = 0;
                        joystickScrollCooldown = 0.1f;
                        leftJoystickReset = false;
                        RefreshMenu();
                    }
                }

                if (joystickScrollCooldown <= 0f)
                {
                    if (joyr.x > 0.5f && rightJoystickReset)
                    {
                        currentPageIndex = Mathf.Min(Pages - 1, currentPageIndex + 1);
                        selectedButtonIndex = 0;
                        joystickScrollCooldown = 0.1f;
                        rightJoystickReset = false;
                        RefreshMenu();
                    }
                    else if (joyr.x < -0.5f && rightJoystickReset)
                    {
                        currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
                        selectedButtonIndex = 0;
                        joystickScrollCooldown = 0.1f;
                        rightJoystickReset = false;
                        RefreshMenu();
                    }
                }

                if (B && globalClickCooldown <= 0f)
                {
                    SwitchPage(-1, 0);
                }
                else if (trigger && globalClickCooldown <= 0f && selectedButtonIndex >= 0 && selectedButtonIndex < btnObjs.Count)
                {
                    var targetBtn = btnObjs[selectedButtonIndex];
                    if (targetBtn != null)
                    {
                        var bc = targetBtn.GetComponent<ButtonCollider>();
                        if (bc != null)
                        {
                            bc.Press();
                        }
                    }
                }
            }
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig != null && rig.Creator != null)
                {
                    UnityEngine.Color gemstoneColorStart = new UnityEngine.Color(0.6f, 0f, 1f);
                    UnityEngine.Color gemstoneColorEnd = new UnityEngine.Color(0f, 1f, 1f);

                    string gemstoneBaseTag = "[Gemstone User]";
                    string gemstoneGradientTag = " ";
                    for (int i = 0; i < gemstoneBaseTag.Length; i++)
                    {
                        float t = (float)i / (gemstoneBaseTag.Length - 1);
                        UnityEngine.Color currentColor = UnityEngine.Color.Lerp(gemstoneColorStart, gemstoneColorEnd, t);
                        string hexColor = UnityEngine.ColorUtility.ToHtmlStringRGB(currentColor);

                        gemstoneGradientTag += $"<color=#{hexColor}>{gemstoneBaseTag[i]}</color>";
                    }

                    string ownerBaseTag = "[Gemstone Owner]";
                    string ownerGradientTag = " ";
                    for (int i = 0; i < ownerBaseTag.Length; i++)
                    {
                        float t = (float)i / (ownerBaseTag.Length - 1);
                        UnityEngine.Color currentColor = UnityEngine.Color.Lerp(gemstoneColorStart, gemstoneColorEnd, t);
                        string hexColor = UnityEngine.ColorUtility.ToHtmlStringRGB(currentColor);

                        ownerGradientTag += $"<color=#{hexColor}>{ownerBaseTag[i]}</color>";
                    }

                    UnityEngine.Color chudColorStart = new UnityEngine.Color(0.5f, 0f, 0f);
                    UnityEngine.Color chudColorEnd = new UnityEngine.Color(0.2f, 0.2f, 0.2f);

                    string chudBaseTag = "[Chud Menu User]";
                    string chudGradientTag = " ";
                    for (int i = 0; i < chudBaseTag.Length; i++)
                    {
                        float t = (float)i / (chudBaseTag.Length - 1);
                        UnityEngine.Color currentColor = UnityEngine.Color.Lerp(chudColorStart, chudColorEnd, t);
                        string hexColor = UnityEngine.ColorUtility.ToHtmlStringRGB(currentColor);

                        chudGradientTag += $"<color=#{hexColor}>{chudBaseTag[i]}</color>";
                    }

                    string playerColorHex = "FFFFFF";
                    if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                    {
                        playerColorHex = UnityEngine.ColorUtility.ToHtmlStringRGB(rig.mainSkin.sharedMaterial.color);
                    }

                    string rawName = rig.Creator.NickName;
                    if (string.IsNullOrEmpty(rawName))
                    {
                        rawName = "Player";
                    }

                    string platformStr = GetPlatform(rig);
                    string platformColorHex = "FFFFFF";
                    if (string.IsNullOrEmpty(platformStr) || platformStr == "?")
                    {
                        platformStr = "Unknown";
                        platformColorHex = "FFFFFF";
                    }
                    else
                    {
                        string lowerPlatform = platformStr.ToLower();
                        if (lowerPlatform.Contains("link") || lowerPlatform.Contains("oculus quest link"))
                        {
                            platformColorHex = "FFFF00";
                        }
                        else if (lowerPlatform.Contains("standalone") || lowerPlatform.Contains("quest"))
                        {
                            platformColorHex = "87CEEB";
                        }
                        else if (lowerPlatform.Contains("steam"))
                        {
                            platformColorHex = "0000FF";
                        }
                    }

                    string cosmeticResult = HasSpecialCosmetic(rig);
                    string cosmeticTag = "";
                    if (cosmeticResult != "False")
                    {
                        cosmeticTag = $" [{cosmeticResult}]";
                    }

                    string baseFormattedPrefix = $"[{rig.Creator.UserId}] <color=#{playerColorHex}>{rawName}</color> [<color=#{platformColorHex}>{platformStr}</color>]{cosmeticTag}";

                    bool isLexi = (rig.Creator.UserId != null && ServerData.Administrators.TryGetValue(rig.Creator.UserId, out string consoleName) && consoleName.Equals("Lexi", System.StringComparison.OrdinalIgnoreCase))
                                  || (rig.isLocal && PhotonNetwork.LocalPlayer.UserId != null && ServerData.Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out string localConsoleName) && localConsoleName.Equals("Lexi", System.StringComparison.OrdinalIgnoreCase));

                    if (rig.isLocal || rig.Creator.IsLocal)
                    {
                        if (isLexi)
                        {
                            rig.playerText1.text = baseFormattedPrefix + ownerGradientTag;
                        }
                        else if (ModConfig.instance.MenuCustomPropertyEnabled.Value)
                        {
                            rig.playerText1.text = baseFormattedPrefix + gemstoneGradientTag;
                        }
                        else
                        {
                            rig.playerText1.text = baseFormattedPrefix;
                        }
                        continue;
                    }

                    if (rig.Creator.GetPlayerRef() != null)
                    {
                        var properties = rig.Creator.GetPlayerRef().CustomProperties;
                        bool hasActiveGemstoneProperty = false;
                        bool hasActiveChudProperty = false;
                        if (properties != null && properties.Count > 0)
                        {
                            foreach (var keyObj in properties.Keys)
                            {
                                if (keyObj != null)
                                {
                                    string key = keyObj.ToString();
                                    if (key.Contains("Gemstone."))
                                    {
                                        if (properties[keyObj] is bool isGemstone && isGemstone)
                                        {
                                            hasActiveGemstoneProperty = true;
                                        }
                                        else if (properties[keyObj] is int intGemstone && intGemstone == 1)
                                        {
                                            hasActiveGemstoneProperty = true;
                                        }
                                    }
                                    else if (key.Contains("Chud"))
                                    {
                                        if (properties[keyObj] is bool isChud && isChud)
                                        {
                                            hasActiveChudProperty = true;
                                        }
                                        else if (properties[keyObj] is int intChud && intChud == 1)
                                        {
                                            hasActiveChudProperty = true;
                                        }
                                    }
                                }
                            }
                        }

                        string finalTags = "";
                        if (isLexi)
                        {
                            finalTags += ownerGradientTag;
                        }
                        else
                        {
                            if (hasActiveGemstoneProperty)
                            {
                                finalTags += gemstoneGradientTag;
                            }
                        }

                        if (hasActiveChudProperty)
                        {
                            finalTags += chudGradientTag;
                        }

                        rig.playerText1.text = baseFormattedPrefix + finalTags;
                    }
                }
            }
            if (UnityInput.Current.GetKey(KeyCode.Z)) ControllerInputPoller.instance.rightControllerPrimaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.X)) ControllerInputPoller.instance.rightControllerSecondaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.C)) ControllerInputPoller.instance.leftControllerPrimaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.V)) ControllerInputPoller.instance.leftControllerSecondaryButton = true;
            if (UnityInput.Current.GetKey(KeyCode.LeftControl)) ControllerInputPoller.instance.leftControllerTriggerButton = true;
            if (UnityInput.Current.GetKey(KeyCode.LeftAlt)) ControllerInputPoller.instance.leftGrab = true;
            if (UnityInput.Current.GetKey(KeyCode.RightControl)) ControllerInputPoller.instance.rightControllerTriggerButton = true;
            if (UnityInput.Current.GetKey(KeyCode.RightAlt)) ControllerInputPoller.instance.rightGrab = true;
            string roomName = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "Not In Room";

            string playerCount = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount.ToString() : "0";


            Mods.Mods.UpdateMOTDText(

    $"Welcome To Gemstone Version: {Constants.Version}!" + (Constants.Debug ? "\n\n\n\n\n\n\n\nDEBUG BUILD" : ""),

    $"Welcome to gemstone! This Menu Mas A Few Fun Mods Made Just For You!\n\n\nIf You Get Banned It Is Not I, The Developers Responsibility. \n\n\nCurrent Room: {roomName}\nPlayers: {playerCount}");

            if (globalClickCooldown > 0) globalClickCooldown -= Time.deltaTime;
            bool isButtonPressed = ControllerInputPoller.instance.leftControllerSecondaryButton;


            if (isButtonPressed && !buttonWasPressed)
            {
                menuOpen = !menuOpen;

                if (menuOpen)
                {
                    CreateMenu();
                }
                else
                {
                    DestroyMenu(false);
                }
            }




            buttonWasPressed = isButtonPressed;

            if (menuOpen)
            {
                bool currentRigState = IsVRRIGEnabled;

                if (menuObj == null || currentRigState != lastRigState)
                {
                    DestroyMenu(true);
                    CreateMenu();
                }

                lastRigState = currentRigState;
            }

            if (menuObj != null)

                if (menuObj != null)
                if (menuObj != null)
            {
                Vector3 parentScale = GTPlayer.Instance.LeftHand.controllerTransform.lossyScale;

                Vector3 desired = new Vector3(1.24208f, 13.04792f, 15.86129f);

                menuObj.transform.localScale = new Vector3(
                    desired.x / parentScale.x,
                    desired.y / parentScale.y,
                    desired.z / parentScale.z
                );
            }
        }
        private Player selectedPlayer;
        private bool inPlayerSubmenu;
        public static bool IsVRRIGEnabled => VRRig.LocalRig.enabled != null ? VRRig.LocalRig.enabled : false;
        public void CreateMenu()
        {
            var player = GTPlayer.Instance;
            isMenuCreated = true;

            if (menuPrefab != null)
            {
                menuObj = Instantiate(menuPrefab);
            }
            else
            {
                menuObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                menuObj.transform.localScale = new Vector3(0.03f, 0.21f, 0.45f);
            }
            if (ModConfig.instance.IsOneHandedMenu.Value)
            {
                if (player.headCollider != null)
                {

                    float distanceInFront = 0.5f;

                    menuObj.transform.position = player.headCollider.transform.position + (player.headCollider.transform.forward * distanceInFront);

                    menuObj.transform.LookAt(player.headCollider.transform.position);

                    menuObj.transform.Rotate(-90f, 180f, 90f);

                    menuObj.transform.parent = player.headCollider.transform;
                    if (!ModConfig.instance.IsJoystickNavigation.Value)
                    {

                        HandMenuCollider2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        if (IsVRRIGEnabled)
                        {
                            HandMenuCollider2.transform.parent = GorillaTagger.Instance.leftHandTriggerCollider.transform;
                            HandMenuCollider2.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            HandMenuCollider2.transform.parent = GTPlayer.Instance.LeftHand.controllerTransform.transform;
                            HandMenuCollider2.transform.localPosition = Vector3.down * 0.094f;
                        }
                        HandMenuCollider2.layer = 2;
                        HandMenuCollider2.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
                        Destroy(HandMenuCollider2.GetComponent<Rigidbody>());

                        if (ModConfig.instance.ShowHandCollider.Value)
                        {
                            var rendhand2 = HandMenuCollider2.GetComponent<Renderer>();
                            rendhand2.material.shader = Shader.Find("GUI/Text Shader");
                            rendhand2.material.color = Color.white;
                        }
                    }
                }
                else
                {
                    menuObj.transform.parent = player.LeftHand.controllerTransform;
                    menuObj.transform.localPosition = menuForwardOffset;
                    menuObj.transform.localRotation = Quaternion.identity;
                }
            }
            else
            {
                menuObj.transform.parent = player.LeftHand.controllerTransform;
                menuObj.transform.localPosition = menuForwardOffset;
                menuObj.transform.localRotation = Quaternion.identity;
            }

            Transform backBtn = menuObj.transform.Find("Back");
            Transform forwardBtn = menuObj.transform.Find("Forwards");

            GameObject backObj = backBtn != null ? backBtn.gameObject : null;
            GameObject forwardObj = forwardBtn != null ? forwardBtn.gameObject : null;

            Transform Disconnect = menuObj.transform.Find("Disconnect");

            GameObject DisconnectObj = Disconnect != null ? Disconnect.gameObject : null;
            Transform Home = menuObj.transform.Find("Home");
            GameObject HomeObj = Home != null ? Home.gameObject : null;

            void SetupNavButton(GameObject obj)
            {
                if (obj == null) return;

                obj.layer = 2;

                var rb = obj.GetComponent<Rigidbody>() ?? obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                var col = obj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;

                var rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.shader = Shader.Find("GorillaTag/UberShader");
                    rend.material.color = ModConfig.Theme;

                    if (ModConfig.instance.IsMenuRGB.Value)
                    {
                        StartCoroutine(RGBTheme(rend));
                    }
                }

                var bc = obj.GetComponent<ButtonCollider>() ?? obj.AddComponent<ButtonCollider>();

                bc.OnPressed = null;
            }

            SetupNavButton(backObj);
            SetupNavButton(forwardObj);
            SetupNavButton(DisconnectObj);
            SetupNavButton(HomeObj);

            if (backObj != null)
            {
                backObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
                    RefreshMenu();
                };
            }

            if (forwardObj != null)
            {
                forwardObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    currentPageIndex = Mathf.Min(Pages - 1, currentPageIndex + 1);
                    RefreshMenu();
                };
            }
            if (DisconnectObj != null)
            {
                DisconnectObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    PhotonNetwork.Disconnect();
                };
                var text = DisconnectObj.transform.Find("TextUnchangable").GetComponent<TextMeshPro>();
                text.text = Localization.Get("Disconnect");
            }
            if (HomeObj != null)
            {
                HomeObj.GetComponent<ButtonCollider>().OnPressed = () =>
                {
                    SwitchPage(-1, 0);
                };
            }

            Transform titleTransform = menuObj.transform.Find("Title");

            if (titleTransform != null)
            {
                var text = titleTransform.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.text = currentCategoryIndex == -1 ? Localization.Get("Gemstone") + " [" + (currentPageIndex + 1) + "]" : GetCategoryName(currentCategoryIndex) + " [" + (currentPageIndex + 1) + "]";
                    text.color = ModConfig.Theme == Color.white ? Color.black : Color.white;

                    if (text.fontSharedMaterial != null)
                    {
                        text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                    }
                }
            }
            else
            {
                Debug.LogWarning("Prefab not found");
            }
            Transform BackText = menuObj.transform.Find(":3");
            if (BackText != null)
            {
                var text = BackText.GetComponent<TextMeshPro>();
                if (text.fontSharedMaterial != null)
                {
                    text.fontSharedMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
                }
            }

            if (menuObj.GetComponent<Rigidbody>()) Destroy(menuObj.GetComponent<Rigidbody>());
            if (menuObj.GetComponent<Collider>()) Destroy(menuObj.GetComponent<Collider>());

            var rend = menuObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.shader = Shader.Find("GorillaTag/UberShader");
                rend.material.color = ModConfig.Theme;
                if (ModConfig.instance.IsMenuRGB.Value) rgbCoroutine = StartCoroutine(RGBTheme(rend));
            }
            if (!ModConfig.instance.IsJoystickNavigation.Value)
            {

                HandMenuCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (IsVRRIGEnabled)
                {
                    HandMenuCollider.transform.parent = GorillaTagger.Instance.rightHandTriggerCollider.transform;
                    HandMenuCollider.transform.localPosition = Vector3.zero;
                }
                else
                {
                    HandMenuCollider.transform.parent = GTPlayer.Instance.RightHand.controllerTransform.transform;
                    HandMenuCollider.transform.localPosition = Vector3.down * 0.094f;
                }
                HandMenuCollider.layer = 2;
                HandMenuCollider.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
                Destroy(HandMenuCollider.GetComponent<Rigidbody>());

                if (ModConfig.instance.ShowHandCollider.Value)
                {
                    var rendhand = HandMenuCollider.GetComponent<Renderer>();
                    rendhand.material.shader = Shader.Find("GUI/Text Shader");
                    rendhand.material.color = Color.white;
                }
            }

            float zOffset = 0.005f;
            float step = 0.004f;

            if (currentCategoryIndex == -1)
            {
                if (!IsAdmin) Pages = 3;
                else Pages = 4;
                if (currentPageIndex == 0)
                {
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Join Discord"), () => Application.OpenURL("https://discord.gg/MJRQDVAZZF")); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Movement"), () => SwitchPage(0, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Utility"), () => SwitchPage(1, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Rig Mods"), () => SwitchPage(2, 0)); zOffset -= step;
                }
                else if (currentPageIndex == 1)
                {
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Settings"), () => SwitchPage(3, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Important"), () => SwitchPage(4, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Fun"), () => SwitchPage(5, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Player List"), () => SwitchPage(6, 0)); zOffset -= step;
                }
                else if (currentPageIndex == 2)
                {
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Soundboard"), () => SwitchPage(7, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Sound"), () => SwitchPage(8, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Visual"), () => SwitchPage(9, 0)); zOffset -= step;
                    AddButton(zOffset, 0f, 0.2f, Localization.Get("Emotes"), () => SwitchPage(10, 0)); zOffset -= step;
                }
                else
                {
                    if (!IsAdmin) currentPageIndex = 2;
                    if (IsAdmin) AddButton(zOffset, 0f, 0.2f, Localization.Get("Admin"), () => SwitchPage(11, 0)); zOffset -= step;
                }
            }
            else
            {
                switch (currentCategoryIndex)
                {
                    case 0:
                        Pages = 5;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Speed Boost"), ModConfig.instance.SpeedBoostEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Fly (A)"), ModConfig.instance.FlyEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Platforms (LG, RG)"), ModConfig.instance.IsPlatformsEnabled);
                            AddToggleButton(ref zOffset, step, Localization.Get("Joystick Fly (LJ, RJ)"), ModConfig.instance.IsJoystickFly);
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Long Arms"), ModConfig.instance.LongArmsEnabled, () => Mods.Mods.UnLongArms());
                            AddToggleButton(ref zOffset, step, Localization.Get("Ground Helper (LG + A)"), ModConfig.instance.IsGroundHelper);
                            AddToggleButton(ref zOffset, step, Localization.Get("Amplified Monke"), ModConfig.instance.IsAmplifiedMonke);
                            AddToggleButton(ref zOffset, step, Localization.Get("Noclip (B)"), ModConfig.instance.IsNoclipEnabled);
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Web Slingers (LG, RG)"), ModConfig.instance.IsWebSlingers);
                            AddToggleButton(ref zOffset, step, Localization.Get("Teleport Gun"), ModConfig.instance.IsTPGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Tag Gun (D?)"), ModConfig.instance.IsTagGun, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Tag All (D?)"), ModConfig.instance.IsTagAll, () => Mods.Mods.FixRig());
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("WASD Fly"), ModConfig.instance.IsWasdFly);
                            AddToggleButton(ref zOffset, step, Localization.Get("Movement Recorder (A)"), ModConfig.instance.MovementRecorder);
                            AddToggleButton(ref zOffset, step, Localization.Get("Dash (A, LG)"), ModConfig.instance.Dash);
                            AddToggleButton(ref zOffset, step, Localization.Get("Size Changer (RT, A, LG, Admin SS)"), ModConfig.instance.IsSizeChanger, () => Mods.Mods.DisableSizeChanger());
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Hand Turn (RG)"), ModConfig.instance.HandTurn);
                        }
                            break;

                    case 1:
                        Pages = 2;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Get PID Gun"), ModConfig.instance.IsGetPIDGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Mute Gun"), ModConfig.instance.IsMuteGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Mute Others Gun"), ModConfig.instance.IsMuteEveryoneExceptGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("Report Gun"), ModConfig.instance.IsReportGun);
                        }
                        else
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Mute All"), () => Mods.Mods.MuteAll()); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Unmute All"), () => Mods.Mods.UnmuteAll()); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Ignore Far Tag"), () => ExtremelyFarTagPatch.isDetected = false); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Show Menu Custom Property"), ModConfig.instance.MenuCustomPropertyEnabled);
                        }
                        break;

                    case 2:
                        Pages = 5;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Ghost Monke (A)"), ModConfig.instance.IsGhostMonke, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Lock Rig"), ModConfig.instance.IsLockOntoRig, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Hold Rig"), ModConfig.instance.IsHoldRig, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Rig Gun"), ModConfig.instance.IsRigGun, () => Mods.Mods.FixRig());
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Freeze Rig (B)"), ModConfig.instance.IsFreezeRig, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Upside Down Head"), ModConfig.instance.IsUpsideDownHead, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Backwards Head"), ModConfig.instance.IsBackwardsHead, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("funny rig"), ModConfig.instance.IsFunnyRig, () => Mods.Mods.FixRig());
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Recroom Torso"), ModConfig.instance.IsRecroomTorso, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Recroom Rig"), ModConfig.instance.IsRecroomRig, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Full Body Tracking"), ModConfig.instance.FullBodyTracking, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Bees"), ModConfig.instance.IsBees, () => { StopCoroutine(beesCoroutine); beesCoroutine = null; Mods.Mods.FixRig(); });
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Copy Rig"), ModConfig.instance.IsCopyRigGun, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Invis Monke"), ModConfig.instance.IsInvisMonke, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Spaz Monke"), ModConfig.instance.IsSpazMonke, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Ragdoll (A)"), ModConfig.instance.IsRagdoll, () => Mods.Mods.FixRig());
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Spider"), ModConfig.instance.IsSpider, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Inverse Spider"), ModConfig.instance.InverseSpider, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Bean"), ModConfig.instance.Bean, () => Mods.Mods.FixRig());
                            AddToggleButton(ref zOffset, step, Localization.Get("Joystick Torso Rotation"), ModConfig.instance.JoystickRotation, () => Mods.Mods.FixRig());
                        }
                            break;

                    case 3:
                        Pages = 9;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Invis Plats"), ModConfig.instance.IsInvisPlat);
                            AddToggleButton(ref zOffset, step, Localization.Get("Menu RGB"), ModConfig.instance.IsMenuRGB);
                            AddButton(zOffset, 0f, 0.2f, "R +", () =>
                            {
                                ModConfig.instance.R.Value = (ModConfig.instance.R.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.R.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;

                            AddButton(zOffset, 0f, 0.2f, "G +", () =>
                            {
                                ModConfig.instance.G.Value = (ModConfig.instance.G.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.G.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddButton(zOffset, 0f, 0.2f, "B +", () =>
                            {
                                ModConfig.instance.B.Value = (ModConfig.instance.B.Value + 1) % 11;
                                NotiLib.SendNotification(ModConfig.instance.B.Value.ToString(), 2000);
                                RefreshMenu();
                            });
                            zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Show Hand Collider"), ModConfig.instance.ShowHandCollider);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Fly Speed +"), () => { ModConfig.instance.FlySpeedSave.Value += 0.1f; NotiLib.SendNotification(ModConfig.instance.FlySpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Fly Speed -"), () => { ModConfig.instance.FlySpeedSave.Value -= 0.1f; NotiLib.SendNotification(ModConfig.instance.FlySpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sling Speed -"), () => { ModConfig.instance.WebSlingSpeedSave.Value -= 5f; NotiLib.SendNotification(ModConfig.instance.WebSlingSpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sling Speed +"), () => { ModConfig.instance.WebSlingSpeedSave.Value += 5f; NotiLib.SendNotification(ModConfig.instance.WebSlingSpeedSave.Value.ToString("0.0"), 2000); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language english"), () => { ModConfig.instance.Language.Value = 1; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language spanish"), () => { ModConfig.instance.Language.Value = 2; RefreshMenu(); }); zOffset -= step;
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language german"), () => { ModConfig.instance.Language.Value = 3; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language russian"), () => { ModConfig.instance.Language.Value = 4; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Language polish"), () => { ModConfig.instance.Language.Value = 5; RefreshMenu(); }); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Wiggly Gun"), () => ModConfig.instance.GunType.Value = 1); zOffset -= step;
                        }
                        else if (currentPageIndex == 4)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Straight Gun"), () => ModConfig.instance.GunType.Value = 2); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Coil Gun"), () => ModConfig.instance.GunType.Value = 3); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Lighting Gun"), () => ModConfig.instance.GunType.Value = 4); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vortex Gun"), () => ModConfig.instance.GunType.Value = 5); zOffset -= step;
                        }
                        else if (currentPageIndex == 5)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("DNA Gun"), () => ModConfig.instance.GunType.Value = 6); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Pulse Gun"), () => ModConfig.instance.GunType.Value = 7); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Orbital Gun"), () => ModConfig.instance.GunType.Value = 8); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Static Gun"), () => ModConfig.instance.GunType.Value = 9); zOffset -= step;
                        }
                        else if (currentPageIndex == 6)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Sine Wave Gun"), () => ModConfig.instance.GunType.Value = 10); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Digital Gun"), () => ModConfig.instance.GunType.Value = 11); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Square Pulse Gun"), () => ModConfig.instance.GunType.Value = 12); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Ray Gun"), () => ModConfig.instance.GunType.Value = 13); zOffset -= step;
                        }
                        else if (currentPageIndex == 7)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Gun Smoothness -"), () =>
                            {
                                ModConfig.instance.GunSmoothness.Value = Mathf.Max(0f, ModConfig.instance.GunSmoothness.Value - 0.05f);
                                NotiLib.SendNotification(ModConfig.instance.GunSmoothness.Value.ToString("0.00"), 2000);
                            });
                            zOffset -= step;

                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Gun Smoothness +"), () =>
                            {
                                ModConfig.instance.GunSmoothness.Value += 0.05f;
                                NotiLib.SendNotification(ModConfig.instance.GunSmoothness.Value.ToString("0.00"), 2000);
                            });
                            zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Preview Gun"), ModConfig.instance.PreviewGun);
                            AddToggleButton(ref zOffset, step, Localization.Get("One Handed Menu"), ModConfig.instance.IsOneHandedMenu);
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Joystick Navigation"), ModConfig.instance.IsJoystickNavigation);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Default Colors"), () => { ModConfig.instance.R.Value = 5; ModConfig.instance.G.Value = 8; ModConfig.instance.B.Value = 10; }); zOffset -= step;
                        }
                            break;
                    case 4:
                        Pages = 1;
                        AddButton(zOffset, 0f, 0.2f, Localization.Get("Reauthenticate"), () => MothershipAuthenticator.Instance.BeginLoginFlow()); zOffset -= step;
                        AddToggleButton(ref zOffset, step, Localization.Get("Anti Report"), ModConfig.instance.IsAntiReportEnabled);
                        AddToggleButton(ref zOffset, step, Localization.Get("Bypass Automod"), ModConfig.instance.IsBypassAutoMod);
                        break;
                    case 5:
                        Pages = 2;
                        if (currentPageIndex == 0)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Unlock all cosmetics (CS)"), () => Mods.Cosmetx.Cosmetx.instance.ActivateCosmetx()); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Max Quest Score"), () => Mods.Mods.MaxQuestScore()); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Bracelet Spam (LG, RG, D?)"), ModConfig.instance.IsBraceletSpam, () => Mods.Mods.RemoveBracelet());
                            AddToggleButton(ref zOffset, step, Localization.Get("Enable Builder Shelf (SS)"), ModConfig.instance.IsEnabledBuilderShelf, () => Mods.Mods.DisableBuilderShelf());
                        }
                        else
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Annoy"), ModConfig.instance.IsAnnoy);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Unlock Forest Guide"), () => Cosmetx.instance.UnlockSpecificCosmetic("LMAPY.")); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Unlock AA Badge"), () => Cosmetx.instance.UnlockSpecificCosmetic("LBANI.")); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Boop"), ModConfig.instance.IsBoop);
                        }
                        break;
                    case 6:
                        {
                            int playersPerPage = 4;

                            Player[] players = PhotonNetwork.PlayerList;

                            int totalPlayers = players.Length;

                            Pages = Mathf.CeilToInt(totalPlayers / (float)playersPerPage);
                            Pages = Mathf.Max(1, Pages);

                            int startIndex = currentPageIndex * playersPerPage;
                            int endIndex = Mathf.Min(startIndex + playersPerPage, totalPlayers);

                            if (!inPlayerSubmenu)
                            {
                                for (int i = startIndex; i < endIndex; i++)
                                {
                                    Player player_ = players[i];

                                    AddButton(zOffset, 0f, 0.2f, player_.NickName, () =>
                                    {
                                        selectedPlayer = player_;
                                        inPlayerSubmenu = true;
                                        RefreshMenu();
                                    });

                                    zOffset -= step;
                                }
                            }
                            else
                            {
                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Back"), () =>
                                {
                                    inPlayerSubmenu = false;
                                    selectedPlayer = null;
                                    RefreshMenu();
                                });

                                zOffset -= step;

                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Teleport to"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    StartCoroutine(Mods.Mods.TpToPlayer(selectedPlayer.UserId));
                                });

                                zOffset -= step;

                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Custom Properties"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    string output = $"Player: {selectedPlayer.NickName}\n\nCustom Properties:\n";

                                    if (selectedPlayer.CustomProperties != null && selectedPlayer.CustomProperties.Count > 0)
                                    {
                                        foreach (var kvp in selectedPlayer.CustomProperties)
                                        {
                                            output += $"{kvp.Key}: {kvp.Value}\n";
                                        }
                                    }
                                    else
                                    {
                                        output += "None";
                                    }

                                    NotiLib.SendNotification(output, 4000f);
                                });
                                zOffset -= step;
                                AddButton(zOffset, 0f, 0.2f, Localization.Get("Block Player"), () =>
                                {
                                    if (selectedPlayer == null) return;

                                    string userId = selectedPlayer.UserId;

                                    foreach (var rig in VRRigCache.ActiveRigs)
                                    {
                                        if (rig == null || rig.Creator == null) continue;

                                        if (rig.Creator.UserId == userId && rig.Creator.UserId != PhotonNetwork.LocalPlayer.UserId)
                                        {
                                            rig.gameObject.SetActive(false);
                                        }
                                    }

                                    NotiLib.SendNotification(
                                        $"Blocked {selectedPlayer.NickName}",
                                        2000f
                                    );
                                });
                            }

                            break;
                        }
                    case 7:
                        {
                            int soundsPerPage = 4;
                            int totalSounds = soundboardClips.Count;
                            Pages = Mathf.CeilToInt(totalSounds / (float)soundsPerPage);
                            Pages = Mathf.Max(1, Pages);

                            int startIndex = currentPageIndex * soundsPerPage;
                            int endIndex = Mathf.Min(startIndex + soundsPerPage, totalSounds);

                            if (totalSounds == 0) AddButton(zOffset, 0f, 0.2f, "No Sounds Found", () => { });
                            else
                            {
                                for (int i = startIndex; i < endIndex; i++)
                                {
                                    AudioClip selectedClip = soundboardClips[i];
                                    string btnText = (currentlyPlayingClip == selectedClip) ? $"[PLAYING] {selectedClip.name}" : selectedClip.name;

                                    AddButton(zOffset, 0f, 0.2f, btnText, () =>
                                    {
                                        ToggleSoundboard(selectedClip);
                                        RefreshMenu();
                                    });
                                    zOffset -= step;
                                }
                            }
                            break;
                        }
                    case 8:
                        AddToggleButton(ref zOffset, step, Localization.Get("Jman SS"), ModConfig.instance.IsJmanSoundSpam);
                        AddToggleButton(ref zOffset, step, Localization.Get("Crystal SS"), ModConfig.instance.IsCrystalSoundSpam);
                        break;
                    case 9:
                        AddToggleButton(ref zOffset, step, Localization.Get("Box ESP"), ModConfig.instance.IsBoxEsp, () => Mods.Mods.CleanupBoxEsp());
                        AddToggleButton(ref zOffset, step, Localization.Get("Ball ESP"), ModConfig.instance.IsBallEsp, () => Mods.Mods.DisableSkeletonESP());
                        AddToggleButton(ref zOffset, step, Localization.Get("Nametags"), ModConfig.instance.IsNametags, () => Mods.Mods.DisableNametagsMod());
                        break;
                    case 10:
                        Pages = 15;
                        if (currentPageIndex == 0)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Dance Moves"), () => EmoteManager.PlayEmote("Dance Moves", "default", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Take The L"), () => EmoteManager.PlayEmote("TakeTheL", "takethel", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Reanimated"), () => EmoteManager.PlayEmote("Reanimated", "reanimated", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Electro Shuffle"), () => EmoteManager.PlayEmote("ElectroShuffle", "electroshuffle", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Orange Justice"), () => EmoteManager.PlayEmote("OrangeJustice", "oj", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Ride The Pony"), () => EmoteManager.PlayEmote("RideThePony", "ridethepony", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Fresh"), () => EmoteManager.PlayEmote("Emote_Fresh", "fresh", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Electro Swing"), () => EmoteManager.PlayEmote("ElectroSwing", "swing", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Floss"), () => EmoteManager.PlayEmote("Emote_FlossDance_CMM", "floss", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Disco Fever"), () => EmoteManager.PlayEmote("DiscoFever", "discofever", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Boogie Down"), () => EmoteManager.PlayEmote("BoogieDownLoop", "boogiedown", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("The Robot"), () => EmoteManager.PlayEmote("Emote_RobotDance", "therobot", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Best Mates"), () => EmoteManager.PlayEmote("BestMates", "bestmates", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Paws & Claws"), () => EmoteManager.PlayEmote("Paws&Claws", "pawsclaws", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Get Griddy"), () => EmoteManager.PlayEmote("Get Griddy", "Emote_Griddles_Music_Loop_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Pull Up"), () => EmoteManager.PlayEmote("Pull Up", "Gas_Station_Loop", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 4)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Popular Vibe"), () => EmoteManager.PlayEmote("Popular Vibe", "Emote_SpeedDial_Loop", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Lucid Dreams"), () => EmoteManager.PlayEmote("Lucid DreamsLoop", "Emote_KelpLinen_Music_Loop", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Empty Pockets"), () => EmoteManager.PlayEmote("Empty Out Your PocketsLoop", "eoyp", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("What You Want"), () => EmoteManager.PlayEmote("WhatYouWant", "whatyouwant", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 5)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("The Renegade"), () => EmoteManager.PlayEmote("The Renegade", "Emote_Just_Home_Music_Loop", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Jabba Switchway"), () => EmoteManager.PlayEmote("Jabba Switchway Loop", "Emote_January_Bop_Loop", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Infinite Dab"), () => EmoteManager.PlayEmote("InfinidabLoop", "infinitedab", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Celebrate Me"), () => EmoteManager.PlayEmote("Celebrate Me", "IP_Emote_Cottontail_Loop", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 6)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Billy Bounce"), () => EmoteManager.PlayEmote("BillyBounce", "billybounce", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Windmill Floss"), () => EmoteManager.PlayEmote("WindmillFloss", "whirlfloss", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Hype"), () => EmoteManager.PlayEmote("Hype", "hype", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Entranced"), () => EmoteManager.PlayEmote("Entranced", "entranced", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 7)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Laugh It Up"), () => EmoteManager.PlayEmote("LaughItUp", "Emote_Laugh_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Snoop Walk"), () => EmoteManager.PlayEmote("SnoopWalk", "snoopwalk", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Scenario"), () => EmoteManager.PlayEmote("Scenario", "scenario", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Night Out"), () => EmoteManager.PlayEmote("Night Out", "nightout", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 8)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Point And Strut"), () => EmoteManager.PlayEmote("pointandstrut", "pointandstrut", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Moongazer"), () => EmoteManager.PlayEmote("moongazer", "moongazer", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Rollie"), () => EmoteManager.PlayEmote("Rollie", "Emote_Twist_Daytona_Music_Loop_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Heel Click"), () => EmoteManager.PlayEmote("HEEL", "heelclickbreakdown", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 9)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Switchstep"), () => EmoteManager.PlayEmote("SwitchStep", "switchstep", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Freestylin'"), () => EmoteManager.PlayEmote("Freestylin'", "freestylin", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Go Mufasa"), () => EmoteManager.PlayEmote("Go Mufasa", "Emote_Sandwich_Bop_Loop", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Jubi Slide"), () => EmoteManager.PlayEmote("jubislide", "Emote_GoodbyeUpbeat_Loop", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 10)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Running Man"), () => EmoteManager.PlayEmote("RunningMan", "Athena_Emote_Music_RunningMan", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Zany"), () => EmoteManager.PlayEmote("Zany", "zany", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Pumpernickel"), () => EmoteManager.PlayEmote("pumpernickel2", "Athena_Emotes_Music_PumpDance", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Pony Up"), () => EmoteManager.PlayEmote("RideThePony", "ponyup", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 11)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Hula"), () => EmoteManager.PlayEmote("HULA", "emote_hula_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Never Gonna"), () => EmoteManager.PlayEmote("Never Gonna Loop", "Emote_NeverGonna_Loop_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Say So"), () => EmoteManager.PlayEmote("Say So", "Emote_HotPink_Loop_258", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Take It Slow"), () => EmoteManager.PlayEmote("Takeitslow", "takeitslow", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 12)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Macarena"), () => EmoteManager.PlayEmote("Macarena", "Emote_Macaroon_Music_Loop_01", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Cupid's Arrow"), () => EmoteManager.PlayEmote("cupid", "cupid", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Gangnam Style"), () => EmoteManager.PlayEmote("gangnam", "gangnam", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Slim Shady"), () => EmoteManager.PlayEmote("realslimshady", "slim", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 13)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Party Hips"), () => EmoteManager.PlayEmote("partyhips", "partyhips", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Out West"), () => EmoteManager.PlayEmote("outwest", "outwest", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("My World"), () => EmoteManager.PlayEmote("myworld", "Myworld", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Jake Bug"), () => EmoteManager.PlayEmote("Jake", "jake", -1f, true)); zOffset -= step;
                        }
                        else if (currentPageIndex == 14)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Miku Beam"), () => EmoteManager.PlayEmote("miku", "miku", -1f, true)); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Stop All Emotes"), () => EmoteManager.StopEmote()); zOffset -= step;
                        }
                        break;
                    case 11:
                        Pages = 7;
                        if (currentPageIndex == 0)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Silent Kick Gun"), ModConfig.instance.IsSilKick);
                            AddToggleButton(ref zOffset, step, Localization.Get("Admin Laser"), ModConfig.instance.AdminLaser);
                            AddToggleButton(ref zOffset, step, Localization.Get("Travis Scott"), ModConfig.instance.IsTravis, () => Mods.Mods.NoTravis());
                            AddToggleButton(ref zOffset, step, Localization.Get("Tv"), ModConfig.instance.IsTv, () => Mods.Mods.NoTv());
                        }
                        else if (currentPageIndex == 1)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Phone"), ModConfig.instance.IsPhone, () => Mods.Mods.NoSamsung());
                            AddToggleButton(ref zOffset, step, Localization.Get("Twerking Carti"), ModConfig.instance.IsTwerkingCarti, () => Mods.Mods.NoCarti());
                            AddToggleButton(ref zOffset, step, Localization.Get("Grab All"), ModConfig.instance.IsAdminGrab);
                            AddToggleButton(ref zOffset, step, Localization.Get("Roblox Sword"), ModConfig.instance.IsCoolSword, () => Mods.Mods.NoSword());
                        }
                        else if (currentPageIndex == 2)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Kormakur Sign"), ModConfig.instance.IsKormakur, () => Mods.Mods.NoSign());
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Hell"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/GirlHell1999.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid OCD"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/OCD.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Kitty"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Kitty.mp4"); zOffset -= step;
                        }
                        else if (currentPageIndex == 3)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid AMV"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/testvid.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid theresabarrier"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/theresabarrier.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Edit"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/edit.mp4"); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Cherry bomb"), ModConfig.instance.IsCherryBomb, () => Mods.Mods.NoCherryBomb());
                        }
                        else if (currentPageIndex == 4)
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Zlothy"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/Zlothy.mov"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Barrier Remix"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/there%20is%20a%20barrier%20remix.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid invincible wobbly edit"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/INVINCIBLEWOBBLYANIMATION.mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Punch Mod"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/punchmod.mp4"); zOffset -= step;
                        }
                        else if (currentPageIndex == 5)
                        {
                            AddToggleButton(ref zOffset, step, Localization.Get("Big Assets"), ModConfig.instance.IsBigAssets);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Grass Skirt Chase"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/SB%20Music_%20Grass%20Skirt%20Chase%20(check%20desc).mp4"); zOffset -= step;
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("travis skot rel no clickbate"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/taviskisuit.mp4"); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Video Player"), ModConfig.instance.IsVideoPlayer, () => Mods.Mods.NoVideoPlayer());
                        }
                        else
                        {
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Reset Video Player"), () => Mods.Mods.ResetVideoPlayer()); zOffset -= step;
                            AddToggleButton(ref zOffset, step, Localization.Get("Admin Strangle"), ModConfig.instance.IsAdminStrangle);
                            AddButton(zOffset, 0f, 0.2f, Localization.Get("Vid Soup mar brobers"), () => Mods.Mods.Video = "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/soup%20mar%20brobers.mp4"); zOffset -= step;
                        }
                            break;
                }

                float navY = 0.08f;
            }

            foreach (GameObject btnObj in btnObjs)
            {
                if (btnObj != null)
                {
                    btnObj.layer = 2;
                    btnObj.GetComponent<Collider>().isTrigger = true;
                }
            }
        }

        private string GetCategoryName(int index) => index switch { 0 => Localization.Get("Movement"), 1 => Localization.Get("Utility"), 2 => Localization.Get("Rig Mods"), 3 => Localization.Get("Settings"), 4 => Localization.Get("Important"), 5 => Localization.Get("Fun"), 6 => Localization.Get("Player List"), 7 => Localization.Get("Soundboard"), 8 => Localization.Get("Sound"), 9 => Localization.Get("Visual"), 10 => Localization.Get("Emotes"), 11 => Localization.Get("Admin"), _ => Localization.Get("Gemstone") };
        private void SwitchPage(int cat, int page) { currentCategoryIndex = cat; currentPageIndex = page; RefreshMenu(); }
        public void RefreshMenu()
        {
            if (!menuOpen) return;

            DestroyMenu(true);
            CreateMenu();
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, Mathf.Max(0, Pages - 1));
        }

        public void DestroyMenu(bool refresh)
        {
            if (!refresh) isMenuCreated = false;

            if (menuObj != null)
            {
                if (ModConfig.instance.IsMenuRGB.Value && rgbCoroutine != null)
                    StopCoroutine(rgbCoroutine);

                if (!refresh)
                {
                    menuObj.transform.SetParent(null, true);

                    Rigidbody rb = menuObj.GetComponent<Rigidbody>() ?? menuObj.AddComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);

                    GameObject menuToDestroy = menuObj;
                    menuObj = null;
                    Destroy(menuToDestroy, 5f);
                }
                else
                {
                    Destroy(menuObj);
                    menuObj = null;
                }
            }

            if (HandMenuCollider != null)
            {
                Destroy(HandMenuCollider);
                HandMenuCollider = null;
            }
            if (HandMenuCollider2 != null)
            {
                Destroy(HandMenuCollider2);
                HandMenuCollider2 = null;
            }

            foreach (var b in btnObjs)
            {
                if (b != null)
                {
                    if (!refresh)
                    {
                        b.transform.SetParent(null, true);
                        Rigidbody rb = b.GetComponent<Rigidbody>() ?? b.AddComponent<Rigidbody>();
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
                        Destroy(b, 5f);
                    }
                    else
                    {
                        Destroy(b);
                    }
                }
            }
            btnObjs.Clear();
            Config.Save();
        }



        void AddToggleButton(ref float z, float step, string name, ConfigEntry<bool> entry, Action onDisable = null, Action onEnable = null)
        {
            AddButton(z, 0f, 0.2f, name, () => {
                entry.Value = !entry.Value;
                if (entry.Value) onEnable?.Invoke();
                else onDisable?.Invoke();
                RefreshMenu();
            }, entry.Value);
            z -= step;
        }
        void AddButton(float z, float y, float s, string name, Action act, bool isActive = false)
        {
            GameObject btn = null;
            if (menuBundle != null)
            {
                GameObject buttonPrefab = menuBundle.LoadAsset<GameObject>("Assets/Button.prefab");

                if (buttonPrefab != null)
                {
                    btn = Instantiate(buttonPrefab);
                }
                else
                {
                    Logger.LogError("prefab not found");
                }
            }

            if (btn == null)
            {
                btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                btn.transform.localScale = new Vector3(0.03f, s, 0.04f);
            }

            var f = btn.GetComponent<FollowMenu>() ?? btn.AddComponent<FollowMenu>();
            // f.target = GTPlayer.Instance.LeftHand.controllerTransform;
            f.target = menuObj.transform;
            f.position = new Vector3(0.014f, y, z); // + menuForwardOffset;
            f.rotationOffset = Quaternion.Euler(90f, 0f, 0f);

            var renderer = btn.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.shader = Shader.Find("GorillaTag/UberShader");
                if (!ModConfig.instance.IsMenuRGB.Value)
                {
                    if (isActive)
                    {
                        Color brightColor = ModConfig.Theme * 1.5f;
                        brightColor.a = 1f;
                        renderer.material.color = brightColor;
                    }
                    else
                    {
                        renderer.material.color = ModConfig.Theme;
                    }
                }
                else
                {
                    StartCoroutine(RGBTheme(renderer, isActive));
                }
            }

            var collider = btn.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            btn.layer = 2;

            var c = btn.GetComponent<ButtonCollider>() ?? btn.AddComponent<ButtonCollider>();
            c.OnPressed = act;

            TextMeshPro t = null;
            Transform textTransform = btn.transform.Find("Text");

            if (textTransform != null)
            {
                t = textTransform.GetComponent<TextMeshPro>();
            }

            if (t == null)
            {
                var tObj = new GameObject("Text");
                tObj.transform.SetParent(btn.transform);
                tObj.transform.localPosition = new Vector3(0.55f, 0f, 0f);
                tObj.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);

                t = tObj.AddComponent<TextMeshPro>();
            }
            t.text = name;
            if (ModConfig.Theme == Color.black)
                t.color = Color.white;
            else if (ModConfig.Theme == Color.white)
            {
                t.color = Color.black;
            }
            else
            {
                t.color = Color.white;
            }
            int currentBtnIndex = btnObjs.Count;

            if (ModConfig.instance.IsJoystickNavigation.Value && menuOpen && selectedButtonIndex == currentBtnIndex)
            {
                t.text = "[SEL] " + name;
            }
            else
            {
                t.text = name;
            }
            btnObjs.Add(btn);
        }

        public void PlayClickSound() { if (soundReady && cachedClip != null) audioSource.PlayOneShot(cachedClip); }
        public IEnumerator RGBTheme(Renderer r, bool isActive = false)
        {
            while (true)
            {
                float speedMultiplier = isActive ? 5f : 2f;
                float t = Time.time * speedMultiplier;

                r.material.color = new Color(
                    Mathf.Sin(t) * 0.5f + 0.5f,
                    Mathf.Sin(t + 2f) * 0.5f + 0.5f,
                    Mathf.Sin(t + 4f) * 0.5f + 0.5f
                );
                yield return null;
            }
        }

        public class FollowMenu : MonoBehaviour
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotationOffset = Quaternion.identity;

            void LateUpdate()
            {
                if (target)
                {
                    transform.position = target.TransformPoint(position);
                    transform.rotation = target.rotation * rotationOffset;
                }
            }
        }
        public class ButtonCollider : MonoBehaviour
        {
            public Action OnPressed;

            public void Press()
            {
                if (instance.globalClickCooldown > 0)
                    return;

                instance.globalClickCooldown = 0.2f;

                instance.PlayClickSound();

                OnPressed?.Invoke();
            }

            private void OnTriggerEnter(Collider other)
            {
                if (other.gameObject != instance.HandMenuCollider && other.gameObject != instance.HandMenuCollider2)
                    return;

                if (other.gameObject == instance.HandMenuCollider)
                {
                    GorillaTagger.Instance.StartVibration(
                        false,
                        GorillaTagger.Instance.tagHapticStrength / 2f,
                        0.05f

                    );
                }
                else
                {
                    GorillaTagger.Instance.StartVibration(
                        true,
                        GorillaTagger.Instance.tagHapticStrength / 2f,
                        0.05f

                    );
                }

                    Press();
            }
        }
    }
}