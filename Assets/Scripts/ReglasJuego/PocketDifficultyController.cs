using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuestBilliards.GameRules
{
    public enum BilliardsDifficulty { Easy, Normal, Hard }
    public sealed class PocketDifficultyController : MonoBehaviour
    {
        public MeshFilter playingSurface;
        public MeshCollider surfaceCollider;
        public Mesh pocketWallMesh;
        public Material pocketWallMaterial;
        public PhysicsMaterial pocketContactMaterial;
        public BilliardsDifficulty CurrentDifficulty { get; private set; } = BilliardsDifficulty.Easy;
        public float OpeningRadius { get; private set; } = 0.1f;
        private Mesh generatedSurface;
        public static float RadiusFor(BilliardsDifficulty level) => level == BilliardsDifficulty.Easy ? 0.1f : level == BilliardsDifficulty.Normal ? 0.08f : 0.065f;
        public void Apply(BilliardsDifficulty level)
        {
            CurrentDifficulty = level; OpeningRadius = RadiusFor(level);
            var pockets = GetComponentsInChildren<PocketTrigger>(true);
            foreach (var pocket in pockets)
            {
                pocket.openingRadius = OpeningRadius;
                pocket.GetComponent<SphereCollider>().radius = OpeningRadius - 0.005f;
                var well = pocket.transform.Find("DarkWell");
                if (well != null) well.localScale = new Vector3(2 * (OpeningRadius + 0.01f), well.localScale.y, 2 * (OpeningRadius + 0.01f));
                var rim = pocket.transform.Find("PocketRim");
                if (rim != null) rim.localScale = new Vector3(OpeningRadius / 0.1f, 1, OpeningRadius / 0.1f);
                BuildPocketWalls(pocket);
            }
            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                var scale = part.localScale;
                if (part.name == "SideRail") { scale.z = 1.26f - 2 * OpeningRadius; part.localScale = scale; }
                if (part.name == "EndRail") { scale.x = 1.27f - 2 * OpeningRadius; part.localScale = scale; }
            }
            BuildFlatSurface(pockets);
            var cue = FindFirstObjectByType<QuestBilliards.CueInteraction.CueTableConstraint>();
            if (cue != null) cue.RefreshSolidObstacles();
        }
        private void BuildPocketWalls(PocketTrigger pocket)
        {
            for (int i = pocket.transform.childCount - 1; i >= 0; i--)
            {
                var previous = pocket.transform.GetChild(i);
                if (previous.name != "PocketPhysicsWalls") continue;
                previous.gameObject.SetActive(false); Destroy(previous.gameObject);
            }
            var root = new GameObject("PocketPhysicsWalls"); root.transform.SetParent(pocket.transform, false);
            float radius = OpeningRadius + 0.012f;
            float length = 2 * radius * Mathf.Tan(Mathf.PI / 12) + 0.008f;
            for (int i = 0; i < 12; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2 / 12;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                MakePocketWall(root.transform, radial * radius + Vector3.up * (0.54f - pocket.transform.position.y), new Vector3(length,0.34f,0.018f), radial, false);
                Vector3 edge = pocket.transform.position + radial * radius;
                var b = surfaceCollider.bounds;
                if (edge.x < b.min.x || edge.x > b.max.x || edge.z < b.min.z || edge.z > b.max.z)
                    MakePocketWall(root.transform, radial * radius + Vector3.up * (0.80f - pocket.transform.position.y), new Vector3(length,0.16f,0.018f), radial, true);
            }
        }
        private void MakePocketWall(Transform parent, Vector3 pos, Vector3 scale, Vector3 outward, bool visible)
        {
            var go = new GameObject(visible ? "PocketOuterGuard" : "PocketWellWall");
            go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(outward); go.transform.localScale = scale;
            var collider = go.AddComponent<BoxCollider>(); collider.sharedMaterial = pocketContactMaterial;
            if (visible && pocketWallMesh != null && pocketWallMaterial != null)
            { go.AddComponent<MeshFilter>().sharedMesh = pocketWallMesh; go.AddComponent<MeshRenderer>().sharedMaterial = pocketWallMaterial; }
        }
        private void BuildFlatSurface(PocketTrigger[] pockets)
        {
            const int nx = 96, nz = 192;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++)
            {
                float x0 = -0.635f + 1.27f * x / nx, x1 = -0.635f + 1.27f * (x + 1) / nx;
                float z0 = -1.27f + 2.54f * z / nz, z1 = -1.27f + 2.54f * (z + 1) / nz;
                Vector3 mid = transform.TransformPoint(new Vector3((x0 + x1) * 0.5f, 0.78f, (z0 + z1) * 0.5f));
                bool hole = false;
                foreach (var p in pockets) if (Vector3.ProjectOnPlane(mid - p.transform.position, Vector3.up).sqrMagnitude < OpeningRadius * OpeningRadius) { hole = true; break; }
                if (hole) continue;
                int n = vertices.Count;
                foreach (var v in new[] { new Vector3(x0,0.78f,z0), new Vector3(x0,0.78f,z1), new Vector3(x1,0.78f,z0), new Vector3(x1,0.78f,z1) })
                { vertices.Add(playingSurface.transform.InverseTransformPoint(transform.TransformPoint(v))); uv.Add(new Vector2(v.x / 1.27f, v.z / 2.54f)); }
                triangles.AddRange(new[] { n,n+1,n+2,n+2,n+1,n+3 });
            }
            var mesh = new Mesh { name = "FlatTable_" + CurrentDifficulty, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            playingSurface.sharedMesh = mesh; surfaceCollider.sharedMesh = null; surfaceCollider.sharedMesh = mesh;
            if (generatedSurface != null) Destroy(generatedSurface);
            generatedSurface = mesh;
            Physics.SyncTransforms();
        }
        private void OnDestroy() { if (generatedSurface != null) Destroy(generatedSurface); }
    }
}
