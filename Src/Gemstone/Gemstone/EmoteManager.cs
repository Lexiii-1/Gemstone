using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BepInEx;
using GorillaLocomotion;
using Photon.Voice.Unity;
using GorillaNetworking;
using UnityEngine.Networking;

public class EmoteManager : MonoBehaviour // Creds to IIDK for the asset bundle AND most of the emote code
{
    private static EmoteManager instance;
    private static AssetBundle assetBundle;
    private static GameObject activeKyle;
    private static float emoteEndTime;
    private static Vector3 archivePosition;
    private static Coroutine activeSoundCoroutine;
    private static readonly List<GameObject> portedCosmetics = new List<GameObject> { };

    private void Awake()
    {
        instance = this;
    }

    private static string GetBundlePath() => Path.Combine(Paths.GameRootPath, "Gemstone", "fn");

    public static void DisableCosmetics()
    {
        try
        {
            VRRig.LocalRig.transform.Find("rig/head/gorillaface").gameObject.layer = LayerMask.NameToLayer("Default");
            foreach (GameObject Cosmetic in VRRig.LocalRig.cosmetics)
            {
                if (Cosmetic.activeSelf && Cosmetic.transform.parent == VRRig.LocalRig.mainCamera.transform.Find("HeadCosmetics"))
                {
                    portedCosmetics.Add(Cosmetic);
                    Cosmetic.transform.SetParent(VRRig.LocalRig.headMesh.transform, false);
                    Cosmetic.transform.localPosition += new Vector3(0f, 0.1333f, 0.1f);
                }
            }
        }
        catch { }
    }

    public static void PlayEmoteFromUrl(string animationName, string audioUrl, float duration = -1f, bool looping = false)
    {
        StopEmote();

        if (assetBundle == null)
            assetBundle = AssetBundle.LoadFromFile(GetBundlePath());

        archivePosition = GorillaTagger.Instance.transform.position;
        VRRig.LocalRig.enabled = false;

        DisableCosmetics();

        activeKyle = Instantiate(assetBundle.LoadAsset<GameObject>("Rig"));
        activeKyle.transform.position = VRRig.LocalRig.transform.Find("rig/body_pivot").position - new Vector3(0f, 1.15f, 0f);
        activeKyle.transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;

        activeKyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

        Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
        animator.enabled = true;

        AnimationClip clip = null;
        foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
        {
            if (c.name == animationName)
            {
                clip = c;
                break;
            }
        }

        if (clip != null)
        {
            clip.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;
            animator.Play(clip.name);
            emoteEndTime = Time.time + (duration > 0f ? duration : clip.length) + (looping ? 9999f : 0f);
        }

        if (instance != null)
            instance.StartCoroutine(PlayAudioFromUrlCoroutine(audioUrl));
    }

    private static IEnumerator PlayAudioFromUrlCoroutine(string url)
    {
        string dir = Path.Combine(Paths.GameRootPath, "Gemstone", "cache");
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, "emote.wav");

        using (UnityWebRequest download = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            download.downloadHandler = new DownloadHandlerFile(filePath);

            yield return download.SendWebRequest();

            if (download.result != UnityWebRequest.Result.Success)
                yield break;
        }

