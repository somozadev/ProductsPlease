namespace ProductsPlease.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    namespace ProductsPlease.Environment
    {
        [RequireComponent(typeof(Collider))]
        public class SimpleConveyorBelt : MonoBehaviour
        {
            public bool isWorking = true;

            public enum BeltForceMode { PushByPosition, PushByAcceleration }
            public enum RelativeDirection { Up, Down, Left, Right, Forward, Backward }

            [Header("Belt")] [Tooltip("Speed in m/s along the belt direction.")]
            public float speed = 2.0f;

            [Tooltip("Local direction of motion.")]
            public RelativeDirection direction = RelativeDirection.Forward;

            [Tooltip("How the belt moves objects.")]
            public BeltForceMode forceMode = BeltForceMode.PushByPosition;

            [Header("Stability (no constraints used)")]
            [Tooltip("Linear damping only while on the belt.")]
            public float extraLinearDamping = 0.0f;

            [Tooltip("Damp X/Z angular velocity so items don’t roll like crazy.")]
            public float angularDampXZ = 2.0f;

            [Tooltip("Cap magnitude of angular velocity while on belt (0 = ignore).")]
            public float maxAngularVel = 20f;

            Collider beltCol;

            struct ContactInfo
            {
                public Vector3 lastAvgNormal;
                public float lastTouchTime;
            }

            readonly Dictionary<Rigidbody, ContactInfo> _touching = new Dictionary<Rigidbody, ContactInfo>(16);

            public void ToggleConveyor()
            {
                isWorking = !isWorking;

                if (isWorking)
                {
                    WakeAllTouching();
                }
            }

            public void SetWorking(bool value)
            {
                if (isWorking == value) return;
                isWorking = value;
                if (isWorking) WakeAllTouching();
            }

            void Awake()
            {
                beltCol = GetComponent<Collider>();
                if (beltCol.isTrigger)
                    Debug.LogWarning("[ConveyorBelt] Use a NON-trigger collider for OnCollisionStay.");
            }

            void FixedUpdate()
            {
                if (_touching.Count == 0) return;

                _toRemoveBuffer.Clear();

                foreach (var kv in _touching)
                {
                    var rb = kv.Key;
                    if (!rb) { _toRemoveBuffer.Add(kv.Key); continue; }
                    if (!rb.gameObject.activeInHierarchy) { _toRemoveBuffer.Add(kv.Key); continue; }

                    if (isWorking)
                    {
                        ApplyBelt(rb, kv.Value.lastAvgNormal);
                        ApplyStability(rb);
                    }
                }

                if (_toRemoveBuffer.Count > 0)
                {
                    foreach (var dead in _toRemoveBuffer) _touching.Remove(dead);
                    _toRemoveBuffer.Clear();
                }
            }

            readonly List<Rigidbody> _toRemoveBuffer = new List<Rigidbody>(8);

            void OnCollisionEnter(Collision collision)
            {
                var rb = collision.rigidbody;
                if (!rb || rb.isKinematic) return;

                var info = new ContactInfo
                {
                    lastAvgNormal = AverageContactNormal(collision),
                    lastTouchTime = Time.time
                };
                _touching[rb] = info;

                if (isWorking) rb.WakeUp();
            }

            void OnCollisionStay(Collision collision)
            {
                var rb = collision.rigidbody;
                if (!rb || rb.isKinematic) return;

                var info = new ContactInfo
                {
                    lastAvgNormal = AverageContactNormal(collision),
                    lastTouchTime = Time.time
                };
                _touching[rb] = info;
            }

            void OnCollisionExit(Collision collision)
            {
                var rb = collision.rigidbody;
                if (!rb) return;

                _touching.Remove(rb);
            }

            void WakeAllTouching()
            {
                foreach (var rb in _touching.Keys)
                {
                    if (!rb) continue;
                    rb.WakeUp();
                }
            }

            void ApplyBelt(Rigidbody rb, Vector3 avgNormal)
            {
                Vector3 beltDir = GetWorldDirection();

                Vector3 slideDir = Vector3.ProjectOnPlane(beltDir, avgNormal).normalized;
                if (slideDir.sqrMagnitude < 1e-6f) slideDir = beltDir;

                float dt = Time.fixedDeltaTime;

                if (forceMode == BeltForceMode.PushByPosition)
                {
                    Vector3 move = slideDir * (speed * dt);
                    rb.MovePosition(rb.position + move);
                }
                else 
                {
                    float along = Vector3.Dot(rb.linearVelocity, slideDir);
                    float target = speed;
                    float delta = (target - along);
                    float accel = delta / dt;
                    rb.AddForce(slideDir * accel, ForceMode.Acceleration);
                }
            }

            void ApplyStability(Rigidbody rb)
            {
                float dt = Time.fixedDeltaTime;

                if (extraLinearDamping > 0f)
                {
                    rb.linearVelocity *= Mathf.Clamp01(1f - extraLinearDamping * dt);
                }

                if (angularDampXZ > 0f || (maxAngularVel > 0f && rb.angularVelocity.sqrMagnitude > maxAngularVel * maxAngularVel))
                {
                    Vector3 w = rb.angularVelocity;
                    w.x = Mathf.MoveTowards(w.x, 0f, angularDampXZ * dt);
                    w.z = Mathf.MoveTowards(w.z, 0f, angularDampXZ * dt);

                    if (maxAngularVel > 0f && w.sqrMagnitude > maxAngularVel * maxAngularVel)
                        w = w.normalized * maxAngularVel;

                    rb.angularVelocity = w;
                }
            }

            Vector3 GetWorldDirection()
            {
                switch (direction)
                {
                    case RelativeDirection.Up: return transform.up;
                    case RelativeDirection.Down: return -transform.up;
                    case RelativeDirection.Left: return -transform.right;
                    case RelativeDirection.Right: return transform.right;
                    case RelativeDirection.Forward: return transform.forward;
                    case RelativeDirection.Backward: return -transform.forward;
                    default: return transform.forward;
                }
            }

            static Vector3 AverageContactNormal(Collision c)
            {
                if (c.contactCount == 0) return Vector3.up;
                Vector3 n = Vector3.zero;
                int count = c.contactCount;
                for (int i = 0; i < count; i++)
                    n += c.GetContact(i).normal;
                n /= count;
                if (n.sqrMagnitude < 1e-6f) n = Vector3.up;
                return n.normalized;
            }
        }
    }
}
