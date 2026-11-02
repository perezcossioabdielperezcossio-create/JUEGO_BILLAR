using UnityEngine;
using QuestBilliards.BallPhysics;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace QuestBilliards.CueInteraction
{
    // Editor training drives the cue, not the ball: impacts still use CueStrikeController.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CueGrabController), typeof(CueStrikeController))]
    public sealed class DesktopCuePractice : MonoBehaviour
    {
        public bool IsActive { get; private set; }
        public string Status { get; private set; }
        public bool IsAlignedWithWhite
        {
            get
            {
#if UNITY_EDITOR
                if (white == null) return false;
                Vector3 tip = transform.position + transform.forward * 0.725f;
                Vector3 delta = white.transform.position - tip;
                return Vector3.Dot(delta, transform.forward) > 0 && delta.magnitude < 0.4f && Vector3.Cross(delta, transform.forward).magnitude < 0.003f;
#else
                return false;
#endif
            }
        }
        public bool IsStrokeInProgress
        {
            get
            {
#if UNITY_EDITOR
                return IsActive && stroking;
#else
                return false;
#endif
            }
        }
#if UNITY_EDITOR
        private CueGrabController grab;
        private BilliardBallBody white;
        private BilliardBallBody[] balls;
        private Quaternion dockRotation;
        private Vector3 dockPosition;
        private float yaw, charge, strokeTime, strokeCharge;
        private float elevation = 8f, elevationAxis;
        private bool charging, stroking, pendingRelease;
        private float aimAxis;
        private TextMesh label;
        private QuestBilliards.Inputs.BilliardsInputDiagnostics diagnostics;
        private bool diagnosticsWasEnabled;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousRouting;
        private bool savedInputSettings;
        private bool previousPrimary, previousTrigger;
        private QuestBilliards.Inputs.BilliardsPlayerMode playerMode;
        private bool previousAimMode;
        private bool needsPreparation;
        private float stableTime;
        private CueTableConstraint tableConstraint;
        private CueStrikeController strike;
        private int lastAimSession = -1;
        public int CompletedStrokes { get; private set; }
        private void Start()
        {
            grab = GetComponent<CueGrabController>();
            tableConstraint = GetComponent<CueTableConstraint>(); strike = GetComponent<CueStrikeController>();
            playerMode = grab.playerMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            savedInputSettings = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            if (Keyboard.current != null) InputSystem.EnableDevice(Keyboard.current);
            diagnostics = FindFirstObjectByType<QuestBilliards.Inputs.BilliardsInputDiagnostics>();
            diagnosticsWasEnabled = diagnostics != null && diagnostics.enabled;
            dockPosition = transform.position; dockRotation = transform.rotation;
            balls = FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None);
            foreach (var ball in balls) if (ball.isCueBall) white = ball;
            if (Camera.main != null)
            {
                var go = new GameObject("DesktopCueHelp");
                go.transform.SetParent(Camera.main.transform, false);
                go.transform.localPosition = new Vector3(0, -0.16f, 0.85f);
                label = go.AddComponent<TextMesh>(); label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center; label.fontSize = 40; label.characterSize = 0.007f;
                label.color = Color.cyan;
            }
            SetActive(false);
        }
        private void SetActive(bool active)
        {
            IsActive = active && white != null;
            charging = stroking = pendingRelease = false; charge = 0;
            needsPreparation = IsActive; stableTime = 0;
            grab.enabled = !IsActive;
            if (diagnostics != null) diagnostics.enabled = !IsActive && diagnosticsWasEnabled;
            if (grab.statusLabel != null) grab.statusLabel.gameObject.SetActive(!IsActive);
            if (!IsActive) transform.SetPositionAndRotation(dockPosition, dockRotation);
        }
        private bool BallsStopped()
        {
            if (white == null || white.IsPocketed) return false;
            foreach (var b in balls)
                if (b != null && !b.IsPocketed && b.Body != null && b.Body.linearVelocity.sqrMagnitude > 0.0025f) return false;
            return true;
        }
        private void Update()
        {
            var keys = Keyboard.current;
            bool xrPrimary = grab.inputs != null && (grab.inputs.Left.PrimaryButton || grab.inputs.Right.PrimaryButton);
            bool xrTrigger = grab.inputs != null && (grab.inputs.Left.Trigger || grab.inputs.Right.Trigger);
            bool primaryPressed = xrPrimary && !previousPrimary;
            bool triggerPressed = xrTrigger && !previousTrigger;
            previousPrimary = xrPrimary; previousTrigger = xrTrigger;
            if (label != null) label.gameObject.SetActive(playerMode == null || !playerMode.ControlsLocked);
            if (playerMode != null && playerMode.ControlsLocked) return;
            if (playerMode != null)
            {
                if (playerMode.IsAiming != previousAimMode) SetActive(playerMode.IsAiming);
                previousAimMode = playerMode.IsAiming;
                if (playerMode.IsAiming && lastAimSession != playerMode.AimSessionVersion)
                {
                    Vector3 direction = playerMode.AimEntryDirection;
                    yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    elevation = 8f; needsPreparation = true; stableTime = 0;
                    lastAimSession = playerMode.AimSessionVersion;
                }
                if (keys != null && keys.f2Key.wasPressedThisFrame && playerMode.IsAiming) SetActive(!IsActive);
            }
            else if ((keys != null && keys.f2Key.wasPressedThisFrame) || primaryPressed) SetActive(!IsActive);
            // A real/simulated grip always gives the cue back to tracked hands.
            if (IsActive && grab.inputs != null)
            {
                var dominant = grab.leftHandDominant ? grab.inputs.Left : grab.inputs.Right;
                if (dominant.Tracked && dominant.Grip) SetActive(false);
            }
            if (!IsActive) { if (label != null) label.text = playerMode != null ? "X izquierdo: movimiento / apuntado\nSIM: B equivale a X | F2: ayuda de golpe al apuntar" : "SIMULADOR: B activa practica (soltar U)\nUNITY: F2 activa practica"; return; }
            float stick = grab.inputs != null ? grab.inputs.Right.Stick.x : 0;
            float keyboardAim = keys != null ? (keys.lKey.isPressed ? 1 : 0) - (keys.jKey.isPressed ? 1 : 0) : 0;
            aimAxis = Mathf.Abs(stick) > 0.15f ? stick : keyboardAim;
            float vertical = grab.inputs != null ? grab.inputs.Right.Stick.y : 0;
            float keyboardVertical = keys != null ? (keys.iKey.isPressed ? 1 : 0) - (keys.kKey.isPressed ? 1 : 0) : 0;
            elevationAxis = Mathf.Abs(vertical) > 0.15f ? vertical : keyboardVertical;
            if (keys != null && keys.backspaceKey.wasPressedThisFrame)
            {
                foreach (var b in balls) if (b != null && b.Body != null) b.ResetBall();
                charging = stroking = pendingRelease = false; charge = 0;
                needsPreparation = true; stableTime = 0;
            }
            bool spaceHeld = keys != null && keys.spaceKey.isPressed;
            bool spacePressed = keys != null && keys.spaceKey.wasPressedThisFrame;
            if (!stroking && !needsPreparation && (spacePressed || triggerPressed) && BallsStopped() && (playerMode == null || !playerMode.IsViewTransitioning) && TryAlignRestPose(false)) { charging = true; charge = 0; }
            if (charging && !spaceHeld && !xrTrigger) { pendingRelease = true; charging = false; }
            Status = stroking ? "GOLPE" : charging ? $"FUERZA {charge / 0.22f:P0}" : !BallsStopped() ? "ESPERA A QUE PAREN LAS BOLAS" : needsPreparation || !IsAlignedWithWhite || playerMode != null && playerMode.IsViewTransitioning ? "ALINEANDO CON LA BLANCA" : "LISTO";
            if (label != null) label.text = "PRACTICA: " + Status + $" | ANGULO {elevation:F0} grados\nSIM: G/J horizontal | Y/H vertical | T golpe\nUNITY: J/L horizontal | I/K vertical | ESPACIO";
        }
        private void FixedUpdate()
        {
            if (!IsActive || white == null) return;
            if (!needsPreparation && !stroking && !charging && !pendingRelease && BallsStopped() && !IsAlignedWithWhite)
            { needsPreparation = true; stableTime = 0; }
            if (needsPreparation && !stroking && !charging && !pendingRelease)
            {
                stableTime = BallsStopped() ? stableTime + Time.fixedDeltaTime : 0;
                if (stableTime >= 0.25f && TryAlignRestPose(true)) needsPreparation = false;
                return;
            }
            if (!stroking)
            {
                yaw += aimAxis * 35f * Time.fixedDeltaTime;
                elevation = Mathf.Clamp(elevation + elevationAxis * 20f * Time.fixedDeltaTime, 0, 75);
            }
            if (charging) charge = Mathf.Min(0.22f, charge + Time.fixedDeltaTime * 0.18f);
            if (pendingRelease) { pendingRelease = false; stroking = true; strokeTime = 0; strokeCharge = charge; }
            Vector3 direction = Quaternion.Euler(elevation, yaw, 0) * Vector3.forward;
            float behind = 0.085f + charge;
            // Lock the aiming point for the entire stroke so the cue doesn't chase a moving ball.
            if (stroking)
            {
                strokeTime += Time.fixedDeltaTime;
                behind = Mathf.Lerp(0.085f + strokeCharge, -0.045f, Mathf.Clamp01(strokeTime / 0.12f));
                if (strokeTime >= 0.12f) { stroking = false; charge = 0; CompletedStrokes++; needsPreparation = true; stableTime = 0; }
            }
            if (!stroking && !charging && !pendingRelease && strokeTime == 0 && BallsStopped()) aimPoint = white.transform.position;
            if (charging) aimPoint = white.transform.position;
            transform.SetPositionAndRotation(aimPoint - direction * (0.725f + behind), Quaternion.LookRotation(direction));
            if (!stroking && strokeTime > 0) strokeTime = 0;
        }
        private bool TryAlignRestPose(bool refreshView)
        {
            if (white == null || !BallsStopped()) return false;
            aimPoint = white.transform.position;
            for (float angle = elevation; angle <= 75.01f; angle += 2f)
            {
                Quaternion rotation = Quaternion.Euler(angle, yaw, 0);
                Vector3 direction = rotation * Vector3.forward;
                Vector3 position = aimPoint - direction * (0.725f + 0.085f);
                Vector3 withdrawn = aimPoint - direction * (0.725f + 0.085f + 0.22f);
                if (tableConstraint != null && !tableConstraint.IsPoseFree(withdrawn, rotation)) continue;
                if (tableConstraint != null && !tableConstraint.TryPlaceAtRest(position, rotation)) continue;
                if (tableConstraint == null) transform.SetPositionAndRotation(position, rotation);
                elevation = angle; charge = 0;
                if (strike != null) strike.ResetStrokeHistory();
                if (refreshView && playerMode != null) playerMode.RefreshAimingView(direction);
                return IsAlignedWithWhite;
            }
            return false;
        }
        private Vector3 aimPoint;
        private void OnDisable()
        {
            IsActive = false;
            if (savedInputSettings)
            {
                InputSystem.settings.backgroundBehavior = previousBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
            }
            if (diagnostics != null) diagnostics.enabled = diagnosticsWasEnabled;
            if (grab != null) { grab.enabled = true; if (grab.statusLabel != null) grab.statusLabel.gameObject.SetActive(true); }
            if (label != null) Destroy(label.gameObject);
        }
#endif
    }
}