        using (UnityWebRequest load =
            UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            ((DownloadHandlerAudioClip)load.downloadHandler).streamAudio = false;

            yield return load.SendWebRequest();

            if (load.result != UnityWebRequest.Result.Success)
                yield break;

            AudioClip sound = DownloadHandlerAudioClip.GetContent(load);

            if (sound == null)
                yield break;

            yield return null;

            var recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;

            recorder.StopRecording();

            yield return null;

            recorder.SourceType = Recorder.InputSourceType.AudioClip;
            recorder.AudioClip = sound;

            recorder.RestartRecording(true);
            recorder.DebugEchoMode = true;

            yield return new WaitForSeconds(sound.length + 0.4f);

            recorder.DebugEchoMode = false;

            recorder.StopRecording();

            yield return null;

            recorder.AudioClip = null;
            recorder.SourceType = Recorder.InputSourceType.Microphone;

            recorder.RestartRecording(true);

            activeSoundCoroutine = null;
        }
    }

    public static void EnableCosmetics()
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
        StopEmote();

        if (assetBundle == null)
            assetBundle = AssetBundle.LoadFromFile(GetBundlePath());

        archivePosition = GorillaTagger.Instance.transform.position;
        VRRig.LocalRig.enabled = false;

        DisableCosmetics();

        activeKyle = Instantiate(assetBundle.LoadAsset<GameObject>("Rig"));
        activeKyle.transform.position = VRRig.LocalRig.transform.Find("rig/body_pivot").position - new Vector3(0f, 1.15f, 0f);
        activeKyle.transform.rotation = VRRig.LocalRig.transform.Find("rig/body_pivot").rotation;

        activeKyle.transform.Find("KyleRobot/RobotKile").gameObject.GetComponent<Renderer>().renderingLayerMask = 0;

        Animator animator = activeKyle.transform.Find("KyleRobot").GetComponent<Animator>();
        animator.enabled = true;

        AnimationClip clip = null;
        foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
        {
            if (c.name == animationName) { clip = c; break; }
        }

        if (clip != null)
        {
            clip.wrapMode = looping ? WrapMode.Loop : WrapMode.Default;
            animator.Play(clip.name);
            emoteEndTime = Time.time + (duration > 0f ? duration : clip.length) + (looping ? 9999f : 0f);
        }

        AudioClip sound = assetBundle.LoadAsset<AudioClip>(soundName);
        if (sound != null && instance != null)
        {
            activeSoundCoroutine = instance.StartCoroutine(PlayAudioCoroutine(sound));
        }
    }

    private static IEnumerator PlayAudioCoroutine(AudioClip sound)
    {
        var recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
        recorder.SourceType = Recorder.InputSourceType.AudioClip;
        recorder.AudioClip = sound;
        recorder.RestartRecording(true);
        recorder.DebugEchoMode = true;

        yield return new WaitForSeconds(sound.length + 0.4f);

        var resetRecorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
        resetRecorder.SourceType = Recorder.InputSourceType.Microphone;
        resetRecorder.AudioClip = null;
        resetRecorder.RestartRecording(true);
        resetRecorder.DebugEchoMode = false;
        activeSoundCoroutine = null;
    }

    public static void StopEmote()
    {
        if (activeSoundCoroutine != null && instance != null)
        {
            instance.StopCoroutine(activeSoundCoroutine);
            activeSoundCoroutine = null;
        }

        var recorder = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
        recorder.SourceType = Recorder.InputSourceType.Microphone;
        recorder.AudioClip = null;
        recorder.RestartRecording(true);
        recorder.DebugEchoMode = false;

        if (activeKyle != null)
        {
            Destroy(activeKyle);
            EnableCosmetics();
        }
        activeKyle = null;

        VRRig.LocalRig.enabled = true;
        emoteEndTime = -1f;
    }

    private void Update()
    {
        if (activeKyle != null && Time.time < emoteEndTime)
        {
            VRRig.LocalRig.enabled = false;

            Transform spine = activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2");
            VRRig.LocalRig.transform.position = spine.position - (spine.right / 2.5f);
            VRRig.LocalRig.transform.rotation = Quaternion.Euler(0f, spine.rotation.eulerAngles.y, 0f);

            Transform lHand = activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/LeftShoulder/LeftUpperArm/LeftArm/LeftHand");
            Transform rHand = activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/RightShoulder/RightUpperArm/RightArm/RightHand");

            VRRig.LocalRig.leftHand.rigTarget.transform.position = lHand.position;
            VRRig.LocalRig.rightHand.rigTarget.transform.position = rHand.position;

            VRRig.LocalRig.leftHand.rigTarget.transform.rotation = lHand.rotation * Quaternion.Euler(0, 0, 75);
            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = rHand.rotation * Quaternion.Euler(180, 0, -75);

            VRRig.LocalRig.head.rigTarget.transform.rotation = activeKyle.transform.Find("KyleRobot/ROOT/Hips/Spine1/Spine2/Neck/Head").transform.rotation * Quaternion.Euler(0f, 0f, 90f);

            SyncFinger(lHand.Find("Index1"), VRRig.LocalRig.leftIndex);
            SyncFinger(lHand.Find("Middle1"), VRRig.LocalRig.leftMiddle);
            SyncFinger(rHand.Find("Index1"), VRRig.LocalRig.rightIndex);
            SyncFinger(rHand.Find("Middle1"), VRRig.LocalRig.rightMiddle);
        }
        else if (activeKyle != null)
        {
            StopEmote();
        }
    }

    private void SyncFinger(Transform animBone, object fingerField)
    {
        if (animBone == null || fingerField == null) return;
        float curl = Mathf.Clamp01(animBone.localRotation.x * -10f);

        var type = fingerField.GetType();
        type.GetField("calcT")?.SetValue(fingerField, curl);
        type.GetMethod("LerpFinger")?.Invoke(fingerField, new object[] { curl, false });
    }
}