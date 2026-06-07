using BepInEx;
using Gemstone.Gemstone;
using Gemstone.patches;
using GorillaGameModes;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using static Bindings;
using Object = UnityEngine.Object;

namespace Gemstone.Mods
{
    public class Mods : MonoBehaviour
    {
        public static bool HasGhostMonked = false;
        private static bool prevRightPrimary = false;

        public static Mods instance;

        private static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
        private static readonly WaitForSeconds beeDelay = new WaitForSeconds(0.3f);

        private static readonly Vector3 sphereScaleHand = new Vector3(0.1f, 0.1f, 0.1f);
        private static readonly Vector3 sphereScaleHead = new Vector3(0.2f, 0.2f, 0.2f);
        private static readonly Vector3 platScale = new Vector3(0.03f, 0.3f, 0.45f);
        private static readonly Vector3 boxEspScale = new Vector3(0.2f, 0.4f, 0.2f);
        private static readonly Vector3 upOffset02 = new Vector3(0, 0.2f, 0);
        private static readonly Vector3 upOffset07 = new Vector3(0, 0.7f, 0);
        private static readonly Vector3 upOffset08 = new Vector3(0, 0.8f, 0);
        private static readonly Vector3 cherryBombPosOffset = new Vector3(0f, 9.5f, 0f);
        private static readonly Vector3 upOffset2 = new Vector3(0, 2, 0);
        private static readonly Vector3 upOffset09 = new Vector3(0f, 0.9f, 0f);
        private static readonly Vector3 scale03 = new Vector3(0.3f, 0.3f, 0.3f);

        void Awake()
        {
            instance = this;
            InitializeLayerMasks();
        }

        public static bool HasCreated = false;

        public static bool IsLeftPlat = false;
        public static bool IsRightPlat = false;
        public static GameObject LeftPlat;
        public static GameObject RightPlat;

        public static GameObject LeftS;
        public static GameObject RightS;
        public static GameObject HeadS;

        public static void SpeedBoost()
        {
            var player = GTPlayer.Instance;
            player.maxJumpSpeed = 8f;
            player.jumpMultiplier = 5.3f;
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

                    var rendL = LeftS.GetComponent<Renderer>();
                    rendL.material.shader = uberShader;
                    if (!ModConfig.instance.IsMenuRGB.Value)
                        rendL.material.color = themeColor;
                    else
                        Main.instance.StartCoroutine(Main.instance.RGBTheme(rendL));

                        Destroy(LeftS.GetComponent<Rigidbody>());
                    Destroy(LeftS.GetComponent<Collider>());

                    // right hand
                    RightS = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    RightS.transform.parent =  GTPlayer.Instance.RightHand.handFollower.transform;
                    RightS.transform.localPosition = Vector3.zero;
                    RightS.transform.localRotation = Quaternion.identity;
                    RightS.transform.localScale = sphereScaleHand;

                    var rendR = RightS.GetComponent<Renderer>();
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

                    var rendH = HeadS.GetComponent<Renderer>();
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
                GTPlayer.Instance.transform.position += GTPlayer.Instance.headCollider.transform.forward * ModConfig.instance.FlySpeedSave.Value;

                var rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
        }
        private const float MouseSensitivity = 0.08f;

        public static bool DisableMovement;
        private static readonly Dictionary<Transform, Vector3> LastHandPositions = new Dictionary<Transform, Vector3>();
        private static bool hasTouchedWithHand = false;
        private static bool isJumping = false;

        private static float jumpCooldownTime = 0f;
        private static SphereCollider _probeCollider;
        public static void WasdFly()
        {
            Rigidbody rigidbody = GorillaTagger.Instance.rigidbody;
            Transform body = rigidbody.transform;
            Transform head = GorillaTagger.Instance.headCollider.transform;

            Transform leftHand = GorillaTagger.Instance.leftHandTransform;
            Transform rightHand = GorillaTagger.Instance.rightHandTransform;

            leftHand.localPosition += Vector3.down * 0.5f;
            rightHand.localPosition += Vector3.down * 0.5f;

            leftHand.localPosition -= Vector3.right * 0.2f;
            rightHand.localPosition -= Vector3.left * 0.2f;

            leftHand.localRotation = Quaternion.Euler(40f, 0f, 0f);
            rightHand.localRotation = Quaternion.Euler(40f, 0f, 0f);

            if (UnityInput.Current.GetKey(KeyCode.Q))
            {
                leftHand.localPosition += Vector3.forward * 0.2f;
                leftHand.localPosition += Vector3.up * 0.4f;
            }

            if (UnityInput.Current.GetKey(KeyCode.E))
            {
                rightHand.localPosition += Vector3.forward * 0.2f;
                rightHand.localPosition += Vector3.up * 0.4f;
            }

            Camera thirdPersonCam = null;
            GameObject cameraObj = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");

            if (cameraObj != null)
            {
                thirdPersonCam = cameraObj.GetComponent<Camera>();
            }

            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();

                body.Rotate(Vector3.up, mouseDelta.x * MouseSensitivity, Space.World);
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
                {
                    raycastCamera = thirdPersonCam;
                }

                if (raycastCamera != null)
                {
                    Ray ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

                    if (Physics.Raycast(
                        ray,
                        out RaycastHit hit,
                        100f,
                        GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        rightHand.position = hit.point;
                    }
                }
            }

            Vector3 movementDirection = Vector3.zero;

            bool isWalkMode = ModConfig.instance.IsWasdWalk.Value;

            Vector3 forwardDirection = isWalkMode
                ? Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized
                : head.forward;

            Vector3 rightDirection = isWalkMode
                ? Vector3.ProjectOnPlane(head.right, Vector3.up).normalized
                : head.right;

            if (UnityInput.Current.GetKey(KeyCode.W))
                movementDirection += forwardDirection;

            if (UnityInput.Current.GetKey(KeyCode.S))
                movementDirection -= forwardDirection;

            if (UnityInput.Current.GetKey(KeyCode.A))
                movementDirection -= rightDirection;

            if (UnityInput.Current.GetKey(KeyCode.D))
                movementDirection += rightDirection;

            if (!isWalkMode && UnityInput.Current.GetKey(KeyCode.Space))
                movementDirection += head.up;

            float speed;

            if (isWalkMode)
            {
                speed = UnityInput.Current.GetKey(KeyCode.LeftShift)
                    ? 12f
                    : 4.5f;
            }
            else
            {
                speed = UnityInput.Current.GetKey(KeyCode.LeftShift)
                    ? 40f
                    : 10f;
            }

            if (isJumping && Time.time >= jumpCooldownTime)
            {
                Collider[] groundCheck = Physics.OverlapSphere(
                    body.position + Vector3.down * 0.5f,
                    0.35f,
                    GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers,
                    QueryTriggerInteraction.Ignore);

                if (groundCheck.Length > 0)
                {
                    isJumping = false;
                }
            }

            if (isWalkMode)
            {
                if (!DisableMovement && movementDirection != Vector3.zero)
                {
                    float currentSpeed = isJumping
                        ? speed * 0.15f
                        : speed;

                    Vector3 targetWalkVelocity =
                        movementDirection.normalized * currentSpeed;

                    rigidbody.velocity = new Vector3(
                        targetWalkVelocity.x,
                        rigidbody.velocity.y,
                        targetWalkVelocity.z);

                    if (hasTouchedWithHand && !isJumping)
                    {
                        float swingSpeed = speed * 1.2f;
                        float swingAmount = 0.25f;
                        float wave = Mathf.Sin(Time.time * swingSpeed);

                        leftHand.localPosition += Vector3.forward * (wave * swingAmount);
                        rightHand.localPosition += Vector3.forward * (-wave * swingAmount);

                        leftHand.localPosition += Vector3.up * (Mathf.Abs(wave) * 0.08f);
                        rightHand.localPosition += Vector3.up * (Mathf.Abs(Mathf.Cos(Time.time * swingSpeed)) * 0.08f);
                    }
                }
                else
                {
                    if (!isJumping)
                    {
                        rigidbody.velocity = new Vector3(
                            0f,
                            rigidbody.velocity.y,
                            0f);
                    }
                }

                if (UnityInput.Current.GetKeyDown(KeyCode.Space) &&
                    hasTouchedWithHand &&
                    !isJumping)
                {
                    rigidbody.velocity = new Vector3(
                        rigidbody.velocity.x,
                        6.5f,
                        rigidbody.velocity.z);

                    isJumping = true;
                    hasTouchedWithHand = false;

                    jumpCooldownTime = Time.time + 0.15f;
                }

                rigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                if (!DisableMovement && movementDirection != Vector3.zero)
                {
                    body.position += movementDirection.normalized *
                                     (Time.deltaTime * speed);
                }

                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            ResolveHandCollision(leftHand);
            ResolveHandCollision(rightHand);
        }

        private static void ResolveHandCollision(Transform hand)
        {
            const float handRadius = 0.075f;
            const float skinWidth = 0.0025f;

            if (!LastHandPositions.TryGetValue(hand, out Vector3 previousPosition))
            {
                previousPosition = hand.position;
            }

            Vector3 currentPosition = hand.position;
            Vector3 moveDelta = currentPosition - previousPosition;

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
                        GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    if (!processingJumpRelease)
                    {
                        isJumping = false;
                        hasTouchedWithHand = true;
                    }

                    hand.position = hit.point + hit.normal * (handRadius + skinWidth);
                    Vector3 remainingMovement = currentPosition - hand.position;
                    Vector3 surfaceSlide = Vector3.ProjectOnPlane(remainingMovement, hit.normal);

                    if (!Physics.SphereCast(
                            hand.position,
                            handRadius,
                            surfaceSlide.normalized,
                            out RaycastHit slideHit,
                            surfaceSlide.magnitude,
                            GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers,
                            QueryTriggerInteraction.Ignore))
                    {
                        hand.position += surfaceSlide;
                    }
                }
            }

