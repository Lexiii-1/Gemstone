using UnityEngine;
using UnityEngine.InputSystem;
using GorillaLocomotion;
using System.Collections;

namespace Gemstone.Gemstone
{
    public class GunLib : MonoBehaviour
    {
        public static GameObject GunObject;
        public static Transform GunPos;
        public static int GunType => ModConfig.instance.GunType.Value;

        private static LineRenderer lineRenderer;

        private static GunLib instance;

        private static Coroutine rgbCoroutine;

        private static bool isHolding = false;
        private static bool allowThisFrame = false;

        private static VRRig lockedRig = null;

        private static Vector3 lockedOffset;

        private const float unlockDistance = 1.35f;

        private static Vector3 lastGunPosition;
        private static Vector3 gunVelocity;

        private static Vector3 smoothedEndPoint;
        private static Vector3 smoothedVelocity;

        private static bool isRightHandActive = true;

        public static readonly string[] ignoreLayers =
        {
            "Gorilla Trigger",
            "Gorilla Boundary",
            "GorillaHand",
            "GorillaObject",
            "Zone",
            "Water",
            "GorillaCosmetics",
            "GorillaParticle",
        };

        public static bool IsOverVrrig => lockedRig != null;

        public static Transform VrrigTransform =>
            lockedRig != null ? lockedRig.transform : null;

        public static VRRig LockedRig => lockedRig;

        public static NetPlayer LockedRigOwner =>
            lockedRig != null ? lockedRig.Creator : null;

        public static string LockedRigOwnerNick =>
            lockedRig != null && lockedRig.Creator != null
                ? lockedRig.Creator.NickName
                : null;

        public static bool Triggering =>
            (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
            (Mouse.current != null && Mouse.current.rightButton.isPressed && Mouse.current.leftButton.isPressed) ||
            (ControllerInputPoller.instance != null &&
            (isRightHandActive
                ? ControllerInputPoller.instance.rightControllerTriggerButton
                : ControllerInputPoller.instance.leftControllerTriggerButton));

        public static void LetGun()
        {
            allowThisFrame = true;
        }

        void Awake()
        {
            instance = this;
        }

        void Update()
        {
            if (!allowThisFrame)
            {
                DestroyGun();
                lockedRig = null;
                return;
            }

            allowThisFrame = false;

            if (GTPlayer.Instance == null || ControllerInputPoller.instance == null)
                return;

            bool rightGrab = ControllerInputPoller.instance.rightGrab;
            bool leftGrab = ControllerInputPoller.instance.leftGrab;

            bool isMouseRightPressed = Mouse.current != null && Mouse.current.rightButton.isPressed;
            bool holding = rightGrab || leftGrab || isMouseRightPressed;

            if (lockedRig == VRRig.LocalRig)
                lockedRig = null;

            if (holding && !isHolding)
            {
                isRightHandActive = isMouseRightPressed ? true : rightGrab;

                Transform hand = isRightHandActive
                    ? GTPlayer.Instance.RightHand.controllerTransform
                    : GTPlayer.Instance.LeftHand.controllerTransform;

                SpawnGun();
                isHolding = true;

                if (isMouseRightPressed)
                {
                    GameObject cameraObj = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
                    Vector3 spawnPos = cameraObj != null ? cameraObj.transform.position : hand.position;
                    lastGunPosition = spawnPos;
                    smoothedEndPoint = spawnPos;
                }
                else
                {
                    lastGunPosition = hand.position;
                    smoothedEndPoint = hand.position;
                }

                smoothedVelocity = Vector3.zero;

                GorillaTagger.Instance.StartVibration(
                    !isRightHandActive,
                    GorillaTagger.Instance.tagHapticStrength / 1.2f,
                    0.02f
                );
            }

            if (isHolding && !isMouseRightPressed)
            {
                if (isRightHandActive && !rightGrab && leftGrab)
                {
                    isRightHandActive = false;
                }
                else if (!isRightHandActive && !leftGrab && rightGrab)
                {
                    isRightHandActive = true;
                }
            }

            if (holding && GunObject != null)
            {
                Transform hand = isRightHandActive
                    ? GTPlayer.Instance.RightHand.controllerTransform
                    : GTPlayer.Instance.LeftHand.controllerTransform;

                if (Triggering)
                {
                    GorillaTagger.Instance.StartVibration(
                        !isRightHandActive,
                        GorillaTagger.Instance.tagHapticStrength / 1.2f,
                        0.02f
                    );
                }

                Ray ray;
                Vector3 originPoint;

                if (isMouseRightPressed)
                {
                    GameObject cameraObj = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
                    if (cameraObj != null)
                    {
                        ray = new Ray(cameraObj.transform.position, cameraObj.transform.forward);
                        originPoint = cameraObj.transform.position;
                    }
                    else
                    {
                        float downwardAngle = 50f;
                        Vector3 direction = Quaternion.AngleAxis(downwardAngle, hand.right) * hand.forward;
                        ray = new Ray(hand.position, direction);
                        originPoint = hand.position;
                    }
                }
                else
                {
                    float downwardAngle = 50f;
                    Vector3 direction = Quaternion.AngleAxis(downwardAngle, hand.right) * hand.forward;
                    ray = new Ray(hand.position, direction);
                    originPoint = hand.position;
                }

                int mask = ~LayerMask.GetMask(ignoreLayers);

                bool hitSomething = Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    1000f,
                    mask,
                    QueryTriggerInteraction.Collide
                );

                Vector3 targetPoint = ray.origin + ray.direction * 1000f;

                if (hitSomething)
                {
                    VRRig hitRig = hit.collider.GetComponentInParent<VRRig>();
                    if (hitRig != null && hitRig != VRRig.LocalRig)
                    {
                        if (lockedRig != hitRig)
                        {
                            lockedRig = hitRig;
                            lockedOffset = new Vector3(0, 0, 0);
                        }
                    }
                }

                if (lockedRig != null)
                {
                    Vector3 rigCenter = lockedRig.transform.position + lockedOffset;
                    float distanceFromAim = Vector3.Cross(ray.direction, rigCenter - ray.origin).magnitude;

                    if (distanceFromAim > unlockDistance)
                    {
                        lockedRig = null;
                    }
                    else
                    {
                        targetPoint = rigCenter;
                        hitSomething = true;
                    }
                }
                else if (hitSomething)
                {
                    targetPoint = hit.point;
                }

                smoothedEndPoint = Vector3.SmoothDamp(
                    smoothedEndPoint,
                    targetPoint,
                    ref smoothedVelocity,
                    ModConfig.instance.GunSmoothness.Value
                );

                GunObject.transform.position = smoothedEndPoint;

                if (hitSomething)
                {
                    Vector3 normalDirection = (smoothedEndPoint - originPoint).normalized;
                    if (normalDirection != Vector3.zero)
                    {
                        GunObject.transform.rotation = Quaternion.LookRotation(normalDirection);
                    }
                }
                else
                {
                    if (isMouseRightPressed)
                    {
                        GameObject cameraObj = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
                        GunObject.transform.rotation = cameraObj != null ? cameraObj.transform.rotation : hand.rotation;
                    }
                    else
                    {
                        GunObject.transform.rotation = hand.rotation;
                    }
                }

                gunVelocity = (GunObject.transform.position - lastGunPosition) / Time.deltaTime;
                lastGunPosition = GunObject.transform.position;

                DrawLine(originPoint, smoothedEndPoint);
                GunPos = GunObject.transform;
            }

            if (!holding && isHolding)
            {
                DestroyGun();
                isHolding = false;
                lockedRig = null;
            }
        }

