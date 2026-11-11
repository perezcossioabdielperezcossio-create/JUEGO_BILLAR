using UnityEngine;
using QuestBilliards.BallPhysics;

namespace QuestBilliards.GameRules
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PocketTrigger : MonoBehaviour
    {
        public float openingRadius = 0.1f;
        public float surfaceHeight = 0.78f;
        public int Captures { get; private set; }
        private void OnTriggerEnter(Collider other) { TryCapture(other); }
        private void OnTriggerStay(Collider other) { TryCapture(other); }
        private void TryCapture(Collider other)
        {
            var ball = other.GetComponentInParent<BilliardBallBody>();
            if (ball != null) TryCaptureMotion(ball, ball.transform.position, ball.transform.position);
        }
        public bool TryCaptureMotion(BilliardBallBody ball, Vector3 previous, Vector3 current)
        {
            if (ball == null || ball.IsPocketed) return false;
            Vector3 flatStart = Vector3.ProjectOnPlane(previous - transform.position, Vector3.up);
            Vector3 flatDelta = Vector3.ProjectOnPlane(current - previous, Vector3.up);
            float t = flatDelta.sqrMagnitude > 0.000001f ? Mathf.Clamp01(-Vector3.Dot(flatStart, flatDelta) / flatDelta.sqrMagnitude) : 1;
            Vector3 closest = Vector3.Lerp(previous, current, t);
            // Require a real fall from the flat cloth, but don't wait until a fast ball has left the pocket.
            const float ballRadius = 0.028575f;
            if (closest.y > surfaceHeight + ballRadius - 0.004f) return false;
            float catchRadius = openingRadius + 0.012f;
            if (Vector3.ProjectOnPlane(closest - transform.position, Vector3.up).sqrMagnitude > catchRadius * catchRadius) return false;
            if (!ball.TryPocketAt(transform.position)) return false;
            Captures++; return true;
        }
    }
}
