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
        public bool NeedsPlacement { get; private set; }
        public Rigidbody Body { get; private set; }
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 previousPhysicsPosition, pocketSinkCenter;
        private QuestBilliards.GameRules.PocketTrigger[] pockets;
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
            previousPhysicsPosition = initialPosition;
        }
        private void Start() { pockets = FindObjectsByType<QuestBilliards.GameRules.PocketTrigger>(FindObjectsSortMode.None); }
        private void FixedUpdate()
        {
            if (!IsPocketed && pockets != null)
                foreach (var pocket in pockets)
                    if (pocket != null && pocket.TryCaptureMotion(this, previousPhysicsPosition, Body.position)) break;
            previousPhysicsPosition = Body.position;
            if (!IsPocketed && Body.position.y < -1f)
            {
                if (isCueBall) TryPocket(); else ResetBall();
            }
        }
        public void ResetBall()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            IsPocketed = false;
            NeedsPlacement = false;
            if (Body == null) Body = GetComponent<Rigidbody>();
            Body.isKinematic = false; Body.useGravity = true; Body.detectCollisions = true;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = initialPosition;
            Body.rotation = initialRotation;
            previousPhysicsPosition = initialPosition;
            Body.Sleep();
            var trail = GetComponent<TrailRenderer>(); if (trail != null) trail.Clear();
        }
        public bool TryPocket()
        {
            return BeginPocket(Body != null ? Body.position : transform.position);
        }
        public bool TryPocketAt(Vector3 center) { return BeginPocket(center); }
        private bool BeginPocket(Vector3 center)
        {
            if (IsPocketed || Body == null) return false;
            IsPocketed = true;
            pocketSinkCenter = center;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.isKinematic = true; Body.detectCollisions = false;
            StartCoroutine(Sink());
            return true;
        }
        private IEnumerator Sink()
        {
            Vector3 from = Body.position;
            Vector3 destination = new Vector3(pocketSinkCenter.x, from.y - 0.28f, pocketSinkCenter.z);
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            { Body.position = Vector3.Lerp(from, destination, Mathf.Clamp01(t / 0.3f)); yield return null; }
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
            NeedsPlacement = true;
        }
        public bool PlaceCueBall(Vector3 position)
        {
            if (!isCueBall || !NeedsPlacement) return false;
            ResetBall(); Body.position = position; transform.position = position;
            previousPhysicsPosition = position;
            return true;
        }
    }
}
