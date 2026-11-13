using UnityEngine;
using QuestBilliards.Inputs;
using QuestBilliards.CueInteraction;

namespace QuestBilliards.BallPhysics
{
    public sealed class BallVisualGuide : MonoBehaviour
    {
        public BilliardsPlayerMode mode;
        public Transform cue;
        public Collider surface;
        public Material lineMaterial;
        private BilliardBallBody body;
        private BilliardBallBody[] balls;
        private QuestBilliards.GameRules.PocketTrigger[] pockets;
        private LineRenderer guide;
        private TrailRenderer trail;
        private const float Radius = 0.028575f;
        private void Awake()
        {
            body = GetComponent<BilliardBallBody>();
            balls = FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None);
            pockets = FindObjectsByType<QuestBilliards.GameRules.PocketTrigger>(FindObjectsSortMode.None);
            var path = new GameObject("AimTrajectory"); path.transform.SetParent(transform, false);
            guide = path.AddComponent<LineRenderer>(); guide.useWorldSpace = true; guide.sharedMaterial = lineMaterial;
            guide.startWidth = guide.endWidth = 0.006f; guide.startColor = guide.endColor = Color.cyan;
            trail = gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial = lineMaterial;
            trail.time = 0.8f; trail.minVertexDistance = 0.01f; trail.startWidth = 0.012f; trail.endWidth = 0;
            trail.startColor = new Color(1,1,1,0.7f); trail.endColor = new Color(1,1,1,0); trail.emitting = false;
        }
        private void LateUpdate()
        {
            if (body.Body == null) return;
            trail.emitting = !body.IsPocketed && body.Body.linearVelocity.sqrMagnitude > 0.0025f;
            guide.enabled = !body.IsPocketed && mode != null && mode.IsAiming && !trail.emitting && cue != null && surface != null;
            if (!guide.enabled) return;
            Vector3 start = transform.position;
            Vector3 dir = Vector3.ProjectOnPlane(cue.forward, Vector3.up).normalized;
            if (dir.sqrMagnitude < 0.5f) { guide.enabled = false; return; }
            float first = Segment(start, dir, out var normal, out bool hitBall);
            Vector3 end = start + dir * first;
            guide.positionCount = hitBall ? 2 : 3;
            guide.SetPosition(0, start + Vector3.up * 0.008f); guide.SetPosition(1, end + Vector3.up * 0.008f);
            if (!hitBall)
            {
                Vector3 reflected = Vector3.Reflect(dir, normal);
                float next = Segment(end + reflected * 0.002f, reflected, out _, out _);
                guide.SetPosition(2, end + reflected * Mathf.Min(next, 1.2f) + Vector3.up * 0.008f);
            }
        }
        private float Segment(Vector3 start, Vector3 dir, out Vector3 normal, out bool hitBall)
        {
            Bounds b = surface.bounds;
            float x = Mathf.Abs(dir.x) > 0.0001f ? ((dir.x > 0 ? b.max.x - Radius : b.min.x + Radius) - start.x) / dir.x : float.PositiveInfinity;
            float z = Mathf.Abs(dir.z) > 0.0001f ? ((dir.z > 0 ? b.max.z - Radius : b.min.z + Radius) - start.z) / dir.z : float.PositiveInfinity;
            float distance = Mathf.Clamp(Mathf.Min(x, z), 0, 4f);
            normal = x < z ? Vector3.right : Vector3.forward; hitBall = false;
            foreach (var ball in balls)
            {
                if (ball == null || ball == body || ball.IsPocketed || !ball.gameObject.activeInHierarchy) continue;
                Vector3 delta = Vector3.ProjectOnPlane(ball.transform.position - start, Vector3.up);
                float projection = Vector3.Dot(delta, dir);
                float perpendicular = delta.sqrMagnitude - projection * projection;
                float diameter = 2 * Radius;
                if (projection <= 0 || perpendicular > diameter * diameter) continue;
                float entry = projection - Mathf.Sqrt(Mathf.Max(0, diameter * diameter - perpendicular));
                if (entry >= 0 && entry < distance) { distance = entry; hitBall = true; }
            }
            foreach (var pocket in pockets)
            {
                if (!pocket.TryGetCueRebound(start, dir, distance, out float entry, out Vector3 reboundNormal)) continue;
                if (entry >= distance) continue;
                distance = entry;
                normal = reboundNormal;
                hitBall = false;
            }
            return distance;
        }
    }
}