            Collider[] overlaps = Physics.OverlapSphere(
                hand.position,
                handRadius,
                GorillaLocomotion.GTPlayer.Instance.locomotionEnabledLayers,
                QueryTriggerInteraction.Ignore);

            if (overlaps.Length > 0 && !processingJumpRelease)
            {
                isJumping = false;
                hasTouchedWithHand = true;
            }

            foreach (Collider col in overlaps)
            {
                if (Physics.ComputePenetration(
                        GetHandProbe(handRadius, hand.position),
                        hand.position,
                        Quaternion.identity,
                        col,
                        col.transform.position,
                        col.transform.rotation,
                        out Vector3 direction,
                        out float penetration))
                {
                    hand.position += direction * (penetration + skinWidth);
                }
            }

            LastHandPositions[hand] = hand.position;
        }

        private static SphereCollider GetHandProbe(float radius, Vector3 position)
        {
            if (_probeCollider == null)
            {
                GameObject obj = new GameObject("HandCollisionProbe");
                obj.hideFlags = HideFlags.HideAndDontSave;

                _probeCollider = obj.AddComponent<SphereCollider>();
                _probeCollider.isTrigger = true;
            }

            _probeCollider.radius = radius;
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
                HasGhostMonked = !HasGhostMonked;
                GorillaTagger.Instance.offlineVRRig.enabled = !HasGhostMonked;
            }

