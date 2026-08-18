using System;
using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using Gemstone.Gemstone;
using Photon.Pun;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Networking;

public class EmoteManager : MonoBehaviour
{
    private static EmoteManager instance;
    private static AssetBundle assetBundle;
    private static GameObject activeKyle;
    private static float emoteEndTime;
    private static Vector3 archivePosition;
    private static Coroutine activeSoundCoroutine;
    private static string currentAnimationName = "";
    private static readonly List<GameObject> portedCosmetics = [];
    private static AudioSource activeAudioSource;
    private static float lastMicPlayTime;

    private void Awake()
    {
        instance = this;
        StartCoroutine(DownloadBundle());
    }

    private void Update()
    {
        if (activeKyle != null)
        {
            Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (Time.time >= emoteEndTime ||
                !animator.GetCurrentAnimatorStateInfo(0).loop && stateInfo.normalizedTime >= 1.0f)
            {
                StopEmote();

                return;
            }

            VRRig.LocalRig.enabled = false;

            Transform spine = activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2");
            VRRig.LocalRig.transform.position = spine.position - spine.right / 2.5f;
            VRRig.LocalRig.transform.rotation = Quaternion.Euler(spine.rotation.eulerAngles.x,
                    spine.rotation.eulerAngles.y, spine.rotation.eulerAngles.z + 90);

            Transform lHand =
                    activeKyle.transform.Find(
                            "KyleRobot/ROOT/Hips/Spine1/Spine2/LeftShoulder/LeftUpperArm/LeftArm/LeftHand");

            Transform rHand =
                    activeKyle.transform.Find(
                            "KyleRobot/ROOT/Hips/Spine1/Spine2/RightShoulder/RightUpperArm/RightArm/RightHand");

            VRRig.LocalRig.leftHand.rigTarget.transform.position = lHand.position;
            VRRig.LocalRig.rightHand.rigTarget.transform.position = rHand.position;

            VRRig.LocalRig.leftHand.rigTarget.transform.rotation = lHand.rotation * Quaternion.Euler(0, 0, 75);
            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = rHand.rotation * Quaternion.Euler(180, 0, -75);

            VRRig.LocalRig.head.rigTarget.transform.rotation =
                    activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/Neck/Head").transform.rotation *
                    Quaternion.Euler(0f, 0f, 90f);

            SyncFinger(lHand.Find("Index1"), VRRig.LocalRig.leftIndex);
            SyncFinger(lHand.Find("Middle1"), VRRig.LocalRig.leftMiddle);
            SyncFinger(rHand.Find("Index1"), VRRig.LocalRig.rightIndex);
            SyncFinger(rHand.Find("Middle1"), VRRig.LocalRig.rightMiddle);
        }
    }

    private static string GetBundlePath() => Path.Combine(Paths.GameRootPath, "Gemstone", "fn");