        private static void DrawLine(Vector3 start, Vector3 end)
        {
            if (lineRenderer == null)
                return;

            int segments = 100;
            switch (GunType)
            {
                case 1: segments = 460; break;
                case 2: segments = 2; break;
                case 3: segments = 300; break;
                case 4: segments = 50; break;
                case 5: segments = 400; break;
                case 6: segments = 400; break;
                case 7: segments = 250; break;
                case 8: segments = 350; break;
                case 9: segments = 150; break;
                case 10: segments = 300; break;
                case 11: segments = 100; break;
                case 12: segments = 200; break;
                case 13: segments = 500; break;
            }

            lineRenderer.positionCount = segments;

            Vector3 totalDirection = end - start;
            float totalDistance = totalDirection.magnitude;

            if (totalDistance <= 0.001f)
                return;

            Vector3 forward = totalDirection.normalized;
            Vector3 perpendicular = Vector3.Cross(forward, Vector3.up);
            if (perpendicular == Vector3.zero)
                perpendicular = Vector3.Cross(forward, Vector3.right);
            perpendicular.Normalize();

            Vector3 binormal = Vector3.Cross(forward, perpendicular).normalized;
            Vector3 currentPoint = start;
            float lightningSeed = Mathf.Floor(Time.time * 18f);

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 targetPoint = Vector3.Lerp(start, end, t);
                Vector3 offset = Vector3.zero;
                float envelope = Mathf.Sin(t * Mathf.PI);

                switch (GunType)
                {
                    case 1:
                        offset = perpendicular * (Mathf.Sin(t * Mathf.PI) * Mathf.Clamp(totalDistance * 0.06f, 0.05f, 0.45f) + Mathf.Sin((t * 15f) + (Time.time * 7f)) * 0.06f * t);
                        break;
                    case 2: break;
                    case 3:
                        offset = ((perpendicular * Mathf.Sin(t * 15f + Time.time * 20f) * 0.15f) + (binormal * Mathf.Cos(t * 15f + Time.time * 20f) * 0.15f)) * envelope;
                        break;
                    case 4:
                        if (i > 0 && i < segments - 1)
                        {
                            Random.InitState((int)(i + lightningSeed));
                            offset = (perpendicular * Random.Range(-0.25f, 0.25f) + binormal * Random.Range(-0.25f, 0.25f)) * envelope;
                        }
                        break;
                    case 5:
                        float vRad = t * 0.4f;
                        offset = (perpendicular * Mathf.Sin(t * 30f + Time.time * 25f) * vRad + binormal * Mathf.Cos(t * 30f + Time.time * 25f) * vRad);
                        break;
                    case 6:
                        float strand = (i % 2 == 0) ? 1f : -1f;
                        offset = (perpendicular * Mathf.Sin(t * 12f + Time.time * 5f) * 0.12f * strand + binormal * Mathf.Cos(t * 12f + Time.time * 5f) * 0.12f * strand) * envelope;
                        break;
                    case 7:
                        float pulse = Mathf.Sin(t * 20f - Time.time * 10f);
                        offset = perpendicular * pulse * ((pulse > 0.8f) ? 0.2f : 0.02f) * envelope;
                        break;
                    case 8:
                        offset = (perpendicular * Mathf.Sin(Time.time * 10f + (t * 2f)) * 0.25f + binormal * Mathf.Cos(Time.time * 10f + (t * 2f)) * 0.25f) * envelope;
                        break;
                    case 9:
                        offset = (perpendicular * Random.Range(-0.1f, 0.1f) + binormal * Random.Range(-0.1f, 0.1f)) * envelope;
                        break;
                    case 10:
                        offset = perpendicular * Mathf.Sin(t * 20f + Time.time * 15f) * 0.15f * envelope;
                        break;
                    case 11:
                        offset = (perpendicular * Mathf.Round(Mathf.Sin(t * 10f + Time.time * 5f) * 2f) * 0.1f + binormal * Mathf.Round(Mathf.Cos(t * 10f + Time.time * 5f) * 2f) * 0.1f) * envelope;
                        break;
                    case 12:
                        offset = perpendicular * ((Mathf.Sin(t * 25f + Time.time * 10f) > 0) ? 0.15f : -0.15f) * envelope;
                        break;
                    case 13:
                        float rayCircles = 8f;
                        float raySpeed = 15f;
                        float rayAngle = (t * rayCircles * 2f * Mathf.PI) - (Time.time * raySpeed);
                        float rayRadius = (1f - t) * 0.3f;
                        offset = (perpendicular * Mathf.Sin(rayAngle) + binormal * Mathf.Cos(rayAngle)) * rayRadius;
                        break;
                }

                targetPoint += offset;

                if (i > 0 && GunType != 2 && GunType != 4 && GunType != 9 && GunType != 12 && GunType != 14)
                {
                    float follow = (GunType == 1) ? 0.65f : 0.82f;
                    targetPoint = Vector3.Lerp(currentPoint + forward * (totalDistance / segments), targetPoint, follow);
                }

                currentPoint = targetPoint;
                lineRenderer.SetPosition(i, currentPoint);
            }
        }

