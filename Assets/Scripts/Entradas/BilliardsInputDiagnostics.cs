using UnityEngine;

namespace QuestBilliards.Inputs
{
    [RequireComponent(typeof(BilliardsInputReader))]
    public sealed class BilliardsInputDiagnostics : MonoBehaviour
    {
        public bool showDetailedReadout;
        private BilliardsInputReader reader;
        private TextMesh headsetLabel;
        private Renderer leftMarker, rightMarker;
        private void Awake() { reader = GetComponent<BilliardsInputReader>(); }
        private void Update()
        {
            var camera = Camera.main;
            if (!showDetailedReadout && headsetLabel != null) { Destroy(headsetLabel.gameObject); headsetLabel = null; }
            if (showDetailedReadout && headsetLabel == null && camera != null)
            {
                var label = new GameObject("InputStatusInHeadset");
                label.transform.SetParent(camera.transform, false);
                label.transform.localPosition = new Vector3(0, -0.12f, 0.8f);
                headsetLabel = label.AddComponent<TextMesh>();
                headsetLabel.anchor = TextAnchor.MiddleCenter;
                headsetLabel.alignment = TextAlignment.Center;
                headsetLabel.characterSize = 0.012f;
                headsetLabel.fontSize = 48;
                headsetLabel.color = Color.white;
            }
            if (headsetLabel != null)
                headsetLabel.text = "PRUEBA DE CONTROLES\n" + Status("IZQ", reader.Left) + "\n" + Status("DER", reader.Right) + "\nU: agarre | T: gatillo";
            if (leftMarker == null) leftMarker = GameObject.Find("LeftHandAnchor_TrackingMarker")?.GetComponent<Renderer>();
            if (rightMarker == null) rightMarker = GameObject.Find("RightHandAnchor_TrackingMarker")?.GetComponent<Renderer>();
            Tint(leftMarker, reader.Left);
            Tint(rightMarker, reader.Right);
        }
        private static string Status(string name, HandInput h) => $"{name}: {(h.Tracked ? "OK" : "SIN TRACKING")}  AGARRE {h.GripValue:F1}  GATILLO {h.TriggerValue:F1}";
        private static void Tint(Renderer marker, HandInput h)
        {
            if (marker == null) return;
            var color = !h.Tracked ? Color.gray : h.Grip && h.Trigger ? Color.magenta : h.Grip ? Color.green : h.Trigger ? new Color(1f, 0.45f, 0f) : Color.white;
            marker.material.color = color;
        }
        private void OnDestroy()
        {
            if (headsetLabel != null) Destroy(headsetLabel.gameObject);
        }
        private void OnDisable()
        {
            if (headsetLabel != null) Destroy(headsetLabel.gameObject);
            headsetLabel = null;
        }
        private void OnGUI()
        {
            if (!showDetailedReadout) return;
            GUILayout.BeginArea(new Rect(15, 15, 590, 190), GUI.skin.box);
            GUILayout.Label("BILLAR - Diagnostico de controles XR (etapa 1)");
            Draw("Izquierda", reader.Left);
            Draw("Derecha", reader.Right);
            GUILayout.Label("Sin seguimiento: agarre y gatillo quedan desactivados.");
            GUILayout.EndArea();
        }
        private static void Draw(string label, HandInput h)
        {
            GUILayout.Label($"{label}: conectado={h.Connected} seguimiento={h.Tracked}");
            GUILayout.Label($"Grip={h.GripValue:F2} Trigger={h.TriggerValue:F2} Boton={h.PrimaryButton} Stick={h.Stick}");
            GUILayout.Label($"Posicion local={h.Position:F3}");
        }
    }
}
