using UnityEngine;
using QuestBilliards.BallPhysics;

namespace QuestBilliards.GameRules
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PocketTrigger : MonoBehaviour
    {
        public float openingRadius = 0.1f;
        public float surfaceHeight = 0.78f;
        public const float BallRadius = 0.028575f;
        private const float CueBounceRetention = 0.8f;
        private const float CueBounceClearance = 0.003f;
        public Bounds PlayableBounds { private get; set; }
        public int Captures { get; private set; }
        private void OnTriggerEnter(Collider other) { TryCapture(other); }
        private void OnTriggerStay(Collider other) { TryCapture(other); }
        private void TryCapture(Collider other)
        {
            var ball = other.GetComponentInParent<BilliardBallBody>();
            if (ball != null && !ball.isCueBall) TryCaptureMotion(ball, ball.Body.position, ball.Body.position);
        }

        public bool TryGetCueRebound(Vector3 start, Vector3 direction, float distance, out float entry, out Vector3 normal)
        {
            Vector3 offset = Vector3.ProjectOnPlane(start - transform.position, Vector3.up);
            float radius = openingRadius + BallRadius;
            float separation = offset.sqrMagnitude - radius * radius;
            float along = Vector3.Dot(offset, direction);
            entry = 0f;
            normal = Vector3.zero;
            if (separation > 0f)
            {
                float discriminant = along * along - separation;
                if (along >= 0f || discriminant < 0f) return false;
                entry = -along - Mathf.Sqrt(discriminant);
                if (entry > distance) return false;
            }
            normal = (offset + direction * entry).normalized;
            if (normal == Vector3.zero) normal = direction == Vector3.zero ? Vector3.forward : -direction;
            return true;
        }

        public bool TryReboundCueBall(BilliardBallBody ball, Vector3 previous, Vector3 current)
        {
            Vector3 travel = Vector3.ProjectOnPlane(current - previous, Vector3.up);
            if (!TryGetCueRebound(previous, travel.normalized, travel.magnitude, out _, out Vector3 normal)) return false;

            var body = ball.Body;
            Vector3 position = transform.position + normal * (openingRadius + BallRadius + CueBounceClearance);
            var bounds = PlayableBounds;
            if (bounds.size.x > 2f * BallRadius && bounds.size.z > 2f * BallRadius)
            {
                position.x = Mathf.Clamp(position.x, bounds.min.x + BallRadius, bounds.max.x - BallRadius);
                position.z = Mathf.Clamp(position.z, bounds.min.z + BallRadius, bounds.max.z - BallRadius);
            }
            position.y = surfaceHeight + BallRadius;
            body.position = position;
            Vector3 velocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            if (Vector3.Dot(velocity, normal) < 0f) velocity = Vector3.Reflect(velocity, normal);
            body.linearVelocity = velocity * CueBounceRetention;
            return true;
        }

        public bool TryCaptureMotion(BilliardBallBody ball, Vector3 previous, Vector3 current)
        {
            if (ball == null || ball.isCueBall || ball.IsPocketed) return false;
            Vector3 flatStart = Vector3.ProjectOnPlane(previous - transform.position, Vector3.up);
            Vector3 flatDelta = Vector3.ProjectOnPlane(current - previous, Vector3.up);
            float t = flatDelta.sqrMagnitude > 0.000001f ? Mathf.Clamp01(-Vector3.Dot(flatStart, flatDelta) / flatDelta.sqrMagnitude) : 1;
            Vector3 closest = Vector3.Lerp(previous, current, t);
            // Capture on a fall or when the whole ball clears the cloth at the mouth.
            float centerHeight = surfaceHeight + BallRadius;
            if (closest.y > centerHeight + 0.01f) return false;
            float catchRadius = openingRadius + 0.012f;
            if (closest.y > centerHeight - 0.004f) catchRadius = openingRadius - BallRadius * 0.65f;
            if (Vector3.ProjectOnPlane(closest - transform.position, Vector3.up).sqrMagnitude > catchRadius * catchRadius) return false;
            if (!ball.TryPocketAt(transform.position)) return false;
            Captures++; return true;
        }
    }
}