        public static IEnumerator RGBTheme(Renderer targetRenderer)
        {
            float speed = 2f;
            while (true)
            {
                float t = Time.time * speed;
                Color rgb = new Color(Mathf.Sin(t) * 0.5f + 0.5f, Mathf.Sin(t + 2f) * 0.5f + 0.5f, Mathf.Sin(t + 4f) * 0.5f + 0.5f);
                if (targetRenderer != null) targetRenderer.material.color = rgb;
                yield return null;
            }
        }

        private static void SpawnGun()
        {
            GunObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GunObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            Destroy(GunObject.GetComponent<Rigidbody>());
            Destroy(GunObject.GetComponent<Collider>());

            Renderer rend = GunObject.GetComponent<Renderer>();
            rend.material.shader = Shader.Find("GUI/Text Shader");
            rend.material.color = ModConfig.Theme;

            lineRenderer = GunObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.015f;
            lineRenderer.useWorldSpace = true;

            Material mat = new Material(Shader.Find("GUI/Text Shader"));
            if (!ModConfig.instance.IsMenuRGB.Value)
            {
                mat.color = ModConfig.Theme;
            }
            else
            {
                if (rgbCoroutine != null) instance.StopCoroutine(rgbCoroutine);
                rgbCoroutine = Main.instance.StartCoroutine(RGBTheme(rend));
            }
            lineRenderer.material = mat;
        }

        private static void DestroyGun()
        {
            if (GunObject != null)
            {
                Destroy(GunObject);
                GunObject = null;
                GunPos = null;
                lineRenderer = null;
                smoothedVelocity = Vector3.zero;
                smoothedEndPoint = Vector3.zero;
            }
        }
    }
}