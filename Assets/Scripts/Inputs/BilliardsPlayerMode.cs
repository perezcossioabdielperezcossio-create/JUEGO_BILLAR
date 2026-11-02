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
        private void Awake() { body = GetComponent<CharacterController>(); }
        private void Start()
        {
            if (head == null) return;
            if (trackingSpace != null)
            {
                baseTrackingPosition = desiredTrackingPosition = trackingSpace.localPosition;
                baseTrackingRotation = desiredTrackingRotation = trackingSpace.localRotation;
            }
            var go = new GameObject("PlayerModeStatus"); go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0, 0.12f, 0.85f);
            modeLabel = go.AddComponent<TextMesh>(); modeLabel.anchor = TextAnchor.MiddleCenter;
            modeLabel.alignment = TextAlignment.Center; modeLabel.characterSize = 0.007f;
            modeLabel.fontSize = 40; modeLabel.color = Color.yellow;
        }
        private void Update()
        {
            if (inputs == null || head == null) return;
            bool x = inputs.Left.PrimaryButton;
#if UNITY_EDITOR
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            x |= keyboard != null && keyboard.xKey.isPressed;
#endif
            if (modeLabel != null) modeLabel.gameObject.SetActive(!ControlsLocked);
            if (ControlsLocked) { previousX = x; return; }
            if (x && !previousX)
            {
                IsAiming = !IsAiming;
                if (IsAiming) CaptureLookHeading();
                SetViewTarget();
            }
            previousX = x;
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
            if (!IsAiming && inputs.Left.Tracked)
            {
                Vector2 stick = inputs.Left.Stick;
                if (stick.magnitude > 0.15f)
                {
                    Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward);
                    motion = Vector3.ClampMagnitude(forward * stick.y + right * stick.x, 1f) * movementSpeed;
                }
            }
            motion.y = -2f;
            if (!IsAiming && !IsViewTransitioning) body.Move(motion * Time.deltaTime);
            if (modeLabel != null) modeLabel.text = IsAiming ? "APUNTADO | X: volver a moverse" : "MOVIMIENTO | Joystick izquierdo | X: apuntar";
        }
        private void OnDisable()
        {
            IsAiming = false; previousX = false;
            if (trackingSpace != null) { trackingSpace.localPosition = baseTrackingPosition; trackingSpace.localRotation = baseTrackingRotation; }
            IsViewTransitioning = false;
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
            if (!IsAiming || trackingSpace == null || head == null || whiteBall == null) return;
            var ball = whiteBall.GetComponent<QuestBilliards.BallPhysics.BilliardBallBody>();
            if (ball != null && ball.IsPocketed) return;
            Vector3 direction = Vector3.ProjectOnPlane(shotDirection, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f) direction = Vector3.forward;
            Vector3 eye = whiteBall.position - direction * aimViewDistance + Vector3.up * aimViewAboveBall;
            Quaternion look = Quaternion.LookRotation(whiteBall.position - eye, Vector3.up);
            Quaternion worldRotation = look * Quaternion.Inverse(head.rotation) * trackingSpace.rotation;
            Vector3 localEye = trackingSpace.InverseTransformPoint(head.position);
            Vector3 worldPosition = eye - worldRotation * Vector3.Scale(localEye, trackingSpace.lossyScale);
            var parent = trackingSpace.parent;
            desiredTrackingPosition = parent != null ? parent.InverseTransformPoint(worldPosition) : worldPosition;
            desiredTrackingRotation = parent != null ? Quaternion.Inverse(parent.rotation) * worldRotation : worldRotation;
            IsViewTransitioning = true;
        }
        private void OnDestroy() { if (modeLabel != null) Destroy(modeLabel.gameObject); }
    }
}