            prevRightPrimary = current;
        }
        public static void Platforms()
        {
            Color platcolor = ModConfig.Theme;
            var input = ControllerInputPoller.instance;
            Shader uberShader = Shader.Find("GorillaTag/UberShader");

            bool isRGB = ModConfig.instance.IsMenuRGB.Value;
            bool isInvis = ModConfig.instance.IsInvisPlat.Value;

            if (input.leftGrab && !IsLeftPlat)
            {
                IsLeftPlat = true;

                LeftPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                LeftPlat.transform.position = GTPlayer.Instance.LeftHand.controllerTransform.position;
                LeftPlat.transform.rotation = GTPlayer.Instance.LeftHand.controllerTransform.rotation;
                LeftPlat.transform.localScale = platScale;

                Rigidbody rb = LeftPlat.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                if (!isInvis)
                {
                    var rend = LeftPlat.GetComponent<Renderer>();
                    rend.material = new Material(uberShader);

                    if (!isRGB)
                    {
                        rend.material.color = platcolor;
                    }
                    else
                    {
                        Main.instance.StartCoroutine(Main.instance.RGBTheme(rend));
                    }
                }
                else
                {
                    LeftPlat.GetComponent<Renderer>().enabled = false;
                }
            }

            if (input.rightGrab && !IsRightPlat)
            {
                IsRightPlat = true;

                RightPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                RightPlat.transform.position = GTPlayer.Instance.RightHand.controllerTransform.position;
                RightPlat.transform.rotation = GTPlayer.Instance.RightHand.controllerTransform.rotation;
                RightPlat.transform.localScale = platScale;

                Rigidbody rb = RightPlat.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;

                if (!isInvis)
                {
                    var rend = RightPlat.GetComponent<Renderer>();
                    rend.material = new Material(uberShader);

                    if (!isRGB)
                    {
                        rend.material.color = platcolor;
                    }
                    else
                    {
                        Main.instance.StartCoroutine(Main.instance.RGBTheme(rend));
                    }
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
                    Object.Destroy(LeftPlat.GetComponent<Collider>());

                    Rigidbody rb = LeftPlat.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    GameObject platToDelete = LeftPlat;
                    Object.Destroy(platToDelete, 5f);

                    LeftPlat = null;
                }
            }

            if (!input.rightGrab && IsRightPlat)
            {
                IsRightPlat = false;

                if (RightPlat != null)
                {
                    Object.Destroy(RightPlat.GetComponent<Collider>());

                    Rigidbody rb = RightPlat.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    GameObject platToDelete = RightPlat;
                    Object.Destroy(platToDelete, 5f);

                    RightPlat = null;
                }
            }
        }


        public static void JoystickFly()
        {
            var tagger = GorillaTagger.Instance;
            var rb = tagger.rigidbody;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            if (ModConfig.instance.IsJoystickNavigation.Value && Main.instance.isMenuCreated)
                return;

            Vector2 joyl = ControllerInputPoller.instance.leftControllerPrimary2DAxis;
            Vector2 joyr = ControllerInputPoller.instance.rightControllerPrimary2DAxis;
            float speedMultiplier = Time.deltaTime * ModConfig.instance.FlySpeedSave.Value * 15f;

            if (joyl.magnitude > 0.6f)
            {
                Transform bodyTransform = tagger.bodyCollider.transform;
                GTPlayer.Instance.transform.position += (bodyTransform.forward * (joyl.y * speedMultiplier)) + (bodyTransform.right * (joyl.x * speedMultiplier));
            }

            if (joyr.magnitude > 0.6f)
            {
                GTPlayer.Instance.transform.position += tagger.bodyCollider.transform.up * (joyr.y * speedMultiplier);
            }
        }
        private static GameObject lineObject;
        private static LineRenderer line;
        private static bool isGrabbing = false;
        private static float initialHandAngle;
        private static float initialPlayerAngle;

        public static void HandTurn()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                float downwardAngle = 50f;
                Transform handTransform = GTPlayer.Instance.RightHand.controllerTransform;
                Transform playerTransform = GTPlayer.Instance.transform;

                Vector3 rayDirection = Quaternion.AngleAxis(downwardAngle, handTransform.right) * handTransform.forward;

                if (lineObject == null)
                {
                    lineObject = new GameObject("HandTurn_LineIndicator");
                    line = lineObject.AddComponent<LineRenderer>();
                    line.startWidth = 0.02f;
                    line.endWidth = 0.02f;
                    line.positionCount = 2;
                    line.useWorldSpace = true;

                    Material lineMat = new Material(Shader.Find("Sprites/Default"));
                    line.material = lineMat;
                    line.startColor = Color.red;
                    line.endColor = Color.red;
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
                        isGrabbing = true;
                        initialHandAngle = currentHandAngle;
                        initialPlayerAngle = playerTransform.eulerAngles.y;
                    }

                    float angleDelta = currentHandAngle - initialHandAngle;
                    float targetPlayerAngle = initialPlayerAngle + angleDelta;

                    Quaternion targetRotation = Quaternion.Euler(0f, targetPlayerAngle, 0f);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRotation, 15f * Time.deltaTime);
                }
                else
                {
                    line.SetPosition(1, handTransform.position + (rayDirection * 100f));
                    isGrabbing = false;
                }
            }
            else
            {
                isGrabbing = false;
                if (line != null)
                {
                    line.enabled = false;
                }
            }
        }

        public static bool noclipBool = false;
        private static MeshCollider[] cachedMeshColliders;

        public static void Noclip()
        {
            if (ControllerInputPoller.instance.rightControllerSecondaryButton)
            {
                if (!noclipBool)
                {
                    noclipBool = true;
                    cachedMeshColliders = Resources.FindObjectsOfTypeAll<MeshCollider>();
                    for (int i = 0; i < cachedMeshColliders.Length; i++)
                    {
                        if (cachedMeshColliders[i] != null) cachedMeshColliders[i].enabled = false;
                    }
                }
            }
            else
            {
                if (noclipBool)
                {
                    noclipBool = false;
                    if (cachedMeshColliders != null)
                    {
                        for (int i = 0; i < cachedMeshColliders.Length; i++)
                        {
                            if (cachedMeshColliders[i] != null) cachedMeshColliders[i].enabled = true;
                        }
                    }
                }
            }
        }

        public static void CopyRigGun()
        {
            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;

            if (GunLib.Triggering)
            {
                var lockedRig = GunLib.LockedRig;
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = lockedRig.syncPos;
                VRRig.LocalRig.transform.rotation = lockedRig.syncRotation;

                VRRig.LocalRig.leftHand.rigTarget.transform.position = lockedRig.leftHand.rigTarget.transform.position;
                VRRig.LocalRig.rightHand.rigTarget.transform.position = lockedRig.rightHand.rigTarget.transform.position;

                VRRig.LocalRig.leftHand.rigTarget.transform.rotation = lockedRig.leftHand.rigTarget.transform.rotation;
                VRRig.LocalRig.rightHand.rigTarget.transform.rotation = lockedRig.rightHand.rigTarget.transform.rotation;

                VRRig.LocalRig.head.rigTarget.transform.rotation = lockedRig.head.rigTarget.transform.rotation;
            }
            if (!GunLib.Triggering)
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void AntiReport()
        {
            if (NetworkSystem.Instance == null || !NetworkSystem.Instance.InRoom) return;

            var localPlayer = NetworkSystem.Instance.LocalPlayer;
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            var rigs = VRRigCache.ActiveRigs;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.linePlayer != localPlayer) continue;
                Vector3 reportBtnPos = line.reportButton.transform.position;

                for (int j = 0; j < rigs.Count; j++)
                {
                    var vrrig = rigs[j];
                    if (vrrig == null || vrrig.isLocal || vrrig.isOfflineVRRig) continue;

                    if (Vector3.Distance(vrrig.rightHandTransform.position, reportBtnPos) < 0.7f ||
                        Vector3.Distance(vrrig.leftHandTransform.position, reportBtnPos) < 0.7f)
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
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
            if (GunLib.Triggering && GunLib.IsOverVrrig && GunLib.GunPos != null)
            {
                VRRig.LocalRig.enabled = false;
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
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GunLib.GunPos.position + upOffset07;
            }
            if (!GunLib.Triggering)
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void FreezeRig()
        {
            if (ControllerInputPoller.instance.rightControllerSecondaryButton)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GTPlayer.Instance.bodyCollider.transform.position + upOffset02;
            }
            else
            {
                VRRig.LocalRig.enabled = true;
            }
        }

        public static float muteDelay;

        public static void MuteGun()
        {
            GunLib.LetGun();
            if (!GunLib.IsOverVrrig) return;
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
            if (GunLib.Triggering && Time.time > muteDelay)
            {
                var owner = GunLib.LockedRigOwner;
                if (owner != null && !owner.IsLocal)
                {
                    var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (line.linePlayer == owner)
                        {
                            muteDelay = Time.time + 0.5f;
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
                var target = GunLib.LockedRigOwner;
                if (target == null) return;

                muteDelay = Time.time + 0.5f;
                var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
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
                var owner = GunLib.LockedRigOwner;
                if (owner != null && !owner.IsLocal)
                {
                    GorillaPlayerScoreboardLine.ReportPlayer(owner.UserId, GorillaPlayerLineButton.ButtonType.Toxicity, owner.NickName);
                    muteDelay = Time.time + 0.2f;
                }
            }
        }

        public static bool HasShot = false;
        public static void TPGun()
        {
            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
            if (GunLib.Triggering && !HasShot)
            {
                GTPlayer.Instance.TeleportTo(GunLib.GunPos);
                var rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                HasShot = true;
            }
            if (!GunLib.Triggering && HasShot)
            {
                HasShot = false;
            }
        }

        public static void HoldRig()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                VRRig.LocalRig.enabled = false;
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
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
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
            var lines = GorillaScoreboardTotalUpdater.allScoreboardLines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.linePlayer == null || line.linePlayer.IsLocal) continue;

                if (line.muteButton.isOn)
                {
                    line.muteButton.isOn = false;
                    line.PressButton(false, GorillaPlayerLineButton.ButtonType.Mute);
                }
            }
        }

        public static bool HeldTriggerGetPID = false;
        public static void GetPID()
        {
            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;

            if (GunLib.Triggering && GunLib.IsOverVrrig && !HeldTriggerGetPID)
            {
                string userId = GunLib.LockedRigOwner.UserId;
                string nick = GunLib.LockedRigOwner.NickName;

                string dirPath = Path.Combine(BepInEx.Paths.GameRootPath, "Gemstone", "IDS");
                Directory.CreateDirectory(dirPath);

                File.WriteAllText(Path.Combine(dirPath, nick + ".txt"), "ID: " + userId);
                NotiLib.SendNotification("ID: " + userId, 2000);

                HeldTriggerGetPID = true;
            }

            if (!GunLib.Triggering && HeldTriggerGetPID)
            {
                HeldTriggerGetPID = false;
            }
        }

        public static void UpsideDownNeck() => VRRig.LocalRig.head.trackingRotationOffset.z = 180f;

        public static void silkickgun()
        {
            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
            if (GunLib.Triggering && GunLib.IsOverVrrig)
            {
                Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, GunLib.LockedRig.Creator.UserId);
            }
        }

        public static int assetId;
        public static bool hastwerked = false;
        public static void TwerkingCarti()
        {
            if (!hastwerked)
            {
                assetId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "carti", assetId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, assetId, new Vector3(-76f, 1.7f, -80f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, assetId, Quaternion.Euler(0f, 40f, 0f));

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

        private static int allocatedSwordId = -1;
        private static int allocatedSwordVidId = -1;
        private static bool HasSpawnedSword = false;
        private static bool HasPlayed = false;

        public static void Sword()
        {
            if (!HasSpawnedSword)
            {
                if (allocatedSwordId < 0)
                {
                    allocatedSwordId = Console.Console.GetFreeAssetID();
                    Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Sword", allocatedSwordId);
                    if (ModConfig.instance.IsBigAssets.Value)
                        Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordId, Vector3.one * 5);

                    Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSwordId, 2);
                    Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Unsheath");
                }

                if (allocatedSwordVidId < 0)
                {
                    allocatedSwordVidId = Console.Console.GetFreeAssetID();
                    Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer", allocatedSwordVidId);
                    Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordVidId, new Vector3(0.1f, 0.1f, 0.1f));
                    Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedSwordVidId, Vector3.zero);
                    Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedSwordVidId);
                    Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedSwordVidId, "Video", "https://github.com/Lexiii-1/testvid/raw/refs/heads/main/RobloxSword.mp4");
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
            {
                HasPlayed = false;
            }
        }

        public static void NoSword()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordId);
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordVidId);
            allocatedSwordId = -1;
            allocatedSwordVidId = -1;
            HasSpawnedSword = false;
        }

        private static int allocatedTravisId;
        public static bool HasTravisTravised = false;
        public static void TravisScott()
        {
            if (!HasTravisTravised)
            {

                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "travis", "TravisScott", allocatedTravisId);
                Console.Console.ExecuteCommand("asset-setposition", Photon.Realtime.ReceiverGroup.All, allocatedTravisId, new Vector3(-70f, 2f, -52f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 3.5f : 0.4f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedTravisId, Vector3.one * scaleFactor);

                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, allocatedTravisId, Quaternion.Euler(0f, 20f, 0f));
                HasTravisTravised = true;
            }
        }

        public static void NoTravis()
        {
            HasTravisTravised = false;
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedTravisId);
        }

        

        public static int phoneid;
        public static bool HasCreatedPhone = false;
        public static string Video = "";
        public static void Samsung()
        {
            if (!HasCreatedPhone)
            {
                phoneid = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "samsungphone", phoneid);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, phoneid, 1);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, phoneid, new Vector3(-0.075f, 0.1f, 0f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, phoneid, Quaternion.Euler(80f, 90f, 180f));

                float scaleFactor = ModConfig.instance.IsBigAssets.Value ? 5f : 0.3f;
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, phoneid, Vector3.one * scaleFactor);

                Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, phoneid, "VideoPlayer", Video);
                Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, phoneid);
                HasCreatedPhone = true;
            }
        }

        public static void NoSamsung()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, phoneid);
            HasCreatedPhone = false;
        }

        public static void AdminGrabAll()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                Console.Console.ExecuteCommand("tp", ReceiverGroup.Others, GTPlayer.Instance.RightHand.controllerTransform.position);
            }
        }

        public static NetPlayer GetPlayerFromVRRig(VRRig p) =>
            p.Creator;
        private static float stdell;
        private static float adminEventDelay;
        private static VRRig thestrangled;
        private static VRRig thestrangledleft;
        public static void AdminStrangle()
        {
            var input = ControllerInputPoller.instance;
            var tagger = GorillaTagger.Instance;
            var rigs = VRRigCache.ActiveRigs;
            if (input.leftGrab)
            {
                if (thestrangledleft == null)
                {
                    for (int i = 0; i < rigs.Count; i++)
                    {
                        var rig = rigs[i];
                        if (rig.isLocal) continue;
                        if (Vector3.Distance(rig.headMesh.transform.position, tagger.leftHandTransform.position) >= 0.2f) continue;
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
                        Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber, tagger.leftHandTransform.position);
                    }
                }
            }
            else
            {
                if (thestrangledleft != null)
                {
                    try
                    {
                        Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber, tagger.leftHandTransform.position);
                        Console.Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangledleft).ActorNumber, GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0));
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
                        var rig = rigs[i];
                        if (rig.isLocal) continue;
                        if (Vector3.Distance(rig.headMesh.transform.position, tagger.rightHandTransform.position) >= 0.2f) continue;
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
                        Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber, tagger.rightHandTransform.position);
                    }
                }
            }
            else
            {
                if (thestrangled != null)
                {
                    try
                    {
                        Console.Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber, tagger.rightHandTransform.position);
                        Console.Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangled).ActorNumber, GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0));
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
        public static float sizeScale = 1f;

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
                sizeScale += increment;
                scaleChanged = true;
            }

            if (ControllerInputPoller.instance.leftGrab)
            {
                sizeScale -= increment;
                scaleChanged = true;
            }

            if (ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                sizeScale = 1f;
                scaleChanged = true;
            }

            if (sizeScale < 0.05f)
                sizeScale = 0.05f;

            if (scaleChanged)
            {
                if (VRRig.LocalRig != null)
                {
                    VRRig.LocalRig.transform.localScale = Vector3.one * sizeScale;

                    System.Reflection.FieldInfo vrrigField = typeof(VRRig).GetField("NativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    if (vrrigField != null)
                    {
                        vrrigField.SetValue(VRRig.LocalRig, sizeScale);
                    }
                    else
                    {
                        System.Reflection.PropertyInfo vrrigProp = typeof(VRRig).GetProperty("NativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                        vrrigProp?.SetValue(VRRig.LocalRig, sizeScale, null);
                    }
                }

                if (GTPlayer.Instance != null)
                {
                    System.Reflection.FieldInfo gtField = typeof(GTPlayer).GetField("nativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    if (gtField != null)
                    {
                        gtField.SetValue(GTPlayer.Instance, sizeScale);
                    }
                    else
                    {
                        System.Reflection.PropertyInfo gtProp = typeof(GTPlayer).GetProperty("nativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                        gtProp?.SetValue(GTPlayer.Instance, sizeScale, null);
                    }
                }

                if (PhotonNetwork.InRoom)
                {
                    Console.Console.ExecuteCommand("scale", PhotonNetwork.LocalPlayer.ActorNumber, sizeScale);
                }
            }
        }

        public static void DisableSizeChanger()
        {
            sizeScale = 1f;

            if (VRRig.LocalRig != null)
            {
                VRRig.LocalRig.transform.localScale = Vector3.one * sizeScale;

                System.Reflection.FieldInfo vrrigField = typeof(VRRig).GetField("NativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                if (vrrigField != null)
                {
                    vrrigField.SetValue(VRRig.LocalRig, sizeScale);
                }
                else
                {
                    System.Reflection.PropertyInfo vrrigProp = typeof(VRRig).GetProperty("NativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    vrrigProp?.SetValue(VRRig.LocalRig, sizeScale, null);
                }
            }

            if (GTPlayer.Instance != null)
            {
                System.Reflection.FieldInfo gtField = typeof(GTPlayer).GetField("nativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                if (gtField != null)
                {
                    gtField.SetValue(GTPlayer.Instance, sizeScale);
                }
                else
                {
                    System.Reflection.PropertyInfo gtProp = typeof(GTPlayer).GetProperty("nativeScale", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                    gtProp?.SetValue(GTPlayer.Instance, sizeScale, null);
                }
            }

            if (PhotonNetwork.InRoom)
            {
                Console.Console.ExecuteCommand("scale", PhotonNetwork.LocalPlayer.ActorNumber, sizeScale);
            }
        }

        private static int KormakurId;
        private static bool HasSignSigned = false;
        public static void KormakurFemboys()
        {
            if (!HasSignSigned)
            {
                KormakurId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "KormakurSign", KormakurId);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, KormakurId, 2);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, KormakurId, new Vector3(0.29f, -0.2f, -0.1272f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, KormakurId, Quaternion.Euler(355f, 275f, 265f));

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

        private static int Axeid;
        public static bool HasAxeAxed = false;
        public static void Axe()
        {
            if (!HasAxeAxed)
            {
                Axeid = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "Axe", Axeid);
                Console.Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, Axeid, 2);
                Console.Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, Axeid, new Vector3(0.05f, 0.03f, 0f));
                Console.Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, Axeid, Quaternion.Euler(0f, 0f, 90f));

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

        private static int TvID;
        private static int sofaAssetId;
        public static bool Hastvtved;
        public static void SkidTV()
        {
            if (!Hastvtved)
            {
                TvID = Console.Console.GetFreeAssetID();
                sofaAssetId = Console.Console.GetFreeAssetID();

                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "TV", TvID);
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "sofa", sofaAssetId);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, TvID, new Vector3(-57.1f, 5.6f, -37f));
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, sofaAssetId, new Vector3(-51.8f, 4.2f, -37.4f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, TvID, Quaternion.Euler(270f, 0f, 0f));
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, sofaAssetId, Quaternion.Euler(270f, 270f, 0f));
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
        private static int PlayerId;
        private static bool HasSpawnedVideoPlayer;
        private static bool isAdjustingScale;
        private static bool primaryButtonWasPressed;
        private static float currentForwardOffset = 2f;
        private static Vector3 currentScale = scale03;

        private static Vector3 savedSpawnPosition = Vector3.zero;
        private static Quaternion savedSpawnRotation = Quaternion.identity;
        private static bool hasSavedPosition = false;
        private static string lastVideoUrl = string.Empty;


        public static void VideoPlayer()
        {
            if (HasSpawnedVideoPlayer && lastVideoUrl != Video)
            {
                NoVideoPlayer();
            }

            if (!HasSpawnedVideoPlayer)
            {
                PlayerId = Console.Console.GetFreeAssetID();
                Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer", PlayerId);

                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);

                Vector3 spawnPosition = hasSavedPosition ? savedSpawnPosition : Vector3.zero;
                Quaternion targetRotation = hasSavedPosition ? savedSpawnRotation : Quaternion.identity;

                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, PlayerId, spawnPosition);
                Console.Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, PlayerId);

                Console.Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, PlayerId, "Video", Video);
                lastVideoUrl = Video;

                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, targetRotation);

                HasSpawnedVideoPlayer = true;
            }

            bool primaryButtonPressed = ControllerInputPoller.instance.leftControllerPrimaryButton;
            if (primaryButtonPressed && !primaryButtonWasPressed)
            {
                isAdjustingScale = !isAdjustingScale;
            }
            primaryButtonWasPressed = primaryButtonPressed;

            if (isAdjustingScale)
            {
                if (ControllerInputPoller.instance.rightControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
                {
                    currentScale *= 1.05f;
                    Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);
                    if (Console.Console.consoleAssets.TryGetValue(PlayerId, out var localAsset) && localAsset?.assetObject != null)
                    {
                        localAsset.assetObject.transform.localScale = currentScale;
                    }
                }
                if (ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
                {
                    currentScale *= 0.95f;
                    if (currentScale.x < 0.01f) currentScale = new Vector3(0.01f, 0.01f, 0.01f);
                    Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);
                    if (Console.Console.consoleAssets.TryGetValue(PlayerId, out var localAsset) && localAsset?.assetObject != null)
                    {
                        localAsset.assetObject.transform.localScale = currentScale;
                    }
                }
            }
            else
            {
                if (ControllerInputPoller.instance.rightControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
                {
                    currentForwardOffset += 0.25f;
                }
                if (ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.leftGrab)
                {
                    currentForwardOffset -= 0.25f;
                    if (currentForwardOffset < 0.1f) currentForwardOffset = 0.1f;
                }
            }

            if (ControllerInputPoller.instance.leftGrab && HasSpawnedVideoPlayer)
            {
                Transform controllerTransform = GTPlayer.Instance.LeftHand.controllerTransform;
                Vector3 targetPosition = controllerTransform.position + (controllerTransform.forward * (currentForwardOffset - 2f));
                Quaternion targetRotation = controllerTransform.rotation * Quaternion.Euler(30f, 180f, 0f);

                if (Console.Console.consoleAssets.TryGetValue(PlayerId, out var localAsset) && localAsset?.assetObject != null)
                {
                    localAsset.assetObject.transform.position = targetPosition;
                    localAsset.assetObject.transform.rotation = targetRotation;
                }

                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, PlayerId, targetPosition);
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, targetRotation);

                savedSpawnPosition = targetPosition;
                savedSpawnRotation = targetRotation;
                hasSavedPosition = true;
            }
        }

        public static void NoVideoPlayer()
        {
            Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, PlayerId);
            HasSpawnedVideoPlayer = false;
        }
        public static void ResetVideoPlayer()
        {
            currentScale = scale03;
            currentForwardOffset = 2f;
            savedSpawnPosition = Vector3.zero;
            savedSpawnRotation = Quaternion.identity;
            hasSavedPosition = false;

            if (HasSpawnedVideoPlayer)
            {
                Console.Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, PlayerId, currentScale);
                Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, PlayerId, savedSpawnPosition);
                Console.Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, PlayerId, savedSpawnRotation);

                if (Console.Console.consoleAssets.TryGetValue(PlayerId, out var localAsset) && localAsset?.assetObject != null)
                {
                    localAsset.assetObject.transform.localScale = currentScale;
                    localAsset.assetObject.transform.position = savedSpawnPosition;
                    localAsset.assetObject.transform.rotation = savedSpawnRotation;
                }
            }
        }

        public static void NoIndicator() => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
        public static void ShowIndicator() => Console.Console.ExecuteCommand("nocone", ReceiverGroup.All, false);
        public static void BackwardsHead() => VRRig.LocalRig.head.trackingRotationOffset.y = 180f;

        private static readonly Vector3 gravityForce = new Vector3(0, -8f, 0);
        public static void GroundHelper()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                GorillaTagger.Instance.rigidbody.AddForce(gravityForce, ForceMode.Acceleration);
                SpeedBoost();
                if (ControllerInputPoller.instance.rightControllerPrimaryButton)
                {
                    Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
                    Vector3 vel = rb.linearVelocity;
                    vel.y = 0f;

                    if (vel.sqrMagnitude > 0.001f)
                    {
                        rb.MovePosition(rb.position + (vel.normalized * (3.9f * Time.deltaTime * GTPlayer.Instance.scale)));
                    }
                }
            }
        }

        public static void AmplifiedMonke()
        {
            Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
            Vector3 vel = rb.linearVelocity;
            if (vel.sqrMagnitude > 0.001f)
            {
                rb.MovePosition(rb.position + (vel.normalized * (4.2f * Time.deltaTime * GTPlayer.Instance.scale)));
            }
        }

        public static readonly string[] ignoreLayers = { "Gorilla Trigger", "Gorilla Boundary", "GorillaHand", "GorillaObject", "Zone", "Water", "GorillaCosmetics", "GorillaParticle", };
        public static LineRenderer webLineLeft;
        public static LineRenderer webLineRight;

        private static bool leftActive;
        private static bool rightActive;
        private static bool leftLocked;
        private static bool rightLocked;

        private static Vector3 leftAnchor;
        private static Vector3 rightAnchor;
        private static float leftLength;
        private static float rightLength;

        private static Vector3 lastLeftPos;
        private static Vector3 lastRightPos;
        private static Vector3 leftHandVel;
        private static Vector3 rightHandVel;
        private static int cachedIgnoreMask = -1;

        private static void InitializeLayerMasks()
        {
            int mask = ~0;
            for (int i = 0; i < ignoreLayers.Length; i++)
            {
                int layer = LayerMask.NameToLayer(ignoreLayers[i]);
                if (layer != -1) mask &= ~(1 << layer);
            }
            cachedIgnoreMask = mask;
        }

        public static void WebSlingers()
        {
            var left = GTPlayer.Instance.LeftHand.controllerTransform;
            var right = GTPlayer.Instance.RightHand.controllerTransform;

            bool leftGrab = ControllerInputPoller.instance.leftGrab;
            bool rightGrab = ControllerInputPoller.instance.rightGrab;

            Rigidbody rb = GTPlayer.Instance.GetComponent<Rigidbody>();
            if (rb == null) return;

            float maxSpeed = ModConfig.instance.WebSlingSpeedSave.Value;
            float dragFactor = 0.995f;

            if (cachedIgnoreMask == -1) InitializeLayerMasks();

            float dt = Time.deltaTime;
            if (dt > 0)
            {
                leftHandVel = (left.position - lastLeftPos) / dt;
                rightHandVel = (right.position - lastRightPos) / dt;
            }

            lastLeftPos = left.position;
            lastRightPos = right.position;

            if (webLineLeft == null)
            {
                GameObject obj = new GameObject("WebLeftHand");
                webLineLeft = obj.AddComponent<LineRenderer>();
                webLineLeft.positionCount = 2;
                webLineLeft.startWidth = 0.02f;
                webLineLeft.endWidth = 0.02f;
                webLineLeft.material = new Material(Shader.Find("Sprites/Default"));
            }

            if (webLineRight == null)
            {
                GameObject obj = new GameObject("WebRightHand");
                webLineRight = obj.AddComponent<LineRenderer>();
                webLineRight.positionCount = 2;
                webLineRight.startWidth = 0.02f;
                webLineRight.endWidth = 0.02f;
                webLineRight.material = new Material(Shader.Find("Sprites/Default"));
            }

            Vector3 playerPos = GTPlayer.Instance.transform.position;

            if (leftGrab && !leftLocked)
            {
                if (Physics.Raycast(left.position, left.forward, out RaycastHit hitL, Mathf.Infinity, cachedIgnoreMask))
                {
                    leftLocked = true;
                    leftActive = true;
                    leftAnchor = hitL.point;
                    leftLength = Vector3.Distance(playerPos, leftAnchor);
                }
            }

            if (!leftGrab)
            {
                leftLocked = false;
                leftActive = false;
                webLineLeft.enabled = false;
            }

            if (rightGrab && !rightLocked)
            {
                if (Physics.Raycast(right.position, right.forward, out RaycastHit hitR, Mathf.Infinity, cachedIgnoreMask))
                {
                    rightLocked = true;
                    rightActive = true;
                    rightAnchor = hitR.point;
                    rightLength = Vector3.Distance(playerPos, rightAnchor);
                }
            }

            if (!rightGrab)
            {
                rightLocked = false;
                rightActive = false;
                webLineRight.enabled = false;
            }

            if (leftActive)
            {
                Vector3 toAnchor = leftAnchor - playerPos;
                float dist = toAnchor.magnitude;
                Vector3 dir = toAnchor.normalized;

                if (dist > leftLength)
                {
                    playerPos = leftAnchor - dir * leftLength;
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
                float dist = toAnchor.magnitude;
                Vector3 dir = toAnchor.normalized;

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
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        }
        private static bool HasPressed;
        private static bool HasPressed2;

        public static void Dash()
        {
            var hc = GTPlayer.Instance.headCollider.transform.forward;

            if (ControllerInputPoller.instance.rightControllerPrimaryButton && !HasPressed)
            {
                GorillaTagger.Instance.rigidbody.velocity = new Vector3(0, 0, 0);
                GorillaTagger.Instance.rigidbody.AddForce(Vector3.up * 18f, ForceMode.VelocityChange);
                HasPressed = true;
            }
            if (!ControllerInputPoller.instance.rightControllerPrimaryButton && HasPressed)
            {
                HasPressed = false;
            }

            if (ControllerInputPoller.instance.rightGrab && !HasPressed2)
            {
                GorillaTagger.Instance.rigidbody.velocity = new Vector3(0, 0, 0);
                GorillaTagger.Instance.rigidbody.AddForce(hc * 25f, ForceMode.VelocityChange);
                HasPressed2 = true;
            }
            if (!ControllerInputPoller.instance.rightGrab && HasPressed2)
            {
                HasPressed2 = false;
            }

            if (Mathf.Abs(GorillaTagger.Instance.rigidbody.velocity.y) > 2.5f)
            {
                SoundSpamContinous(18, true, false, true);
            }
        }


        public static float startX = -1f;
        public static float startY = -1f;
        public static float subThingy;
        public static float subThingyZ;
        public static Vector3 lastPosition = Vector3.zero;
        private static Vector3 lastLeftHandPos;
        private static Vector3 lastRightHandPos;
        private struct RigFrame
        {
            public Vector3 rootPos;
            public Quaternion rootRot;
            public Vector3 headPos;
            public Quaternion headRot;
            public Vector3 leftHandPos;
            public Quaternion leftHandRot;
            public Vector3 rightHandPos;
            public Quaternion rightHandRot;
        }

        private static List<RigFrame> recordedFrames = new List<RigFrame>();

        private static bool isRecording = false;
        private static bool isPlayingBack = false;

        private static int playbackIndex = 0;

        private static bool prevAButton = false;

        public static void MovementRecorder()
        {
            var input = ControllerInputPoller.instance;
            var localRig = VRRig.LocalRig;

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
                        isPlayingBack = true;
                        playbackIndex = 0;
                        localRig.enabled = false;
                    }
                }
                else if (isPlayingBack)
                {
                    isPlayingBack = false;
                    playbackIndex = 0;
                    localRig.enabled = true;
                }
            }

            prevAButton = aButton;

            if (isRecording)
            {
                RigFrame frame = new RigFrame
                {
                    rootPos = localRig.transform.position,
                    rootRot = localRig.transform.rotation,

                    headPos = localRig.head.rigTarget.transform.position,
                    headRot = localRig.head.rigTarget.transform.rotation,

                    leftHandPos = localRig.leftHand.rigTarget.transform.position,
                    leftHandRot = localRig.leftHand.rigTarget.transform.rotation,

                    rightHandPos = localRig.rightHand.rigTarget.transform.position,
                    rightHandRot = localRig.rightHand.rigTarget.transform.rotation
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
                    isPlayingBack = false;
                    playbackIndex = 0;
                    localRig.enabled = true;
                }
            }
        }
        private static float soundSpamDelayC;
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
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, true, 999999f);
                    }
                    else
                    {
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, soundId, LeftHand, 999999f);
                    }

                    RPCProtection();
                }
                else
                {
                    if (both)
                    {
                        VRRig.LocalRig.PlayHandTapLocal(soundId, false, 999999f);
                        VRRig.LocalRig.PlayHandTapLocal(soundId, true, 999999f);
                    }
                    else
                    {
                        VRRig.LocalRig.PlayHandTapLocal(soundId, LeftHand, 999999f);
                    }
                }
            }
        }
        private static float soundSpamDelay;
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
                    VRRig.LocalRig.PlayHandTapLocal(soundId, false, 999999f);
            }
        }
        public static void Annoy() // here I said I wouldnt make annoying mods, but here we are
        {
            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;
            if (GunLib.Triggering && GunLib.IsOverVrrig)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = GunLib.LockedRig.transform.position;
                VRRig.LocalRig.leftHand.rigTarget.transform.localPosition = new Vector3(UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 2));
                VRRig.LocalRig.rightHand.rigTarget.transform.localPosition = new Vector3(UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 2));
                VRRig.LocalRig.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));
                SoundSpamContinous(UnityEngine.Random.Range(336, 338));
            }
            else
            {
                VRRig.LocalRig.enabled = true;
            }
        }
        private static bool lastlhboop;
        private static bool lastrhboop;
        public static void Boop(int sound = 84)
        {
            var tagger = GorillaTagger.Instance;
            var rigs = VRRigCache.ActiveRigs;
            var leftHandPos = tagger.leftHandTransform.position;
            var rightHandPos = tagger.rightHandTransform.position;
            bool isBoopLeft = false;
            bool isBoopRight = false;

            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig vrrig = rigs[i];
                if (vrrig == null || vrrig.isLocal) continue;

                float threshold = 0.275f;
                Vector3 headPos = vrrig.headMesh.transform.position;

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
                    VRRig.LocalRig.PlayHandTapLocal(sound, true, 999999f);
            }
            if (isBoopRight && !lastrhboop)
            {
                if (PhotonNetwork.InRoom)
                {
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, sound, false, 999999f);
                    RPCProtection();
                }
                else
                    VRRig.LocalRig.PlayHandTapLocal(sound, false, 999999f);
            }
            lastlhboop = isBoopLeft;
            lastrhboop = isBoopRight;
        }
        public static void MessUpRig()
        {
            var head = VRRig.LocalRig.head;
            head.trackingRotationOffset.y = 90;
            head.trackingRotationOffset.x = 12;
            VRRig.LocalRig.leftHand.trackingPositionOffset.z = 0.2f;
            VRRig.LocalRig.rightHand.trackingPositionOffset.z = 0.2f;
            SetBodyPatch(true, 4);
        }

        public static void FixRig()
        {
            var head = VRRig.LocalRig.head;
            head.trackingRotationOffset.x = 0;
            head.trackingRotationOffset.y = 0;
            head.trackingRotationOffset.z = 0;

            var leftHand = VRRig.LocalRig.leftHand;
            leftHand.trackingPositionOffset.x = 0.02f;
            leftHand.trackingPositionOffset.y = -0.06f;
            leftHand.trackingPositionOffset.z = 0f;

            var rightHand = VRRig.LocalRig.rightHand;
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

        public static void TorsoPatch_VRRigLateUpdate() => VRRig.LocalRig.transform.rotation *= Quaternion.Euler(0f, (Time.time * 180f) % 360f, 0f);

        public static void SetBodyPatch(bool enabled, int mode = 0)
        {
            TorsoPatch.enabled = enabled;
            TorsoPatch.mode = mode;

            if (!enabled && recBodyRotary != null)
                Destroy(recBodyRotary);
        }

        public static GameObject recBodyRotary;

        private static void UpdateRecBodyRotary()
        {
            if (recBodyRotary == null)
                recBodyRotary = new GameObject("Gemstone_recBodyRotary");

            recBodyRotary.transform.rotation = Quaternion.Lerp(recBodyRotary.transform.rotation, Quaternion.Euler(0f, GorillaTagger.Instance.headCollider.transform.rotation.eulerAngles.y, 0f), Time.deltaTime * 6.5f);
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
            VRRig.LocalRig.head.rigTarget.transform.rotation = VRRig.LocalRig.transform.rotation * Quaternion.Euler(0, 0, 180f);
            VRRig.LocalRig.transform.position = GTPlayer.Instance.bodyCollider.transform.position + upOffset02;
            VRRig.LocalRig.transform.rotation = GTPlayer.Instance.bodyCollider.transform.rotation;
            VRRig.LocalRig.leftHand.rigTarget.transform.position = VRRig.LocalRig.transform.position;
            VRRig.LocalRig.rightHand.rigTarget.transform.position = VRRig.LocalRig.transform.position;
            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = VRRig.LocalRig.transform.rotation;
            VRRig.LocalRig.rightHand.rigTarget.transform.rotation = VRRig.LocalRig.transform.rotation;
        }
        public static void DisableRecRoomBody() => SetBodyPatch(false);



        private static GameObject BodyCollider;
        private static GameObject LeftHandCollider;
        private static GameObject RightHandCollider;
        private static GameObject HeadCollider;

        private static bool IsHoldingRig;
        private static bool isRagdollActive = false;
        private static bool wasButtonHeldLastFrame = false;
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

                    if (BodyCollider != null)
                    {
                        GameObject.Destroy(BodyCollider);
                        BodyCollider = null;
                    }

                    if (HeadCollider != null)
                    {
                        GameObject.Destroy(HeadCollider);
                        HeadCollider = null;
                    }

                    if (LeftHandCollider != null)
                    {
                        GameObject.Destroy(LeftHandCollider);
                        LeftHandCollider = null;
                    }

                    if (RightHandCollider != null)
                    {
                        GameObject.Destroy(RightHandCollider);
                        RightHandCollider = null;
                    }
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
                    BodyCollider.layer = 2;
                    BodyCollider.transform.position = VRRig.LocalRig.transform.position;
                    BodyCollider.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

                    Collider collider = BodyCollider.GetComponent<Collider>();

                    Rigidbody rb = BodyCollider.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.drag = 0.2f;
                    rb.angularDrag = 0.4f;
                    rb.maxAngularVelocity = 50f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.solverIterations = 12;
                    rb.solverVelocityIterations = 12;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    rb.AddForce(Vector3.up * 0.3f, ForceMode.Impulse);

                    rb.AddTorque(
                        new Vector3(
                            UnityEngine.Random.Range(-2f, 2f),
                            UnityEngine.Random.Range(-2f, 2f),
                            UnityEngine.Random.Range(-2f, 2f)
                        ),
                        ForceMode.Impulse
                    );
                }

                if (HeadCollider == null)
                {
                    HeadCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    HeadCollider.layer = 2;
                    HeadCollider.transform.position = GTPlayer.Instance.headCollider.transform.position;
                    HeadCollider.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                    Collider collider = HeadCollider.GetComponent<Collider>();

                    Rigidbody rb = HeadCollider.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.drag = 0.2f;
                    rb.angularDrag = 0.4f;
                    rb.maxAngularVelocity = 50f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.solverIterations = 12;
                    rb.solverVelocityIterations = 12;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    rb.AddTorque(
                        new Vector3(
                            UnityEngine.Random.Range(-10f, 10f),
                            UnityEngine.Random.Range(-10f, 10f),
                            UnityEngine.Random.Range(-10f, 10f)
                        ),
                        ForceMode.Impulse
                    );
                }

                if (LeftHandCollider == null)
                {
                    LeftHandCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    LeftHandCollider.layer = 2;
                    LeftHandCollider.transform.position = GTPlayer.Instance.LeftHand.handFollower.transform.position;
                    LeftHandCollider.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                    Collider collider = LeftHandCollider.GetComponent<Collider>();

                    Rigidbody rb = LeftHandCollider.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.drag = 1f;
                    rb.angularDrag = 2f;
                    rb.maxAngularVelocity = 50f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.solverIterations = 12;
                    rb.solverVelocityIterations = 12;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    Vector3 dirToRig =
                        (VRRig.LocalRig.transform.position - LeftHandCollider.transform.position).normalized;

                    Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.15f;

                    rb.AddForce(
                        (dirToRig + randomOffset).normalized * 0.8f,
                        ForceMode.Impulse
                    );

                    rb.AddTorque(
                        UnityEngine.Random.insideUnitSphere * 1.5f,
                        ForceMode.Impulse
                    );
                }

                if (RightHandCollider == null)
                {
                    RightHandCollider = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    RightHandCollider.layer = 2;
                    RightHandCollider.transform.position = GTPlayer.Instance.RightHand.handFollower.transform.position;
                    RightHandCollider.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                    Collider collider = RightHandCollider.GetComponent<Collider>();

                    Rigidbody rb = RightHandCollider.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.drag = 1f;
                    rb.angularDrag = 2f;
                    rb.maxAngularVelocity = 50f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.solverIterations = 12;
                    rb.solverVelocityIterations = 12;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    Vector3 dirToRig =
                        (VRRig.LocalRig.transform.position - RightHandCollider.transform.position).normalized;

                    Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 0.15f;

                    rb.AddForce(
                        (dirToRig + randomOffset).normalized * 0.8f,
                        ForceMode.Impulse
                    );

                    rb.AddTorque(
                        UnityEngine.Random.insideUnitSphere * 1.5f,
                        ForceMode.Impulse
                    );
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

                    HeadCollider.transform.rotation =
                        rigRotation * Quaternion.Euler(localEuler);
                }

                PushOutOfGeometry(BodyCollider);
                PushOutOfGeometry(HeadCollider);
                PushOutOfGeometry(LeftHandCollider);
                PushOutOfGeometry(RightHandCollider);

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

                VRRig.LocalRig.leftHand.rigTarget.transform.position =
                    LeftHandCollider.transform.position;

                VRRig.LocalRig.rightHand.rigTarget.transform.position =
                    RightHandCollider.transform.position;
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

            const float maxArmLength = 0.5f;

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
        private static Transform currentGrabbingHand;

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

        private static void PushOutOfGeometry(GameObject obj)
        {
            if (obj == null)
                return;

            SphereCollider sphere = obj.GetComponent<SphereCollider>();

            if (sphere == null)
                return;

            Vector3 worldCenter = sphere.bounds.center;
            float radius = sphere.radius * obj.transform.lossyScale.x;

            Collider[] overlaps = Physics.OverlapSphere(
                worldCenter,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in overlaps)
            {
                if (hit == sphere)
                    continue;

                Vector3 direction;
                float distance;

                bool overlapping = Physics.ComputePenetration(
                    colliderA: sphere,
                    positionA: obj.transform.position,
                    rotationA: obj.transform.rotation,
                    colliderB: hit,
                    positionB: hit.transform.position,
                    rotationB: hit.transform.rotation,
                    direction: out direction,
                    distance: out distance
                );

                if (overlapping)
                {
                    obj.transform.position += direction * (distance + 0.02f);

                    Rigidbody rb = obj.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        rb.AddForce(direction * 2f, ForceMode.VelocityChange);
                    }
                }
            }
        }
        // I hate this. I hate how this is split into multiple voids but it has to be.. For simplicity, of course! (I'm ass at programming.)


        public static readonly int TransparentFX = LayerMask.NameToLayer("TransparentFX");
        public static readonly int IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        public static readonly int Zone = LayerMask.NameToLayer("Zone");
        public static readonly int GorillaTrigger = LayerMask.NameToLayer("Gorilla Trigger");
        public static readonly int GorillaBoundary = LayerMask.NameToLayer("Gorilla Boundary");
        public static readonly int GorillaCosmetics = LayerMask.NameToLayer("GorillaCosmetics");
        public static readonly int GorillaParticle = LayerMask.NameToLayer("GorillaParticle");

        public static int NoInvisLayerMask() => ~(1 << TransparentFX | 1 << IgnoreRaycast | 1 << Zone | 1 << GorillaTrigger | 1 << GorillaBoundary | 1 << GorillaCosmetics | 1 << GorillaParticle);
        private static bool lastLaserState = false;

        public static void AdminLaser()
        {
            bool isRightHandPressed = ControllerInputPoller.instance.rightControllerPrimaryButton;
            bool isLeftHandPressed = ControllerInputPoller.instance.leftControllerPrimaryButton;

            if (isRightHandPressed || isLeftHandPressed)
            {
                bool useRightHand = isRightHandPressed;
                Transform handTransform = useRightHand ? VRRig.LocalRig.rightHandTransform : VRRig.LocalRig.leftHandTransform;
                Vector3 dir = useRightHand ? handTransform.right : -handTransform.right;
                Vector3 startPos = handTransform.position + (dir * 0.1f);

                try
                {
                    if (Physics.Raycast(startPos + (dir / 3f), dir, out RaycastHit Ray, 512f, NoInvisLayerMask()))
                    {
                        VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                        if (gunTarget && !gunTarget.isLocal)
                        {
                            Console.Console.ExecuteCommand("silkick", ReceiverGroup.All, gunTarget.Creator.UserId);
                        }
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
            {
                if (allColliders[i] != null) allColliders[i].enabled = false;
            }

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                var rigs = VRRigCache.ActiveRigs;
                for (int i = 0; i < rigs.Count; i++)
                {
                    var rig = rigs[i];
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
            {
                if (allColliders[i] != null) allColliders[i].enabled = true;
            }
        }

        public static IEnumerator Bees()
        {
            VRRig.LocalRig.enabled = false;
            var listBuffer = new List<VRRig>();

            while (ModConfig.instance.IsBees.Value)
            {
                listBuffer.Clear();
                listBuffer.AddRange(VRRigCache.ActiveRigs);
                BackwardsHead();
                VRRig.LocalRig.leftHand.rigTarget.transform.position += upOffset2;
                VRRig.LocalRig.rightHand.rigTarget.transform.position += upOffset2;

                for (int i = 0; i < listBuffer.Count; i++)
                {
                    if (!ModConfig.instance.IsBees.Value) break;
                    VRRig rig = listBuffer[i];

                    if (rig != null && rig.transform != null && VRRig.LocalRig != null)
                    {
                        VRRig.LocalRig.transform.position = rig.transform.position;
                    }
                    yield return beeDelay;
                }
                if (listBuffer.Count == 0) yield return null;
            }
            FixRig();
        }

        public static bool HasRemovedThisFrame;
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

                var monke = MonkeAgent.instance;
                monke.rpcErrorMax = int.MaxValue;
                monke.rpcCallLimit = int.MaxValue;
                monke.logErrorMax = int.MaxValue;

                PhotonNetwork.MaxResendsBeforeDisconnect = int.MaxValue;
                PhotonNetwork.QuickResends = int.MaxValue;
                PhotonNetwork.SendAllOutgoingCommands();

                instance.StartCoroutine(ResetHasRemovedFlag());
            }
            catch (Exception ex)
            {
                Debug.Log($"RPC protection failed: {ex.Message}");
            }
        }

        private static float lastReportTime;
        private static bool hasTaggedCurrentTarget;

        public static void TagGun()
        {
            RPCProtection();
            if (ExtremelyFarTagPatch.isDetected)
            {
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
                NotiLib.SendNotification("Tag mods are blocked", 2000);
                return;
            }

            GunLib.LetGun();
            //bool isFiring = ControllerInputPoller.instance.rightControllerTriggerButton  || ControllerInputPoller.instance.leftControllerTriggerButton && ControllerInputPoller.instance.rightGrab;

            if (GunLib.Triggering)
            {
                var rigs = VRRigCache.ActiveRigs;
                if (!GameMode.LocalIsTagged(PhotonNetwork.LocalPlayer))
                {
                    VRRig hunterRig = null;
                    for (int i = 0; i < rigs.Count; i++)
                    {
                        if (!rigs[i].isLocal && GameMode.LocalIsTagged(rigs[i].Creator))
                        {
                            hunterRig = rigs[i];
                            break;
                        }
                    }

                    if (hunterRig != null)
                    {
                        VRRig.LocalRig.enabled = false;
                        VRRig.LocalRig.transform.position = hunterRig.leftHand.rigTarget.position;
                    }
                }
                else if (GunLib.IsOverVrrig && !hasTaggedCurrentTarget)
                {
                    var targetOwner = GunLib.LockedRigOwner;
                    if (GameMode.LocalIsTagged(targetOwner))
                    {
                        hasTaggedCurrentTarget = true;
                        VRRig.LocalRig.enabled = true;
                    }
                    else
                    {
                        var targetTransform = GunLib.LockedRig.transform;
                        VRRig.LocalRig.enabled = false;
                        VRRig.LocalRig.transform.position = targetTransform.position - upOffset08;

                        if (Vector3.Distance(VRRig.LocalRig.transform.position, targetTransform.position) <= 1f && Time.time > lastReportTime + 2f)
                        {
                            GameMode.ReportTag(GunLib.LockedRig.Creator);
                            lastReportTime = Time.time;
                        }
                    }
                }
            }
            else
            {
                hasTaggedCurrentTarget = false;
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
            }
        }

        private static float lastTagAllTime;
        public static void TagAll()
        {
            RPCProtection();
            if (ExtremelyFarTagPatch.isDetected)
            {
                if (!VRRig.LocalRig.enabled) VRRig.LocalRig.enabled = true;
                NotiLib.SendNotification("Tag mods are blocked", 5000);
                return;
            }

            var rigs = VRRigCache.ActiveRigs;
            if (!GameMode.LocalIsTagged(PhotonNetwork.LocalPlayer))
            {
                VRRig hunterRig = null;
                for (int i = 0; i < rigs.Count; i++)
                {
                    if (!rigs[i].isLocal && GameMode.LocalIsTagged(rigs[i].Creator))
                    {
                        hunterRig = rigs[i];
                        break;
                    }
                }

                if (hunterRig != null)
                {
                    VRRig.LocalRig.enabled = false;
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
            {
                if (!rigs[i].isLocal && !GameMode.LocalIsTagged(rigs[i].Creator))
                {
                    targetRig = rigs[i];
                    break;
                }
            }

            if (targetRig != null)
            {
                VRRig.LocalRig.enabled = false;
                VRRig.LocalRig.transform.position = targetRig.transform.position - Vector3.up;

                if (Vector3.Distance(VRRig.LocalRig.transform.position, targetRig.transform.position) <= 1f && Time.time > lastTagAllTime + 1f)
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

        private static float delaybetweenscore;
        public static void MaxQuestScore()
        {
            if (Time.time > delaybetweenscore)
            {
                delaybetweenscore = Time.time + 1f;
                VRRig.LocalRig.SetQuestScore(int.MaxValue);
            }
        }

        private static float lastVol;
        private static float startSilenceTime = -1f;
        private static bool reloaded;
        public static void BypassAutomod()
        {
            if (!PhotonNetwork.InRoom) return;
            GorillaTagger.moderationMutedTime = -1f;

            var computer = GorillaComputer.instance;
            if (computer.autoMuteType != "OFF")
            {
                computer.autoMuteType = "OFF";
                PlayerPrefs.SetInt("autoMute", 0);
                PlayerPrefs.Save();
            }

            Recorder mic = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            if (mic == null || mic.SourceType == Recorder.InputSourceType.AudioClip) return;

            float volume = 0f;
            GorillaSpeakerLoudness recorder = VRRig.LocalRig.GetComponent<GorillaSpeakerLoudness>();
            if (recorder != null) volume = recorder.Loudness;

            if (volume == 0f)
            {
                if (lastVol != 0f)
                {
                    startSilenceTime = Time.time;
                    reloaded = false;
                }

                if (startSilenceTime > 0f && !reloaded && (Time.time - startSilenceTime >= 0.25f))
                {
                    mic.RestartRecording(true);
                    reloaded = true;
                }
            }
            else
            {
                startSilenceTime = -1f;
                reloaded = false;
            }

            lastVol = volume;
        }
        private class ESPSkeletonData
        {
            public VRRig Rig;
            public GameObject HeadObj;
            public GameObject LeftHandObj;
            public GameObject RightHandObj;
            public Renderer HeadRenderer;
            public Renderer LeftHandRenderer;
            public Renderer RightHandRenderer;
        }

        private static readonly Dictionary<int, ESPSkeletonData> ESPSkeletons = new Dictionary<int, ESPSkeletonData>();
        private static Material skeletonEspMaterial;
        private static float lastSkeletonCleanupTime;
        private static readonly List<int> removeSkeletonListBuffer = new List<int>();

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
                        if (data?.HeadObj != null) Object.Destroy(data.HeadObj);
                        if (data?.LeftHandObj != null) Object.Destroy(data.LeftHandObj);
                        if (data?.RightHandObj != null) Object.Destroy(data.RightHandObj);
                        removeSkeletonListBuffer.Add(pair.Key);
                    }
                }

                for (int i = 0; i < removeSkeletonListBuffer.Count; i++)
                {
                    ESPSkeletons.Remove(removeSkeletonListBuffer[i]);
                }
            }

            var rigs = VRRigCache.ActiveRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null || !rig.gameObject.activeInHierarchy) continue;

                int id = rig.isLocal ? 999999 : rig.GetInstanceID();
                if (!ESPSkeletons.TryGetValue(id, out ESPSkeletonData data) || data == null)
                {
                    GameObject head = null;
                    Renderer headRend = null;

                    if (!rig.isLocal)
                    {
                        head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        head.name = "SkeletonESP_Head";
                        var headCol = head.GetComponent<Collider>();
                        if (headCol != null) Object.Destroy(headCol);
                        var headRb = head.GetComponent<Rigidbody>();
                        if (headRb != null) Object.Destroy(headRb);
                        head.transform.localScale = sphereScaleHead;
                        headRend = head.GetComponent<Renderer>();
                        headRend.material = skeletonEspMaterial;
                    }

                    GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    leftHand.name = "SkeletonESP_Left";
                    rightHand.name = "SkeletonESP_Right";

                    GameObject[] handParts = { leftHand, rightHand };
                    for (int j = 0; j < handParts.Length; j++)
                    {
                        var col = handParts[j].GetComponent<Collider>();
                        if (col != null) Object.Destroy(col);

                        var rb = handParts[j].GetComponent<Rigidbody>();
                        if (rb != null) Object.Destroy(rb);
                    }

                    leftHand.transform.localScale = sphereScaleHand;
                    rightHand.transform.localScale = sphereScaleHand;

                    Renderer leftRend = leftHand.GetComponent<Renderer>();
                    Renderer rightRend = rightHand.GetComponent<Renderer>();

                    leftRend.material = skeletonEspMaterial;
                    rightRend.material = skeletonEspMaterial;

                    data = new ESPSkeletonData
                    {
                        Rig = rig,
                        HeadObj = head,
                        LeftHandObj = leftHand,
                        RightHandObj = rightHand,
                        HeadRenderer = headRend,
                        LeftHandRenderer = leftRend,
                        RightHandRenderer = rightRend
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
                {
                    color = Color.red;
                }
                else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                {
                    color = rig.mainSkin.sharedMaterial.color;
                }

                if (data.HeadRenderer != null) data.HeadRenderer.material.color = color;
                if (data.LeftHandRenderer != null) data.LeftHandRenderer.material.color = color;
                if (data.RightHandRenderer != null) data.RightHandRenderer.material.color = color;
            }
        }

        public static void DisableSkeletonESP()
        {
            foreach (ESPSkeletonData data in ESPSkeletons.Values)
            {
                if (data != null)
                {
                    if (data.HeadObj != null) Object.Destroy(data.HeadObj);
                    if (data.LeftHandObj != null) Object.Destroy(data.LeftHandObj);
                    if (data.RightHandObj != null) Object.Destroy(data.RightHandObj);
                }
            }
            ESPSkeletons.Clear();

            if (skeletonEspMaterial != null)
            {
                Object.Destroy(skeletonEspMaterial);
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

        private class ESPBoxData
        {
            public VRRig Rig;
            public GameObject BoxObject;
            public Renderer Renderer;
        }

        private static readonly Dictionary<int, ESPBoxData> ESPBoxes = new Dictionary<int, ESPBoxData>();
        private static Material espMaterial;
        private static float lastCleanupTime;
        private static readonly List<int> removeListBuffer = new List<int>();

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
                    if (data == null || data.Rig == null || data.BoxObject == null || !data.Rig.gameObject.activeInHierarchy)
                    {
                        if (data?.BoxObject != null) Object.Destroy(data.BoxObject);
                        removeListBuffer.Add(pair.Key);
                    }
                }

                for (int i = 0; i < removeListBuffer.Count; i++)
                {
                    ESPBoxes.Remove(removeListBuffer[i]);
                }
            }

            var rigs = VRRigCache.ActiveRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null || rig.isLocal || !rig.gameObject.activeInHierarchy) continue;

                int id = rig.GetInstanceID();
                if (!ESPBoxes.TryGetValue(id, out ESPBoxData data) || data == null || data.BoxObject == null)
                {
                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = "BoxESP";

                    var col = box.GetComponent<Collider>();
                    if (col != null) Object.Destroy(col);

                    var rb = box.GetComponent<Rigidbody>();
                    if (rb != null) Object.Destroy(rb);

                    Renderer renderer = box.GetComponent<Renderer>();
                    renderer.material = espMaterial;
                    box.transform.localScale = boxEspScale;

                    data = new ESPBoxData { Rig = rig, BoxObject = box, Renderer = renderer };
                    ESPBoxes[id] = data;
                }

                if (data.BoxObject == null || data.Renderer == null || rig.transform == null) continue;

                Transform rigTransform = rig.transform;
                data.BoxObject.transform.position = rigTransform.position;
                data.BoxObject.transform.rotation = rigTransform.rotation;

                Color color = Color.white;
                if (GameMode.LocalIsTagged(rig.Creator))
                {
                    color = Color.red;
                }
                else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                {
                    color = rig.mainSkin.sharedMaterial.color;
                }

                data.Renderer.material.color = color;
            }
        }

        public static void CleanupBoxEsp()
        {
            foreach (ESPBoxData data in ESPBoxes.Values)
            {
                if (data?.BoxObject != null) Object.Destroy(data.BoxObject);
            }
            ESPBoxes.Clear();

            if (espMaterial != null)
            {
                Object.Destroy(espMaterial);
                espMaterial = null;
            }
        }

        private static bool HasInvised;
        private static bool prevRightPrimaryInvis;
        public static void InvisMonke()
        {
            bool current = ControllerInputPoller.instance.rightControllerPrimaryButton;

            if (current && !prevRightPrimaryInvis)
            {
                HasInvised = !HasInvised;
                if (HasInvised)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                    VRRig.LocalRig.transform.position = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
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
                var input = ControllerInputPoller.instance;
                SetBraceletState(input.leftGrab && state, true);
                SetBraceletState(input.rightGrab && state, false);
                RPCProtection();
            }
        }

        private static bool previousBraceletSpamState;
        private static float braceletSpamDelay;

        public static void BraceletSpam()
        {
            if (PhotonNetwork.InRoom && Time.time > braceletSpamDelay)
            {
                previousBraceletSpamState = !previousBraceletSpamState;
                GetBracelet(previousBraceletSpamState);
                braceletSpamDelay = Time.time + 0.02f;
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
            var input = ControllerInputPoller.instance;
            if (input.leftGrab || input.rightGrab)
            {
                System.Random random = new System.Random();
                var head = VRRig.LocalRig.head;
                var leftHand = VRRig.LocalRig.leftHand;
                var rightHand = VRRig.LocalRig.rightHand;

                head.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
                leftHand.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
                rightHand.trackingRotationOffset += new Vector3(random.Next(0, 360), random.Next(0, 360), random.Next(0, 360));
                VRRig.LocalRig.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(1, 360), UnityEngine.Random.Range(0, 360));
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
                var heading = motdHeadingObj.GetComponent<TextMeshPro>();
                if (heading != null) heading.text = titleText;
            }

            GameObject motdBodyObj = GameObject.Find("Environment Objects/LocalObjects_Prefab/TreeRoom/motdBodyText");
            if (motdBodyObj != null)
            {
                var body = motdBodyObj.GetComponent<TextMeshPro>();
                if (body != null)
                {
                    var motdUpdater = (MonoBehaviour)motdBodyObj.GetComponent("PlayFabTitleDataTextDisplay");
                    if (motdUpdater != null) motdUpdater.enabled = false;

                    body.text = bodyText;
                }
            }
        }

        private static int cherryBombId = -1;
        private static bool hasSpawnedCherry = false;
        private static bool cherryAnimationPlayed = false;
        private static float cherrySpawnTime = -1f;

        public static void CherryBomb()
        {
            if (!hasSpawnedCherry)
            {
                if (cherryBombId < 0)
                {
                    cherryBombId = Console.Console.GetFreeAssetID();
                    Console.Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "cherrybomb", "beam", cherryBombId);
                    Console.Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, cherryBombId, GorillaTagger.Instance.bodyCollider.transform.position + cherryBombPosOffset + (GorillaTagger.Instance.bodyCollider.transform.forward * -0.25f));
                    Console.Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cherryBombId, "beam", "cherrybomb");

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

            if (Console.Console.consoleAssets.TryGetValue(cherryBombId, out var asset))
            {
                Vector3 targetPos = asset.assetObject.transform.position + new Vector3(0f, -2f + Mathf.Sin(Time.time * 5f) * 1.25f, 0f);

                GTPlayer.Instance.TeleportTo(targetPos, GTPlayer.Instance.transform.rotation);

                var rb = GorillaTagger.Instance.rigidbody;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public static void NoCherryBomb()
        {
            if (cherryBombId >= 0)
            {
                Console.Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, cherryBombId);
            }

            cherryBombId = -1;
            hasSpawnedCherry = false;
            cherryAnimationPlayed = false;
            cherrySpawnTime = -1f;
        }
        public static void UpdateCustomProperties()
        {
            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
            {
                string propertyKey = "Gemstone. Version: " + Gemstone.Constants.Version;

                if (ModConfig.instance.MenuCustomPropertyEnabled.Value)
                {
                    if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(propertyKey) || !(bool)PhotonNetwork.LocalPlayer.CustomProperties[propertyKey])
                    {
                        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
                        customProperties[propertyKey] = true;
                        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
                    }
                }
                else
                {
                    if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(propertyKey))
                    {
                        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
                        customProperties[propertyKey] = null;
                        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
                    }
                }
            }
        }
        private class NametagData
        {
            public VRRig Rig;
            public GameObject CanvasObject;
            public TextMeshPro TextComponent;
        }

        private static readonly Dictionary<int, NametagData> ActiveNametags = new Dictionary<int, NametagData>();
        private static float lastNametagCleanupTime;
        private static readonly List<int> nametagCleanupBuffer = new List<int>();

        public static void NametagsMod()
        {
            if (Time.time >= lastNametagCleanupTime + 1f)
            {
                lastNametagCleanupTime = Time.time;
                nametagCleanupBuffer.Clear();

                foreach (KeyValuePair<int, NametagData> pair in ActiveNametags)
                {
                    NametagData data = pair.Value;
                    if (data == null || data.Rig == null || data.CanvasObject == null || !data.Rig.gameObject.activeInHierarchy)
                    {
                        if (data?.CanvasObject != null) Object.Destroy(data.CanvasObject);
                        nametagCleanupBuffer.Add(pair.Key);
                    }
                }

                for (int i = 0; i < nametagCleanupBuffer.Count; i++)
                {
                    ActiveNametags.Remove(nametagCleanupBuffer[i]);
                }
            }

            var rigs = VRRigCache.ActiveRigs;
            for (int i = 0; i < rigs.Count; i++)
            {
                VRRig rig = rigs[i];
                if (rig == null || rig.isLocal || !rig.gameObject.activeInHierarchy) continue;

                int id = rig.GetInstanceID();
                if (!ActiveNametags.TryGetValue(id, out NametagData data) || data == null || data.CanvasObject == null)
                {
                    GameObject canvasObj = new GameObject("Gemstone_Nametag_Canvas");

                    var textMesh = canvasObj.AddComponent<TextMeshPro>();
                    textMesh.fontSize = 2f;
                    textMesh.alignment = TextAlignmentOptions.Center;
                    textMesh.rectTransform.sizeDelta = new Vector2(4f, 1f);

                    data = new NametagData
                    {
                        Rig = rig,
                        CanvasObject = canvasObj,
                        TextComponent = textMesh
                    };
                    ActiveNametags[id] = data;
                }

                if (data.CanvasObject == null || data.TextComponent == null || rig.transform == null || rig.head == null) continue;

                Transform headTransform = rig.head.rigTarget != null ? rig.head.rigTarget.transform : rig.transform;
                data.CanvasObject.transform.position = headTransform.position + upOffset09;

                Camera cam = Camera.main;
                if (cam != null)
                {
                    Transform camTransform = cam.transform;
                    data.CanvasObject.transform.LookAt(data.CanvasObject.transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
                }

                string photonNick = "Unknown";
                int playerPing = 0;

                var creator = rig.Creator;
                if (creator != null)
                {
                    photonNick = creator.NickName;

                    if (PhotonNetwork.InRoom)
                    {
                        int basePing = PhotonNetwork.GetPing();
                        UnityEngine.Random.InitState(id + Mathf.FloorToInt(Time.time * 0.5f));
                        playerPing = Mathf.Max(5, basePing + UnityEngine.Random.Range(-15, 35));
                    }
                }

                string hexColor = "#FFFFFF";
                if (creator != null && GameMode.LocalIsTagged(creator))
                {
                    hexColor = "#FF3333";
                }
                else if (rig.mainSkin != null && rig.mainSkin.sharedMaterial != null)
                {
                    hexColor = "#" + ColorUtility.ToHtmlStringRGB(rig.mainSkin.sharedMaterial.color);
                }

                data.TextComponent.text = $"<color={hexColor}>{photonNick}</color>\n<size=75%>Ping: {playerPing}ms</size>";
            }
        }
        public static void DisableNametagsMod()
        {
            foreach (NametagData data in ActiveNametags.Values)
            {
                if (data?.CanvasObject != null) Object.Destroy(data.CanvasObject);
            }
            ActiveNametags.Clear();
        }
    }
}