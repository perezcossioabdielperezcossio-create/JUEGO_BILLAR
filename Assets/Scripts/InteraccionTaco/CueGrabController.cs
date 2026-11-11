using UnityEngine;
using QuestBilliards.Inputs;

namespace QuestBilliards.CueInteraction
{
    // Grip equips the cue. Release docks it without introducing ball physics.
    public sealed class CueGrabController : MonoBehaviour
    {
        public BilliardsInputReader inputs;
        public BilliardsPlayerMode playerMode;
        public Transform cue, leftHand, rightHand;
        public bool leftHandDominant;
        [Min(0.1f)] public float minimumBridgeDistance = 0.15f;
        [Min(0.2f)] public float maximumBridgeDistance = 1.1f;
        public float shaftCenterAheadOfGrip = 0.45f;
        public TextMesh statusLabel;
        public bool IsHeld { get; private set; }
        public bool IsBridged { get; private set; }
        private Vector3 dockPosition;
        private Quaternion dockRotation;
        private bool previousGrip;

        private void Awake()
        {
            if (cue != null) { dockPosition = cue.position; dockRotation = cue.rotation; }
        }
        private void LateUpdate()
        {
            if (inputs == null || cue == null || leftHand == null || rightHand == null) return;
            var dominant = leftHandDominant ? inputs.Left : inputs.Right;
            var support = leftHandDominant ? inputs.Right : inputs.Left;
            var hand = leftHandDominant ? leftHand : rightHand;
            var bridge = leftHandDominant ? rightHand : leftHand;
            bool hold = dominant.Tracked && (playerMode != null ? playerMode.IsAiming : dominant.Grip);
            if (!hold) Dock();
            else if (!previousGrip) IsHeld = true;
            previousGrip = hold;
            IsBridged = false;
            if (IsHeld)
            {
                Vector3 direction = hand.forward;
                Vector3 betweenHands = bridge.position - hand.position;
                float distance = betweenHands.magnitude;
                if (support.Tracked && support.Grip && distance >= minimumBridgeDistance && distance <= maximumBridgeDistance)
                { direction = betweenHands / distance; IsBridged = true; }
                Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f ? hand.right : Vector3.up;
                cue.SetPositionAndRotation(hand.position + direction * shaftCenterAheadOfGrip, Quaternion.LookRotation(direction, up));
            }
            if (statusLabel != null)
                statusLabel.text = IsHeld ? (IsBridged ? "TACO: APOYO CON DOS MANOS" : "TACO: EN MANO") : "TACO: LIBRE - ACTIVA GRIP PARA TOMAR";
        }
        private void Dock()
        {
            if (IsHeld && cue != null) cue.SetPositionAndRotation(dockPosition, dockRotation);
            IsHeld = false; IsBridged = false;
        }
        private void OnDisable() { Dock(); previousGrip = false; }
    }
}
