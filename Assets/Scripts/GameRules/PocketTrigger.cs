using UnityEngine;
using QuestBilliards.BallPhysics;

namespace QuestBilliards.GameRules
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PocketTrigger : MonoBehaviour
    {
        public float openingRadius = 0.1f;
        public int Captures { get; private set; }
        private void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<BilliardBallBody>();
            if (ball != null && ball.TryPocket()) Captures++;
        }
    }
}
