using UnityEngine;

namespace QuestBilliards.Inputs
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class BilliardsPlayerMode : MonoBehaviour
    {
        public BilliardsInputReader inputs;
        public Transform head;
        public float movementSpeed = 1.2f;
        [Header("Juego sentado")]
        public bool seatedMode = true;
        public float seatedEyeHeight = 1.6f;
        public float joystickLookSpeed = 65f;
        public bool IsSeated => seatedMode;
        private bool previousB;
        private float lookPitch;
        private Quaternion originalTrackingRotation;
        private Vector3 originalTrackingPosition;
        private float standingHeadHeight;
        private bool heightCalibrated;
        private Vector3 spawnPosition;
        private float verticalSpeed;
        private bool originChanged;
        private readonly System.Collections.Generic.List<UnityEngine.XR.XRInputSubsystem> xrOrigins = new System.Collections.Generic.List<UnityEngine.XR.XRInputSubsystem>();
        public Transform trackingSpace;
        public float aimingEyeHeight = 0.96f;
        public float viewTransitionSpeed = 1.5f;
        public Transform whiteBall;
        public float aimViewDistance = 0.6f;
        public float aimViewAboveBall = 0.15f;
        public bool IsViewTransitioning { get; private set; }
        public bool IsAiming { get; private set; }
        public bool ControlsLocked { get; set; }
        public Vector3 AimEntryDirection { get; private set; } = Vector3.forward;
        public int AimSessionVersion { get; private set; }
        private CharacterController body;
        private bool previousX;
        private TextMesh modeLabel;
        private Vector3 baseTrackingPosition;
        private Quaternion baseTrackingRotation;
        private Vector3 desiredTrackingPosition;
        private Quaternion desiredTrackingRotation;
        private void Awake() { body = GetComponent<CharacterController>(); spawnPosition = transform.position; }
        private void Start()
        {
            if (head == null) return;
            if (trackingSpace != null)
            {
                originalTrackingPosition = baseTrackingPosition = desiredTrackingPosition = trackingSpace.localPosition;
                originalTrackingRotation = baseTrackingRotation = desiredTrackingRotation = trackingSpace.localRotation;
            }
            var go = new GameObject("PlayerModeStatus"); go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0, 0.12f, 0.85f);
            modeLabel = go.AddComponent<TextMesh>(); modeLabel.anchor = TextAnchor.MiddleCenter;
            modeLabel.alignment = TextAlignment.Center; modeLabel.characterSize = 0.007f;
            modeLabel.fontSize = 40; modeLabel.color = Color.yellow;
            UnityEngine.SubsystemManager.GetSubsystems(xrOrigins);
            foreach (var origin in xrOrigins) origin.trackingOriginUpdated += OnTrackingOriginUpdated;
        }
        private void Update()
        {
            if (inputs == null || head == null) return;
            bool x = inputs.Left.PrimaryButton;
            bool b = inputs.Right.Connected && inputs.Right.SecondaryButton;
#if UNITY_EDITOR
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            x |= keyboard != null && keyboard.xKey.isPressed;
            b |= keyboard != null && keyboard.f3Key.isPressed;
#endif
            if (b && !previousB) SetSeatedMode(!seatedMode);
            previousB = b;
            var headset = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            if ((!heightCalibrated || originChanged) && headset.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked)
            {
                RecenterFromTrackedPose();
            }
            if (modeLabel != null) modeLabel.gameObject.SetActive(!ControlsLocked);
            if (ControlsLocked) { previousX = x; return; }
            if (x && !previousX)
            {
                IsAiming = !IsAiming;
                if (IsAiming) CaptureLookHeading();
                SetViewTarget();
            }
            previousX = x;
            if (!IsAiming && !IsViewTransitioning) UpdateJoystickLook();
            if (trackingSpace != null)
            {
                trackingSpace.localPosition = Vector3.MoveTowards(trackingSpace.localPosition, desiredTrackingPosition, viewTransitionSpeed * Time.deltaTime);
                trackingSpace.localRotation = Quaternion.RotateTowards(trackingSpace.localRotation, desiredTrackingRotation, 100f * Time.deltaTime);
                IsViewTransitioning = Vector3.Distance(trackingSpace.localPosition, desiredTrackingPosition) > 0.005f || Quaternion.Angle(trackingSpace.localRotation, desiredTrackingRotation) > 0.5f;
            }
            Vector3 localHead = transform.InverseTransformPoint(head.position);
            body.height = Mathf.Clamp(localHead.y, 0.8f, 2.2f);
            body.center = new Vector3(localHead.x, body.height * 0.5f + 0.03f, localHead.z);
            Vector3 motion = Vector3.zero;
            if (!IsAiming && inputs.Left.Connected)
            {
                Vector2 stick = inputs.Left.Stick;
                if (stick.magnitude > 0.15f)
                {
                    Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    motion = Vector3.ClampMagnitude(forward * stick.y + right * stick.x, 1f) * movementSpeed;
                }
            }
            // Freeze gravity until a real head pose has been aligned to the authored spawn.
            // Stage-space positions on Quest can otherwise place the capsule outside the room.
            verticalSpeed = !heightCalibrated || body.isGrounded ? -2f : Mathf.Max(-20f, verticalSpeed + Physics.gravity.y * Time.deltaTime);
            motion.y = heightCalibrated ? verticalSpeed : 0;
            if (!IsAiming && !IsViewTransitioning) body.Move(motion * Time.deltaTime);
            if (transform.position.y < spawnPosition.y - 1f) RecoverSpawn();
            if (modeLabel != null) modeLabel.text = (seatedMode ? "SENTADO" : "MOVIMIENTO FISICO") + " | B: cambiar modo\n" +
                (IsAiming ? (seatedMode ? "Apunta: joystick derecho | Mantener gatillo y soltar: golpe" : "Golpea moviendo el mando derecho | X: volver") :
                "Joystick izquierdo: mover | Derecho: mirar | X: apuntar");
        }
        private void OnDisable()
        {
            IsAiming = false; previousX = previousB = false;
            if (trackingSpace != null) { trackingSpace.localPosition = baseTrackingPosition; trackingSpace.localRotation = baseTrackingRotation; }
            IsViewTransitioning = false;
        }
        public void SetSeatedMode(bool seated)
        {
            if (seatedMode == seated) return;
            seatedMode = seated; lookPitch = 0;
            if (heightCalibrated && RecenterFromTrackedPose()) return;
            UpdateSeatedHeight();
            if (IsAiming) { CaptureLookHeading(); SetViewTarget(); }
        }
        private void UpdateSeatedHeight()
        {
            if (trackingSpace == null) return;
            baseTrackingPosition = originalTrackingPosition;
            if (seatedMode && heightCalibrated)
                baseTrackingPosition.y += Mathf.Clamp(seatedEyeHeight - standingHeadHeight, -0.5f, 1.2f);
            baseTrackingRotation = originalTrackingRotation;
            if (!IsAiming) { desiredTrackingPosition = baseTrackingPosition; desiredTrackingRotation = baseTrackingRotation; }
        }
        private void OnTrackingOriginUpdated(UnityEngine.XR.XRInputSubsystem origin) { originChanged = true; }
        public bool RecenterFromTrackedPose()
        {
            if (trackingSpace == null || head == null) return false;
            Vector3 localEye = trackingSpace.InverseTransformPoint(head.position);
            float physicalHeight = localEye.y * trackingSpace.lossyScale.y;
            if (physicalHeight < 0.3f || physicalHeight > 2.5f) return false;
            Vector3 heading = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            Vector3 targetHeading = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            float yaw = heading.sqrMagnitude > 0.01f ? Vector3.SignedAngle(heading, targetHeading, Vector3.up) : 0;
            Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up) * trackingSpace.rotation;
            Vector3 eye = transform.position + Vector3.up * physicalHeight;
            Vector3 worldPosition = eye - rotation * Vector3.Scale(localEye, trackingSpace.lossyScale);
            Transform parent = trackingSpace.parent;
            originalTrackingPosition = parent != null ? parent.InverseTransformPoint(worldPosition) : worldPosition;
            originalTrackingRotation = parent != null ? Quaternion.Inverse(parent.rotation) * rotation : rotation;
            standingHeadHeight = physicalHeight; heightCalibrated = true; originChanged = false; lookPitch = 0;
            UpdateSeatedHeight();
            trackingSpace.localPosition = baseTrackingPosition;
            trackingSpace.localRotation = baseTrackingRotation;
            desiredTrackingPosition = baseTrackingPosition; desiredTrackingRotation = baseTrackingRotation;
            verticalSpeed = 0;
            if (IsAiming) { CaptureLookHeading(); SetViewTarget(); }
            Physics.SyncTransforms();
            Debug.Log("Billar: visor alineado al jugador; altura fisica=" + physicalHeight.ToString("F2"));
            return true;
        }
        private void RecoverSpawn()
        {
            body.enabled = false; transform.position = spawnPosition; body.enabled = true;
            IsAiming = false; verticalSpeed = 0;
            RecenterFromTrackedPose();
            Debug.LogWarning("Billar: jugador recuperado en el punto de inicio.");
        }
        private void UpdateJoystickLook()
        {
            Vector2 stick = inputs.Right.Connected ? inputs.Right.Stick : Vector2.zero;
#if UNITY_EDITOR
            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (keys != null) stick += new Vector2((keys.lKey.isPressed ? 1 : 0) - (keys.jKey.isPressed ? 1 : 0), (keys.iKey.isPressed ? 1 : 0) - (keys.kKey.isPressed ? 1 : 0));
#endif
            if (Mathf.Abs(stick.x) > 0.15f) transform.RotateAround(head.position, Vector3.up, stick.x * joystickLookSpeed * Time.deltaTime);
            Vector3 eye = head.position;
            if (Mathf.Abs(stick.y) > 0.15f) lookPitch = Mathf.Clamp(lookPitch - stick.y * joystickLookSpeed * Time.deltaTime, -55, 55);
            baseTrackingRotation = originalTrackingRotation * Quaternion.Euler(lookPitch, 0, 0);
            desiredTrackingRotation = baseTrackingRotation;
            // Rotate around the eye, not the room-scale tracking origin.
            // The physical headset may be several metres away from that origin.
            if (trackingSpace != null)
            {
                Transform parent = trackingSpace.parent;
                Quaternion rotation = parent != null ? parent.rotation * desiredTrackingRotation : desiredTrackingRotation;
                Vector3 localEye = trackingSpace.InverseTransformPoint(head.position);
                Vector3 position = eye - rotation * Vector3.Scale(localEye, trackingSpace.lossyScale);
                desiredTrackingPosition = parent != null ? parent.InverseTransformPoint(position) : position;
                baseTrackingPosition = desiredTrackingPosition;
                trackingSpace.localRotation = desiredTrackingRotation;
                trackingSpace.localPosition = desiredTrackingPosition;
            }
        }
        private void SetViewTarget()
        {
            ConfigureAimView(IsAiming ? AimEntryDirection : Vector3.forward);
        }
        private void CaptureLookHeading()
        {
            Vector3 direction = head != null ? Vector3.ProjectOnPlane(head.forward, Vector3.up) : transform.forward;
            if (direction.sqrMagnitude > 0.01f) AimEntryDirection = direction.normalized;
            AimSessionVersion++;
        }
        public void RefreshAimFromCurrentLook()
        {
            if (!IsAiming) return;
            CaptureLookHeading(); SetViewTarget();
        }
        public void RefreshAimingView(Vector3 shotDirection)
        {
            if (IsAiming) ConfigureAimView(shotDirection);
        }
        private void ConfigureAimView(Vector3 shotDirection)
        {
            desiredTrackingPosition = baseTrackingPosition; desiredTrackingRotation = baseTrackingRotation;
            IsViewTransitioning = true;
            if (!IsAiming || trackingSpace == null || head == null || whiteBall == null) return;
            var ball = whiteBall.GetComponent<QuestBilliards.BallPhysics.BilliardBallBody>();
            if (ball != null && ball.IsPocketed) return;
            Vector3 direction = Vector3.ProjectOnPlane(shotDirection, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f) direction = Vector3.forward;
            Vector3 viewDirection = seatedMode ? shotDirection.normalized : direction;
            Vector3 eye = whiteBall.position - viewDirection * aimViewDistance + Vector3.up * aimViewAboveBall;
            Quaternion look = Quaternion.LookRotation(whiteBall.position - eye, Vector3.up);
            Quaternion worldRotation = look * Quaternion.Inverse(head.rotation) * trackingSpace.rotation;
            Vector3 localEye = trackingSpace.InverseTransformPoint(head.position);
            Vector3 worldPosition = eye - worldRotation * Vector3.Scale(localEye, trackingSpace.lossyScale);
            var parent = trackingSpace.parent;
            desiredTrackingPosition = parent != null ? parent.InverseTransformPoint(worldPosition) : worldPosition;
            desiredTrackingRotation = parent != null ? Quaternion.Inverse(parent.rotation) * worldRotation : worldRotation;
            IsViewTransitioning = true;
        }
        private void OnDestroy()
        {
            foreach (var origin in xrOrigins) if (origin != null) origin.trackingOriginUpdated -= OnTrackingOriginUpdated;
            if (modeLabel != null) Destroy(modeLabel.gameObject);
        }
    }
}
