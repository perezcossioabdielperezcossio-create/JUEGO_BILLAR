using System.Collections;
using UnityEngine;

namespace QuestBilliards.BallPhysics
{
    // Player/hand/cue contact must never transfer rigidbody forces to balls.
    // The only cue impulse is issued explicitly by CueStrikeController to the white ball.
    public sealed class BallContactIsolation : MonoBehaviour
    {
        public Transform cueRoot;
        private IEnumerator Start()
        {
            while (true) { RefreshContacts(); yield return new WaitForSeconds(0.5f); }
        }
        public void RefreshContacts()
        {
            var player = GetComponentsInChildren<Collider>(true);
            var cue = cueRoot != null ? cueRoot.GetComponentsInChildren<Collider>(true) : new Collider[0];
            foreach (var ball in FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None))
                foreach (var ballCollider in ball.GetComponentsInChildren<Collider>(true))
                {
                    foreach (var col in player) if (col != null && col != ballCollider) Physics.IgnoreCollision(col, ballCollider, true);
                    foreach (var col in cue) if (col != null && col != ballCollider) Physics.IgnoreCollision(col, ballCollider, true);
                }
        }
    }
}