    private IEnumerator DownloadBundle()
    {
        string path = GetBundlePath();

        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

            const string URL = "https://github.com/Lexiii-1/Gemstone/raw/refs/heads/main/fn";
            using UnityWebRequest webRequest = UnityWebRequest.Get(URL);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(path, webRequest.downloadHandler.data);
                Debug.Log("Emote bundle downloaded successfully.");
            }
            else
            {
                Debug.LogError("Failed to download emote bundle: " + webRequest.error);
                yield break;
            }
        }

        if (assetBundle == null)
        {
            assetBundle = AssetBundle.LoadFromFile(path);
        }
    }

    private static void DisableCosmetics()
    {
        try
        {
            VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("Default");
            foreach (GameObject Cosmetic in VRRig.LocalRig.cosmetics.Where(Cosmetic => Cosmetic.activeSelf &&
                                                     Cosmetic.transform.parent ==
                                                     VRRig.LocalRig.mainCamera.transform.Find(
                                                          "HeadCosmetics")))
            {
                portedCosmetics.Add(Cosmetic);
                Cosmetic.transform.SetParent(VRRig.LocalRig.headMesh.transform, false);
                Cosmetic.transform.localPosition += new Vector3(0f, 0.1333f, 0.1f);
            }
        }
        catch
        {

        }
    }

    private static bool IsInLobby()
    {
        return PhotonNetwork.InRoom || (NetworkSystem.Instance != null && NetworkSystem.Instance.InRoom);
    }

    public static void PlayEmoteFromUrl(string animationName, string audioUrl, float duration = -1f,
                                        bool looping = false)
    {
        if (activeKyle != null && currentAnimationName == animationName)
        {
            StopEmote();

            return;
        }

        StopEmote();
        currentAnimationName = animationName;

        if (assetBundle == null)
            assetBundle = AssetBundle.LoadFromFile(GetBundlePath());

        archivePosition = GorillaTagger.Instance.transform.position;
        VRRig.LocalRig.enabled = false;

        DisableCosmetics();

        activeKyle = Instantiate(assetBundle.LoadAsset<GameObject>("Rig"));
        activeKyle.transform.position =
                VRRig.LocalRig.transform.Find("rig/body_pivot").position - new Vector3(0f, 1.15f, 0f);

        activeKyle.transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;

        if (!ModConfig.instance.ShowKyleWhileEmoting.Value)
            activeKyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

        Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
        animator.enabled = true;

        AnimationClip clip =
                animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == animationName);

        if (clip != null)
        {
            clip.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;
            animator.Play(clip.name);
            emoteEndTime = Time.time + (duration > 0f ? duration : clip.length) + (looping ? 9999f : 0f);
        }

        if (instance != null && ModConfig.instance.EmoteSounds.Value)
            activeSoundCoroutine = instance.StartCoroutine(PlayAudioFromUrlCoroutine(audioUrl, animationName));
    }

    private static IEnumerator PlayAudioFromUrlCoroutine(string url, string animationName)
    {
        string dir = Path.Combine(Paths.GameRootPath, "Gemstone", "cache");
        Directory.CreateDirectory(dir);
        string extension = url.EndsWith(".mp3") ? ".mp3" : ".wav";
        string filePath = Path.Combine(dir, animationName + extension);

        if (!File.Exists(filePath))
        {
            using UnityWebRequest download = new(url, UnityWebRequest.kHttpVerbGET);
            download.downloadHandler = new DownloadHandlerFile(filePath);

            yield return download.SendWebRequest();

            if (download.result != UnityWebRequest.Result.Success) yield break;
        }

        AudioType type = filePath.EndsWith(".mp3") ? AudioType.MPEG : AudioType.WAV;

        using UnityWebRequest load = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, type);
        ((DownloadHandlerAudioClip)load.downloadHandler).streamAudio = false;

        yield return load.SendWebRequest();

        if (load.result != UnityWebRequest.Result.Success) yield break;

        AudioClip sound = DownloadHandlerAudioClip.GetContent(load);

        if (sound == null) yield break;

        yield return instance.StartCoroutine(PlayAudioOutputCoroutine(sound));

        StopEmote();
    }

    private static IEnumerator PlayAudioOutputCoroutine(AudioClip sound)
    {
        Recorder? recorder = NetworkSystem.Instance?.VoiceConnection?.PrimaryRecorder;

        if (IsInLobby())
        {
            float timeSinceLastMic = Time.time - lastMicPlayTime;
            if (timeSinceLastMic < 0.8f)
            {
                yield return new WaitForSeconds(0.8f - timeSinceLastMic);
            }
            lastMicPlayTime = Time.time;

            if (recorder != null)
            {
                recorder.StopRecording();
                recorder.SourceType = Recorder.InputSourceType.AudioClip;
                recorder.AudioClip = sound;
                recorder.RestartRecording(true);
                recorder.DebugEchoMode = false;
            }
        }
        else
        {
            if (recorder != null)
            {
                recorder.StopRecording();
                recorder.SourceType = Recorder.InputSourceType.Microphone;
                recorder.AudioClip = null;
                recorder.RestartRecording(true);
                recorder.DebugEchoMode = false;
            }
        }

        if (activeKyle != null)
        {
            activeAudioSource = activeKyle.GetComponent<AudioSource>();
            if (activeAudioSource == null)
            {
                activeAudioSource = activeKyle.AddComponent<AudioSource>();
            }
            activeAudioSource.clip = sound;
            activeAudioSource.spatialBlend = 1f;
            activeAudioSource.Play();
        }

        yield return new WaitForSeconds(sound.length);
    }

    private static void EnableCosmetics()
    {
        VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("MirrorOnly");
        foreach (GameObject Cosmetic in portedCosmetics)
        {
            Cosmetic.transform.SetParent(VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"), false);
            Cosmetic.transform.localPosition -= new Vector3(0f, 0.1333f, 0.1f);
        }

        portedCosmetics.Clear();
    }

    public static void PlayEmote(string animationName, string soundName, float duration = -1f, bool looping = false)
    {
        if (activeKyle != null && currentAnimationName == animationName)
        {
            StopEmote();

            return;
        }

        StopEmote();
        currentAnimationName = animationName;

        if (assetBundle == null)
            assetBundle = AssetBundle.LoadFromFile(GetBundlePath());

        archivePosition = GorillaTagger.Instance.transform.position;
        VRRig.LocalRig.enabled = false;

        DisableCosmetics();

        activeKyle = Instantiate(assetBundle.LoadAsset<GameObject>("Rig"));
        activeKyle.transform.position =
                VRRig.LocalRig.transform.Find("rig/body_pivot").position - new Vector3(0f, 1.15f, 0f);

        activeKyle.transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;
        if (!ModConfig.instance.ShowKyleWhileEmoting.Value)
            activeKyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

        Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
        animator.enabled = true;

        AnimationClip clip = null;
        foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
            if (c.name == animationName)
            {
                clip = c;

                break;
            }

        if (clip != null)
        {
            clip.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;
            animator.Play(clip.name);
            emoteEndTime = Time.time + (duration > 0f ? duration : clip.length) + (looping ? 9999f : 0f);
        }

        AudioClip sound = assetBundle.LoadAsset<AudioClip>(soundName);
        if (sound != null && instance != null && ModConfig.instance.EmoteSounds.Value)
            activeSoundCoroutine = instance.StartCoroutine(PlayAudioCoroutine(sound));
    }

    private static IEnumerator PlayAudioCoroutine(AudioClip sound)
    {
        float audioLength = sound != null ? sound.length : 0f;
        float animLength = 0f;

        if (activeKyle != null)
        {
            Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
            AnimationClip clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == currentAnimationName);
            if (clip != null)
            {
                animLength = clip.length;
            }
        }

        yield return instance.StartCoroutine(PlayAudioOutputCoroutine(sound));

        float remainingTime = animLength - audioLength;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        StopEmote();
    }

    public static void StopEmote()
    {
        if (activeSoundCoroutine != null && instance != null)
        {
            instance.StopCoroutine(activeSoundCoroutine);
            activeSoundCoroutine = null;
        }

        if (activeAudioSource != null)
        {
            UnityEngine.Object.Destroy(activeAudioSource);
            activeAudioSource = null;
        }

        Recorder? recorder = NetworkSystem.Instance?.VoiceConnection?.PrimaryRecorder;
        if (recorder != null)
        {
            recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.AudioClip = null;
            recorder.RestartRecording(true);
            recorder.DebugEchoMode = false;
        }

        if (activeKyle != null)
        {
            Destroy(activeKyle);
            activeKyle = null;
            EnableCosmetics();
        }

        currentAnimationName = "";
        VRRig.LocalRig.enabled = true;
        emoteEndTime = -1f;
    }

    private void SyncFinger(Transform animBone, object fingerField)
    {
        if (animBone == null || fingerField == null) return;
        float curl = Mathf.Clamp01(animBone.localRotation.x * -10f);

        Type type = fingerField.GetType();
        type.GetField("calcT")?.SetValue(fingerField, curl);
        type.GetMethod("LerpFinger")?.Invoke(fingerField, [curl, false,]);
    }
}