using UnityEngine;

namespace QuestBilliards.CueInteraction
{
    [DefaultExecutionOrder(50)]
    public sealed class CueTableConstraint : MonoBehaviour
    {
        public Collider[] tableColliders;
        public CapsuleCollider cueShape;
        public bool IsBlocked { get; private set; }
        private Vector3 safePosition;
        private Quaternion safeRotation;
        private bool initialized;
        private CueGrabController grab;
        private DesktopCuePractice practice;
        private void Awake() { grab = GetComponent<CueGrabController>(); practice = GetComponent<DesktopCuePractice>(); RefreshSolidObstacles(); }
        public void RefreshSolidObstacles()
        {
            var solids = new System.Collections.Generic.List<Collider>();
            foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!col.enabled || col.isTrigger || col is CharacterController || col.transform.IsChildOf(transform)) continue;
                if (col.GetComponentInParent<QuestBilliards.BallPhysics.BilliardBallBody>() != null || col.GetComponentInParent<QuestBilliards.Inputs.BilliardsPlayerMode>() != null) continue;
                solids.Add(col);
            }
            tableColliders = solids.ToArray();
        }
        private void FixedUpdate() { Constrain(); }
        private void LateUpdate() { Constrain(); }
        public bool TryPlaceAtRest(Vector3 position, Quaternion rotation)
        {
            if (cueShape == null || tableColliders == null || Overlaps(position, rotation, out _)) return false;
            safePosition = position; safeRotation = rotation; initialized = true; IsBlocked = false;
            transform.SetPositionAndRotation(position, rotation);
            return true;
        }
        public bool IsPoseFree(Vector3 position, Quaternion rotation)
        {
            return cueShape != null && tableColliders != null && !Overlaps(position, rotation, out _);
        }
        private bool Overlaps(Vector3 p, Quaternion q, out Vector3 correction)
        {
            correction = Vector3.zero; bool overlap = false;
            var candidateBounds = new Bounds(p, Vector3.one * (cueShape.height + 0.05f));
            foreach (var col in tableColliders)
            {
                if (col == null || !col.enabled) continue;
                if (!candidateBounds.Intersects(col.bounds)) continue;
                if (Physics.ComputePenetration(cueShape, p, q, col, col.transform.position, col.transform.rotation, out var direction, out var distance))
                { overlap = true; correction += direction * (distance + 0.001f); }
            }
            return overlap;
        }
        private void Constrain()
        {
            if (cueShape == null || tableColliders == null) return;
            bool active = grab != null && grab.IsHeld || practice != null && practice.IsActive;
            if (!active) { initialized = false; IsBlocked = false; return; }
            Vector3 desired = transform.position; Quaternion rotation = transform.rotation;
            IsBlocked = false;
            if (!initialized)
            {
                for (int i = 0; i < 12 && Overlaps(desired, rotation, out var correction); i++) { desired += correction; IsBlocked = true; }
                if (Overlaps(desired, rotation, out _)) return;
                safePosition = desired; safeRotation = rotation; initialized = true;
            }
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(safePosition, desired) / 0.015f, Quaternion.Angle(safeRotation, rotation) / 2f)), 1, 128);
            Vector3 from = safePosition; Quaternion fromRotation = safeRotation;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 candidate = Vector3.Lerp(from, desired, t); Quaternion angle = Quaternion.Slerp(fromRotation, rotation, t);
                if (Overlaps(candidate, angle, out _)) { IsBlocked = true; break; }
                safePosition = candidate; safeRotation = angle;
            }
            transform.SetPositionAndRotation(safePosition, safeRotation);
        }
    }
}
