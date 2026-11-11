using UnityEngine;
using QuestBilliards.BallPhysics;
using QuestBilliards.Inputs;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace QuestBilliards.GameRules
{
    [DefaultExecutionOrder(-150)]
    public sealed class CueBallPlacement : MonoBehaviour
    {
        public BilliardBallBody whiteBall;
        public BilliardsInputReader inputs;
        public BilliardsPlayerMode mode;
        public Transform rightHand, head;
        public Collider tableSurface;
        public Material previewMaterial;
        public bool HasValidPlacement { get; private set; }
        public Vector3 ProposedPosition { get; private set; }
        private LineRenderer ring, rayLine;
        private TextMesh label;
        private bool previousTrigger;
        private BilliardBallBody[] balls;
        private PocketTrigger[] pockets;
        private void Start()
        {
            balls = FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None);
            pockets = FindObjectsByType<PocketTrigger>(FindObjectsSortMode.None);
            ring = Line("CueBallPlacementRing", 0.004f); ring.loop = true; ring.positionCount = 40;
            rayLine = Line("CueBallPlacementRay", 0.002f); rayLine.positionCount = 2;
            if (head != null)
            {
                var go = new GameObject("ScratchInstructions"); go.transform.SetParent(head, false);
                go.transform.localPosition = new Vector3(0, -0.06f, 0.85f);
                label = go.AddComponent<TextMesh>(); label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                label.fontSize = 40; label.characterSize = 0.007f; label.color = Color.yellow;
            }
        }
        private LineRenderer Line(string name, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = previewMaterial;
            line.useWorldSpace = true; line.startWidth = line.endWidth = width; line.enabled = false; return line;
        }
        public bool IsPositionLegal(Vector3 position)
        {
            if (tableSurface == null) return false;
            var b = tableSurface.bounds; const float radius = 0.028575f;
            if (position.x < b.min.x + radius || position.x > b.max.x - radius || position.z < b.min.z + radius || position.z > b.max.z - radius) return false;
            foreach (var ball in balls)
                if (ball != null && ball != whiteBall && !ball.IsPocketed && ball.gameObject.activeInHierarchy && Vector3.Distance(position, ball.transform.position) < 0.062f) return false;
            foreach (var pocket in pockets)
                if (Vector3.ProjectOnPlane(position - pocket.transform.position, Vector3.up).magnitude < pocket.openingRadius + radius + 0.005f) return false;
            return tableSurface.Raycast(new Ray(position + Vector3.up * 0.2f, Vector3.down), out _, 0.3f);
        }
        public bool ConfirmPlacement(Vector3 position)
        {
            if (whiteBall == null || !whiteBall.NeedsPlacement || !IsPositionLegal(position)) return false;
            position.y = tableSurface.bounds.max.y + 0.028575f;
            if (!whiteBall.PlaceCueBall(position)) return false;
            if (mode != null) mode.RefreshAimFromCurrentLook();
            return true;
        }
        private void Update()
        {
            bool trigger = inputs != null && inputs.Right.Trigger;
            bool confirm = trigger && !previousTrigger;
            previousTrigger = trigger;
#if UNITY_EDITOR
            confirm |= Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#endif
            bool scratch = whiteBall != null && whiteBall.IsPocketed;
            if (label != null) label.gameObject.SetActive(scratch);
            ring.enabled = rayLine.enabled = false; HasValidPlacement = false;
            if (!scratch) return;
            if (!whiteBall.NeedsPlacement) { if (label != null) label.text = "FALTA: BLANCA EMBOCADA\nEspera a que se detengan las bolas"; return; }
            RaycastHit hit; Transform source = rightHand;
            bool found = source != null && tableSurface.Raycast(new Ray(source.position, source.forward), out hit, 8f);
            if (!found)
            {
                source = head;
                found = source != null && tableSurface.Raycast(new Ray(source.position, source.forward), out hit, 8f);
            }
            if (label != null) label.text = "FALTA: BOLA EN MANO\nApunta a un lugar libre del paño\nGatillo derecho / T: colocar | Unity: Enter";
            if (!found) return;
            // RaycastHit is initialized by the successful surface raycast above.
            tableSurface.Raycast(new Ray(source.position, source.forward), out hit, 8f);
            ProposedPosition = hit.point + Vector3.up * 0.028575f;
            HasValidPlacement = IsPositionLegal(ProposedPosition);
            Color color = HasValidPlacement ? Color.green : Color.red;
            ring.enabled = rayLine.enabled = true; ring.startColor = ring.endColor = rayLine.startColor = rayLine.endColor = color;
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; ring.SetPosition(i, hit.point + new Vector3(Mathf.Cos(a) * 0.03f, 0.003f, Mathf.Sin(a) * 0.03f)); }
            rayLine.SetPosition(0, source.position); rayLine.SetPosition(1, hit.point);
            if (confirm && HasValidPlacement) ConfirmPlacement(ProposedPosition);
        }
        private void OnDestroy() { if (label != null) Destroy(label.gameObject); }
    }
}
