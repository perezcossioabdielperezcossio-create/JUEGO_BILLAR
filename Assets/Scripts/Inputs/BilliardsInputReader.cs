using UnityEngine;
using UnityEngine.XR;

namespace QuestBilliards.Inputs
{
    public struct HandInput
    {
        public bool Connected, Tracked, Grip, Trigger, PrimaryButton;
        public float GripValue, TriggerValue;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector2 Stick;
    }

    // Quest and Meta XR Simulator both feed the active OpenXR runtime.
    public sealed class BilliardsInputReader : MonoBehaviour
    {
        [Range(0.1f, 0.95f)] public float pressThreshold = 0.65f;
        public HandInput Left { get; private set; }
        public HandInput Right { get; private set; }

        private void Update()
        {
            Left = Read(XRNode.LeftHand);
            Right = Read(XRNode.RightHand);
        }

        private HandInput Read(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            var hand = new HandInput { Connected = device.isValid, Rotation = Quaternion.identity };
            if (!device.isValid) return hand;
            device.TryGetFeatureValue(CommonUsages.isTracked, out hand.Tracked);
            device.TryGetFeatureValue(CommonUsages.grip, out hand.GripValue);
            device.TryGetFeatureValue(CommonUsages.trigger, out hand.TriggerValue);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out hand.PrimaryButton);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out hand.Stick);
            bool positionValid = device.TryGetFeatureValue(CommonUsages.devicePosition, out hand.Position);
            bool rotationValid = device.TryGetFeatureValue(CommonUsages.deviceRotation, out hand.Rotation);
            hand.Tracked &= positionValid && rotationValid;
            hand.Grip = hand.Tracked && hand.GripValue >= pressThreshold;
            hand.Trigger = hand.Tracked && hand.TriggerValue >= pressThreshold;
            return hand;
        }

        private void OnDisable() { Left = default; Right = default; }
    }
}
