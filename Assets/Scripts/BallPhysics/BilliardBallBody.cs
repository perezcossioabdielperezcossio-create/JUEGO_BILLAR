using UnityEngine;
using System.Collections;

namespace QuestBilliards.BallPhysics
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BilliardBallBody : MonoBehaviour
    {
        public bool isCueBall;
        public int ballNumber;
        public float mass = 0.13f;
        public float rollingDamping = 0.07f;
        public float spinDamping = 0.045f;
        public bool IsPocketed { get; private set; }
        public Rigidbody Body { get; private set; }
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = mass;
            Body.linearDamping = rollingDamping;
            Body.angularDamping = spinDamping;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = 100f;
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
        private void FixedUpdate()
        {
            if (!IsPocketed && Body.position.y < -1f) ResetBall();
        }
        public void ResetBall()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            IsPocketed = false;
            if (Body == null) Body = GetComponent<Rigidbody>();
            Body.isKinematic = false; Body.useGravity = true; Body.detectCollisions = true;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = initialPosition;
            Body.rotation = initialRotation;
            Body.Sleep();
            var trail = GetComponent<TrailRenderer>(); if (trail != null) trail.Clear();
        }
        public bool TryPocket()
        {
            if (IsPocketed || Body == null) return false;
            IsPocketed = true;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.isKinematic = true; Body.detectCollisions = false;
            StartCoroutine(Sink());
            return true;
        }
        private IEnumerator Sink()
        {
            Vector3 from = Body.position;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            { Body.position = from + Vector3.down * (0.25f * Mathf.Clamp01(t / 0.3f)); yield return null; }
            if (!isCueBall) { gameObject.SetActive(false); yield break; }
            yield return new WaitForSeconds(0.6f);
            bool moving;
            do
            {
                moving = false;
                foreach (var ball in FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None))
                    if (!ball.IsPocketed && ball.Body != null && ball.Body.linearVelocity.sqrMagnitude > 0.0025f) moving = true;
                if (moving) yield return new WaitForSeconds(0.2f);
            } while (moving);
            Vector3 respawn = initialPosition;
            var others = FindObjectsByType<BilliardBallBody>(FindObjectsSortMode.None);
            for (int i = 0; i < 30; i++)
            {
                bool free = true;
                foreach (var other in others) if (other != this && !other.IsPocketed && Vector3.Distance(other.transform.position, respawn) < 0.065f) free = false;
                if (free) break;
                respawn = initialPosition + new Vector3((i % 3 - 1) * 0.08f, 0, (i / 3) * 0.08f);
            }
            // StopAllCoroutines in ResetBall is safe after all pocket work is complete.
            ResetBall(); Body.position = respawn;
        }
    }
}
