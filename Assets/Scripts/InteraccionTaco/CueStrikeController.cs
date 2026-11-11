using UnityEngine;
using QuestBilliards.BallPhysics;

namespace QuestBilliards.CueInteraction
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(CueGrabController))]
    public sealed class CueStrikeController : MonoBehaviour
    {
        public float halfCueLength = 0.725f;
        public float tipRadius = 0.006f;
        public float minimumSpeed = 0.08f;
        public float maximumSpeed = 3f;
        public float velocityTransfer = 0.9f;
        public int StrikeCount { get; private set; }
        public float LastBallSpeed { get; private set; }
        private CueGrabController grab;
        private Vector3 previousTip;
        private bool wasHeld, armed = true;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private BilliardBallBody blockedBall;
        private Vector3 blockedDirection;
        private float contactRadius;
        private BilliardBallBody[] balls;
        private Vector3 previousHand;
        private float backswing;
        private void Awake()
        {
            grab = GetComponent<CueGrabController>();
            balls = FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None);
        }
        public void ResetStrokeHistory()
        {
            wasHeld = false; armed = false; backswing = 0; blockedBall = null;
            previousTip = transform.position + transform.forward * halfCueLength;
        }
        private void FixedUpdate()
        {
            Vector3 tip = transform.position + transform.forward * halfCueLength;
            bool held = grab.IsHeld;
            bool practiceActive = false;
            var practice = GetComponent<DesktopCuePractice>();
            practiceActive = practice != null && practice.IsActive;
            held = practiceActive ? practice.IsStrokeInProgress : held;
            if (grab.playerMode != null && (!grab.playerMode.IsAiming || grab.playerMode.IsViewTransitioning)) held = false;
            var hand = grab.leftHandDominant ? grab.leftHand : grab.rightHand;
            Vector3 handPosition = hand != null ? hand.position : transform.position;
            if (!held || !wasHeld)
            { previousTip = tip; previousHand = handPosition; backswing = 0; wasHeld = held; armed = practiceActive; blockedBall = null; return; }
            Vector3 movement = tip - previousTip;
            Vector3 handMovement = handPosition - previousHand;
            previousHand = handPosition;
            previousTip = tip;
            float distance = movement.magnitude;
            float forward = Vector3.Dot(movement, transform.forward);
            float handForward = Vector3.Dot(handMovement, transform.forward);
            if (!practiceActive)
            {
                if (handMovement.magnitude > 0.25f) { armed = false; backswing = 0; return; }
                if (handForward < 0) backswing += -handForward;
                if (backswing >= 0.02f) armed = true;
                if (handForward <= 0 || handMovement.sqrMagnitude < 0.000001f) return;
                if (Vector3.Dot(handMovement.normalized, transform.forward) < 0.65f) return;
            }
            if (forward < -0.005f) blockedBall = null;
            // Equipping, tracking jumps and sideways sweeps must not strike a ball.
            if (!armed || distance < 0.0001f || distance > 0.5f || forward / distance < 0.35f) return;
            float speed = (practiceActive ? distance : handForward) / Time.fixedDeltaTime;
            if (speed < minimumSpeed) return;
            int count = Physics.SphereCastNonAlloc(tip - movement, tipRadius, movement / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            BilliardBallBody target = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var ball = hits[i].collider.GetComponentInParent<BilliardBallBody>();
                if (ball != null && ball.isCueBall && !ball.IsPocketed && hits[i].distance < nearest)
                { target = ball; nearest = hits[i].distance; }
            }
            // SphereCast does not report a collider that already overlaps its origin.
            // Check the complete tip segment against the actual spherical ball as well.
            Vector3 start = tip - movement;
            foreach (var ball in balls)
            {
                if (ball == null || !ball.isCueBall || ball.IsPocketed) continue;
                var collider = ball.GetComponentInChildren<SphereCollider>();
                if (collider == null || !collider.enabled) continue;
                Vector3 center = collider.transform.TransformPoint(collider.center);
                Vector3 scale = collider.transform.lossyScale;
                float radius = collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) + tipRadius;
                Vector3 toward = center - start;
                float along = Vector3.Dot(toward, movement / distance);
                if (along < 0) continue; // moving out of an overlap is not a strike
                float closest = Mathf.Clamp(along, 0, distance);
                if ((start + movement / distance * closest - center).sqrMagnitude > radius * radius) continue;
                float perpendicular = Mathf.Max(0, toward.sqrMagnitude - along * along);
                float entry = Mathf.Max(0, along - Mathf.Sqrt(Mathf.Max(0, radius * radius - perpendicular)));
                if (entry <= distance && entry < nearest) { target = ball; nearest = entry; }
            }
            if (target == null || target.Body == null) return;
            // First prototype: level central strike, without spin or jump shots.
            Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.5f) return;
            LastBallSpeed = Mathf.Min(speed, maximumSpeed) * velocityTransfer;
            target.Body.AddForce(direction * LastBallSpeed, ForceMode.VelocityChange);
            StrikeCount++;
            armed = false;
            backswing = 0;
            blockedBall = target;
            blockedDirection = movement / distance;
            var sphere = target.GetComponentInChildren<SphereCollider>();
            contactRadius = sphere != null ? sphere.bounds.extents.x + tipRadius : 0.035f;
            StopAtContact();
            previousTip = transform.position + transform.forward * halfCueLength;
        }
        private void LateUpdate() { if (blockedBall != null) StopAtContact(); }
        private void StopAtContact()
        {
            if (blockedBall == null) return;
            Vector3 tip = transform.position + transform.forward * halfCueLength;
            Vector3 stop = blockedBall.transform.position - blockedDirection * contactRadius;
            float penetration = Vector3.Dot(tip - stop, blockedDirection);
            if (penetration > 0) transform.position -= blockedDirection * penetration;
        }
    }
}
