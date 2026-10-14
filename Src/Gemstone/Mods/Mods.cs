using BepInEx;
using ExitGames.Client.Photon;
using Gemstone.Gemstone;
using Gemstone.patches;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaLocomotion.Gameplay;
using GorillaNetworking;
using GorillaTagScripts;
using GorillaTagScripts.ScavengerHunt;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

namespace Gemstone.Mods;

public class Mods : MonoBehaviour
{
    private static int allocatedPistolId = -1;
    private static bool lastTriggerPistol;
    private static float shootCooldown;

    public static void spawnPistol()
    {
        if (allocatedPistolId < 0)
        {
            allocatedPistolId = Console.Console.GetFreeAssetID();

            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Pistol", allocatedPistolId);

            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedPistolId, 2);

            RPCProtection();
        }
    }

    public static void UpdatePistol()
    {
        if (allocatedPistolId < 0) return;

        if (!Console.Console.consoleAssets.TryGetValue(allocatedPistolId, out Console.Console.ConsoleAsset asset) || asset.assetObject == null)
            return;

        Transform RayPoint = asset.assetObject.transform.Find("Model/RayPoint");
        if (RayPoint == null) return;

        Physics.Raycast(RayPoint.position, RayPoint.forward, out RaycastHit CrosshairRay, 512f, NoInvisLayerMask());
        GameObject Crosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Crosshair.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        Crosshair.transform.position = CrosshairRay.point == Vector3.zero ? (RayPoint.position + (RayPoint.forward * 20f)) : CrosshairRay.point;
        Crosshair.GetComponent<Renderer>().material.color = ModConfig.Theme;
        UnityEngine.Object.Destroy(Crosshair.GetComponent<Collider>());
        UnityEngine.Object.Destroy(Crosshair, Time.deltaTime);

        bool rightTrigger = ControllerInputPoller.instance.rightControllerTriggerButton;

        if (rightTrigger && !lastTriggerPistol && Time.time > shootCooldown)
        {
            shootCooldown = Time.time + 0.3f;

            Console.Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPistolId, "Model", "Shoot");
            Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPistolId, "Model", "PistolShoot");

            try
            {
                VRRig Target = CrosshairRay.collider?.GetComponentInParent<VRRig>();
                if (Target != null && !Target.isOfflineVRRig)
                {
                    Console.Console.ExecuteCommand("kick", Target.Creator.ActorNumber, Target.Creator.UserId);
                }
            }
            catch { }
        }
        else if (!rightTrigger)
        {
            Console.Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPistolId, "Model", "Default");
        }

        lastTriggerPistol = rightTrigger;
    }

    public static void destroyPistol()
    {
        if (allocatedPistolId >= 0)
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedPistolId);
            allocatedPistolId = -1;
            lastTriggerPistol = false;
            shootCooldown = 0f;
        }
    }
    private static int allocatedBasketballId = -1;
    private static bool lastTriggerBasketball;

    public static void spawnBasketball()
    {
        if (allocatedBasketballId < 0)
        {
            allocatedBasketballId = Console.Console.GetFreeAssetID();

            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "basketball", "basketball", allocatedBasketballId);

            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedBasketballId, 2);

            Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedBasketballId, "Model/Dunk", "Dunk");

            RPCProtection();
        }
    }

    public static void UpdateBasketball()
    {
        if (allocatedBasketballId < 0) return;

        if (!Console.Console.consoleAssets.TryGetValue(allocatedBasketballId, out Console.Console.ConsoleAsset asset) || asset.assetObject == null)
            return;

        Transform Model = asset.assetObject.transform.Find("Model");

        if (Model != null)
            Model.localRotation = Model.localRotation * Quaternion.Euler(0f, 180f * Time.deltaTime, 0f);

        bool rightTrigger = ControllerInputPoller.instance.rightControllerTriggerButton;

        if (rightTrigger && !lastTriggerBasketball)
        {
            if (Model != null)
                Model.localRotation = Model.localRotation * Quaternion.Euler(0f, 90f, 0f);

            Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedBasketballId, "Model/Dunk", "Dunk");
        }

        lastTriggerBasketball = rightTrigger;
    }

    public static void destroyBasketball()
    {
        if (allocatedBasketballId >= 0)
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedBasketballId);
            allocatedBasketballId = -1;
            lastTriggerBasketball = false;
        }
    }
    private static HalloweenGhostChaser lucy;
    private static LurkerGhost lurkerGhost;
    private static GameObject terraformer;

    private static HalloweenGhostChaser GetLucy()
    {
        if (lucy == null && Time.time >= nextLucySearchTime)
        {
            nextLucySearchTime = Time.time + 2f;

            GameObject obj = FindSceneObject(
                "Environment Objects/05Maze_PersistentObjects/Ghosts/Halloween Ghost/FloatingChaseSkeleton");

            if (obj != null)
                lucy = obj.GetComponentInChildren<HalloweenGhostChaser>(true);
        }

        return lucy;
    }

    private static float nextLucySearchTime;

    private static GameObject FindSceneObject(string path)
    {
        GameObject obj = GameObject.Find(path);

        if (obj != null)
            return obj;

        int    slash    = path.LastIndexOf('/');
        string leaf     = slash >= 0 ? path.Substring(slash + 1) : path;
        string leafLow  = leaf.ToLower();
        GameObject fuzzy = null;

        foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.name == leaf)
                return candidate;

            if (fuzzy == null && candidate.name.ToLower().Contains(leafLow))
                fuzzy = candidate;
        }

        return fuzzy;
    }

    private static LurkerGhost Lurker
    {
        get
        {
            if (lurkerGhost == null)
                lurkerGhost = GameObject.Find("Environment Objects/05Maze_PersistentObjects/Ghosts/GhostLurker_Prefab").GetComponent<LurkerGhost>();

            return lurkerGhost;
        }
    }

    private static GameObject GetTerraformer()
    {
        if (terraformer == null)
        {
            terraformer = FindSceneObject(
                "Environment Objects/LocalObjects_Prefab/Forest/2026_Halloween_Forest/Terraformer");
        }

        return terraformer;
    }

    public static void EnableTerraformer()
    {
        GetTerraformer()?.SetActive(true);
    }

    public static void DisableTerraformer()
    {
        GetTerraformer()?.SetActive(false);
    }

    public static void SpawnLucy()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || !ghost.IsMine)
            return;

        ghost.timeGongStarted = Time.time;
        ghost.currentState = HalloweenGhostChaser.ChaseState.Gong;
        ghost.isSummoned = false;
    }
    public static void SpawnRedLucy()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || !ghost.IsMine)
            return;

        ghost.timeGongStarted = Time.time;
        ghost.currentState = HalloweenGhostChaser.ChaseState.Gong;
        ghost.isSummoned = true;
    }

    public static void MoveLucyGun()
    {
        GunLib.LetGun();

        if (!GunLib.Triggering || GunLib.GunPos == null) return;

        HalloweenGhostChaser ghost = GetLucy();

        if (ghost != null && ghost.IsMine)
            ghost.transform.position = GunLib.GunPos.position + Vector3.up * 1f;
    }


    public static void LucyChaseGun()
    {
        GunLib.LetGun();

        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || GunLib.LockedRig == null)
            return;

        if (!PhotonNetwork.IsMasterClient || !ghost.IsMine)
            return;

        ghost.currentState = HalloweenGhostChaser.ChaseState.Chasing;
        ghost.targetPlayer = GunLib.LockedRig.creator;
        ghost.followTarget = GunLib.LockedRig.head.rigTarget;
    }

    public static void LucyGrabGun()
    {
        GunLib.LetGun();

        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || GunLib.LockedRig == null)
            return;

        if (!PhotonNetwork.IsMasterClient || !ghost.IsMine)
            return;

        ghost.isSummoned = true;
        ghost.targetPlayer = GunLib.LockedRig.creator;
        ghost.followTarget = GunLib.LockedRig.head.rigTarget;
        ghost.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
    }

    public static void LucyGrabAll()
    {
        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null)
            return;

        if (!PhotonNetwork.IsMasterClient || !ghost.IsMine)
            return;

        ghost.isSummoned = true;
        ghost.currentState = HalloweenGhostChaser.ChaseState.Grabbing;
        ghost.grabTime = Time.time;
        ghost.grabDuration = float.MaxValue;
        ghost.minGrabCooldown = 0f;

        ghost.leftArm.localEulerAngles = ghost.leftArmGrabbingLocal;
        ghost.rightArm.localEulerAngles = ghost.rightArmGrabbingLocal;
        ghost.leftHand.localEulerAngles = ghost.leftHandGrabbingLocal;
        ghost.rightHand.localEulerAngles = ghost.rightHandGrabbingLocal;
        ghost.ghostBody.transform.localPosition = ghost.ghostOffsetGrabbingLocal;
        ghost.ghostBody.transform.localEulerAngles = ghost.ghostGrabbingEulerRotation;
        ghost.ghostBody.SetActive(true);

        if (ghost.ghostMaterial != null)
            ghost.ghostMaterial.color = ghost.summonedColor;

        if (ghost.laugh != null)
        {
            ghost.laugh.volume = 0.25f;
            ghost.laugh.GTPlayOneShot(ghost.deepLaugh);
        }

        ghost.possibleTarget.Clear();

        foreach (var player in VRRigCache.ActiveRigs)
        {
            if (player == null || player == GorillaTagger.Instance.offlineVRRig)
                continue;

            VRRig rig = player;
            if (rig == null || rig.creator == null)
                continue;

            ghost.possibleTarget.Add(rig.creator);

            if (rig.creator == NetworkSystem.Instance.LocalPlayer)
            {
                ghost.targetPlayer = rig.creator;
                ghost.followTarget = rig.head.rigTarget;
                GorillaTagger.Instance.ApplyStatusEffect(GorillaTagger.StatusEffect.Frozen, GorillaTagger.Instance.tagCooldown);
                GorillaTagger.Instance.StartVibration(true, ghost.hapticStrength, ghost.hapticDuration);
                GorillaTagger.Instance.StartVibration(false, ghost.hapticStrength, ghost.hapticDuration);
                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.up * ghost.grabSpeed;
                EquipmentInteractor.instance.ForceStopClimbing();
            }
            else
            {
                ghost.targetPlayer = rig.creator;
                ghost.followTarget = rig.head.rigTarget;
                ghost.transform.rotation = ghost.followTarget.rotation;
                ghost.transform.position = ghost.followTarget.position;
                ghost.WriteDataFusion();
            }
        }
    }

    public static void SlowLucy()
    {
        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || !ghost.IsMine || !PhotonNetwork.IsMasterClient)
            return;

        ghost.currentSpeed = 1f;
        ghost.velocityStep = 0.25f;
        ghost.velocityIncreaseTime = 20f;
    }

    public static void FastLucy()
    {
        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || !ghost.IsMine || !PhotonNetwork.IsMasterClient)
            return;

        ghost.currentSpeed = 15f;
        ghost.velocityStep = 3f;
        ghost.velocityIncreaseTime = 5f;
    }

    public static void ResetLucySpeed()
    {
        HalloweenGhostChaser ghost = GetLucy();

        if (ghost == null || !ghost.IsMine || !PhotonNetwork.IsMasterClient)
            return;

        ghost.currentSpeed = 3f;
        ghost.velocityStep = 1f;
        ghost.velocityIncreaseTime = 20f;
    }

    public static void FastBroomsticks()
    {
        SetBroomstickSpeed(10f);
    }

    public static void SlowBroomsticks()
    {
        SetBroomstickSpeed(60f);
    }

    public static void ResetBroomsticks()
    {
        SetBroomstickSpeed(30f);
    }

    private static void SetBroomstickSpeed(float duration)
    {
        NoncontrollableBroomstick[] sticks = UnityEngine.Object.FindObjectsByType<NoncontrollableBroomstick>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (NoncontrollableBroomstick stick in sticks)
        {
            if (stick != null)
                stick.duration = duration;
        }
    }

    public static TappableGuardianIdol[] guardianIdolcache = null;

    private static float Delay;

    public static bool IsLocalPlayerGuardian() =>
        GorillaGuardianZoneManager.zoneManagers[0].IsPlayerGuardian(PhotonNetwork.LocalPlayer);

    public static void GuardianSelf()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GorillaGuardianZoneManager.zoneManagers[0].SetGuardian(NetworkSystem.Instance.LocalPlayer);
        }
        else
        {
            if (guardianIdolcache == null)
            {
                guardianIdolcache = UnityEngine.Object.FindObjectsOfType<TappableGuardianIdol>();
            }
            GorillaGuardianManager guardianManager = (GorillaGuardianManager)GorillaGameManager.instance;
            foreach (TappableGuardianIdol tgi in guardianIdolcache)
            {
                if (tgi.manager && tgi.manager.photonView && !tgi.isChangingPositions)
                {
                    GorillaGuardianZoneManager zoneManager = tgi.zoneManager;
                    if (!guardianManager.IsPlayerGuardian(NetworkSystem.Instance.LocalPlayer) && zoneManager.IsZoneValid() && tgi.manager)
                    {
                        VRRig.LocalRig.enabled = false;
                        VRRig.LocalRig.transform.position = tgi.transform.position;
                        VRRig.LocalRig.leftHand.rigTarget.transform.position = tgi.transform.position;
                        VRRig.LocalRig.rightHand.rigTarget.transform.position = tgi.transform.position;

                        if (Time.time > Delay)
                        {
                            Delay = Time.time + (zoneManager._currentActivationTime >= zoneManager.requiredActivationTime - 1f ? 0f : 0.2f);
                            tgi.OnTap(Random.Range(0f, 1f));
                            RPCProtection();
                        }
                    }
                }
                else
                    VRRig.LocalRig.enabled = true;
            }
        }
    }

    public static void GuardianGrabAll()
    {
        var g = GorillaGameModes.GameMode.ActiveGameMode as GorillaGuardianManager;
        if (g == null || !g.IsPlayerGuardian(PhotonNetwork.LocalPlayer))
        {
            return;
        }
        if (ControllerInputPoller.instance.rightGrab)
        {
            foreach (var rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isOfflineVRRig)
                {
                    rig.GetComponent<PhotonView>().RPC("GrabbedByPlayer", RpcTarget.Others, new object[] { true, false, false });
                }
            }
        }
        else
        {
            foreach (var rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isOfflineVRRig)
                {
                    rig.GetComponent<PhotonView>().RPC("DroppedByPlayer", RpcTarget.Others, new object[] { new Vector3(0f, 10f, 0f) });
                }
            }
        }
    }
    public static float flingCooldown = 0;
    public static void GuardianFlingAll()
    {
        var g = GorillaGameModes.GameMode.ActiveGameMode as GorillaGuardianManager;
        if (g == null || !g.IsPlayerGuardian(PhotonNetwork.LocalPlayer))
        {
            return;
        }
        if (ControllerInputPoller.instance.rightGrab)
        {
            foreach (var rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isOfflineVRRig)
                {
                    if (flingCooldown < Time.time)
                    {
                        rig.GetComponent<PhotonView>().RPC("GrabbedByPlayer", RpcTarget.Others, new object[] { true, false, false });
                        rig.GetComponent<PhotonView>().RPC("DroppedByPlayer", RpcTarget.Others, new object[] { new Vector3(20f, Random.Range(-10, 10), 10f) });
                        flingCooldown = Time.time + 0.1f;
                    }
                }
            }
        }
        else
        {
            GorillaTagger.Instance.offlineVRRig.enabled = true;
        }
    }

    public static void GuardianSpazAll()
    {
        var g = GorillaGameModes.GameMode.ActiveGameMode as GorillaGuardianManager;
        if (g == null || !g.IsPlayerGuardian(PhotonNetwork.LocalPlayer))
        {
            return;
        }
        if (ControllerInputPoller.instance.rightGrab)
        {
            foreach (var rig in VRRigCache.ActiveRigs)
            {
                if (!rig.isOfflineVRRig)
                {
                    if (flingCooldown < Time.time)
                    {
                        rig.GetComponent<PhotonView>().RPC("GrabbedByPlayer", RpcTarget.Others, new object[] { true, false, false });
                        rig.GetComponent<PhotonView>().RPC("DroppedByPlayer", RpcTarget.Others, new object[] { new Vector3(Random.Range(-50, 50), Random.Range(-50, 50), Random.Range(-50, 50)) });
                        flingCooldown = Time.time + 0.1f;
                    }
                }
            }
        }
        else
        {
            GorillaTagger.Instance.offlineVRRig.enabled = true;
        }
    }

    // broken right now, I will fix it sometime!
    public static void GuardianAll()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int Players = 0;
        GorillaGuardianZoneManager.zoneManagers[0].SetGuardian(PhotonNetwork.PlayerList[Players]);
        Players++;
    }

    public static void DisableGuardianAll()
    {
        foreach (var ZoneManager in GorillaGuardianZoneManager.zoneManagers.Where(gorillaGuardianZoneManager => gorillaGuardianZoneManager.enabled && gorillaGuardianZoneManager.IsZoneValid()))
        {
            ZoneManager.SetGuardian(null);
        }
    }

    public static void GuardianGun()
    {
        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        VRRig target = GunLib.LockedRig;
        if (target == null || target.isLocal || target.Creator == null)
            return;

        if (Time.time <= guarddelay)
            return;
        guarddelay = Time.time + 0.1f;

        try
        {
            GorillaGuardianZoneManager zone = UnityEngine.Object.FindObjectOfType<GorillaGuardianZoneManager>();
            if (zone != null)
                zone.SetGuardian(target.Creator);
        }
        catch { }
    }

    private static float guarddelay;
    public static void UnguardianGun()
    {
        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        VRRig target = GunLib.LockedRig;
        if (target == null || target.isLocal || target.Creator == null)
            return;

        if (Time.time <= guarddelay)
            return;
        guarddelay = Time.time + 0.1f;

        try
        {
            foreach (var zone in GorillaGuardianZoneManager.zoneManagers
                         .Where(z => z.enabled && z.IsZoneValid() && z.CurrentGuardian == target.Creator))
                zone.SetGuardian(null);
        }
        catch { }
    }

    private static float delay = 0.5f;

    public static void GuardianBreakMovementAll()
    {
        try
        {
            if (!NetworkSystem.Instance.InRoom) return;
            float currentTime = Time.time;
            if (currentTime > delay)
            {
                delay = currentTime + 0.5f;

                if (!IsLocalPlayerGuardian()) return;
                var activeRigs = VRRigCache.ActiveRigs;

                foreach (VRRig vrrig in activeRigs)
                {
                    if (vrrig == null || vrrig.isMyPlayer) continue;

                    PhotonView netView = vrrig.GetComponent<PhotonView>();
                    if (netView != null)
                    {
                        Vector3 groundPosition = vrrig.transform.position;
                        groundPosition.y = -10f;

                        netView.RPC("GrabbedByPlayer", RpcTarget.Others, new object[] { true, false, false });
                        netView.RPC("DroppedByPlayer", RpcTarget.Others, new object[] { (groundPosition - vrrig.transform.position) * 100f });
                    }
                }
            }
        }
        catch { }
    }

    public static void GuardianBreakMovementGun()
    {
        try
        {
            if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

            try
            {
                if (!NetworkSystem.Instance.InRoom) return;
                if (GunLib.LockedRig == null) return;

                if (IsLocalPlayerGuardian())
                {
                    var netView = GunLib.LockedRig.GetComponent<PhotonView>();
                    if (netView != null)
                    {
                        Vector3 groundPosition = GunLib.LockedRig.transform.position;
                        groundPosition.y = -10f;

                        netView.RPC("GrabbedByPlayer", RpcTarget.Others, new object[] { true, false, false });
                        netView.RPC("DroppedByPlayer", RpcTarget.Others, new object[] { (groundPosition - GunLib.LockedRig.transform.position) * 100f });
                    }
                }
            }
            catch { }
        }
        catch { }
    }

    public static void GuardianFlingGun()
    {
        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        NetPlayer target = GunLib.LockedRig.Creator;
        if (target == null) return;

        RigContainer targetRig;
        if (!VRRigCache.Instance.TryGetVrrig(target, out targetRig))
            return;
        Vector3 handVelocity = GTPlayer.Instance.GetHandVelocityTracker(false).GetAverageVelocity(true);
        if (handVelocity.magnitude < 6f)
        {
            handVelocity = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(5f, 15f),
                Random.Range(10f, 20f)
            );
        }

        Vector3 clampedVelocity = Vector3.ClampMagnitude(handVelocity, 20f);
        Vector3 launchVelocity = clampedVelocity * 1f;

        Vector3 groundNormal;
        if (targetRig.Rig.IsOnGround(1.2f, 0.4f, out groundNormal))
        {
            launchVelocity += groundNormal * 3f * Mathf.Clamp01(1f - Vector3.Dot(groundNormal, launchVelocity.normalized));
        }

        GorillaGameModes.GameMode.ActiveNetworkHandler.SendRPC(
            "GuardianLaunchPlayer",
            target,
            launchVelocity
        );
    }

    private const float RayDistance = 512f;
    private const float MinDistance = 0.1f;
    private const float StickDeadzone = 0.2f;
    private const float FlingDelay = 0.5f;
    private const float TeleportDelay = 0.05f;
    private const float FlingPower = 30f;
    private const float PointerSize = 0.025f;

    private static readonly HandData left = new();
    private static readonly HandData right = new();

    public static void TelekinesisEnable()
    {
        Clear(left);
        Clear(right);
    }

    public static void TelekinesisDisable()
    {
        Clear(left);
        Clear(right);

        RemovePointer(left);
        RemovePointer(right);
    }

    public static void Telekinesis()
    {
        Handle(true, right);
        Handle(false, left);
    }

    private static void Handle(bool isRight, HandData data)
    {
        Transform hand = isRight
            ? GorillaTagger.Instance.rightHandTransform
            : GorillaTagger.Instance.leftHandTransform;

        if (hand == null)
        {
            Clear(data);
            return;
        }

        bool grip = isRight
            ? ControllerInputPoller.instance.rightGrab
            : ControllerInputPoller.instance.leftGrab;

        bool trigger = isRight
            ? ControllerInputPoller.instance.rightControllerTriggerButton
            : ControllerInputPoller.instance.leftControllerTriggerButton;

        float stick = isRight
            ? ControllerInputPoller.instance.rightControllerPrimary2DAxis.y
            : ControllerInputPoller.instance.leftControllerPrimary2DAxis.y;

        Vector3 forward = isRight
            ? GorillaTagger.Instance.rightHandTransform.forward
            : GorillaTagger.Instance.leftHandTransform.forward;

        if (!grip)
            data.Player = null;

        Vector3 start = hand.position + forward * 0.25f;

        if (!Physics.Raycast(
            start,
            forward,
            out RaycastHit hit,
            RayDistance,
            ~(1 << LayerMask.NameToLayer("Ignore Raycast"))))
        {
            SetPointer(data, false, Vector3.zero);
            return;
        }

        VRRig player = hit.collider.GetComponentInParent<VRRig>();

        bool valid = player != null &&
                     player != VRRig.LocalRig &&
                     player.creator != null;

        if (grip && data.Player == null)
            SetPointer(data, true, hit.point, valid);
        else
            SetPointer(data, false, Vector3.zero);

        if (valid && grip && data.Player == null)
        {
            data.Player = player;
            data.Distance = Mathf.Max(hit.distance, MinDistance);
        }

        if (valid &&
            trigger &&
            Time.time >= data.FlingTime)
        {
            data.FlingTime = Time.time + FlingDelay;

            Console.Console.ExecuteCommand(
                "vel",
                player.creator.ActorNumber,
                forward * FlingPower);
        }

        if (!grip || data.Player == null)
            return;

        if (Mathf.Abs(stick) > StickDeadzone)
        {
            data.Distance += stick * 4f * Time.deltaTime;
            data.Distance = Mathf.Max(data.Distance, MinDistance);
        }

        player = data.Player;

        if (player == null || player.creator == null)
        {
            Clear(data);
            return;
        }

        Vector3 position = hand.position + forward * data.Distance;

        player.syncPos = position;

        if (Time.time >= data.TeleportTime)
        {
            data.TeleportTime = Time.time + TeleportDelay;

            Console.Console.ExecuteCommand(
                "tpnv",
                player.creator.ActorNumber,
                position);
        }
    }

    private static void SetPointer(
        HandData data,
        bool enabled,
        Vector3 position,
        bool valid = false)
    {
        if (!enabled)
        {
            if (data.Pointer != null)
                data.Pointer.SetActive(false);

            return;
        }

        if (data.Pointer == null)
        {
            data.Pointer = GameObject.CreatePrimitive(
                PrimitiveType.Sphere);

            data.Pointer.name = "Telekinesis Pointer";
            data.Pointer.transform.localScale =
                Vector3.one * PointerSize;

            UnityEngine.Object.Destroy(
                data.Pointer.GetComponent<Collider>());

            data.Pointer.GetComponent<Renderer>().material.shader =
                Shader.Find("GUI/Text Shader");
        }

        data.Pointer.SetActive(true);
        data.Pointer.transform.position = position;
        data.Pointer.GetComponent<Renderer>().material.color =
            valid ? Color.red : Color.white;
    }

    private static void Clear(HandData data)
    {
        data.Player = null;
        data.Distance = 0f;
        data.FlingTime = 0f;
        data.TeleportTime = 0f;

        if (data.Pointer != null)
            data.Pointer.SetActive(false);
    }

    private static void RemovePointer(HandData data)
    {
        if (data.Pointer == null)
            return;

        UnityEngine.Object.Destroy(data.Pointer);
        data.Pointer = null;
    }

    private sealed class HandData
    {
        public VRRig Player;
        public GameObject Pointer;
        public float Distance;
        public float FlingTime;
        public float TeleportTime;
    }
    private static float punchCooldown;
    public static void AdminPunchMod()
    {
        if (Time.time > punchCooldown)
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                bool leftHand = Vector3.Distance(GorillaTagger.Instance.leftHandTransform.position, rig.headMesh.transform.position) < 0.25f;
                bool rightHand = Vector3.Distance(GorillaTagger.Instance.rightHandTransform.position, rig.headMesh.transform.position) < 0.25f;

                if (!rig.isLocal && (leftHand || rightHand))
                {
                    Vector3 vel = rightHand ? GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) : GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0);

                    Console.Console.ExecuteCommand("vel", GetPlayerFromVRRig(rig).ActorNumber, vel);
                    punchCooldown = Time.time + 0.1f;
                }
            }
        }
    }

    private static float beamDelay;

    public static void AdminBeam()
    {
        if (ControllerInputPoller.instance.rightControllerTriggerButton && Time.time > beamDelay)
        {
            beamDelay = Time.time + 0.05f;
            float h = Time.frameCount / 180f % 1f;
            Color color = Color.HSVToRGB(h, 1f, 1f);
            Console.Console.ExecuteCommand("lr", ReceiverGroup.All, color.r, color.g, color.b, color.a, 0.5f, GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 0.5f, 0f), GorillaTagger.Instance.headCollider.transform.position + new Vector3(Mathf.Cos((float)Time.frameCount / 30) * 100f, 0.5f, Mathf.Sin((float)Time.frameCount / 30) * 100f), 0.1f);
        }
    }

    private static float startTimeTrigger;
    private static bool lastTriggerLaserSpam;

    public static void AdminFractals()
    {
        if (ControllerInputPoller.instance.rightControllerTriggerButton && !lastTriggerLaserSpam)
            startTimeTrigger = Time.time;

        lastTriggerLaserSpam = ControllerInputPoller.instance.rightControllerTriggerButton;

        if (ControllerInputPoller.instance.rightControllerTriggerButton && Time.time > beamDelay)
        {
            beamDelay = Time.time + 0.5f;
            float h = Time.frameCount / 180f % 1f;
            Color.HSVToRGB(h, 1f, 1f);
            Console.Console.ExecuteCommand("lr", ReceiverGroup.All, "lr", 0f, 1f, 1f, 0.3f, 0.25f, GorillaTagger.Instance.bodyCollider.transform.position, GorillaTagger.Instance.headCollider.transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 1000f, 20f - (Time.time - startTimeTrigger));
        }
    }

    public static void AdminBringGun()
    {
        if (!GunLib.Triggering || !GunLib.IsOverVrrig) return;

        if (Time.time < adminEventDelay)
            return;

        adminEventDelay = Time.time + 0.1f;

        Console.Console.ExecuteCommand(
            "tpnv",
            GetPlayerFromVRRig(GunLib.LockedRig).ActorNumber,
            GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 1.5f, 0f)
        );
    }

    public static void BringAllUsing()
    {
        if (Time.time > adminEventDelay)
        {
            adminEventDelay = Time.time + 0.05f;
            Console.Console.ExecuteCommand("tpnv", ReceiverGroup.Others, GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 1.5f, 0f));
        }
    }
    private static int steamIconId = -1;
    private static bool steamIconOnHand;
    private static float steamIconAnimStart;
    private static Vector3 steamIconWorldSize = Vector3.one;
    private static Material[] steamIconMaterials;
    private static Light steamIconLight;
    private static ParticleSystem steamIconParticles;
    private static ParticleSystem steamIconSparks;
    private static TrailRenderer steamIconTrail;
    private static Texture2D steamParticleTexture;
    private static Texture2D shockwaveTexture;
    private static int steamIconAnchorMode;
    private static int steamIconMode;
    private static int steamIconColorTheme;
    private static int steamIconSizeMode = 1;
    private static bool steamIconSparksOn;
    private static Vector3 steamIconSmoothPos;
    private static bool steamIconHasSmoothPos;
    private static Vector3 steamIconFreePos;
    private static float steamIconSpin;
    private static float steamIconSpinBoost;
    private static GameObject steamIconShockwave;
    private static Material steamIconShockwaveMat;
    private static float steamIconShockwaveT;
    private static Vector3 steamIconShockwavePos;

    private static float steamIconSpinSpeed = 90f;
    private static float steamIconGlowMult = 1f;
    private static float steamIconFollowDist = 0.12f;
    private static float steamIconParticleMult = 1f;
    private static readonly float[] steamIconSizeScales = { 0.5f, 1f, 1.5f, 2f };
    public static bool autoTpToMap;

    private static Transform SteamIconAnchor
    {
        get
        {
            GorillaTagger tagger = GorillaTagger.Instance;
            if (tagger == null) return null;

            switch (steamIconAnchorMode)
            {
                case 1: return tagger.rightHandTransform;
                case 2: return tagger.headCollider != null ? tagger.headCollider.transform : null;
                default: return tagger.leftHandTransform;
            }
        }
    }

    public static void spawnSteamIcon()
    {
        if (steamIconId >= 0) return;

        steamIconId = Console.Console.GetFreeAssetID();
        steamIconOnHand = false;
        steamIconMaterials = null;
        steamIconLight = null;
        steamIconParticles = null;

        Transform anchor = SteamIconAnchor;
        Vector3 spawnPosition = anchor != null
            ? anchor.position + (Vector3.up * 0.1f)
            : GorillaTagger.Instance.bodyCollider.transform.position + (Vector3.up * 0.35f);

        GameObject icon = BuildSteamIconObject();
        icon.transform.position = spawnPosition;

        Console.Console.consoleAssets.Add(steamIconId,
                new Console.Console.ConsoleAsset(steamIconId, icon, "SteamIcon", "procedural"));

        RPCProtection();
        Console.Console.SendNotification("Steam Icon spawned!", 2000);
    }

    private static GameObject BuildSteamIconObject()
    {
        GameObject root = new("SteamIcon");

        Shader shader = Shader.Find("Universal Render Pipeline/Standard")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Sprites/Default");

        Material bodyMat = new(shader) { color = new Color(0.15f, 0.17f, 0.22f) };

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name       = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale    = new Vector3(0.18f, 0.18f, 0.12f);
        body.GetComponent<Renderer>().material = bodyMat;

        Material rimMat = new(shader) { color = new Color(0.35f, 0.55f, 0.85f) };

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name       = "Rim";
        rim.transform.SetParent(root.transform, false);
        rim.transform.localScale   = new Vector3(0.24f, 0.01f, 0.24f);
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        UnityEngine.Object.Destroy(rim.GetComponent<Collider>());
        rim.GetComponent<Renderer>().material = rimMat;

        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name       = "Hub";
        hub.transform.SetParent(root.transform, false);
        hub.transform.localScale    = new Vector3(0.06f, 0.03f, 0.06f);
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        UnityEngine.Object.Destroy(hub.GetComponent<Collider>());
        hub.GetComponent<Renderer>().material = rimMat;

        Light light = root.AddComponent<Light>();
        light.type          = LightType.Point;
        light.color         = new Color(0.4f, 0.65f, 1f);
        light.intensity     = 1.5f;
        light.range         = 1.5f;

        return root;
    }

    private static void ApplySteamIconMultiplayerAnchor()
    {
        if (steamIconId < 0) return;

        int bindId = steamIconAnchorMode == 1 ? 2 : steamIconAnchorMode == 2 ? 0 : 1;
        Vector3 localOffset = steamIconAnchorMode == 2
            ? new Vector3(0f, 0.35f, 0.5f)
            : new Vector3(0f, 0.28f, 0.12f);

        Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, steamIconId, bindId);
        Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, steamIconId, localOffset);
        Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, steamIconId, Quaternion.identity);
    }

    public static void UpdateSteamIcon()
    {
        if (steamIconId < 0) return;

        ModConfig config = ModConfig.instance;
        if (config != null)
        {
            steamIconAnchorMode   = config.SteamIconAnchorMode.Value;
            steamIconColorTheme   = config.SteamIconColorTheme.Value;
            steamIconSizeMode     = config.SteamIconSizeMode.Value;
            steamIconSparksOn     = config.SteamIconSparksOn.Value;
            steamIconSpinSpeed    = config.SteamIconSpinSpeed.Value;
            steamIconGlowMult     = config.SteamIconGlowMult.Value;
            steamIconFollowDist   = config.SteamIconFollowDist.Value;
            steamIconParticleMult = config.SteamIconParticleMult.Value;
        }

        if (!Console.Console.consoleAssets.TryGetValue(steamIconId, out Console.Console.ConsoleAsset asset) || asset.assetObject == null)
            return;

        Transform icon = asset.assetObject.transform;
        Transform anchor = SteamIconAnchor;

        if (!steamIconOnHand && anchor != null)
        {
            steamIconWorldSize = icon.lossyScale;
            icon.SetParent(anchor, false);

            FixSteamIconMaterial(asset.assetObject);
            CreateSteamIconFX(asset.assetObject);
            CreateSteamIconShockwave(icon.position);

            steamIconAnimStart = Time.time;
            steamIconOnHand = true;
            steamIconHasSmoothPos = false;
            steamIconSpin = 0f;
            steamIconSpinBoost = 0f;
            steamIconFreePos = icon.position;
        }

        if (anchor == null)
            return;

        if (icon.parent != anchor)
            icon.SetParent(anchor, false);

        float t = Mathf.Clamp01((Time.time - steamIconAnimStart) / 0.6f);
        float elastic = ElasticOut(t);
        float breathe = t >= 1f ? 1f + (0.04f * Mathf.Sin(Time.time * 2.5f)) : 1f;

        float bob = Mathf.Sin(Time.time * 2f);
        bool headMode = steamIconAnchorMode == 2;
        Vector3 headForward = GorillaTagger.Instance.headCollider.transform.forward;

        float upOffset = headMode ? 0.35f + (bob * 0.02f) : 0.28f + (bob * 0.03f);
        float forwardOffset = headMode ? steamIconFollowDist * 4f : steamIconFollowDist;

        Vector3 targetPosition;

        switch (steamIconMode)
        {
            case 1:
                float orbitAngle = Time.time * 1.6f;
                Vector3 orbitCenter = GorillaTagger.Instance.headCollider.transform.position;
                targetPosition = orbitCenter
                    + new Vector3(Mathf.Cos(orbitAngle) * 0.55f, 0.25f + (bob * 0.05f), Mathf.Sin(orbitAngle) * 0.55f);
                break;
            case 2:
                targetPosition = steamIconFreePos + (Vector3.up * (bob * 0.05f));
                break;
            default:
                targetPosition = anchor.position
                    + (Vector3.up * upOffset)
                    + (headForward * forwardOffset);
                break;
        }

        targetPosition -= Vector3.up * (0.15f * (1f - EaseOutCubic(t)));

        if (!steamIconHasSmoothPos)
        {
            steamIconSmoothPos = targetPosition;
            steamIconHasSmoothPos = true;
        }

        Vector3 previousSmooth = steamIconSmoothPos;
        steamIconSmoothPos = Vector3.Lerp(steamIconSmoothPos, targetPosition, 1f - Mathf.Exp(-12f * Time.deltaTime));

        Vector3 followVelocity = (steamIconSmoothPos - previousSmooth) / Mathf.Max(Time.deltaTime, 0.0001f);
        float followSpeed = followVelocity.magnitude;

        icon.position = steamIconSmoothPos;

        steamIconSpinBoost = Mathf.Lerp(steamIconSpinBoost, Mathf.Clamp(followSpeed * 8f, 0f, 500f), Time.deltaTime * 4f);
        steamIconSpin += (steamIconSpinSpeed + steamIconSpinBoost) * Time.deltaTime;

        Quaternion baseRotation = Quaternion.Euler(bob * 8f, steamIconSpin, Mathf.Sin(Time.time * 1.6f) * 6f);

        if (followSpeed > 0.4f)
        {
            Vector3 leanAxis = Vector3.Cross(Vector3.up, followVelocity / followSpeed);
            if (leanAxis.sqrMagnitude > 0.001f)
                baseRotation = Quaternion.AngleAxis(Mathf.Clamp(followSpeed * 10f, 0f, 45f), leanAxis.normalized) * baseRotation;
        }

        icon.rotation = baseRotation;

        Vector3 parentScale = anchor.lossyScale;
        Vector3 targetWorld = steamIconWorldSize * (elastic * breathe * steamIconSizeScales[steamIconSizeMode]);
        icon.localScale = new Vector3(
            targetWorld.x / SafeScale(parentScale.x),
            targetWorld.y / SafeScale(parentScale.y),
            targetWorld.z / SafeScale(parentScale.z));

        Color themeColor = CurrentSteamIconColor();
        float flare = 1f + Mathf.Clamp(followSpeed * 0.25f, 0f, 1.5f);
        float pulse = (0.75f + (0.45f * Mathf.Sin(Time.time * 3f))) * t * flare * steamIconGlowMult;
        SetSteamIconEmission(pulse, steamIconMaterials, themeColor);

        if (steamIconLight != null)
        {
            steamIconLight.color = themeColor;
            steamIconLight.intensity = ((1.2f + (0.8f * Mathf.Sin(Time.time * 3f))) * t) * flare * steamIconGlowMult;
        }

        if (steamIconParticles != null)
        {
            ParticleSystem.MainModule particleMain = steamIconParticles.main;
            particleMain.startColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.45f);

            ParticleSystem.EmissionModule emission = steamIconParticles.emission;
            emission.rateOverTime = 22f * t * steamIconParticleMult;
        }

        if (steamIconSparks != null)
        {
            ParticleSystem.MainModule sparkMain = steamIconSparks.main;
            sparkMain.startColor = new Color(themeColor.r * 1.4f, themeColor.g * 1.4f, themeColor.b * 1.4f, 0.9f);

            ParticleSystem.EmissionModule sparkEmission = steamIconSparks.emission;
            sparkEmission.rateOverTime = steamIconSparksOn ? (8f + (followSpeed * 40f)) * t * steamIconParticleMult : 0f;
        }

        if (steamIconTrail != null)
            steamIconTrail.emitting = steamIconSparksOn && t >= 1f;

        UpdateSteamIconShockwave();
    }

    private static Color CurrentSteamIconColor()
    {
        switch (steamIconColorTheme)
        {
            case 1: return Color.HSVToRGB(Mathf.Repeat(Time.time * 0.15f, 1f), 0.65f, 1.2f);
            case 2: return new Color(0.2f, 1.2f, 0.5f);
            case 3: return new Color(1.3f, 0.25f, 0.3f);
            case 4: return new Color(1.3f, 0.95f, 0.3f);
            case 5: return new Color(1.2f, 0.35f, 1.1f);
            default: return new Color(0.45f, 0.75f, 1.1f);
        }
    }

    private static float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        return (Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f))) + 1f;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float SafeScale(float scale)
    {
        return Mathf.Abs(scale) < 0.0001f ? 1f : scale;
    }

    private static void SetSteamIconEmission(float intensity, Material[] materials, Color glow)
    {
        if (materials == null) return;

        Color emission = glow * intensity;

        foreach (Material material in materials)
        {
            if (material != null)
                material.SetColor("_EmissionColor", emission);
        }
    }

    private static void FixSteamIconMaterial(GameObject iconObject)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Standard");

        if (shader == null)
            return;

        List<Material> materials = new List<Material>();

        foreach (Renderer renderer in iconObject.GetComponentsInChildren<Renderer>(true))
        {
            Material source = renderer.sharedMaterial;
            if (source == null) continue;

            Material replacement = new Material(shader);

            if (source.mainTexture != null)
            {
                replacement.mainTexture = source.mainTexture;
                replacement.SetTexture("_BaseMap", source.mainTexture);
            }

            replacement.color = Color.white;
            replacement.SetColor("_BaseColor", Color.white);
            replacement.EnableKeyword("_EMISSION");
            replacement.SetColor("_EmissionColor", CurrentSteamIconColor());
            replacement.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            renderer.sharedMaterial = replacement;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            materials.Add(replacement);
        }

        steamIconMaterials = materials.ToArray();
    }

    private static void CreateSteamIconFX(GameObject iconObject)
    {
        GameObject lightObject = new GameObject("SteamIconLight");
        lightObject.transform.SetParent(iconObject.transform, false);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.45f, 0.7f, 1f);
        light.range = 2.4f;
        light.intensity = 0f;
        light.shadows = LightShadows.None;

        steamIconLight = light;

        GameObject particleObject = new GameObject("SteamIconSteam");
        particleObject.transform.SetParent(iconObject.transform, false);

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startColor = new Color(0.55f, 0.8f, 1f, 0.45f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.14f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));

        ParticleSystem.ColorOverLifetimeModule colorOverLife = particles.colorOverLifetime;
        colorOverLife.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();

        Shader particleShader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Particles/Lit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            ?? Shader.Find("Sprites/Default");

        if (particleShader != null)
        {
            Material particleMaterial = new Material(particleShader);
            particleMaterial.mainTexture = GetSteamParticleTexture();
            particleRenderer.material = particleMaterial;
        }

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        steamIconParticles = particles;

        GameObject sparkObject = new GameObject("SteamIconSparks");
        sparkObject.transform.SetParent(iconObject.transform, false);

        ParticleSystem sparks = sparkObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule sparkMainModule = sparks.main;
        sparkMainModule.loop = true;
        sparkMainModule.playOnAwake = true;
        sparkMainModule.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        sparkMainModule.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        sparkMainModule.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
        sparkMainModule.maxParticles = 120;
        sparkMainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        sparkMainModule.gravityModifier = 0.25f;
        sparkMainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule sparkEmission = sparks.emission;
        sparkEmission.rateOverTime = 0f;

        ParticleSystem.ShapeModule sparkShape = sparks.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 0.1f;

        ParticleSystem.SizeOverLifetimeModule sparkSize = sparks.sizeOverLifetime;
        sparkSize.enabled = true;
        sparkSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        ParticleSystem.ColorOverLifetimeModule sparkColorOverLife = sparks.colorOverLifetime;
        sparkColorOverLife.enabled = true;
        sparkColorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer sparkRenderer = sparkObject.GetComponent<ParticleSystemRenderer>();

        if (particleShader != null)
        {
            Material sparkMaterial = new Material(particleShader);
            sparkMaterial.mainTexture = GetSteamParticleTexture();
            sparkRenderer.material = sparkMaterial;
        }

        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;

        steamIconSparks = sparks;

        TrailRenderer trail = iconObject.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.minVertexDistance = 0.025f;
        trail.widthCurve = AnimationCurve.EaseInOut(0f, 0.14f, 1f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        if (particleShader != null)
        {
            Material trailMaterial = new Material(particleShader);
            trailMaterial.mainTexture = GetSteamParticleTexture();
            trail.material = trailMaterial;
        }

        ApplySteamIconTrailColor(trail, CurrentSteamIconColor());

        steamIconTrail = trail;
    }

    private static void ApplySteamIconTrailColor(TrailRenderer trail, Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0f, 1f) });

        trail.colorGradient = gradient;
    }

    private static void CreateSteamIconShockwave(Vector3 position)
    {
        if (steamIconShockwave == null)
        {
            steamIconShockwave = GameObject.CreatePrimitive(PrimitiveType.Quad);

            Collider collider = steamIconShockwave.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");

            if (shader != null)
            {
                steamIconShockwaveMat = new Material(shader);
                steamIconShockwaveMat.mainTexture = GetShockwaveTexture();
                steamIconShockwave.GetComponent<Renderer>().sharedMaterial = steamIconShockwaveMat;
            }

            steamIconShockwave.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            steamIconShockwave.GetComponent<Renderer>().receiveShadows = false;
        }

        steamIconShockwavePos = position;
        steamIconShockwave.transform.position = position;
        steamIconShockwave.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        steamIconShockwave.transform.localScale = Vector3.one * 0.3f;
        steamIconShockwaveT = 0f;
    }

    private static void UpdateSteamIconShockwave()
    {
        if (steamIconShockwave == null || steamIconShockwaveT >= 0.6f)
            return;

        steamIconShockwaveT += Time.deltaTime;

        float p = Mathf.Clamp01(steamIconShockwaveT / 0.6f);
        float alpha = (1f - p) * (1f - p) * 0.8f;

        steamIconShockwave.transform.localScale = Vector3.one * (0.3f + (p * 2.4f));

        if (steamIconShockwaveMat != null)
        {
            Color color = CurrentSteamIconColor();
            steamIconShockwaveMat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));
            steamIconShockwaveMat.color = new Color(color.r, color.g, color.b, alpha);
        }

        if (steamIconShockwaveT >= 0.6f)
        {
            UnityEngine.Object.Destroy(steamIconShockwave);
            steamIconShockwave = null;
            steamIconShockwaveMat = null;
        }
    }

    private static Texture2D GetShockwaveTexture()
    {
        if (shockwaveTexture != null)
            return shockwaveTexture;

        const int size = 128;
        shockwaveTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        shockwaveTexture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))) / center;
                float band = Mathf.Clamp01(1f - (Mathf.Abs(distance - 0.75f) / 0.25f));
                float alpha = band * band;

                shockwaveTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        shockwaveTexture.Apply();

        return shockwaveTexture;
    }

    private static Texture2D GetSteamParticleTexture()
    {
        if (steamParticleTexture != null)
            return steamParticleTexture;

        const int size = 64;
        steamParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        steamParticleTexture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;

                steamParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        steamParticleTexture.Apply();

        return steamParticleTexture;
    }

    public static void destroySteamIcon()
    {
        if (steamIconId < 0) return;

        int id = steamIconId;
        Material[] materials = steamIconMaterials;
        Light light = steamIconLight;
        ParticleSystem particles = steamIconParticles;
        ParticleSystem sparks = steamIconSparks;
        TrailRenderer trail = steamIconTrail;

        steamIconId = -1;
        steamIconOnHand = false;
        steamIconMaterials = null;
        steamIconLight = null;
        steamIconParticles = null;
        steamIconSparks = null;
        steamIconTrail = null;

        if (steamIconShockwave != null)
        {
            UnityEngine.Object.Destroy(steamIconShockwave);
            steamIconShockwave = null;
            steamIconShockwaveMat = null;
        }

        if (Console.Console.consoleAssets.TryGetValue(id, out Console.Console.ConsoleAsset asset) && asset.assetObject != null && GorillaTagger.Instance != null)
        {
            asset.assetObject.transform.SetParent(null, true);

            if (particles != null)
            {
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = 0f;
            }

            if (sparks != null)
            {
                ParticleSystem.EmissionModule sparkEmission = sparks.emission;
                sparkEmission.rateOverTime = 0f;
            }

            if (trail != null)
                trail.emitting = false;

            GorillaTagger.Instance.StartCoroutine(DespawnSteamIconRoutine(asset.assetObject, id, materials, light));
        }
        else
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);
            RPCProtection();
        }
    }

    private static IEnumerator DespawnSteamIconRoutine(GameObject icon, int id, Material[] materials, Light light)
    {
        float elapsed = 0f;
        const float duration = 0.35f;
        Vector3 startScale = icon.transform.localScale;
        Color despawnColor = CurrentSteamIconColor();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float glow = 1f - t;

            icon.transform.localScale = startScale * (1f - (t * t));
            icon.transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.World);

            SetSteamIconEmission(glow, materials, despawnColor);

            if (light != null)
                light.intensity = 2f * glow;

            yield return null;
        }

        SetSteamIconEmission(0f, materials, despawnColor);

        if (light != null)
            light.intensity = 0f;

        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);
        RPCProtection();

        if (icon != null)
            UnityEngine.Object.Destroy(icon);
    }
    private const  float MouseSensitivity = 0.08f;
    public static  bool  HasGhostMonked;
    private static bool  prevRightPrimary;

    public static Mods instance;

    private static readonly WaitForFixedUpdate waitForFixedUpdate = new();
    private static readonly WaitForSeconds     beeDelay           = new(0.3f);

    private static readonly Vector3 sphereScaleHand     = new(0.1f, 0.1f, 0.1f);
    private static readonly Vector3 sphereScaleHead     = new(0.2f, 0.2f, 0.2f);
    private static readonly Vector3 platScale           = new(0.03f, 0.3f, 0.45f);
    private static readonly Vector3 boxEspScale         = new(0.2f, 0.4f, 0.2f);
    private static readonly Vector3 upOffset02          = new(0, 0.2f, 0);
    private static readonly Vector3 upOffset07          = new(0, 0.7f, 0);
    private static readonly Vector3 upOffset08          = new(0, 0.8f, 0);
    private static readonly Vector3 cherryBombPosOffset = new(0f, 9.5f, 0f);
    private static readonly Vector3 upOffset2           = new(0, 2, 0);
    private static readonly Vector3 upOffset09          = new(0f, 0.9f, 0f);
    private static readonly Vector3 scale03             = new(0.3f, 0.3f, 0.3f);

    public static bool HasCreated;

    public static bool       IsLeftPlat;
    public static bool       IsRightPlat;
    public static GameObject LeftPlat;
    public static GameObject RightPlat;

    public static GameObject LeftS;
    public static GameObject RightS;
    public static GameObject HeadS;

    public static           bool                           DisableMovement;
    private static readonly Dictionary<Transform, Vector3> LastHandPositions = new();
    private static          bool                           hasTouchedWithHand;
    private static          bool                           isJumping;

    private static readonly float                  jumpCooldownTime = 0f;
    private static          SphereCollider         _probeCollider;
    private static readonly Dictionary<bool, bool> previousTouchingGround = new();
    private static          GameObject             lineObject;
    private static          LineRenderer           line;
    private static          bool                   isGrabbing;
    private static          float                  initialHandAngle;
    private static          float                  initialPlayerAngle;

    public static  bool           noclipBool;
    private static MeshCollider[] cachedMeshColliders;

    public static float muteDelay;

    public static bool HasShot;

    public static bool HeldTriggerGetPID;

    public static int  assetId;
    public static bool hastwerked;

    private static int  allocatedSwordId    = -1;
    private static int  allocatedSwordVidId = -1;
    private static bool HasSpawnedSword;
    private static bool HasPlayed;

    private static int  allocatedTravisId;
    public static  bool HasTravisTravised;

    public static  int    phoneid;
    public static  bool   HasCreatedPhone;
    public static  string Video = "";
    private static float  stdell;
    private static float  adminEventDelay;
    private static VRRig  thestrangled;
    private static VRRig  thestrangledleft;
    public static  float  sizeScale = 1f;

    private static int  KormakurId;
    private static bool HasSignSigned;

    private static int  Axeid;
    public static  bool HasAxeAxed;

    private static int     TvID;
    private static int     sofaAssetId;
    public static  bool    Hastvtved;
    private static int     PlayerId;
    private static bool    HasSpawnedVideoPlayer;
    private static bool    isAdjustingScale;
    private static bool    primaryButtonWasPressed;
    private static float   currentForwardOffset = 2f;
    private static Vector3 currentScale         = scale03;

    private static Vector3    savedSpawnPosition = Vector3.zero;
    private static Quaternion savedSpawnRotation = Quaternion.identity;
    private static bool       hasSavedPosition;
    private static string     lastVideoUrl = string.Empty;

    private static readonly Vector3 gravityForce = new(0, -8f, 0);

    public static readonly string[] ignoreLayers =
    {
            "Gorilla Trigger", "Gorilla Boundary", "GorillaHand", "GorillaObject", "Zone", "Water", "GorillaCosmetics",
            "GorillaParticle",
    };

    public static LineRenderer webLineLeft;
    public static LineRenderer webLineRight;

    private static bool leftActive;
    private static bool rightActive;
    private static bool leftLocked;
    private static bool rightLocked;

    private static Vector3 leftAnchor;
    private static Vector3 rightAnchor;
    private static float   leftLength;
    private static float   rightLength;

    private static Vector3 lastLeftPos;
    private static Vector3 lastRightPos;
    private static Vector3 leftHandVel;
    private static Vector3 rightHandVel;
    private static int     cachedIgnoreMask = -1;
    private static bool    HasPressed;
    private static bool    HasPressed2;

    public static  float   startX = -1f;
    public static  float   startY = -1f;
    public static  float   subThingy;
    public static  float   subThingyZ;
    public static  Vector3 lastPosition = Vector3.zero;
    private static Vector3 lastLeftHandPos;
    private static Vector3 lastRightHandPos;

    private static readonly List<RigFrame> recordedFrames = new();

    private static bool isRecording;
    private static bool isPlayingBack;

    private static int playbackIndex;

    private static bool  prevAButton;
    private static float soundSpamDelayC;
    private static float soundSpamDelay;
    private static bool  lastlhboop;
    private static bool  lastrhboop;

    public static GameObject recBodyRotary;

    private static GameObject BodyCollider;
    private static GameObject LeftHandCollider;
    private static GameObject RightHandCollider;
    private static GameObject HeadCollider;

    private static bool IsHoldingRig;
    private static bool isRagdollActive;
    private static bool wasButtonHeldLastFrame;

    private static Transform currentGrabbingHand;

    public static readonly int  TransparentFX    = LayerMask.NameToLayer("TransparentFX");
    public static readonly int  IgnoreRaycast    = LayerMask.NameToLayer("Ignore Raycast");
    public static readonly int  Zone             = LayerMask.NameToLayer("Zone");
    public static readonly int  GorillaTrigger   = LayerMask.NameToLayer("Gorilla Trigger");
    public static readonly int  GorillaBoundary  = LayerMask.NameToLayer("Gorilla Boundary");
    public static readonly int  GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
    public static readonly int  GorillaParticle  = LayerMask.NameToLayer("GorillaParticle");
    private static         bool lastLaserState;

    public static bool HasRemovedThisFrame;

    private static float reportTagDelay;

    private static float lastTagAllTime;

    private static float delaybetweenscore;

    private static float lastVol;
    private static float startSilenceTime = -1f;
    private static bool  reloaded;

    private static readonly Dictionary<int, ESPSkeletonData> ESPSkeletons = new();
    private static          Material                         skeletonEspMaterial;
    private static          float                            lastSkeletonCleanupTime;
    private static readonly List<int>                        removeSkeletonListBuffer = new();

    private static readonly Dictionary<int, ESPBoxData> ESPBoxes = new();
    private static          Material                    espMaterial;
    private static          float                       lastCleanupTime;
    private static readonly List<int>                   removeListBuffer = new();

    private static bool HasInvised;
    private static bool prevRightPrimaryInvis;

    private static bool  previousBraceletSpamState;
    private static float braceletSpamDelay;

    private static int   cherryBombId = -1;
    private static bool  hasSpawnedCherry;
    private static bool  cherryAnimationPlayed;
    private static float cherrySpawnTime = -1f;

    private static readonly Dictionary<int, NametagData> ActiveNametags = new();
    private static          float                        lastNametagCleanupTime;
    private static readonly List<int>                    nametagCleanupBuffer = new();

    private void Awake()
    {
        instance = this;
        InitializeLayerMasks();
    }

    public static void SpeedBoost()
    {
        GTPlayer.Instance.maxJumpSpeed   = 8f;
        GTPlayer.Instance.jumpMultiplier = 5.3f;
    }

    public static void CreatePlayerOutline()
    {
        if (VRRig.LocalRig == null) return;

        if (!VRRig.LocalRig.enabled)
        {
            if (!HasCreated)
            {
                Shader uberShader = Shader.Find("GorillaTag/UberShader");
                Color themeColor = ModConfig.Theme;

                // left hand
                LeftS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                LeftS.transform.parent = GTPlayer.Instance.LeftHand.handFollower.transform;
                LeftS.transform.localPosition = Vector3.zero;
                LeftS.transform.localRotation = Quaternion.identity;
                LeftS.transform.localScale = sphereScaleHand;

                Renderer? rendL = LeftS.GetComponent<Renderer>();
                rendL.material.shader = uberShader;
                if (!ModConfig.instance.IsMenuRGB.Value)
                    rendL.material.color = themeColor;
                else
                    Main.instance.StartCoroutine(Main.instance.RGBTheme(rendL));

                Destroy(LeftS.GetComponent<Rigidbody>());
                Destroy(LeftS.GetComponent<Collider>());

                // right hand
                RightS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                RightS.transform.parent = GTPlayer.Instance.RightHand.handFollower.transform;
                RightS.transform.localPosition = Vector3.zero;
                RightS.transform.localRotation = Quaternion.identity;
                RightS.transform.localScale = sphereScaleHand;

                Renderer? rendR = RightS.GetComponent<Renderer>();
                rendR.material.shader = uberShader;
                if (!ModConfig.instance.IsMenuRGB.Value)
                    rendR.material.color = themeColor;
                else
                    Main.instance.StartCoroutine(Main.instance.RGBTheme(rendR));

                Destroy(RightS.GetComponent<Rigidbody>());
                Destroy(RightS.GetComponent<Collider>());

                // head
                HeadS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                HeadS.transform.parent = GTPlayer.Instance.headCollider.transform;
                HeadS.transform.localPosition = Vector3.zero;
                HeadS.transform.localRotation = Quaternion.identity;
                HeadS.transform.localScale = sphereScaleHead;

                Renderer? rendH = HeadS.GetComponent<Renderer>();
                rendH.material.shader = uberShader;
                if (!ModConfig.instance.IsMenuRGB.Value)
                    rendH.material.color = themeColor;
                else
                    Main.instance.StartCoroutine(Main.instance.RGBTheme(rendH));

                Destroy(HeadS.GetComponent<Rigidbody>());
                Destroy(HeadS.GetComponent<Collider>());
                HasCreated = true;
            }
        }
        else
        {
            if (LeftS != null) Destroy(LeftS);
            if (RightS != null) Destroy(RightS);
            if (HeadS != null) Destroy(HeadS);
            HasCreated = false;
        }
    }

    public static void Fly()
    {
        if (ControllerInputPoller.instance.rightControllerPrimaryButton)
        {
            GTPlayer.Instance.transform.position +=
                    GTPlayer.Instance.headCollider.transform.forward * ModConfig.instance.FlySpeedSave.Value;

            Rigidbody? rb                     = GTPlayer.Instance.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }

    public static void WasdFly()
    {
        Rigidbody rigidbody = GorillaTagger.Instance.rigidbody;
        Transform body      = rigidbody.transform;
        Transform head      = GorillaTagger.Instance.headCollider.transform;

        Transform leftHand  = GorillaTagger.Instance.leftHandTransform;
        Transform rightHand = GorillaTagger.Instance.rightHandTransform;

        leftHand.localPosition  += Vector3.down * 0.5f;
        rightHand.localPosition += Vector3.down * 0.5f;

        leftHand.localPosition  -= Vector3.right * 0.2f;
        rightHand.localPosition -= Vector3.left  * 0.2f;

        leftHand.localRotation  = Quaternion.Euler(40f, 0f, 0f);
        rightHand.localRotation = Quaternion.Euler(40f, 0f, 0f);

        if (UnityInput.Current.GetKey(KeyCode.Q))
        {
            leftHand.localPosition += Vector3.forward * 0.2f;
            leftHand.localPosition += Vector3.up      * 0.4f;
        }

        if (UnityInput.Current.GetKey(KeyCode.E))
        {
            rightHand.localPosition += Vector3.forward * 0.2f;
            rightHand.localPosition += Vector3.up      * 0.4f;
        }

        Camera     thirdPersonCam = null;
        GameObject cameraObj      = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");

        if (cameraObj != null)
            thirdPersonCam = cameraObj.GetComponent<Camera>();

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            body.Rotate(Vector3.up, mouseDelta.x     * MouseSensitivity, Space.World);
            head.Rotate(Vector3.right, -mouseDelta.y * MouseSensitivity, Space.Self);

            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            Camera raycastCamera = Camera.main;

            if (thirdPersonCam != null)
                raycastCamera = thirdPersonCam;

            if (raycastCamera != null)
            {
                Ray ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

                if (Physics.Raycast(
                            ray,
                            out RaycastHit hit,
                            100f,
                            GTPlayer.Instance.locomotionEnabledLayers,
                            QueryTriggerInteraction.Ignore))
                    rightHand.position = hit.point;
            }
        }

        Vector3 movementDirection = Vector3.zero;

        Vector3 forwardDirection = head.forward;
        Vector3 rightDirection   = head.right;

        if (UnityInput.Current.GetKey(KeyCode.W))
            movementDirection += forwardDirection;

        if (UnityInput.Current.GetKey(KeyCode.S))
            movementDirection -= forwardDirection;

        if (UnityInput.Current.GetKey(KeyCode.A))
            movementDirection -= rightDirection;

        if (UnityInput.Current.GetKey(KeyCode.D))
            movementDirection += rightDirection;

        if (UnityInput.Current.GetKey(KeyCode.Space))
            movementDirection += head.up;

        float speed = UnityInput.Current.GetKey(KeyCode.LeftShift) ? 40f : 10f;

        if (!DisableMovement && movementDirection != Vector3.zero)
            body.position += movementDirection.normalized * (Time.deltaTime * speed);

        rigidbody.velocity        = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        ResolveHandCollision(leftHand);
        ResolveHandCollision(rightHand);
    }

    private static void ResolveHandCollision(Transform hand)
    {
        const float handRadius = 0.075f;
        const float skinWidth  = 0.0025f;

        bool shouldIgnore = Vector3.Distance(hand.position, GorillaTagger.Instance.rigidbody.transform.position) > 2f;

        if (shouldIgnore)
        {
            LastHandPositions[hand] = hand.position;

            return;
        }

        if (!LastHandPositions.TryGetValue(hand, out Vector3 previousPosition))
            previousPosition = hand.position;

        Vector3 currentPosition = hand.position;
        Vector3 moveDelta       = currentPosition - previousPosition;

        float distance = moveDelta.magnitude;

        bool processingJumpRelease = isJumping && Time.time < jumpCooldownTime;

        if (distance > 0.0001f)
        {
            Vector3 direction = moveDelta.normalized;

            if (Physics.SphereCast(
                        previousPosition,
                        handRadius,
                        direction,
                        out RaycastHit hit,
                        distance + skinWidth,
                        GTPlayer.Instance.locomotionEnabledLayers,
                        QueryTriggerInteraction.Ignore))
            {
                if (!processingJumpRelease)
                {
                    isJumping          = false;
                    hasTouchedWithHand = true;
                }

                hand.position = hit.point + hit.normal * (handRadius + skinWidth);
                Vector3 remainingMovement = currentPosition - hand.position;
                Vector3 surfaceSlide      = Vector3.ProjectOnPlane(remainingMovement, hit.normal);

                if (!Physics.SphereCast(
                            hand.position,
                            handRadius,
                            surfaceSlide.normalized,
                            out RaycastHit slideHit,
                            surfaceSlide.magnitude,
                            GTPlayer.Instance.locomotionEnabledLayers,
                            QueryTriggerInteraction.Ignore))
                    hand.position += surfaceSlide;
            }
        }

        Collider[] overlaps = Physics.OverlapSphere(
                hand.position,
                handRadius,
                GTPlayer.Instance.locomotionEnabledLayers,
                QueryTriggerInteraction.Ignore);

        if (overlaps.Length > 0 && !processingJumpRelease)
        {
            isJumping          = false;
            hasTouchedWithHand = true;
        }

        foreach (Collider col in overlaps)
            if (Physics.ComputePenetration(
                        GetHandProbe(handRadius, hand.position),
                        hand.position,
                        Quaternion.identity,
                        col,
                        col.transform.position,
                        col.transform.rotation,
                        out Vector3 direction,
                        out float penetration))
                hand.position += direction * (penetration + skinWidth);

        LastHandPositions[hand] = hand.position;
    }

    private static SphereCollider GetHandProbe(float radius, Vector3 position)
    {
        if (_probeCollider == null)
        {
            GameObject obj = new("HandCollisionProbe");
            obj.hideFlags = HideFlags.HideAndDontSave;

            _probeCollider           = obj.AddComponent<SphereCollider>();
            _probeCollider.isTrigger = true;
        }

        _probeCollider.radius             = radius;
        _probeCollider.transform.position = position;

        return _probeCollider;
    }

    public static void LongArms()
    {
        if (VRRig.LocalRig != null)
            GTPlayer.Instance.transform.localScale = Vector3.one * (VRRig.LocalRig.NativeScale * 1.15f);
    }

    public static void UnLongArms()
    {
        if (VRRig.LocalRig != null)
            GTPlayer.Instance.transform.localScale = Vector3.one * VRRig.LocalRig.NativeScale;
    }

    public static void GhostMonke()
    {
        bool current = ControllerInputPoller.instance.rightControllerPrimaryButton;

        if (current && !prevRightPrimary)
        {
            HasGhostMonked                              = !HasGhostMonked;
            GorillaTagger.Instance.offlineVRRig.enabled = !HasGhostMonked;
        }

        prevRightPrimary = current;
    }

    public static void GhostWalk(bool left)
    {
        GTPlayer? playerInstance = GTPlayer.Instance;
        VRRig?    vrrig          = GorillaTagger.Instance.offlineVRRig;

        if (vrrig == null) return;

        bool touchingGround = playerInstance.IsHandTouching(left);

        if (!previousTouchingGround.ContainsKey(left))
            previousTouchingGround[left] = false;

        bool wasTouchingGround = previousTouchingGround[left];

        if (touchingGround && !wasTouchingGround)
            GorillaTagger.Instance.StartCoroutine(GhostWalkh(vrrig));

        previousTouchingGround[left] = touchingGround;
    }

    private static IEnumerator GhostWalkh(VRRig vrrig)
    {
        vrrig.enabled = true;

        yield return new WaitForEndOfFrame();

        vrrig.enabled = false;
    }

    public static void Platforms()
    {
        Color                  platcolor  = ModConfig.Theme;
        ControllerInputPoller? input      = ControllerInputPoller.instance;
        Shader                 uberShader = Shader.Find("GorillaTag/UberShader");

        bool isRGB   = ModConfig.instance.IsMenuRGB.Value;
        bool isInvis = ModConfig.instance.IsInvisPlat.Value;

        if (input.leftGrab && !IsLeftPlat)
        {
            IsLeftPlat = true;

            LeftPlat                      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            LeftPlat.transform.position   = GTPlayer.Instance.LeftHand.controllerTransform.position;
            LeftPlat.transform.rotation   = GTPlayer.Instance.LeftHand.controllerTransform.rotation;
            LeftPlat.transform.localScale = platScale;

            Rigidbody rb = LeftPlat.AddComponent<Rigidbody>();
            rb.useGravity  = false;
            rb.isKinematic = true;

            if (!isInvis)
            {
                Renderer? rend = LeftPlat.GetComponent<Renderer>();
                rend.material = new Material(uberShader);

                if (!isRGB)
                    rend.material.color = platcolor;
                else
                    Main.instance.StartCoroutine(Main.instance.RGBTheme(rend));
            }
            else
            {
                LeftPlat.GetComponent<Renderer>().enabled = false;
            }
        }

        if (input.rightGrab && !IsRightPlat)
        {
            IsRightPlat = true;

            RightPlat                      = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RightPlat.transform.position   = GTPlayer.Instance.RightHand.controllerTransform.position;
            RightPlat.transform.rotation   = GTPlayer.Instance.RightHand.controllerTransform.rotation;
            RightPlat.transform.localScale = platScale;

            Rigidbody rb = RightPlat.AddComponent<Rigidbody>();
            rb.useGravity  = false;
            rb.isKinematic = true;

            if (!isInvis)
            {
                Renderer? rend = RightPlat.GetComponent<Renderer>();
                rend.material = new Material(uberShader);

                if (!isRGB)
                    rend.material.color = platcolor;
                else
                    Main.instance.StartCoroutine(Main.instance.RGBTheme(rend));
            }
            else
            {
                RightPlat.GetComponent<Renderer>().enabled = false;
            }
        }

        if (!input.leftGrab && IsLeftPlat)
        {
            IsLeftPlat = false;

            if (LeftPlat != null)
            {
                Destroy(LeftPlat.GetComponent<Collider>());

                Rigidbody rb = LeftPlat.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity  = true;

                GameObject platToDelete = LeftPlat;
                Destroy(platToDelete, 5f);

                LeftPlat = null;
            }
        }

        if (!input.rightGrab && IsRightPlat)
        {
            IsRightPlat = false;

            if (RightPlat != null)
            {
                Destroy(RightPlat.GetComponent<Collider>());

                Rigidbody rb = RightPlat.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity  = true;

                GameObject platToDelete = RightPlat;
                Destroy(platToDelete, 5f);

                RightPlat = null;
            }
        }
    }

    public static void JoystickFly()
    {
        GorillaTagger? tagger = GorillaTagger.Instance;
        Rigidbody?     rb     = tagger.rigidbody;
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(-Physics.gravity, ForceMode.Acceleration);

        if (ModConfig.instance.IsJoystickNavigation.Value && Main.instance.isMenuCreated)
            return;

        Vector2 joyl            = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
        Vector2 joyr            = ControllerInputPoller.instance.rightControllerPrimary2DAxis;
        float   speedMultiplier = Time.deltaTime * ModConfig.instance.FlySpeedSave.Value * 15f;

        if (joyl.magnitude > 0.6f)
        {
            Transform bodyTransform = tagger.bodyCollider.transform;
            GTPlayer.Instance.transform.position += bodyTransform.forward * (joyl.y * speedMultiplier) +
                                                    bodyTransform.right   * (joyl.x * speedMultiplier);
        }

        if (joyr.magnitude > 0.6f)
            GTPlayer.Instance.transform.position += tagger.bodyCollider.transform.up * (joyr.y * speedMultiplier);
    }

    public static void HandTurn()
    {
        if (ControllerInputPoller.instance.rightGrab)
        {
            float     downwardAngle   = 50f;
            Transform handTransform   = GTPlayer.Instance.RightHand.controllerTransform;
            Transform playerTransform = GTPlayer.Instance.transform;

            Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, handTransform.right) * handTransform.forward;

            if (lineObject == null)
            {
                lineObject         = new GameObject("HandTurn_LineIndicator");
                line               = lineObject.AddComponent<LineRenderer>();
                line.startWidth    = 0.02f;
                line.endWidth      = 0.02f;
                line.positionCount = 2;
                line.useWorldSpace = true;

                Material lineMat = new(Shader.Find("Sprites/Default"));
                line.material   = lineMat;
                line.startColor = Color.red;
                line.endColor   = Color.red;
            }

            line.enabled = true;
            line.SetPosition(0, handTransform.position);

            RaycastHit hit;
            if (Physics.Raycast(handTransform.position, rayDirection, out hit, 100f))
            {
                line.SetPosition(1, hit.point);

                Vector3 directionToHit = hit.point - playerTransform.position;
                directionToHit.y = 0f;

                float currentHandAngle = Mathf.Atan2(directionToHit.x, directionToHit.z) * Mathf.Rad2Deg;

                if (!isGrabbing)
                {
                    isGrabbing         = true;
                    initialHandAngle   = currentHandAngle;
                    initialPlayerAngle = playerTransform.eulerAngles.y;
                }

                float angleDelta        = currentHandAngle   - initialHandAngle;
                float targetPlayerAngle = initialPlayerAngle + angleDelta;

                Quaternion targetRotation = Quaternion.Euler(0f, targetPlayerAngle, 0f);
                playerTransform.rotation =
                        Quaternion.Slerp(playerTransform.rotation, targetRotation, 15f * Time.deltaTime);
            }
            else
            {
                line.SetPosition(1, handTransform.position + rayDirection * 100f);
                isGrabbing = false;
            }
        }
        else
        {
            isGrabbing = false;
            if (line != null)
                line.enabled = false;
        }
    }

    public static void Noclip()
    {
        if (ControllerInputPoller.instance.rightControllerSecondaryButton)
        {
            if (!noclipBool)
            {
                noclipBool          = true;
                cachedMeshColliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
                for (int i = 0; i < cachedMeshColliders.Length; i++)
                    if (cachedMeshColliders[i] != null)
                        cachedMeshColliders[i].enabled = false;
            }
        }
        else
        {
            if (noclipBool)
            {
                noclipBool = false;
                if (cachedMeshColliders != null)
                    for (int i = 0; i < cachedMeshColliders.Length; i++)
                        if (cachedMeshColliders[i] != null)
                            cachedMeshColliders[i].enabled = true;
            }
        }
    }
    public enum CopyMode
    {
        Normal,
        Left,
        Right,
        Up
    }

    public static bool isFollowing = false;
    private static bool lastTriggerState = false;
    private static VRRig cachedTargetRig = null;

    private static CopyMode currentMode = CopyMode.Normal;
    private static float lastModeSwitchTime = 0f;

    public static void CopyRigGun()
    {
        GunLib.LetGun();

        if (GunLib.Triggering && !lastTriggerState)
        {
            isFollowing = !isFollowing;

            if (isFollowing)
            {
                cachedTargetRig = GunLib.LockedRig;
            }
            else
            {
                cachedTargetRig = null;
                VRRig.LocalRig.enabled = true;
            }
        }

        lastTriggerState = GunLib.Triggering;

        if (isFollowing && cachedTargetRig != null)
        {
            if (cachedTargetRig.rightMiddle.calcT > 0.4f && (UnityEngine.Time.time - lastModeSwitchTime) > 0.2f)
            {
                currentMode = (CopyMode)(((int)currentMode + 1) % System.Enum.GetNames(typeof(CopyMode)).Length);
                lastModeSwitchTime = UnityEngine.Time.time;
            }

            if (VRRig.LocalRig.enabled)
            {
                VRRig.LocalRig.enabled = false;
            }

            Vector3 localOffset = Vector3.zero;
            if (currentMode == CopyMode.Left) localOffset = Vector3.left;
            else if (currentMode == CopyMode.Right) localOffset = Vector3.right;
            else if (currentMode == CopyMode.Up) localOffset = Vector3.up;

            Vector3 worldOffset = cachedTargetRig.transform.rotation * localOffset;

            VRRig.LocalRig.transform.position = cachedTargetRig.syncPos + worldOffset;
            VRRig.LocalRig.transform.rotation = cachedTargetRig.syncRotation;

            VRRig.LocalRig.leftHand.rigTarget.transform.position = cachedTargetRig.leftHand.rigTarget.transform.position + worldOffset;
            VRRig.LocalRig.rightHand.rigTarget.transform.position = cachedTargetRig.rightHand.rigTarget.transform.position + worldOffset;

            VRRig.LocalRig.leftHand.rigTarget.transform.rotation = cachedTargetRig.leftHand.rigTarget.rotation;
            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = cachedTargetRig.rightHand.rigTarget.rotation;

            VRRig.LocalRig.head.rigTarget.transform.rotation = cachedTargetRig.head.rigTarget.transform.rotation;

            VRRig.LocalRig.leftIndex.calcT = cachedTargetRig.leftIndex.calcT;
            VRRig.LocalRig.leftMiddle.calcT = cachedTargetRig.leftMiddle.calcT;
            VRRig.LocalRig.leftThumb.calcT = cachedTargetRig.leftThumb.calcT;

            VRRig.LocalRig.leftIndex.LerpFinger(1f, false);
            VRRig.LocalRig.leftMiddle.LerpFinger(1f, false);
            VRRig.LocalRig.leftThumb.LerpFinger(1f, false);

            VRRig.LocalRig.rightIndex.calcT = cachedTargetRig.rightIndex.calcT;
            VRRig.LocalRig.rightMiddle.calcT = cachedTargetRig.rightMiddle.calcT;
            VRRig.LocalRig.rightThumb.calcT = cachedTargetRig.rightThumb.calcT;

            VRRig.LocalRig.rightIndex.LerpFinger(1f, false);
            VRRig.LocalRig.rightMiddle.LerpFinger(1f, false);
            VRRig.LocalRig.rightThumb.LerpFinger(1f, false);
        }
        else if (isFollowing && cachedTargetRig == null)
        {
            isFollowing = false;
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void AntiReport()
    {
        if (NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom) return;

        NetPlayer?                         localPlayer = NetworkSystem.Instance.LocalPlayer;
        List<GorillaPlayerScoreboardLine>? lines       = GorillaScoreboardTotalUpdater.allScoreboardLines;
        IReadOnlyList<VRRig>?              rigs        = VRRigCache.ActiveRigs;

        for (int i = 0; i < lines.Count; i++)
        {
            GorillaPlayerScoreboardLine? line = lines[i];

            if (line.linePlayer != localPlayer) continue;
            Vector3 reportBtnPos = line.reportButton.transform.position;

            for (int j = 0; j < rigs.Count; j++)
            {
                VRRig? vrrig = rigs[j];

                if (vrrig == null || vrrig.isLocal || vrrig.isOfflineVRRig) continue;

                if (Vector3.Distance(vrrig.rightHandTransform.position, reportBtnPos) < 0.4f ||
                    Vector3.Distance(vrrig.leftHandTransform.position,  reportBtnPos) < 0.4f)
                {
                    PhotonNetwork.Disconnect();
                    NotiLib.SendNotification(vrrig.Creator.NickName + " Tried to report you!", 2000);

                    return;
                }
            }
        }
    }

    public static void LockOntoRig()
    {
        GunLib.LetGun();
        if (GunLib.Triggering && GunLib.IsOverVrrig && GunLib.GunPos != null)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = GunLib.VrrigTransform.position;
        }
        else if (!GunLib.Triggering || !ControllerInputPoller.instance.rightGrab)
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void RigGun()
    {
        GunLib.LetGun();
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.GunPos != null && GunLib.Triggering)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = GunLib.GunPos.position + upOffset07;
        }

        if (!GunLib.Triggering)
            VRRig.LocalRig.enabled = true;
    }

    public static void FreezeRig()
    {
        if (ControllerInputPoller.instance.rightControllerSecondaryButton)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = GTPlayer.Instance.bodyCollider.transform.position + upOffset02;
        }
        else
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void MuteGun()
    {
        GunLib.LetGun();

        if (!GunLib.IsOverVrrig) return;
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && Time.time > muteDelay)
        {
            NetPlayer? owner = GunLib.LockedRigOwner;
            if (owner != null && !owner.IsLocal)
            {
                List<GorillaPlayerScoreboardLine>? lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
                for (int i = 0; i < lines.Count; i++)
                {
                    GorillaPlayerScoreboardLine? line = lines[i];
                    if (line.linePlayer == owner)
                    {
                        muteDelay            = Time.time + 0.5f;
                        line.muteButton.isOn = !line.muteButton.isOn;
                        line.PressButton(line.muteButton.isOn, GorillaPlayerLineButton.ButtonType.Mute);
                    }
                }
            }
        }
    }

    public static void MuteEveryoneExceptGun()
    {
        GunLib.LetGun();

        if (!GunLib.IsOverVrrig) return;
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && Time.time > muteDelay)
        {
            NetPlayer? target = GunLib.LockedRigOwner;

            if (target == null) return;

            muteDelay = Time.time + 0.5f;
            List<GorillaPlayerScoreboardLine>? lines = GorillaScoreboardTotalUpdater.allScoreboardLines;

            for (int i = 0; i < lines.Count; i++)
            {
                GorillaPlayerScoreboardLine? line = lines[i];

                if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

                if (line.linePlayer == target)
                {
                    if (line.muteButton.isOn)
                    {
                        line.muteButton.isOn = false;
                        line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                    }
                }
                else
                {
                    if (!line.muteButton.isOn)
                    {
                        line.muteButton.isOn = true;
                        line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                    }
                }
            }
        }
    }

    public static void ReportGun()
    {
        GunLib.LetGun();

        if (!GunLib.IsOverVrrig) return;
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && Time.time > muteDelay)
        {
            NetPlayer? owner = GunLib.LockedRigOwner;
            if (owner != null && !owner.IsLocal)
            {
                GorillaPlayerScoreboardLine.ReportPlayer(owner.UserId, GorillaPlayerLineButton.ButtonType.Toxicity,
                        owner.NickName);

                muteDelay = Time.time + 0.2f;
            }
        }
    }

    public static void TPGun()
    {
        GunLib.LetGun();
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && !HasShot)
        {
            GTPlayer.Instance.TeleportTo(GunLib.GunPos);
            Rigidbody? rb                     = GTPlayer.Instance.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            HasShot = true;
        }

        if (!GunLib.Triggering && HasShot)
            HasShot = false;
    }

    public static void HoldRig()
    {
        if (ControllerInputPoller.instance.rightGrab)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = GTPlayer.Instance.RightHand.controllerTransform.position;
            VRRig.LocalRig.transform.rotation = GTPlayer.Instance.RightHand.handFollower.transform.rotation;
        }
        else
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void MuteAll()
    {
        List<GorillaPlayerScoreboardLine>? lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
        for (int i = 0; i < lines.Count; i++)
        {
            GorillaPlayerScoreboardLine? line = lines[i];

            if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

            if (!line.muteButton.isOn)
            {
                line.muteButton.isOn = true;
                line.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
            }
        }
    }

    public static void UnmuteAll()
    {
        List<GorillaPlayerScoreboardLine>? lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
        for (int i = 0; i < lines.Count; i++)
        {
            GorillaPlayerScoreboardLine? line = lines[i];

            if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

            if (line.muteButton.isOn)
            {
                line.muteButton.isOn = false;
                line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
            }
        }
    }

    public static void GetPID()
    {
        GunLib.LetGun();
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;

        if (GunLib.Triggering && GunLib.IsOverVrrig && !HeldTriggerGetPID)
        {
            string userId = GunLib.LockedRigOwner.UserId;
            string nick   = GunLib.LockedRigOwner.NickName;

            string dirPath = Path.Combine(Paths.GameRootPath, "Gemstone", "IDS");
            Directory.CreateDirectory(dirPath);

            File.WriteAllText(Path.Combine(dirPath, nick + ".txt"), "ID: " + userId);
            NotiLib.SendNotification("ID: "                                + userId, 2000);

            HeldTriggerGetPID = true;
        }

        if (!GunLib.Triggering && HeldTriggerGetPID)
            HeldTriggerGetPID = false;
    }

    public static void GetPIDSelf()
    {
        string dirPath = Path.Combine(Paths.GameRootPath, "Gemstone", "IDS");
        Directory.CreateDirectory(dirPath);

        File.WriteAllText(Path.Combine(dirPath, PhotonNetwork.LocalPlayer.NickName + ".txt"),
                "ID: " + VRRig.LocalRig.Creator.UserId);

        NotiLib.SendNotification("ID: " + VRRig.LocalRig.Creator.UserId, 2000);
    }

    public static void UpsideDownNeck() => VRRig.LocalRig.head.trackingRotationOffset.z = 180f;

    public static void silkickgun()
    {
        GunLib.LetGun();
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && GunLib.IsOverVrrig)
            Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, GunLib.LockedRig.Creator.UserId);
    }

    public static void TwerkingCarti()
    {
        if (!hastwerked)
        {
            assetId = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "carti",
                    assetId);

            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, assetId,
                    new Vector3(-76f, 1.7f, -80f));

            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, assetId,
                    Quaternion.Euler(0f, 40f, 0f));

            float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 10f : 5f;
            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, assetId, Vector3.one * scaleFactor);
            hastwerked = true;
        }
    }

    public static void NoCarti()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);
        hastwerked = false;
    }

    public static void Sword()
    {
        if (!HasSpawnedSword)
        {
            if (allocatedSwordId < 0)
            {
                allocatedSwordId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Sword",
                        allocatedSwordId);

                if (ModConfig.instance.IsBigAssets.Value)
                    Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordId,
                            Vector3.one * 5);

                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSwordId, 2);
                Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model",
                        "Unsheath");
            }

            if (allocatedSwordVidId < 0)
            {
                allocatedSwordVidId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer",
                        allocatedSwordVidId);

                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordVidId,
                        new Vector3(0.1f, 0.1f, 0.1f));

                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedSwordVidId,
                        Vector3.zero);

                Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedSwordVidId);
                Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedSwordVidId, "Video",
                        "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/RobloxSword.mp4");
            }

            HasSpawnedSword = true;
        }

        bool trigger = ControllerInputPoller.instance.rightControllerTriggerButton;
        if (trigger && !HasPlayed)
        {
            Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Slash");
            HasPlayed = true;
        }

        if (!trigger && HasPlayed)
            HasPlayed = false;
    }

    public static void NoSword()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordId);
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordVidId);
        allocatedSwordId    = -1;
        allocatedSwordVidId = -1;
        HasSpawnedSword     = false;
    }

    public static void TravisScott()
    {
        if (!HasTravisTravised)
        {
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "travis", "TravisScott",
                    allocatedTravisId);

            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedTravisId,
                    new Vector3(-70f, 2f, -52f));

            float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 3.5f : 0.4f;
            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedTravisId,
                    Vector3.one * scaleFactor);

            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, allocatedTravisId,
                    Quaternion.Euler(0f, 20f, 0f));

            HasTravisTravised = true;
        }
    }

    public static void NoTravis()
    {
        HasTravisTravised = false;
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedTravisId);
    }

    public static void Samsung()
    {
        if (!HasCreatedPhone)
        {
            phoneid = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "samsungphone",
                    phoneid);

            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, phoneid, 1);
            Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, phoneid,
                    new Vector3(-0.075f, 0.1f, 0f));

            Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, phoneid,
                    Quaternion.Euler(80f, 90f, 180f));

            float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 5f : 0.3f;
            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, phoneid, Vector3.one * scaleFactor);

            Console.Console.ExecuteCommand("asset-setvideo",         ReceiverGroup.All, phoneid, "VideoPlayer", Video);
            Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, phoneid);
            HasCreatedPhone = true;
        }
    }

    public static void NoSamsung()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, phoneid);
        HasCreatedPhone = false;
    }

    private static int  casseowatchid     = -1;
    private static bool HasCreatedCasseoWatch;

    public static void CasseoWatch()
    {
        if (!HasCreatedCasseoWatch)
        {
            casseowatchid = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "casseowatch", "CasseoWatch",
                    casseowatchid);

            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, casseowatchid, 1);
            Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, casseowatchid);
            HasCreatedCasseoWatch = true;
        }
    }

    public static void NoCasseoWatch()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, casseowatchid);
        HasCreatedCasseoWatch = false;
    }

    public static void AdminGrabAll()
    {
        if (ControllerInputPoller.instance.rightGrab)
            Console.Console.ExecuteCommand("tp", ReceiverGroup.Others,
                    GTPlayer.Instance.RightHand.controllerTransform.position);
    }

    public static NetPlayer GetPlayerFromVRRig(VRRig p) =>
            p.Creator;

    public static void AdminStrangle()
    {
        ControllerInputPoller? input  = ControllerInputPoller.instance;
        GorillaTagger?         tagger = GorillaTagger.Instance;
        IReadOnlyList<VRRig>?  rigs   = VRRigCache.ActiveRigs;
        if (input.leftGrab)
        {
            if (thestrangledleft == null)
            {
                for (int i = 0; i < rigs.Count; i++)
                {
                    VRRig? rig = rigs[i];

                    if (rig.isLocal) continue;
                    if (Vector3.Distance(rig.headMesh.transform.position, tagger.leftHandTransform.position) >=
                        0.2f) continue;

                    thestrangledleft = rig;
                    if (PhotonNetwork.InRoom)
                        tagger.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                    else
                        VRRig.LocalRig.PlayHandTapLocal(89, true, 999999f);
                }
            }
            else
            {
                if (Time.time > stdell)
                {
                    stdell = Time.time + 0.05f;
                    Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            tagger.leftHandTransform.position);
                }
            }
        }
        else
        {
            if (thestrangledleft != null)
            {
                try
                {
                    Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            tagger.leftHandTransform.position);

                    Console.Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0));
                }
                catch { }

                thestrangledleft = null;
                if (PhotonNetwork.InRoom)
                    tagger.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                else
                    VRRig.LocalRig.PlayHandTapLocal(89, true, 999999f);
            }
        }

        if (input.rightGrab)
        {
            if (thestrangled == null)
            {
                for (int i = 0; i < rigs.Count; i++)
                {
                    VRRig? rig = rigs[i];

                    if (rig.isLocal) continue;
                    if (Vector3.Distance(rig.headMesh.transform.position, tagger.rightHandTransform.position) >=
                        0.2f) continue;

                    thestrangled = rig;
                    if (PhotonNetwork.InRoom)
                        tagger.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                    else
                        VRRig.LocalRig.PlayHandTapLocal(89, false, 999999f);
                }
            }
            else
            {
                if (Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.05f;
                    Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            tagger.rightHandTransform.position);
                }
            }
        }
        else
        {
            if (thestrangled != null)
            {
                try
                {
                    Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            tagger.rightHandTransform.position);

                    Console.Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0));
                }
                catch { }

                thestrangled = null;
                if (PhotonNetwork.InRoom)
                    tagger.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                else
                    VRRig.LocalRig.PlayHandTapLocal(89, false, 999999f);
            }
        }
    }

    public static void SizeChanger()
    {
        if (ModConfig.instance.IsJoystickNavigation.Value && Main.instance.isMenuCreated)
            return;

        float increment = 0.05f;

        if (ControllerInputPoller.instance.leftControllerTriggerButton)
            increment = 0.2f;

        if (ControllerInputPoller.instance.leftGrab)
            increment = 0.01f;

        bool scaleChanged = false;

        if (ControllerInputPoller.instance.rightControllerTriggerButton)
        {
            sizeScale    += increment;
            scaleChanged =  true;
        }

        if (ControllerInputPoller.instance.leftGrab)
        {
            sizeScale    -= increment;
            scaleChanged =  true;
        }

        if (ControllerInputPoller.instance.rightControllerPrimaryButton)
        {
            sizeScale    = 1f;
            scaleChanged = true;
        }

        if (sizeScale < 0.05f)
            sizeScale = 0.05f;

        if (scaleChanged)
        {
            if (VRRig.LocalRig != null)
            {
                VRRig.LocalRig.transform.localScale = Vector3.one * sizeScale;

                FieldInfo vrrigField = typeof(VRRig).GetField("NativeScale",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (vrrigField != null)
                {
                    vrrigField.SetValue(VRRig.LocalRig, sizeScale);
                }
                else
                {
                    PropertyInfo vrrigProp = typeof(VRRig).GetProperty("NativeScale",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    vrrigProp?.SetValue(VRRig.LocalRig, sizeScale, null);
                }
            }

            if (GTPlayer.Instance != null)
            {
                FieldInfo gtField = typeof(GTPlayer).GetField("nativeScale",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (gtField != null)
                {
                    gtField.SetValue(GTPlayer.Instance, sizeScale);
                }
                else
                {
                    PropertyInfo gtProp = typeof(GTPlayer).GetProperty("nativeScale",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                    gtProp?.SetValue(GTPlayer.Instance, sizeScale, null);
                }
            }

            if (PhotonNetwork.InRoom)
                Console.Console.ExecuteCommand("scale", PhotonNetwork.LocalPlayer.ActorNumber, sizeScale);
        }
    }

    public static void DisableSizeChanger()
    {
        sizeScale = 1f;

        if (VRRig.LocalRig != null)
        {
            VRRig.LocalRig.transform.localScale = Vector3.one * sizeScale;

            FieldInfo vrrigField = typeof(VRRig).GetField("NativeScale",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (vrrigField != null)
            {
                vrrigField.SetValue(VRRig.LocalRig, sizeScale);
            }
            else
            {
                PropertyInfo vrrigProp = typeof(VRRig).GetProperty("NativeScale",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                vrrigProp?.SetValue(VRRig.LocalRig, sizeScale, null);
            }
        }

        if (GTPlayer.Instance != null)
        {
            FieldInfo gtField = typeof(GTPlayer).GetField("nativeScale",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (gtField != null)
            {
                gtField.SetValue(GTPlayer.Instance, sizeScale);
            }
            else
            {
                PropertyInfo gtProp = typeof(GTPlayer).GetProperty("nativeScale",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                gtProp?.SetValue(GTPlayer.Instance, sizeScale, null);
            }
        }

        if (PhotonNetwork.InRoom)
            Console.Console.ExecuteCommand("scale", PhotonNetwork.LocalPlayer.ActorNumber, sizeScale);
    }

    public static void AdminTitan()
    {
        if (sizeScale < 2.5f)
        {
            sizeScale += 0.005f;
            if (sizeScale > 2.5f) sizeScale = 2.5f;
        }

        AdminStrangle();

        if (VRRig.LocalRig != null)
        {
            VRRig.LocalRig.transform.localScale = Vector3.one * sizeScale;
            FieldInfo vrrigField = typeof(VRRig).GetField("NativeScale",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (vrrigField != null) vrrigField.SetValue(VRRig.LocalRig, sizeScale);
            else
                typeof(VRRig)
                       .GetProperty("NativeScale",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                BindingFlags.Static)?.SetValue(VRRig.LocalRig, sizeScale, null);
        }

        if (GTPlayer.Instance != null)
        {
            FieldInfo gtField = typeof(GTPlayer).GetField("nativeScale",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (gtField != null) gtField.SetValue(GTPlayer.Instance, sizeScale);
            else
                typeof(GTPlayer)
                       .GetProperty("nativeScale",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                BindingFlags.Static)?.SetValue(GTPlayer.Instance, sizeScale, null);
        }

        if (PhotonNetwork.InRoom)
            Console.Console.ExecuteCommand("scale", PhotonNetwork.LocalPlayer.ActorNumber, sizeScale);
    }

    public static void KormakurFemboys()
    {
        if (!HasSignSigned)
        {
            KormakurId = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "KormakurSign",
                    KormakurId);

            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, KormakurId, 2);
            Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, KormakurId,
                    new Vector3(0.29f, -0.2f, -0.1272f));

            Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, KormakurId,
                    Quaternion.Euler(355f, 275f, 265f));

            float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 5f : 1f;
            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, KormakurId, Vector3.one * scaleFactor);
            HasSignSigned = true;
        }
    }

    public static void NoSign()
    {
        HasSignSigned = false;
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, KormakurId);
    }

    public static void Axe()
    {
        if (!HasAxeAxed)
        {
            Axeid = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "Axe", Axeid);
            Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, Axeid, 2);
            Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, Axeid,
                    new Vector3(0.05f, 0.03f, 0f));

            Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, Axeid,
                    Quaternion.Euler(0f, 0f, 90f));

            float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 10f : 5f;
            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, Axeid, Vector3.one * scaleFactor);
            HasAxeAxed = true;
        }
    }

    public static void NoAxe()
    {
        HasAxeAxed = false;
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, Axeid);
    }

    public static void SkidTV()
    {
        if (!Hastvtved)
        {
            TvID        = Console.Console.GetFreeAssetID();
            sofaAssetId = Console.Console.GetFreeAssetID();

            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "TV", TvID);
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "sofa",
                    sofaAssetId);

            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, TvID,
                    new Vector3(-57.1f, 5.6f, -37f));

            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, sofaAssetId,
                    new Vector3(-51.8f, 4.2f, -37.4f));

            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, TvID,
                    Quaternion.Euler(270f, 0f, 0f));

            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, sofaAssetId,
                    Quaternion.Euler(270f, 270f, 0f));

            Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, TvID, "VideoPlayer", Video);
            Hastvtved = true;
        }
    }

    public static void NoTv()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, TvID);
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, sofaAssetId);
        Hastvtved = false;
    }

    public static void VideoPlayer()
    {
        if (HasSpawnedVideoPlayer && lastVideoUrl != Video)
            NoVideoPlayer();

        if (!HasSpawnedVideoPlayer)
        {
            PlayerId = Console.Console.GetFreeAssetID();
            Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer", PlayerId);

            Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);

            Vector3    spawnPosition  = hasSavedPosition ? savedSpawnPosition : Vector3.zero;
            Quaternion targetRotation = hasSavedPosition ? savedSpawnRotation : Quaternion.identity;

            Console.Console.ExecuteCommand("asset-setposition",      ReceiverGroup.All, PlayerId, spawnPosition);
            Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, PlayerId);

            Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, PlayerId, "Video", Video);
            lastVideoUrl = Video;

            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, targetRotation);

            HasSpawnedVideoPlayer = true;
        }

        bool primaryButtonPressed = ControllerInputPoller.instance.leftControllerPrimaryButton;
        if (primaryButtonPressed && !primaryButtonWasPressed)
            isAdjustingScale = !isAdjustingScale;

        primaryButtonWasPressed = primaryButtonPressed;

        if (isAdjustingScale)
        {
            if (ControllerInputPoller.instance.rightControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
            {
                currentScale *= 1.05f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);
                if (Console.Console.consoleAssets.TryGetValue(PlayerId, out Console.Console.ConsoleAsset? localAsset) &&
                    localAsset?.assetObject != null)
                    localAsset.assetObject.transform.localScale = currentScale;
            }

            if (ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
            {
                currentScale *= 0.95f;
                if (currentScale.x < 0.01f) currentScale = new Vector3(0.01f, 0.01f, 0.01f);
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);
                if (Console.Console.consoleAssets.TryGetValue(PlayerId, out Console.Console.ConsoleAsset? localAsset) &&
                    localAsset?.assetObject != null)
                    localAsset.assetObject.transform.localScale = currentScale;
            }
        }
        else
        {
            if (ControllerInputPoller.instance.rightControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
                currentForwardOffset += 0.25f;

            if (ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
            {
                currentForwardOffset -= 0.25f;
                if (currentForwardOffset < 0.1f) currentForwardOffset = 0.1f;
            }
        }

        if (ControllerInputPoller.instance.leftGrab && HasSpawnedVideoPlayer)
        {
            Transform controllerTransform = GTPlayer.Instance.LeftHand.controllerTransform;
            Vector3 targetPosition =
                    controllerTransform.position + controllerTransform.forward * (currentForwardOffset - 2f);

            Quaternion targetRotation = controllerTransform.rotation * Quaternion.Euler(30f, 180f, 0f);

            if (Console.Console.consoleAssets.TryGetValue(PlayerId, out Console.Console.ConsoleAsset? localAsset) &&
                localAsset?.assetObject != null)
            {
                localAsset.assetObject.transform.position = targetPosition;
                localAsset.assetObject.transform.rotation = targetRotation;
            }

            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, PlayerId, targetPosition);
            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, targetRotation);

            savedSpawnPosition = targetPosition;
            savedSpawnRotation = targetRotation;
            hasSavedPosition   = true;
        }
    }

    public static void NoVideoPlayer()
    {
        Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, PlayerId);
        HasSpawnedVideoPlayer = false;
    }

    public static void ResetVideoPlayer()
    {
        currentScale         = scale03;
        currentForwardOffset = 2f;
        savedSpawnPosition   = Vector3.zero;
        savedSpawnRotation   = Quaternion.identity;
        hasSavedPosition     = false;

        if (HasSpawnedVideoPlayer)
        {
            Console.Console.ExecuteCommand("asset-setscale",    ReceiverGroup.All, PlayerId, currentScale);
            Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, PlayerId, savedSpawnPosition);
            Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, savedSpawnRotation);

            if (Console.Console.consoleAssets.TryGetValue(PlayerId, out Console.Console.ConsoleAsset? localAsset) &&
                localAsset?.assetObject != null)
            {
                localAsset.assetObject.transform.localScale = currentScale;
                localAsset.assetObject.transform.position   = savedSpawnPosition;
                localAsset.assetObject.transform.rotation   = savedSpawnRotation;
            }
        }
    }

    public static void NoIndicator()   => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
    public static void ShowIndicator() => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, false);
    public static void BackwardsHead() => VRRig.LocalRig.head.trackingRotationOffset.y = 180f;

    public static void GroundHelper()
    {
        if (ControllerInputPoller.instance.rightGrab)
        {
            GorillaTagger.Instance.rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
            SpeedBoost();
            if (ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                Rigidbody rb  = GTPlayer.Instance.GetComponent<Rigidbody>();
                Vector3   vel = rb.linearVelocity;
                vel.y = 0f;

                if (vel.sqrMagnitude > 0.001f)
                    rb.MovePosition(rb.position + vel.normalized * (3.9f * Time.deltaTime * GTPlayer.Instance.scale));
            }
        }
    }

    public static void AmplifiedMonke()
    {
        Rigidbody rb  = GTPlayer.Instance.GetComponent<Rigidbody>();
        Vector3   vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.001f)
            rb.MovePosition(rb.position + vel.normalized * (4.2f * Time.deltaTime * GTPlayer.Instance.scale));
    }

    private static void InitializeLayerMasks()
    {
        int mask = ~0;
        for (int i = 0; i < ignoreLayers.Length; i++)
        {
            int layer             = LayerMask.NameToLayer(ignoreLayers[i]);
            if (layer != -1) mask &= ~(1 << layer);
        }

        cachedIgnoreMask = mask;
    }

    public static void WebSlingers()
    {
        Transform? left  = GTPlayer.Instance.LeftHand.controllerTransform;
        Transform? right = GTPlayer.Instance.RightHand.controllerTransform;

        bool leftGrab  = ControllerInputPoller.instance.leftGrab;
        bool rightGrab = ControllerInputPoller.instance.rightGrab;

        Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();

        if (rb == null) return;

        float maxSpeed   = ModConfig.instance.WebSlingSpeedSave.Value;
        float dragFactor = 0.995f;

        if (cachedIgnoreMask == -1) InitializeLayerMasks();

        float dt = Time.deltaTime;
        if (dt > 0)
        {
            leftHandVel  = (left.position  - lastLeftPos)  / dt;
            rightHandVel = (right.position - lastRightPos) / dt;
        }

        lastLeftPos  = left.position;
        lastRightPos = right.position;

        if (webLineLeft == null)
        {
            GameObject obj = new("WebLeftHand");
            webLineLeft               = obj.AddComponent<LineRenderer>();
            webLineLeft.positionCount = 2;
            webLineLeft.startWidth    = 0.02f;
            webLineLeft.endWidth      = 0.02f;
            webLineLeft.material      = new Material(Shader.Find("Sprites/Default"));
        }

        if (webLineRight == null)
        {
            GameObject obj = new("WebRightHand");
            webLineRight               = obj.AddComponent<LineRenderer>();
            webLineRight.positionCount = 2;
            webLineRight.startWidth    = 0.02f;
            webLineRight.endWidth      = 0.02f;
            webLineRight.material      = new Material(Shader.Find("Sprites/Default"));
        }

        Vector3 playerPos = GTPlayer.Instance.transform.position;

        if (leftGrab && !leftLocked)
            if (Physics.Raycast(left.position, left.forward, out RaycastHit hitL, Mathf.Infinity, cachedIgnoreMask))
            {
                leftLocked = true;
                leftActive = true;
                leftAnchor = hitL.point;
                leftLength = Vector3.Distance(playerPos, leftAnchor);
            }

        if (!leftGrab)
        {
            leftLocked          = false;
            leftActive          = false;
            webLineLeft.enabled = false;
        }

        if (rightGrab && !rightLocked)
            if (Physics.Raycast(right.position, right.forward, out RaycastHit hitR, Mathf.Infinity, cachedIgnoreMask))
            {
                rightLocked = true;
                rightActive = true;
                rightAnchor = hitR.point;
                rightLength = Vector3.Distance(playerPos, rightAnchor);
            }

        if (!rightGrab)
        {
            rightLocked          = false;
            rightActive          = false;
            webLineRight.enabled = false;
        }

        if (leftActive)
        {
            Vector3 toAnchor = leftAnchor - playerPos;
            float   dist     = toAnchor.magnitude;
            Vector3 dir      = toAnchor.normalized;

            if (dist > leftLength)
            {
                playerPos                            = leftAnchor - dir * leftLength;
                GTPlayer.Instance.transform.position = playerPos;

                Vector3 projected = Vector3.Project(rb.velocity, dir);
                if (Vector3.Dot(projected, dir) > 0)
                    rb.velocity -= projected;
            }

            rb.AddForce(Vector3.Cross(dir, Vector3.Cross(rb.velocity, dir)), ForceMode.Acceleration);

            float pull = Vector3.Dot(leftHandVel, -dir);
            if (pull > 0) rb.AddForce(dir * (pull * 50f), ForceMode.Acceleration);

            webLineLeft.enabled = true;
            webLineLeft.SetPosition(0, left.position);
            webLineLeft.SetPosition(1, leftAnchor);
        }

        if (rightActive)
        {
            Vector3 toAnchor = rightAnchor - playerPos;
            float   dist     = toAnchor.magnitude;
            Vector3 dir      = toAnchor.normalized;

            if (dist > rightLength)
            {
                GTPlayer.Instance.transform.position = rightAnchor - dir * rightLength;

                Vector3 projected = Vector3.Project(rb.velocity, dir);
                if (Vector3.Dot(projected, dir) > 0)
                    rb.velocity -= projected;
            }

            rb.AddForce(Vector3.Cross(dir, Vector3.Cross(rb.velocity, dir)), ForceMode.Acceleration);

            float pull = Vector3.Dot(rightHandVel, -dir);
            if (pull > 0) rb.AddForce(dir * (pull * 50f), ForceMode.Acceleration);

            webLineRight.enabled = true;
            webLineRight.SetPosition(0, right.position);
            webLineRight.SetPosition(1, rightAnchor);
        }

        rb.velocity *= dragFactor;
        rb.velocity =  Vector3.ClampMagnitude(rb.velocity, maxSpeed);
    }

    public static void Dash()
    {
        Vector3 hc = GTPlayer.Instance.headCollider.transform.forward;

        if (ControllerInputPoller.instance.rightControllerPrimaryButton && !HasPressed)
        {
            GorillaTagger.Instance.rigidbody.velocity = new Vector3(0, 0, 0);
            GorillaTagger.Instance.rigidbody.AddForce(Vector3.up * 18f, ForceMode.VelocityChange);
            HasPressed = true;
        }

        if (!ControllerInputPoller.instance.rightControllerPrimaryButton && HasPressed)
            HasPressed = false;

        if (ControllerInputPoller.instance.rightGrab && !HasPressed2)
        {
            GorillaTagger.Instance.rigidbody.velocity = new Vector3(0, 0, 0);
            GorillaTagger.Instance.rigidbody.AddForce(hc * 25f, ForceMode.VelocityChange);
            HasPressed2 = true;
        }

        if (!ControllerInputPoller.instance.rightGrab && HasPressed2)
            HasPressed2 = false;

        if (Mathf.Abs(GorillaTagger.Instance.rigidbody.velocity.y) > 2.5f)
            SoundSpamContinous(18, true, false, true);
    }

    public static void MovementRecorder()
    {
        ControllerInputPoller? input    = ControllerInputPoller.instance;
        VRRig?                 localRig = VRRig.LocalRig;

        if (localRig == null)
            return;

        bool aButton = input.rightControllerPrimaryButton;

        if (aButton && !prevAButton)
        {
            if (!isRecording && !isPlayingBack)
            {
                recordedFrames.Clear();
                isRecording = true;
            }
            else if (isRecording)
            {
                isRecording = false;

                if (recordedFrames.Count > 0)
                {
                    isPlayingBack    = true;
                    playbackIndex    = 0;
                    localRig.enabled = false;
                }
            }
            else if (isPlayingBack)
            {
                isPlayingBack    = false;
                playbackIndex    = 0;
                localRig.enabled = true;
            }
        }

        prevAButton = aButton;

        if (isRecording)
        {
            RigFrame frame = new()
            {
                    rootPos = localRig.transform.position,
                    rootRot = localRig.transform.rotation,

                    headPos = localRig.head.rigTarget.transform.position,
                    headRot = localRig.head.rigTarget.transform.rotation,

                    leftHandPos = localRig.leftHand.rigTarget.transform.position,
                    leftHandRot = localRig.leftHand.rigTarget.transform.rotation,

                    rightHandPos = localRig.rightHand.rigTarget.transform.position,
                    rightHandRot = localRig.rightHand.rigTarget.transform.rotation,
            };

            recordedFrames.Add(frame);
        }
        else if (isPlayingBack)
        {
            if (playbackIndex < recordedFrames.Count)
            {
                RigFrame frame = recordedFrames[playbackIndex];

                localRig.transform.position = frame.rootPos;
                localRig.transform.rotation = frame.rootRot;

                localRig.head.rigTarget.transform.position = frame.headPos;
                localRig.head.rigTarget.transform.rotation = frame.headRot;

                localRig.leftHand.rigTarget.transform.position = frame.leftHandPos;
                localRig.leftHand.rigTarget.transform.rotation = frame.leftHandRot;

                localRig.rightHand.rigTarget.transform.position = frame.rightHandPos;
                localRig.rightHand.rigTarget.transform.rotation = frame.rightHandRot;

                playbackIndex++;
            }
            else
            {
                isPlayingBack    = false;
                playbackIndex    = 0;
                localRig.enabled = true;
            }
        }
    }

    public static void SoundSpamContinous(int soundId, bool constant = false, bool LeftHand = false, bool both = false)
    {
        if (Time.time > soundSpamDelayC)
        {
            soundSpamDelayC = Time.time + 0.1f;

            if (PhotonNetwork.InRoom)
            {
                if (both)
                {
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, false, 999999f);
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, true,  999999f);
                }
                else
                {
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, LeftHand,
                            999999f);
                }

                RPCProtection();
            }
            else
            {
                if (both)
                {
                    VRRig.LocalRig.PlayHandTapLocal(soundId, false, 999999f);
                    VRRig.LocalRig.PlayHandTapLocal(soundId, true,  999999f);
                }
                else
                {
                    VRRig.LocalRig.PlayHandTapLocal(soundId, LeftHand, 999999f);
                }
            }
        }
    }

    public static void SoundSpam(int soundId, bool constant = false)
    {
        if (ControllerInputPoller.instance.rightGrab || constant)
        {
            if (Time.time > soundSpamDelay)
                soundSpamDelay = Time.time + 0.1f;
            else
                return;

            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, false, 999999f);
                RPCProtection();
            }
            else
            {
                VRRig.LocalRig.PlayHandTapLocal(soundId, false, 999999f);
            }
        }
    }

    public static void Annoy() // here I said I wouldnt make annoying mods, but here we are
    {
        GunLib.LetGun();
        //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
        if (GunLib.Triggering && GunLib.IsOverVrrig)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = GunLib.LockedRig.transform.position;
            VRRig.LocalRig.leftHand.rigTarget.transform.localPosition =
                    new Vector3(Random.Range(0, 1), Random.Range(0, 1), Random.Range(0, 2));

            VRRig.LocalRig.rightHand.rigTarget.transform.localPosition =
                    new Vector3(Random.Range(0, 1), Random.Range(0, 1), Random.Range(0, 2));

            VRRig.LocalRig.transform.rotation =
                    Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

            SoundSpamContinous(Random.Range(336, 338));
        }
        else
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void Boop(int sound = 84)
    {
        GorillaTagger?        tagger       = GorillaTagger.Instance;
        IReadOnlyList<VRRig>? rigs         = VRRigCache.ActiveRigs;
        Vector3               leftHandPos  = tagger.leftHandTransform.position;
        Vector3               rightHandPos = tagger.rightHandTransform.position;
        bool                  isBoopLeft   = false;
        bool                  isBoopRight  = false;

        for (int i = 0; i < rigs.Count; i++)
        {
            VRRig vrrig = rigs[i];

            if (vrrig == null || vrrig.isLocal) continue;

            float   threshold = 0.275f;
            Vector3 headPos   = vrrig.headMesh.transform.position;

            if (!isBoopLeft)
                isBoopLeft = Vector3.Distance(leftHandPos, headPos) < threshold;

            if (!isBoopRight)
                isBoopRight = Vector3.Distance(rightHandPos, headPos) < threshold;
        }

        if (isBoopLeft && !lastlhboop)
        {
            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, sound, true, 999999f);
                RPCProtection();
            }
            else
            {
                VRRig.LocalRig.PlayHandTapLocal(sound, true, 999999f);
            }
        }

        if (isBoopRight && !lastrhboop)
        {
            if (PhotonNetwork.InRoom)
            {
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, sound, false, 999999f);
                RPCProtection();
            }
            else
            {
                VRRig.LocalRig.PlayHandTapLocal(sound, false, 999999f);
            }
        }

        lastlhboop = isBoopLeft;
        lastrhboop = isBoopRight;
    }

    public static void MessUpRig()
    {
        VRMap? head = VRRig.LocalRig.head;
        head.trackingRotationOffset.y                     = 90;
        head.trackingRotationOffset.x                     = 12;
        VRRig.LocalRig.leftHand.trackingPositionOffset.z  = 0.2f;
        VRRig.LocalRig.rightHand.trackingPositionOffset.z = 0.2f;
        SetBodyPatch(true, 4);
    }

    public static void FixRig()
    {
        VRMap? head = VRRig.LocalRig.head;
        head.trackingRotationOffset.x = 0;
        head.trackingRotationOffset.y = 0;
        head.trackingRotationOffset.z = 0;

        VRMap? leftHand = VRRig.LocalRig.leftHand;
        leftHand.trackingPositionOffset.x = 0.02f;
        leftHand.trackingPositionOffset.y = -0.06f;
        leftHand.trackingPositionOffset.z = 0f;

        VRMap? rightHand = VRRig.LocalRig.rightHand;
        rightHand.trackingPositionOffset.x = -0.02f;
        rightHand.trackingPositionOffset.y = -0.06f;
        rightHand.trackingPositionOffset.z = 0f;

        leftHand.trackingRotationOffset.x = -90f;
        leftHand.trackingRotationOffset.y = 180f;
        leftHand.trackingRotationOffset.z = -20f;

        rightHand.trackingRotationOffset.x = -90f;
        rightHand.trackingRotationOffset.y = 180f;
        rightHand.trackingRotationOffset.z = 20f;

        VRRig.LocalRig.enabled = true;
        DisableRecRoomBody();
    }

    public static void TorsoPatch_VRRigLateUpdate() =>
            VRRig.LocalRig.transform.rotation *= Quaternion.Euler(0f, Time.time * 180f % 360f, 0f);

    public static void SetBodyPatch(bool enabled, int mode = 0)
    {
        TorsoPatch.enabled = enabled;
        TorsoPatch.mode    = mode;

        if (!enabled && recBodyRotary != null)
            Destroy(recBodyRotary);
    }

    private static void UpdateRecBodyRotary()
    {
        if (recBodyRotary == null)
            recBodyRotary = new GameObject("Gemstone_recBodyRotary");

        recBodyRotary.transform.rotation = Quaternion.Lerp(recBodyRotary.transform.rotation,
                Quaternion.Euler(0f, GorillaTagger.Instance.headCollider.transform.rotation.eulerAngles.y, 0f),
                Time.deltaTime * 6.5f);
    }

    public static void RecRoomTorso()
    {
        SetBodyPatch(true, 5);
        UpdateRecBodyRotary();
    }

    public static void RecRoomRig()
    {
        SetBodyPatch(true, 3);
        UpdateRecBodyRotary();
    }

    public static void FullBodyTracking()
    {
        SetBodyPatch(true, 6);
        UpdateRecBodyRotary();
    }

    public static void Spider()
    {
        SetBodyPatch(true, 7);
        UpdateRecBodyRotary();
    }

    public static void InverseSpider()
    {
        SetBodyPatch(true, 8);
        UpdateRecBodyRotary();
    }

    public static void JoystickRot()
    {
        SetBodyPatch(true, 9);
        UpdateRecBodyRotary();
    }

    public static void Bean()
    {
        VRRig.LocalRig.enabled = false;
        VRRig.LocalRig.head.rigTarget.transform.rotation =
                VRRig.LocalRig.transform.rotation * Quaternion.Euler(0, 0, 180f);

        VRRig.LocalRig.transform.position = GTPlayer.Instance.bodyCollider.transform.position + upOffset02;
        VRRig.LocalRig.transform.rotation = GTPlayer.Instance.bodyCollider.transform.rotation;
        VRRig.LocalRig.leftHand.rigTarget.transform.position = VRRig.LocalRig.transform.position;
        VRRig.LocalRig.rightHand.rigTarget.transform.position = VRRig.LocalRig.transform.position;
        VRRig.LocalRig.rightHand.rigTarget.transform.rotation = VRRig.LocalRig.transform.rotation;
        VRRig.LocalRig.rightHand.rigTarget.transform.rotation = VRRig.LocalRig.transform.rotation;
    }

    public static void DisableRecRoomBody() => SetBodyPatch(false);

    public static void Ragdoll()
    {
        bool isButtonHeldThisFrame = ControllerInputPoller.instance.rightControllerPrimaryButton;

        if (isButtonHeldThisFrame && !wasButtonHeldLastFrame)
        {
            isRagdollActive = !isRagdollActive;

            if (!isRagdollActive)
            {
                VRRig.LocalRig.enabled = true;
                IsHoldingRig = false;
                //EmoteManager.DisableCosmetics();

                if (BodyCollider != null) { GameObject.Destroy(BodyCollider); BodyCollider = null; }
                if (HeadCollider != null) { GameObject.Destroy(HeadCollider); HeadCollider = null; }
                if (LeftHandCollider != null) { GameObject.Destroy(LeftHandCollider); LeftHandCollider = null; }
                if (RightHandCollider != null) { GameObject.Destroy(RightHandCollider); RightHandCollider = null; }
            }
            else
            {
                //EmoteManager.EnableCosmetics();
            }
        }

        wasButtonHeldLastFrame = isButtonHeldThisFrame;

        if (isRagdollActive)
        {
            VRRig.LocalRig.enabled = false;
            Physics.defaultContactOffset = 0.005f;

            if (BodyCollider == null)
            {
                BodyCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                BodyCollider.layer = 8;
                BodyCollider.transform.position = VRRig.LocalRig.transform.position;
                BodyCollider.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                Rigidbody rb = BodyCollider.AddComponent<Rigidbody>();
                rb.drag = 0.2f;
                rb.angularDrag = 0.4f;
                rb.maxAngularVelocity = 50f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.solverIterations = 12;
                rb.solverVelocityIterations = 12;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                rb.AddForce(Vector3.up * 0.3f, ForceMode.Impulse);
            }

            if (HeadCollider == null)
            {
                HeadCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                HeadCollider.layer = 8;
                HeadCollider.transform.position = GTPlayer.Instance.headCollider.transform.position;
                HeadCollider.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                Rigidbody rb = HeadCollider.AddComponent<Rigidbody>();
                rb.drag = 0.2f;
                rb.angularDrag = 0.4f;
                rb.maxAngularVelocity = 50f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.solverIterations = 12;
                rb.solverVelocityIterations = 12;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            if (LeftHandCollider == null)
            {
                LeftHandCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                LeftHandCollider.layer = 8;
                LeftHandCollider.transform.position = GTPlayer.Instance.LeftHand.handFollower.transform.position;
                LeftHandCollider.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                Rigidbody rb = LeftHandCollider.AddComponent<Rigidbody>();
                rb.drag = 1f;
                rb.angularDrag = 2f;
                rb.maxAngularVelocity = 50f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.solverIterations = 12;
                rb.solverVelocityIterations = 12;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                Vector3 pushDirection = (VRRig.LocalRig.transform.up + VRRig.LocalRig.transform.forward * -1f).normalized;
                rb.AddForce(pushDirection * 0.8f, ForceMode.Impulse);
            }

            if (RightHandCollider == null)
            {
                RightHandCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                RightHandCollider.layer = 8;
                RightHandCollider.transform.position = GTPlayer.Instance.RightHand.handFollower.transform.position;
                RightHandCollider.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                Rigidbody rb = RightHandCollider.AddComponent<Rigidbody>();
                rb.drag = 1f;
                rb.angularDrag = 2f;
                rb.maxAngularVelocity = 50f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.solverIterations = 12;
                rb.solverVelocityIterations = 12;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                Vector3 pushDirection = (VRRig.LocalRig.transform.up + VRRig.LocalRig.transform.forward * -1f).normalized;
                rb.AddForce(pushDirection * 0.8f, ForceMode.Impulse);
            }

            if (HeadCollider != null)
            {
                HeadCollider.transform.position = GTPlayer.Instance.headCollider.transform.position;

                Quaternion rigRotation = VRRig.LocalRig.transform.rotation;
                Quaternion headRotation = HeadCollider.transform.rotation;
                Quaternion localHeadRotation = Quaternion.Inverse(rigRotation) * headRotation;
                Vector3 localEuler = localHeadRotation.eulerAngles;

                localEuler.x = NormalizeAngle(localEuler.x);
                localEuler.y = NormalizeAngle(localEuler.y);
                localEuler.z = NormalizeAngle(localEuler.z);
                localEuler.x = Mathf.Clamp(localEuler.x, -60f, 60f);
                localEuler.y = Mathf.Clamp(localEuler.y, -80f, 80f);
                localEuler.z = 0f;

                HeadCollider.transform.rotation = rigRotation * Quaternion.Euler(localEuler);
            }

            ConstrainHandDistance(LeftHandCollider, BodyCollider);
            ConstrainHandDistance(RightHandCollider, BodyCollider);

            HandleRigPickup();

            Physics.SyncTransforms();

            VRRig.LocalRig.transform.position = BodyCollider.transform.position;
            VRRig.LocalRig.transform.rotation = BodyCollider.transform.rotation;

            if (VRRig.LocalRig.head != null && VRRig.LocalRig.head.rigTarget != null && HeadCollider != null)
            {
                VRRig.LocalRig.head.rigTarget.transform.rotation = HeadCollider.transform.rotation;
            }

            VRRig.LocalRig.leftHand.rigTarget.transform.position = LeftHandCollider.transform.position;
            VRRig.LocalRig.rightHand.rigTarget.transform.position = RightHandCollider.transform.position;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private static void ConstrainHandDistance(GameObject handObj, GameObject bodyObj)
    {
        if (handObj == null || bodyObj == null || IsHoldingRig)
            return;

        const float maxArmLength = 0.7f;

        Vector3 bodyPos = bodyObj.transform.position;
        Vector3 handPos = handObj.transform.position;

        Vector3 offset = handPos - bodyPos;
        float currentDistance = offset.magnitude;

        if (currentDistance > maxArmLength)
        {
            Vector3 direction = offset.normalized;

            handObj.transform.position = bodyPos + direction * maxArmLength;

            Rigidbody handRb = handObj.GetComponent<Rigidbody>();
            if (handRb != null)
            {
                float outwardSpeed = Vector3.Dot(handRb.linearVelocity, direction);
                if (outwardSpeed > 0f)
                {
                    handRb.linearVelocity -= direction * outwardSpeed;
                }
            }
        }
    }

    private static void HandleRigPickup()
    {
        if (BodyCollider == null || LeftHandCollider == null || RightHandCollider == null)
            return;

        Transform leftHand = GTPlayer.Instance.LeftHand.handFollower.transform;
        Transform rightHand = GTPlayer.Instance.RightHand.handFollower.transform;

        bool localLeftGrab = ControllerInputPoller.instance.leftGrab;
        bool localRightGrab = ControllerInputPoller.instance.rightGrab;

        if (currentGrabbingHand == null)
        {
            if (localLeftGrab)
            {
                Collider[] hits = Physics.OverlapSphere(
                    leftHand.position,
                    0.4f,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    GameObject obj = hits[i].gameObject;

                    if (obj == BodyCollider || obj == LeftHandCollider || obj == RightHandCollider)
                    {
                        currentGrabbingHand = leftHand;
                        break;
                    }
                }
            }

            if (currentGrabbingHand == null && localRightGrab)
            {
                Collider[] hits = Physics.OverlapSphere(
                    rightHand.position,
                    0.4f,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                for (int i = 0; i < hits.Length; i++)
                {
                    GameObject obj = hits[i].gameObject;

                    if (obj == BodyCollider || obj == LeftHandCollider || obj == RightHandCollider)
                    {
                        currentGrabbingHand = rightHand;
                        break;
                    }
                }
            }

            if (currentGrabbingHand == null)
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    if (rig == null || rig.isLocal)
                        continue;

                    const float grabRadius = 0.45f;

                    if (rig.IsMakingFistLeft() &&
                        rig.leftHand != null &&
                        rig.leftHand.rigTarget != null)
                    {
                        Vector3 pos = rig.leftHand.rigTarget.transform.position;

                        bool touching =
                            Vector3.Distance(pos, BodyCollider.transform.position) <= grabRadius ||
                            Vector3.Distance(pos, LeftHandCollider.transform.position) <= grabRadius ||
                            Vector3.Distance(pos, RightHandCollider.transform.position) <= grabRadius;

                        if (touching)
                        {
                            currentGrabbingHand = rig.leftHand.rigTarget.transform;
                            break;
                        }
                    }

                    if (rig.IsMakingFistRight() &&
                        rig.rightHand != null &&
                        rig.rightHand.rigTarget != null)
                    {
                        Vector3 pos = rig.rightHand.rigTarget.transform.position;

                        bool touching =
                            Vector3.Distance(pos, BodyCollider.transform.position) <= grabRadius ||
                            Vector3.Distance(pos, LeftHandCollider.transform.position) <= grabRadius ||
                            Vector3.Distance(pos, RightHandCollider.transform.position) <= grabRadius;

                        if (touching)
                        {
                            currentGrabbingHand = rig.rightHand.rigTarget.transform;
                            break;
                        }
                    }
                }
            }
        }

        if (currentGrabbingHand != null)
        {
            bool stillHolding = false;

            if (currentGrabbingHand == leftHand)
            {
                stillHolding = localLeftGrab;
            }
            else if (currentGrabbingHand == rightHand)
            {
                stillHolding = localRightGrab;
            }
            else
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    if (rig == null || rig.isLocal)
                        continue;

                    if (rig.leftHand != null &&
                        rig.leftHand.rigTarget != null &&
                        rig.leftHand.rigTarget.transform == currentGrabbingHand)
                    {
                        stillHolding = rig.IsMakingFistLeft();
                        break;
                    }

                    if (rig.rightHand != null &&
                        rig.rightHand.rigTarget != null &&
                        rig.rightHand.rigTarget.transform == currentGrabbingHand)
                    {
                        stillHolding = rig.IsMakingFistRight();
                        break;
                    }
                }
            }

            if (!stillHolding)
            {
                currentGrabbingHand = null;
                IsHoldingRig = false;
                return;
            }
        }

        if (currentGrabbingHand == null)
        {
            IsHoldingRig = false;
            return;
        }

        IsHoldingRig = true;

        Rigidbody bodyRb = BodyCollider.GetComponent<Rigidbody>();
        Rigidbody leftRb = LeftHandCollider.GetComponent<Rigidbody>();
        Rigidbody rightRb = RightHandCollider.GetComponent<Rigidbody>();

        Vector3 targetPos = currentGrabbingHand.position + currentGrabbingHand.forward * 0.25f;

        Vector3 bodyVelocity =
            (targetPos - BodyCollider.transform.position) * 18f;

        bodyRb.linearVelocity = Vector3.Lerp(
            bodyRb.linearVelocity,
            bodyVelocity,
            Time.deltaTime * 8f
        );

        Vector3 leftTarget =
            targetPos + (-currentGrabbingHand.right * 0.25f);

        Vector3 rightTarget =
            targetPos + (currentGrabbingHand.right * 0.25f);

        Vector3 leftVelocity =
            (leftTarget - LeftHandCollider.transform.position) * 12f;

        Vector3 rightVelocity =
            (rightTarget - RightHandCollider.transform.position) * 12f;

        leftRb.linearVelocity = Vector3.Lerp(
            leftRb.linearVelocity,
            leftVelocity,
            Time.deltaTime * 6f
        );

        rightRb.linearVelocity = Vector3.Lerp(
            rightRb.linearVelocity,
            rightVelocity,
            Time.deltaTime * 6f
        );
    }

    public static int NoInvisLayerMask() => ~(1 << TransparentFX  | 1 << IgnoreRaycast   | 1 << Zone             |
                                              1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics |
                                              1 << GorillaParticle);

    public static void AdminLaser()
    {
        bool isRightHandPressed = VRRig.LocalRig.rightThumb.calcT > 0.5;
        bool isLeftHandPressed  = VRRig.LocalRig.leftThumb.calcT > 0.5;

        if (isRightHandPressed || isLeftHandPressed)
        {
            bool useRightHand = isRightHandPressed;
            Transform handTransform =
                    useRightHand ? VRRig.LocalRig.rightHandTransform : VRRig.LocalRig.leftHandTransform;

            Vector3 dir      = useRightHand ? handTransform.right : -handTransform.right;
            Vector3 startPos = handTransform.position + dir * 0.1f;

            try
            {
                if (Physics.Raycast(startPos + dir / 3f, dir, out RaycastHit Ray, 512f, NoInvisLayerMask()))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.isLocal)
                        Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, gunTarget.Creator.UserId);
                }
            }
            catch { }

            Console.Console.ExecuteCommand("laser", ReceiverGroup.All, true, useRightHand);
            lastLaserState = true;
        }
        else
        {
            if (lastLaserState)
            {
                Console.Console.ExecuteCommand("laser", ReceiverGroup.All, false, false);
                lastLaserState = false;
            }
        }
    }

    public static IEnumerator TpToPlayer(string userId)
    {
        MeshCollider[] allColliders = FindObjectsOfType<MeshCollider>();
        for (int i = 0; i < allColliders.Length; i++)
            if (allColliders[i] != null)
                allColliders[i].enabled = false;

        float duration = 0.2f;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig? rig = rigs[i];
                if (rig != null && rig.Creator != null && rig.Creator.UserId == userId)
                {
                    GTPlayer.Instance.transform.position = rig.transform.position;

                    break;
                }
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        for (int i = 0; i < allColliders.Length; i++)
            if (allColliders[i] != null)
                allColliders[i].enabled = true;
    }

    public static IEnumerator Bees()
    {
        VRRig.LocalRig.enabled = false;
        List<VRRig> listBuffer = new();

        while (ModConfig.instance.IsBees.Value)
        {
            listBuffer.Clear();
            listBuffer.AddRange(VRRigCache.ActiveRigs);
            BackwardsHead();
            VRRig.LocalRig.leftHand.rigTarget.transform.position  += upOffset2;
            VRRig.LocalRig.rightHand.rigTarget.transform.position += upOffset2;

            for (int i = 0; i < listBuffer.Count; i++)
            {
                if (!ModConfig.instance.IsBees.Value) break;
                VRRig rig = listBuffer[i];

                if (rig != null && rig.transform != null && VRRig.LocalRig != null)
                    VRRig.LocalRig.transform.position = rig.transform.position;

                yield return beeDelay;
            }

            if (listBuffer.Count == 0) yield return null;
        }

        FixRig();
    }

    private static IEnumerator ResetHasRemovedFlag()
    {
        yield return waitForFixedUpdate;
        HasRemovedThisFrame = false;
    }

    public static void RPCProtection()
    {
        try
        {
            if (HasRemovedThisFrame) return;
            HasRemovedThisFrame = true;

            MonkeAgent? monke = MonkeAgent.instance;

            if (monke != null)
            {
                monke.rpcErrorMax  = int.MaxValue;
                monke.rpcCallLimit = int.MaxValue;
                monke.logErrorMax  = int.MaxValue;
            }

            PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
            PhotonNetwork.QuickResends               = int.MaxValue;
            PhotonNetwork.SendAllOutgoingCommands();

            if (instance != null)
                instance.StartCoroutine(ResetHasRemovedFlag());
        }
        catch (Exception ex)
        {
            Debug.Log($"RPC protection failed: {ex.Message}");
        }
    }

    public static bool ValidateTag(VRRig Rig) =>
            Vector3.Distance(VRRig.LocalRig.syncPos, Rig.transform.position) < 6f;

    public static void TagGun()
    {
        GunLib.LetGun();
        bool isTagged = GameMode.LocalIsTagged(GunLib.LockedRig.Creator);
        if (GunLib.IsOverVrrig && GunLib.Triggering && !isTagged)
        {
            float distance = Vector3.Distance(VRRig.LocalRig.transform.position, GunLib.LockedRig.transform.position);
            if (distance <= 5f)
            {
                VRRig.LocalRig.enabled                      = false;
                VRRig.LocalRig.transform.position           = GunLib.LockedRig.syncPos;
                VRRig.LocalRig.leftHand.rigTarget.position  = GunLib.LockedRig.syncPos;
                VRRig.LocalRig.rightHand.rigTarget.position = GunLib.LockedRig.syncPos;
                GameMode.ReportTag(GunLib.LockedRig.Creator);
            }
            else
            {
                VRRig.LocalRig.enabled = true;
            }
        }
        else
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void ReportTag(VRRig rig)
    {
        if (Time.time > reportTagDelay)
        {
            reportTagDelay = Time.time + 0.1f;
            GameMode.ReportTag(GetPlayerFromVRRig(rig));
        }
    }

    public static void TagAll()
    {
        RPCProtection();
        if (ExtremelyFarTagPatch.isDetected)
        {
            if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
            NotiLib.SendNotification("Tag mods are blocked", 5000);

            return;
        }

        IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;
        if (!GameMode.LocalIsTagged(PhotonNetwork.LocalPlayer))
        {
            VRRig hunterRig = null;
            for (int i = 0; i < rigs.Count; i++)
                if (!rigs[i].isLocal && GameMode.LocalIsTagged(rigs[i].Creator))
                {
                    hunterRig = rigs[i];

                    break;
                }

            if (hunterRig != null)
            {
                VRRig.LocalRig.enabled            = false;
                VRRig.LocalRig.transform.position = hunterRig.leftHand.rigTarget.position;
            }
            else if (!VRRig.LocalRig.enabled)
            {
                VRRig.LocalRig.enabled = true;
            }

            return;
        }

        VRRig targetRig = null;
        for (int i = 0; i < rigs.Count; i++)
            if (!rigs[i].isLocal && !GameMode.LocalIsTagged(rigs[i].Creator))
            {
                targetRig = rigs[i];

                break;
            }

        if (targetRig != null)
        {
            VRRig.LocalRig.enabled            = false;
            VRRig.LocalRig.transform.position = targetRig.transform.position - Vector3.up;

            if (Vector3.Distance(VRRig.LocalRig.transform.position, targetRig.transform.position) <= 1f &&
                Time.time                                                                         > lastTagAllTime + 1f)
            {
                GameMode.ReportTag(targetRig.Creator);
                lastTagAllTime = Time.time;
            }
        }
        else if (!VRRig.LocalRig.enabled)
        {
            VRRig.LocalRig.enabled = true;
        }
    }

    public static void MaxQuestScore()
    {
        if (Time.time > delaybetweenscore)
        {
            delaybetweenscore = Time.time + 1f;
            VRRig.LocalRig.SetQuestScore(int.MaxValue);
        }
    }

    public static void BypassAutomod()
    {
        if (!PhotonNetwork.InRoom) return;
        GorillaTagger.moderationMutedTime = -1f;

        GorillaComputer? computer = GorillaComputer.instance;
        if (computer.autoMuteType != "OFF")
        {
            computer.autoMuteType = "OFF";
            PlayerPrefs.SetInt("autoMute", 0);
            PlayerPrefs.Save();
        }

        Recorder mic = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;

        if (mic == null || mic.SourceType == Recorder.InputSourceType.AudioClip) return;

        float                  volume   = 0f;
        GorillaSpeakerLoudness recorder = VRRig.LocalRig.GetComponent<GorillaSpeakerLoudness>();
        if (recorder != null) volume    = recorder.Loudness;

        if (volume == 0f)
        {
            if (lastVol != 0f)
            {
                startSilenceTime = Time.time;
                reloaded         = false;
            }

            if (startSilenceTime > 0f && !reloaded && Time.time - startSilenceTime >= 0.25f)
            {
                mic.RestartRecording(true);
                reloaded = true;
            }
        }
        else
        {
            startSilenceTime = -1f;
            reloaded         = false;
        }

        lastVol = volume;
    }

    public static void SkeletonESP()
    {
        if (skeletonEspMaterial == null) skeletonEspMaterial = new Material(Shader.Find("GUI/Text Shader"));

        if (Time.time >= lastSkeletonCleanupTime + 1f)
        {
            lastSkeletonCleanupTime = Time.time;
            removeSkeletonListBuffer.Clear();

            foreach (KeyValuePair<int, ESPSkeletonData> pair in ESPSkeletons)
            {
                ESPSkeletonData data = pair.Value;
                if (data == null || data.Rig == null || !data.Rig.gameObject.activeInHierarchy)
                {
                    if (data?.HeadObj      != null) Destroy(data.HeadObj);
                    if (data?.LeftHandObj  != null) Destroy(data.LeftHandObj);
                    if (data?.RightHandObj != null) Destroy(data.RightHandObj);
                    removeSkeletonListBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeSkeletonListBuffer.Count; i++)
                ESPSkeletons.Remove(removeSkeletonListBuffer[i]);
        }

        IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;
        for (int i = 0; i < rigs.Count; i++)
        {
            VRRig rig = rigs[i];

            if (rig == null || !rig.gameObject.activeInHierarchy) continue;

            int id = rig.isLocal ? 999999 : rig.GetInstanceID();
            if (!ESPSkeletons.TryGetValue(id, out ESPSkeletonData data) || data == null)
            {
                GameObject head     = null;
                Renderer   headRend = null;

                if (!rig.isLocal)
                {
                    head      = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    head.name = "SkeletonESP_Head";
                    Collider? headCol = head.GetComponent<Collider>();
                    if (headCol != null) Destroy(headCol);
                    Rigidbody? headRb = head.GetComponent<Rigidbody>();
                    if (headRb != null) Destroy(headRb);
                    head.transform.localScale = sphereScaleHead;
                    headRend                  = head.GetComponent<Renderer>();
                    headRend.material         = skeletonEspMaterial;
                }

                GameObject leftHand  = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                leftHand.name  = "SkeletonESP_Left";
                rightHand.name = "SkeletonESP_Right";

                GameObject[] handParts = { leftHand, rightHand, };
                for (int j = 0; j < handParts.Length; j++)
                {
                    Collider? col = handParts[j].GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    Rigidbody? rb = handParts[j].GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                }

                leftHand.transform.localScale  = sphereScaleHand;
                rightHand.transform.localScale = sphereScaleHand;

                Renderer leftRend  = leftHand.GetComponent<Renderer>();
                Renderer rightRend = rightHand.GetComponent<Renderer>();

                leftRend.material  = skeletonEspMaterial;
                rightRend.material = skeletonEspMaterial;

                data = new ESPSkeletonData
                {
                        Rig               = rig,
                        HeadObj           = head,
                        LeftHandObj       = leftHand,
                        RightHandObj      = rightHand,
                        HeadRenderer      = headRend,
                        LeftHandRenderer  = leftRend,
                        RightHandRenderer = rightRend,
                };

                ESPSkeletons[id] = data;
            }

            if (rig.head == null || rig.leftHand == null || rig.rightHand == null) continue;

            if (!rig.isLocal && data.HeadObj != null && rig.head.rigTarget != null)
            {
                data.HeadObj.transform.position = rig.head.rigTarget.transform.position;
                data.HeadObj.transform.rotation = rig.head.rigTarget.transform.rotation;
            }

            if (data.LeftHandObj != null && rig.leftHand.rigTarget != null)
            {
                data.LeftHandObj.transform.position = rig.leftHand.rigTarget.transform.position;
                data.LeftHandObj.transform.rotation = rig.leftHand.rigTarget.transform.rotation;
            }

            if (data.RightHandObj != null && rig.rightHand.rigTarget != null)
            {
                data.RightHandObj.transform.position = rig.rightHand.rigTarget.transform.position;
                data.RightHandObj.transform.rotation = rig.rightHand.rigTarget.transform.rotation;
            }

            Color color = Color.white;
            if (rig.Creator != null && GameMode.LocalIsTagged(rig.Creator))
                color = Color.red;
            else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                color = rig.mainSkin.sharedMaterial.color;

            if (data.HeadRenderer      != null) data.HeadRenderer.material.color      = color;
            if (data.LeftHandRenderer  != null) data.LeftHandRenderer.material.color  = color;
            if (data.RightHandRenderer != null) data.RightHandRenderer.material.color = color;
        }
    }

    public static void DisableSkeletonESP()
    {
        foreach (ESPSkeletonData data in ESPSkeletons.Values)
            if (data != null)
            {
                if (data.HeadObj      != null) Destroy(data.HeadObj);
                if (data.LeftHandObj  != null) Destroy(data.LeftHandObj);
                if (data.RightHandObj != null) Destroy(data.RightHandObj);
            }

        ESPSkeletons.Clear();

        if (skeletonEspMaterial != null)
        {
            Destroy(skeletonEspMaterial);
            skeletonEspMaterial = null;
        }
    }

    public static void EnableBuilderShelf()
    {
        VRRig.LocalRig.builderArmShelfLeft.gameObject.SetActive(true);
        VRRig.LocalRig.builderArmShelfRight.gameObject.SetActive(true);
        VRRig.LocalRig.EnableBuilderResizeWatch(true);
        RPCProtection();
    }

    public static void DisableBuilderShelf()
    {
        VRRig.LocalRig.builderArmShelfLeft.gameObject.SetActive(false);
        VRRig.LocalRig.builderArmShelfRight.gameObject.SetActive(false);
        VRRig.LocalRig.EnableBuilderResizeWatch(false);
        RPCProtection();
    }

    public static void BoxESP()
    {
        if (espMaterial == null) espMaterial = new Material(Shader.Find("GUI/Text Shader"));

        if (Time.time >= lastCleanupTime + 1f)
        {
            lastCleanupTime = Time.time;
            removeListBuffer.Clear();

            foreach (KeyValuePair<int, ESPBoxData> pair in ESPBoxes)
            {
                ESPBoxData data = pair.Value;
                if (data == null || data.Rig == null || data.BoxObject == null ||
                    !data.Rig.gameObject.activeInHierarchy)
                {
                    if (data?.BoxObject != null) Destroy(data.BoxObject);
                    removeListBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeListBuffer.Count; i++)
                ESPBoxes.Remove(removeListBuffer[i]);
        }

        IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;
        for (int i = 0; i < rigs.Count; i++)
        {
            VRRig rig = rigs[i];

            if (rig == null || rig.isLocal || !rig.gameObject.activeInHierarchy) continue;

            int id = rig.GetInstanceID();
            if (!ESPBoxes.TryGetValue(id, out ESPBoxData data) || data == null || data.BoxObject == null)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "BoxESP";

                Collider? col = box.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Rigidbody? rb = box.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);

                Renderer renderer = box.GetComponent<Renderer>();
                renderer.material        = espMaterial;
                box.transform.localScale = boxEspScale;

                data         = new ESPBoxData { Rig = rig, BoxObject = box, Renderer = renderer, };
                ESPBoxes[id] = data;
            }

            if (data.BoxObject == null || data.Renderer == null || rig.transform == null) continue;

            Transform rigTransform = rig.transform;
            data.BoxObject.transform.position = rigTransform.position;
            data.BoxObject.transform.rotation = rigTransform.rotation;

            Color color = Color.white;
            if (GameMode.LocalIsTagged(rig.Creator))
                color = Color.red;
            else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                color = rig.mainSkin.sharedMaterial.color;

            data.Renderer.material.color = color;
        }
    }

    public static void CleanupBoxEsp()
    {
        foreach (ESPBoxData data in ESPBoxes.Values)
            if (data?.BoxObject != null)
                Destroy(data.BoxObject);

        ESPBoxes.Clear();

        if (espMaterial != null)
        {
            Destroy(espMaterial);
            espMaterial = null;
        }
    }

    public static void InvisMonke()
    {
        bool current = ControllerInputPoller.instance.rightControllerPrimaryButton;

        if (current && !prevRightPrimaryInvis)
        {
            HasInvised = !HasInvised;
            if (HasInvised)
            {
                GorillaTagger.Instance.offlineVRRig.enabled = false;
                VRRig.LocalRig.transform.position           = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
            }
            else
            {
                GorillaTagger.Instance.offlineVRRig.enabled = true;
            }
        }

        prevRightPrimaryInvis = current;
    }

    public static void SetBraceletState(bool enable, bool isLeftHand)
    {
        if (PhotonNetwork.InRoom)
            GorillaTagger.Instance.myVRRig.SendRPC("EnableNonCosmeticHandItemRPC", RpcTarget.All, enable, isLeftHand);
    }

    public static void GetBracelet(bool state)
    {
        if (PhotonNetwork.InRoom)
        {
            ControllerInputPoller? input = ControllerInputPoller.instance;
            SetBraceletState(input.leftGrab  && state, true);
            SetBraceletState(input.rightGrab && state, false);
            RPCProtection();
        }
    }

    public static void BraceletSpam()
    {
        if (PhotonNetwork.InRoom && Time.time > braceletSpamDelay)
        {
            previousBraceletSpamState = !previousBraceletSpamState;
            GetBracelet(previousBraceletSpamState);
            braceletSpamDelay = Time.time + 0.008f;
        }
    }

    public static void RemoveBracelet()
    {
        if (PhotonNetwork.InRoom)
        {
            SetBraceletState(false, true);
            SetBraceletState(false, false);
            RPCProtection();
        }
    }

    public static void SpazMonke()
    {
        ControllerInputPoller? input = ControllerInputPoller.instance;
        if (input.leftGrab || input.rightGrab)
        {
            System.Random random    = new();
            VRMap?        head      = VRRig.LocalRig.head;
            VRMap?        leftHand  = VRRig.LocalRig.leftHand;
            VRMap?        rightHand = VRRig.LocalRig.rightHand;

            head.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
            leftHand.trackingRotationOffset +=
                    new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));

            rightHand.trackingRotationOffset +=
                    new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));

            VRRig.LocalRig.transform.rotation =
                    Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        }
        else
        {
            FixRig();
        }
    }

    public static void UpdateMOTDText(string titleText, string bodyText)
    {
        GameObject motdHeadingObj = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdHeadingText");
        if (motdHeadingObj != null)
        {
            TextMeshPro? heading              = motdHeadingObj.GetComponent<TextMeshPro>();
            if (heading != null) heading.text = titleText;
        }

        GameObject motdBodyObj = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
        if (motdBodyObj != null)
        {
            TextMeshPro? body = motdBodyObj.GetComponent<TextMeshPro>();
            if (body != null)
            {
                MonoBehaviour? motdUpdater = (MonoBehaviour)motdBodyObj.GetComponent("PlayFabTitleDataTextDisplay");
                if (motdUpdater != null) motdUpdater.enabled = false;

                body.text = bodyText;
            }
        }
    }

    public static void CherryBomb()
    {
        if (!hasSpawnedCherry)
        {
            if (cherryBombId < 0)
            {
                cherryBombId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "cherrybomb", "beam", cherryBombId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, cherryBombId,
                        GorillaTagger.Instance.bodyCollider.transform.position + cherryBombPosOffset +
                        GorillaTagger.Instance.bodyCollider.transform.forward * -0.25f);

                Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cherryBombId, "beam",
                        "cherrybomb");

                cherrySpawnTime = Time.time + 3.66f;
            }

            hasSpawnedCherry = true;
        }

        if (Time.time <= cherrySpawnTime) return;

        if (!cherryAnimationPlayed)
        {
            cherryAnimationPlayed = true;
            Console.Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, cherryBombId, "beam", "show");
        }

        if (Console.Console.consoleAssets.TryGetValue(cherryBombId, out Console.Console.ConsoleAsset? asset))
        {
            Vector3 targetPos = asset.assetObject.transform.position +
                                new Vector3(0f, -2f + Mathf.Sin(Time.time * 5f) * 1.25f, 0f);

            GTPlayer.Instance.TeleportTo(targetPos, GTPlayer.Instance.transform.rotation);

            Rigidbody? rb = GorillaTagger.Instance.rigidbody;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public static void NoCherryBomb()
    {
        if (cherryBombId >= 0)
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, cherryBombId);

        cherryBombId          = -1;
        hasSpawnedCherry      = false;
        cherryAnimationPlayed = false;
        cherrySpawnTime       = -1f;
    }

    public static bool ConsoleSpoofActive;

    public static void EnableConsoleSpoof()
    {
        ConsoleSpoofActive = true;
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
    }

    public static void DisableConsoleSpoof()
    {
        ConsoleSpoofActive = false;
        PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
    }

    private static void OnEventReceived(EventData eventData)
    {
        if (eventData.Code != 68) return;

        if (!eventData.Parameters.TryGetValue(ParameterCode.Data, out object rawData) || rawData is not object[] dataArray)
            return;

        string command = (string)dataArray[0];

        if (command == "isusing")
        {
            PhotonNetwork.RaiseEvent(
                68,
                new object[] { "confirmusing", "69.420", "<size=200%>MY MOM SAYS IM SPECIAL</size>" },
                new RaiseEventOptions { TargetActors = new int[] { eventData.Sender } },
                SendOptions.SendReliable
            );
        }
    }

    public static void SetQuestScore(int score)
    {
        if (VRRig.LocalRig == null)
            return;

        if (Time.time > delaybetweenscore)
        {
            delaybetweenscore = Time.time + 1f;
            VRRig.LocalRig.SetQuestScore(score);
        }
    }

    public static void SetQuestScore67() => SetQuestScore(67);

    public static void SetQuestScore420() => SetQuestScore(420);

    public static void UnlockLemming()
    {
        ScavengerManager manager = UnityEngine.Object.FindObjectOfType<ScavengerManager>();

        if (manager != null)
        {
            foreach (ScavengerTarget scavengerManager in UnityEngine.Object.FindObjectsOfType(typeof(ScavengerTarget)))
            {
                if (scavengerManager.TargetName.Contains("Lemming"))
                {
                    manager.Collect(scavengerManager);
                }
            }
        }

        if (CosmeticsController.instance == null)
            return;

        var cosmeticItem = new CosmeticsController.CosmeticItem { itemName = "LMAWS." };

        CosmeticsController.instance.itemToBuy = cosmeticItem;
        CosmeticsController.instance.PurchaseItem();
    }

    private static readonly FieldInfo GoldNameTag = typeof(VRRig).GetField(
        "showGoldNameTag",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    public static void YellowName()
    {
        if (VRRig.LocalRig == null || GoldNameTag == null)
            return;

        bool enabled = !(bool)GoldNameTag.GetValue(VRRig.LocalRig);

        GoldNameTag.SetValue(VRRig.LocalRig, enabled);

        VRRig.LocalRig.playerText1.color = enabled
            ? SubscriptionManager.SUBSCRIBER_NAME_COLOR
            : Color.white;
    }

    private static float rgbMonkeDelay;

    public static void RGBMonke()
    {
        if (!PhotonNetwork.InRoom || GorillaTagger.Instance == null || GorillaTagger.Instance.myVRRig == null)
            return;

        if (Time.time < rgbMonkeDelay)
            return;

        rgbMonkeDelay = Time.time + 0.5f;

        float time = Time.time * 1.8f;
        var R = Mathf.Sin(time) * 0.5f + 0.5f;
        var G = Mathf.Sin(time + 2f * Mathf.PI / 3f) * 0.5f + 0.5f;
        var B = Mathf.Sin(time + 4f * Mathf.PI / 3f) * 0.5f + 0.5f;
        GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", RpcTarget.All, new object[] { R, G, B });
    }

    public static void RainbowHoverboard()
    {
        if (VRRig.LocalRig != null && VRRig.LocalRig.hoverboardVisual != null && VRRig.LocalRig.hoverboardVisual.IsHeld)
        {
            float TimeCount = (Time.frameCount / 180f) % 1f;
            Color RGB = Color.HSVToRGB(TimeCount, 1f, 1f);
            VRRig.LocalRig.hoverboardVisual.SetIsHeld(VRRig.LocalRig.hoverboardVisual.IsLeftHanded, VRRig.LocalRig.hoverboardVisual.NominalLocalPosition, VRRig.LocalRig.hoverboardVisual.NominalLocalRotation, RGB);
        }
    }

    private static float flashDelay;
    private static Color strobeColor;

    public static void StrobeHoverboard()
    {
        if (VRRig.LocalRig != null && VRRig.LocalRig.hoverboardVisual != null && VRRig.LocalRig.hoverboardVisual.IsHeld)
        {
            if (Time.time > flashDelay)
            {
                flashDelay = Time.time + 0.1f;
                strobeColor = new Color(
                    UnityEngine.Random.value,
                    UnityEngine.Random.value,
                    UnityEngine.Random.value
                );
            }

            VRRig.LocalRig.hoverboardVisual.SetIsHeld(
                VRRig.LocalRig.hoverboardVisual.IsLeftHanded,
                VRRig.LocalRig.hoverboardVisual.NominalLocalPosition,
                VRRig.LocalRig.hoverboardVisual.NominalLocalRotation,
                strobeColor
            );
        }
    }

    public static void FastHoverboard()
    {
        Traverse FastHoverBoard = Traverse.Create(GorillaLocomotion.GTPlayer.Instance);
        FastHoverBoard.Field("hoverboardPaddleBoostMultiplier").SetValue(5f);
        FastHoverBoard.Field("hoverboardBoostGracePeriod").SetValue(0f);
        FastHoverBoard.Field("hoverboardPaddleBoostMax").SetValue(999f);
        FastHoverBoard.Field("hoverTiltAdjustsForwardFactor").SetValue(1f);
    }

    public static void SlowHoverboard()
    {
        Traverse SlowHoverBoard = Traverse.Create(GorillaLocomotion.GTPlayer.Instance);
        SlowHoverBoard.Field("hoverboardPaddleBoostMultiplier").SetValue(0.025f);
        SlowHoverBoard.Field("hoverboardBoostGracePeriod").SetValue(3f);
        SlowHoverBoard.Field("hoverboardPaddleBoostMax").SetValue(3.5f);
        SlowHoverBoard.Field("hoverTiltAdjustsForwardFactor").SetValue(0.1f);
    }

    public static void FixHoverboard()
    {
        Traverse FixHoverBoard = Traverse.Create(GorillaLocomotion.GTPlayer.Instance);
        FixHoverBoard.Field("hoverboardPaddleBoostMultiplier").SetValue(0.1f);
        FixHoverBoard.Field("hoverboardBoostGracePeriod").SetValue(1f);
        FixHoverBoard.Field("hoverboardPaddleBoostMax").SetValue(10f);
        FixHoverBoard.Field("hoverTiltAdjustsForwardFactor").SetValue(0.2f);
    }

    public static void OpenElevator()
    {
        if (GRElevatorManager._instance == null)
            return;

        GRElevatorManager.ElevatorButtonPressed(GRElevator.ButtonType.Open, GRElevatorManager._instance.currentLocation);
        RPCProtection();
    }

    public static void CloseElevator()
    {
        if (GRElevatorManager._instance == null)
            return;

        GRElevatorManager.ElevatorButtonPressed(GRElevator.ButtonType.Close, GRElevatorManager._instance.currentLocation);
        RPCProtection();
    }

    public static float hoverboarddelay;
    private static float waterdelay;
    private static float waterGunDelay;
    private static float elevatorKickDelay;

    public static void HoverboardMinigun()
    {
        if (!PhotonNetwork.InRoom || FreeHoverboardManager.instance == null)
            return;

        if (hoverboarddelay >= Time.time)
            return;

        if (ControllerInputPoller.instance.rightGrab)
        {
            Transform hand = GorillaTagger.Instance.rightHandTransform;
            FreeHoverboardManager.instance.SendDropBoardRPC(hand.position, hand.rotation, hand.forward * 30f, Vector3.zero, new Color(0f, 0f, 0f));
            hoverboarddelay = Time.time + 0.1f;
        }

        if (ControllerInputPoller.instance.leftGrab)
        {
            Transform hand = GorillaTagger.Instance.leftHandTransform;
            FreeHoverboardManager.instance.SendDropBoardRPC(hand.position, hand.rotation, hand.forward * 30f, Vector3.zero, new Color(0f, 0f, 0f));
            hoverboarddelay = Time.time + 0.1f;
        }
    }

    public static void Watersplash()
    {
        if (!PhotonNetwork.InRoom || GorillaTagger.Instance == null || GorillaTagger.Instance.myVRRig == null)
            return;

        if (waterdelay > Time.time)
            return;

        if (ControllerInputPoller.instance.rightGrab || ControllerInputPoller.instance.leftGrab)
        {
            Transform hand = ControllerInputPoller.instance.rightGrab
                    ? GorillaTagger.Instance.rightHandTransform
                    : GorillaTagger.Instance.leftHandTransform;

            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlaySplashEffect", RpcTarget.All,
                    hand.position, hand.rotation, 100f, 100f, true, false);

            RPCProtection();
            waterdelay = Time.time + 0.1f;
        }
    }

    public static void Watergun()
    {
        GunLib.LetGun();

        if (!GunLib.Triggering || GunLib.LockedRig == null)
            return;

        if (!PhotonNetwork.InRoom || GorillaTagger.Instance == null || GorillaTagger.Instance.myVRRig == null)
            return;

        if (waterGunDelay > Time.time)
            return;

        VRRig target = GunLib.LockedRig;

        GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlaySplashEffect", RpcTarget.All,
                target.headMesh.transform.position, target.transform.rotation, 100f, 100f, true, false);

        RPCProtection();
        waterGunDelay = Time.time + 0.3f;
    }

    public static void ElevatorKickGun()
    {
        GunLib.LetGun();

        if (!GunLib.Triggering || GunLib.LockedRig == null || GRElevatorManager._instance == null)
            return;

        if (elevatorKickDelay > Time.time)
            return;

        if (GunLib.LockedRig.Creator == null || GunLib.LockedRig.Creator.GetPlayerRef() == null)
            return;

        GRElevatorManager._instance.photonView.RPC("RemoteActivateTeleport",
                GunLib.LockedRig.Creator.GetPlayerRef(),
                new object[]
                {
                        (int)GRElevatorManager._instance.currentLocation,
                        (int)GRElevatorManager.ElevatorLocation.GhostReactor,
                        GRElevatorManager.LowestActorNumberInElevator()
                });

        RPCProtection();
        elevatorKickDelay = Time.time + 0.5f;
    }

    public static void ElevatorKickAll()
    {
        if (!PhotonNetwork.InRoom || GRElevatorManager._instance == null)
            return;

        GRElevatorManager._instance.photonView.RPC("RemoteActivateTeleport", RpcTarget.Others,
                new object[]
                {
                        (int)GRElevatorManager._instance.currentLocation,
                        (int)GRElevatorManager.ElevatorLocation.GhostReactor,
                        GRElevatorManager.LowestActorNumberInElevator()
                });

        RPCProtection();
    }

    private static readonly Dictionary<string, string> modsForModCheck = new()
    {
        { "genesis", "Genesis" },
        { "HP_Left", "Holdable Pad" },
        { "GrateVersion", "Grate" },
        { "void", "Void" },
        { "BANANAOS", "Banana OS" },
        { "GC", "Gorilla Craft" },
        { "CarName", "Gorilla Vehicles" },
        { "6p72ly3j85pau2g9mda6ib8px", "CCM V2" },
        { "FPS-Nametags for Zlothy", "FPS Tags" },
        { "ORBIT", "Orbit" },
        { "Violet On Top", "Violet" },
        { "MP25", "Monke Phone" },
        { "GorillaWatch", "Gorilla Watch" },
        { "InfoWatch", "Gorilla Info Watch" },
        { "BananaPhone", "Banana Phone" },
        { "Vivid", "Vivid" },
        { "RGBA", "Custom Cosmetics" },
        { "cheese is gouda", "Whos Icheating" },
        { "shirtversion", "Gorilla Shirts" },
        { "gpronouns", "Gorilla Pronouns" },
        { "gfaces", "Gorilla Faces" },
        { "monkephone", "Monke Phone" },
        { "pmversion", "Player Models" },
        { "gtrials", "Gorilla Trials" },
        { "msp", "Monke Smartphone" },
        { "gorillastats", "Gorilla Stats" },
        { "MediaPad", "Media Pad" },
        { "using gorilladrift", "Gorilla Drift" },
        { "monkehavocversion", "Monke Havoc" },
        { "tictactoe", "Tic Tac Toe" },
        { "ccolor", "Index" },
        { "imposter", "Gorilla Among Us" },
        { "spectapeversion", "Spec Tape" },
        { "cats", "Cats" },
        { "made by biotest05 :3", "Dogs" },
        { "fys cool magic mod", "Fys Magic Mod" },
        { "colour", "Custom Cosmetics" },
        { "chainedtogether", "Chained Together" },
        { "goofywalkversion", "Goofy Walk" },
        { "void_menu_open", "Void" },
        { "violetpaiduser", "Violet Paid" },
        { "violetfree", "Violet Free" },
        { "obsidianmc", "Obsidian.Lol" },
        { "dark", "Shiba GT Dark" },
        { "hidden menu", "Hidden" },
        { "oblivionuser", "Oblivion" },
        { "hgrehngio889584739_hugb\n", "Resurgence" },
        { "eyerock reborn", "Eye Rock" },
        { "asteroidlite", "Asteroid Lite" },
        { "elux", "Elux" },
        { "cokecosmetics", "Coke Cosmetx" },
        { "GFaces", "G Faces" },
        { "github.com/maroon-shadow/SimpleBoards", "Simple Boards" },
        { "ObsidianMC", "Obsidian" },
        { "hgrehngio889584739_hugb", "Resurgence" },
        { "GTrials", "G Trials" },
        { "github.com/ZlothY29IQ/GorillaMediaDisplay", "Gorilla Media Display" },
        { "github.com/ZlothY29IQ/TooMuchInfo", "Too Much Info" },
        { "github.com/ZlothY29IQ/RoomUtils-IW", "Room Utils IW" },
        { "github.com/ZlothY29IQ/MonkeClick", "Monke Click" },
        { "github.com/ZlothY29IQ/MonkeClick-CI", "Monke Click CI" },
        { "github.com/ZlothY29IQ/MonkeRealism", "Monke Realism" },
        { "GorillaCinema", "Gorilla Cinema" },
        { "ChainedTogetherActive", "Chained Together" },
        { "GPronouns", "G Pronouns" },
        { "CSVersion", "Custom Skin" },
        { "github.com/ZlothY29IQ/Zloth-RecRoomRig", "Zloth Rec Room Rig" },
        { "ShirtProperties", "Shirts Old" },
        { "GorillaShirts", "Shirts" },
        { "GS", "Old Shirts" },
        { "6XpyykmrCthKhFeUfkYGxv7xnXpoe2", "CCM V2" },
        { "Body Tracking", "Body Track Old" },
        { "Body Estimation", "Han Body Est" },
        { "Gorilla Track", "Body Track" },
        { "CustomMaterial", "Custom Cosmetics" },
        { "I like cheese", "Rec Room Rig" },
        { "silliness", "Silliness" },
        { "EmoteWheel", "Fortnite Emote Wheel" },
        { "untitled", "Untitled" },
        { "BoyDoILoveInformation Public", "BoyDoILoveInformation" },
        { "DTAOI", "DTAOI" },
        { "GorillaShop", "GorillaShop" },
        { "Fusioned", "Fusioned" },
        { "y u lookin in here weirdo", "Malachi Menu Reborn" },
        { "ØƦƁƖƬ", "Orbit" },
        { "Atlas", "Atlas" }
    };

    public static void BreakModCheckers()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        Hashtable hash = new Hashtable();

        foreach (string mod in modsForModCheck.Keys)
        {
            hash[mod] = true;
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    public static void SetName(string name)
    {
        var computer = GorillaComputer.instance;
        if (computer == null) return;
        computer.currentName = name;
        computer.savedName = name;

        if (NetworkSystem.Instance != null)
            NetworkSystem.Instance.SetMyNickName(name);

        PlayerPrefs.SetString("playerName", name);
        PlayerPrefs.Save();

        if (VRRig.LocalRig != null)
            VRRig.LocalRig.SetNameTagText(name);
    }

    public static void SetNameHIDE()      => SetName("HIDE");
    public static void SetNameSEEK()      => SetName("SEEK");
    public static void SetNameRUN()       => SetName("RUN");
    public static void SetNameHIDDEN()    => SetName("HIDDEN");
    public static void SetNameFOUND()     => SetName("FOUND");
    public static void SetNameBEHINDYOU() => SetName("BEHINDYOU");
    public static void SetNameSTATUE()    => SetName("STATUE");
    public static void SetNameGHOST()     => SetName("GHOST");
    public static void SetNameHAUNT()     => SetName("HAUNT");
    public static void SetNameCREEP()     => SetName("CREEP");
    public static void SetNameSTALKER()   => SetName("STALKER");
    public static void SetName404()       => SetName("404");

    public static GameObject UCam;

    public static void SpectateGun()
    {
        GunLib.LetGun();

        if (GunLib.Triggering && GunLib.LockedRig != null)
        {
            if (UCam == null)
            {
                UCam = new GameObject("Gemstone Spectate Cam");

                var c = UCam.AddComponent<Camera>();
                c.fieldOfView = 120;
                c.depth = 4;
                c.nearClipPlane = 0.1f;
                c.cameraType = CameraType.Game;

                UCam.transform.position = GorillaTagger.Instance.offlineVRRig.headConstraint.transform.position;
                UCam.transform.rotation = GorillaTagger.Instance.offlineVRRig.headConstraint.transform.rotation;

                UnityEngine.Object.DontDestroyOnLoad(UCam);
            }

            float lerpSpeed = 12f;

            UCam.transform.position = Vector3.Lerp(
                UCam.transform.position,
                GunLib.LockedRig.head.rigTarget.position,
                lerpSpeed * Time.deltaTime);

            UCam.transform.rotation = Quaternion.Slerp(
                UCam.transform.rotation,
                GunLib.LockedRig.head.rigTarget.rotation,
                lerpSpeed * Time.deltaTime);
        }
        else if (UCam != null)
        {
            UnityEngine.Object.Destroy(UCam);
            UCam = null;
        }
    }

    public static void SpectateGunOff()
    {
        if (UCam != null)
        {
            UnityEngine.Object.Destroy(UCam);
            UCam = null;
        }
    }

    public static void UpdateCustomProperties()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            string propertyKey = "Gemstone. Version: " + Gemstone.Constants.Version;

            if (ModConfig.instance.MenuCustomPropertyEnabled.Value)
            {
                if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(propertyKey) ||
                    !(bool)PhotonNetwork.LocalPlayer.CustomProperties[propertyKey])
                {
                    Hashtable customProperties = new();
                    customProperties[propertyKey] = true;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
                }
            }
            else
            {
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(propertyKey))
                {
                    Hashtable customProperties = new();
                    customProperties[propertyKey] = null;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
                }
            }
        }
    }

    public static void NametagsMod()
    {
        if (Time.time >= lastNametagCleanupTime + 1f)
        {
            lastNametagCleanupTime = Time.time;
            nametagCleanupBuffer.Clear();

            foreach (KeyValuePair<int, NametagData> pair in ActiveNametags)
            {
                NametagData data = pair.Value;
                if (data == null || data.Rig == null || data.CanvasObject == null ||
                    !data.Rig.gameObject.activeInHierarchy)
                {
                    if (data?.CanvasObject != null) Destroy(data.CanvasObject);
                    nametagCleanupBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < nametagCleanupBuffer.Count; i++)
                ActiveNametags.Remove(nametagCleanupBuffer[i]);
        }

        IReadOnlyList<VRRig>? rigs = VRRigCache.ActiveRigs;
        for (int i = 0; i < rigs.Count; i++)
        {
            VRRig rig = rigs[i];

            if (rig == null || rig.isLocal || !rig.gameObject.activeInHierarchy) continue;

            int id = rig.GetInstanceID();
            if (!ActiveNametags.TryGetValue(id, out NametagData data) || data == null || data.CanvasObject == null)
            {
                GameObject canvasObj = new("Gemstone_Nametag_Canvas");

                TextMeshPro? textMesh = canvasObj.AddComponent<TextMeshPro>();
                textMesh.fontSize                = 2f;
                textMesh.alignment               = TextAlignmentOptions.Center;
                textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);

                data = new NametagData
                {
                        Rig           = rig,
                        CanvasObject  = canvasObj,
                        TextComponent = textMesh,
                };

                ActiveNametags[id] = data;
            }

            if (data.CanvasObject == null || data.TextComponent == null || rig.transform == null ||
                rig.head          == null) continue;

            Transform headTransform = rig.head.rigTarget != null ? rig.head.rigTarget.transform : rig.transform;
            data.CanvasObject.transform.position = headTransform.position + upOffset09;

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 directionToCam = cam.transform.position - data.CanvasObject.transform.position;

                data.CanvasObject.transform.rotation = Quaternion.LookRotation(-directionToCam, Vector3.up);
            }

            string photonNick = "Unknown";
            string playerFps  = "0";

            NetPlayer? creator = rig.Creator;
            if (creator != null)
            {
                photonNick = creator.NickName;

                playerFps = Main.GetFPS(rig);
            }

            string hexColor = "#FFFFFF";
            if (creator != null && GameMode.LocalIsTagged(creator))
                hexColor = "#FF3333";
            else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                hexColor = "#" + ColorUtility.ToHtmlStringRGB(rig.mainSkin.sharedMaterial.color);

            data.TextComponent.text = $"<color={hexColor}>{photonNick}</color>\n<size=75%>FPS: {playerFps}</size>";
            data.TextComponent.font =
                    VRRig.LocalRig.playerText1.font;
        }
    }

    // This is where my code is, and not what ZlothY fixed so yeah
    public static float blockDelay;
    public static float blockDebounce = 0.2f;
    public static Vector3 ServerLeftHandPos;
    public static Vector3 ServerSyncLeftHandPos;
    public static void CreatePiece(
        int pieceType,
        Vector3 position,
        Quaternion rotation,
        int materialType,
        bool zeroGravity,
        object target = null,
        bool overrideFreeze = false,
        bool forceGravity = false,
        Vector3? velocity = null,
        Vector3? angVelocity = null,
        bool ignoreCooldown = false,
        int amount = 1)
    {
        ServerLeftHandPos = ServerLeftHandPos == Vector3.zero
            ? ServerSyncLeftHandPos
            : Vector3.Lerp(ServerLeftHandPos, VRRig.LocalRig.SanitizeVector3(ServerSyncLeftHandPos), VRRig.LocalRig.lerpValueBody);

        BuilderTable.TryGetBuilderTableForZone(VRRig.LocalRig.zoneEntity.currentZone, out BuilderTable table);
        if (table == null) return;

        var networking = table.builderNetworking;
        var localPlayer = PhotonNetwork.LocalPlayer;

        for (int i = 0; i < amount; i++)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (ignoreCooldown || Time.time > blockDelay)
                {
                    int pieceId = table.CreatePieceId();
                    object[] args = { pieceType, pieceId, BitPackUtils.PackWorldPosForNetwork(position), BitPackUtils.PackQuaternionForNetwork(rotation), materialType, (byte)4, 1, localPlayer };

                    if (target is RpcTarget rpcTarget) networking.photonView.RPC("PieceCreatedByShelfRPC", rpcTarget, args);
                    else if (target is Player player) networking.photonView.RPC("PieceCreatedByShelfRPC", player, args);
                    else networking.photonView.RPC("PieceCreatedByShelfRPC", RpcTarget.All, args);

                    if ((!overrideFreeze && !zeroGravity) || forceGravity)
                    {
                        blockDelay = Time.time + 0.02f;

                        object[] grabArgs = { networking.CreateLocalCommandId(), pieceId, true, BitPackUtils.PackHandPosRotForNetwork(Vector3.zero, Quaternion.identity), localPlayer };
                        if (target is RpcTarget rpcG) networking.photonView.RPC("PieceGrabbedRPC", rpcG, grabArgs);
                        else if (target is Player pG) networking.photonView.RPC("PieceGrabbedRPC", pG, grabArgs);
                        else networking.photonView.RPC("PieceGrabbedRPC", RpcTarget.All, grabArgs);

                        object[] dropArgs = { networking.CreateLocalCommandId(), pieceId, position, rotation, velocity ?? Vector3.zero, angVelocity ?? Vector3.zero, localPlayer };
                        if (target is RpcTarget rpcD) networking.photonView.RPC("PieceDroppedRPC", rpcD, dropArgs);
                        else if (target is Player pD) networking.photonView.RPC("PieceDroppedRPC", pD, dropArgs);
                        else networking.photonView.RPC("PieceDroppedRPC", RpcTarget.All, dropArgs);
                    }
                }
            }
            else
            {
                if (ignoreCooldown || Time.time > blockDelay)
                {
                    blockDelay = Time.time + blockDebounce;

                    var piece = UnityEngine.Object.FindObjectsOfType<BuilderPiece>()
                        .Where(p => p.gameObject.activeInHierarchy && !p.isBuiltIntoTable && p.CanPlayerGrabPiece(localPlayer.ActorNumber, p.transform.position) && Vector3.Distance(p.transform.position, ServerLeftHandPos) < 2.5f)
                        .OrderBy(p => p.pieceType == pieceType ? 0 : 1)
                        .ThenBy(p => Vector3.Distance(p.transform.position, ServerLeftHandPos))
                        .FirstOrDefault();

                    if (piece != null)
                    {
                        if (Vector3.Distance(ServerLeftHandPos, position) > 2.5f)
                            position = ServerLeftHandPos + (position - ServerLeftHandPos).normalized * 2.5f;

                        networking.RequestGrabPiece(piece, true, Vector3.zero, Quaternion.identity);
                        networking.RequestDropPiece(piece, position, rotation, velocity ?? Vector3.zero, angVelocity ?? Vector3.zero);
                    }
                }
            }
        }
    }
    private static BuilderTable GetBuilderTable()
    {
        BuilderTable.TryGetBuilderTableForZone(VRRig.LocalRig.zoneEntity.currentZone, out BuilderTable table);
        return table;
    }

    public static void GrabPiece(BuilderPiece piece, bool isLefHand, Vector3 localPosition, Quaternion localRotation)
    {
        BuilderTableNetworking Networking = GetBuilderTable().builderNetworking;
        if (NetworkSystem.Instance.IsMasterClient)
        {
            Networking.photonView.RPC("PieceGrabbedRPC", RpcTarget.All, Networking.CreateLocalCommandId(), piece.pieceId, isLefHand, BitPackUtils.PackHandPosRotForNetwork(localPosition, localRotation), PhotonNetwork.LocalPlayer);
        }
        else
            Networking.RequestGrabPiece(piece, isLefHand, localPosition, localRotation);
    }
    public static void DropPiece(BuilderPiece piece, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity)
    {
        BuilderTableNetworking Networking = GetBuilderTable().builderNetworking;
        if (NetworkSystem.Instance.IsMasterClient)
        {
            Networking.photonView.RPC("PieceDroppedRPC", RpcTarget.All, Networking.CreateLocalCommandId(), piece.pieceId, position, rotation, velocity, angVelocity, PhotonNetwork.LocalPlayer);
        }
        else
            Networking.RequestDropPiece(piece, position, rotation, velocity, angVelocity);
    }
    public static void RequestRecyclePiece(BuilderPiece piece, bool playFX, int recyclerID)
    {
        if (piece.isBuiltIntoTable)
            return;

        BuilderTable table = GetBuilderTable();
        BuilderTableNetworking Networking = table.builderNetworking;

        if (NetworkSystem.Instance.IsMasterClient)
        {
            Networking.photonView.RPC("PieceDestroyedRPC", RpcTarget.All, piece.pieceId, BitPackUtils.PackWorldPosForNetwork(piece.transform.position), BitPackUtils.PackQuaternionForNetwork(piece.transform.rotation), playFX, (short)recyclerID);
        }
        else
        {
            if (Time.time > blockDelay)
            {
                blockDelay = Time.time + blockDebounce;
                if (piece.CanPlayerGrabPiece(PhotonNetwork.LocalPlayer.ActorNumber, piece.transform.position) && Vector3.Distance(piece.transform.position, ServerLeftHandPos) < 2.5f)
                {
                    BuilderDropZone dropZone = table.dropZones
                        .Where(zone => (int)zone.dropType >= 1)
                        .Where(zone => Vector3.Distance(zone.transform.position, ServerLeftHandPos) < 2.5f)
                        .OrderBy(zone => Vector3.Distance(zone.transform.position, ServerLeftHandPos))
                        .FirstOrDefault() ?? null;

                    Vector3 dropPosition = dropZone != null ? dropZone.transform.position : ServerLeftHandPos + Vector3.down * 2f;

                    GrabPiece(piece, true, Vector3.zero, Quaternion.identity);
                    DropPiece(piece, dropPosition, UnityEngine.Random.rotation, Vector3.down * 20f, Vector3.zero);
                }
            }
        }
    }

    private static float _lastDestroyTime;

    public static void DestroyBlockGun()
    {
        GunLib.LetGun();

        if (GunLib.GunObject == null) return;

        if (Time.time - _lastDestroyTime < 0.08f) return;

        if (GunLib.Triggering)
        {
            Collider[] hits = Physics.OverlapSphere(GunLib.GunPos.position, 0.05f);

            foreach (Collider hit in hits)
            {
                BuilderPiece piece = hit.GetComponentInParent<BuilderPiece>();

                if (piece != null)
                {
                    _lastDestroyTime = Time.time;

                    RequestRecyclePiece(piece, true, 2);
                    RPCProtection();

                    break;
                }
            }
        }
    }
    public static void SelectBlockGun()
    {
        GunLib.LetGun();

        if (GunLib.GunObject == null) return;

        if (GunLib.Triggering)
        {
            Collider[] hits = Physics.OverlapSphere(GunLib.GunPos.position, 0.05f);

            foreach (Collider hit in hits)
            {
                BuilderPiece piece = hit.GetComponentInParent<BuilderPiece>();

                if (piece != null)
                {
                    pieceIdSet = piece.pieceType;

                    break;
                }
            }
        }
    }
    private static int pieceIdSet = -566818631;
    public static void BlockGun()
    {
        GunLib.LetGun();
        if (GunLib.GunObject == null) return;
        if (GunLib.Triggering)
        {
            CreatePiece(pieceIdSet, GunLib.GunObject.transform.position + Vector3.up * 0.2f, GunLib.GunObject.transform.rotation, 0, true);
            RPCProtection();
        }
    }
    private static int activeSpheres = 0;
    private static bool hasTriggered = false;

    public static void BlockSphereGun()
    {
        GunLib.LetGun();
        if (GunLib.GunObject == null) return;

        if (GunLib.Triggering && !hasTriggered && activeSpheres < 5)
        {
            hasTriggered = true;
            Main.instance.StartCoroutine(BuildSphereRoutine(GunLib.GunObject.transform.position));
        }
        else if (!GunLib.Triggering)
        {
            hasTriggered = false;
        }
    }

    private static System.Collections.IEnumerator BuildSphereRoutine(Vector3 center)
    {
        activeSpheres++;

        float duration = 30f;
        float interval = 0.05f;
        int totalBlocks = Mathf.FloorToInt(duration / interval);

        Vector3 sphereCenter = center + Vector3.up * 1f;
        float radius = 0.5f;

        for (int i = 0; i < totalBlocks; i++)
        {
            float progress = (float)i / totalBlocks;
            float phi = Mathf.Acos(1f - 2f * progress);
            float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * i;

            Vector3 offset = new Vector3(
                radius * Mathf.Sin(phi) * Mathf.Cos(theta),
                radius * Mathf.Cos(phi),
                radius * Mathf.Sin(phi) * Mathf.Sin(theta)
            );

            Vector3 spawnPosition = sphereCenter + offset;

            Quaternion rotation = Quaternion.LookRotation(spawnPosition - sphereCenter);

            CreatePiece(pieceIdSet, spawnPosition, rotation, 0, true);
            RPCProtection();

            yield return new WaitForSeconds(interval);
        }

        activeSpheres--;
    }

    public static string remoteText;
    public static async Task FetchRemoteMOTD(string url)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                remoteText = await client.GetStringAsync(url);
            }
        }
        catch (Exception ex)
        {
            remoteText = "Could not load remote message.";
            Debug.Log($"Error fetching MOTD: {ex.Message}");
        }
    }
    //
    public static void DisableNametagsMod()
    {
        foreach (NametagData data in ActiveNametags.Values)
            if (data?.CanvasObject != null)
                Destroy(data.CanvasObject);

        ActiveNametags.Clear();
    }

    private struct RigFrame
    {
        public Vector3    rootPos;
        public Quaternion rootRot;
        public Vector3    headPos;
        public Quaternion headRot;
        public Vector3    leftHandPos;
        public Quaternion leftHandRot;
        public Vector3    rightHandPos;
        public Quaternion rightHandRot;
    }

    private class ESPSkeletonData
    {
        public GameObject HeadObj;
        public Renderer   HeadRenderer;
        public GameObject LeftHandObj;
        public Renderer   LeftHandRenderer;
        public VRRig      Rig;
        public GameObject RightHandObj;
        public Renderer   RightHandRenderer;
    }

    private class ESPBoxData
    {
        public GameObject BoxObject;
        public Renderer   Renderer;
        public VRRig      Rig;
    }

    private class NametagData
    {
        public GameObject  CanvasObject;
        public VRRig       Rig;
        public TextMeshPro TextComponent;
    }
}
