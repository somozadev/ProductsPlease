using System;
using UnityEngine;

namespace ProductsPlease.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class DragableComponent : InteractableComponent
    {
        // === Only one active drag globally ===
        private static DragableComponent s_active;

        [Header("Drag")]
        public float spring = 600f;
        public float damping = 6f;
        [Tooltip("Distance from camera where the grabbed point is held.")]
        public float holdDistance = 2.5f;

        [Header("Raycast")]
        [SerializeField] private LayerMask raycastIgnoreLayers;
        [SerializeField] private float mouseReach = 5.0f;

        [Header("Line Renderer")]
        public LineRenderer lineRenderer;     // shared or per-instance
        public Transform lineRenderLocation;  // origin (e.g., player hand)
        [Tooltip("If no LineRenderer is assigned/found, one will be created per instance.")]
        public bool autoCreateLineIfMissing = true;

        // Runtime
        private bool isDragging;
        private Transform jointTrans;
        private Rigidbody attachedRb;
        private Collider attachedCol;
        private Vector3 attachmentLocalOnTarget;
        private Camera cam;

        // ===== Interactable hooks (IGNORED) =====
        public override void StartInteract() { /* intentionally ignored (mouse-only) */ }
        public override void StartInteract(RaycastHit hit) { /* intentionally ignored (mouse-only) */ }
        public override void EndInteract() { /* intentionally ignored (mouse-only) */ }

        // ===== Init =====
        public override void Initialise(GameObject owner)
        {
            base.Initialise(owner);
            cam = Camera.main;

            if (!lineRenderer || !lineRenderLocation)
            {
                lineRenderer = FindAnyObjectByType<LineRenderer>(FindObjectsInactive.Include);
                if (lineRenderer) lineRenderLocation = lineRenderer.transform;
            }

            if (!lineRenderer && autoCreateLineIfMissing)
            {
                var go = new GameObject("DragLineRenderer");
                go.transform.SetParent(transform, false);
                lineRenderer = go.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.widthMultiplier = 0.02f;
                lineRenderer.positionCount = 0;
                lineRenderLocation = go.transform; // fallback
            }

            if (lineRenderer) lineRenderer.useWorldSpace = true;
        }

        private void Awake()
        {
            if (!cam) cam = Camera.main;
        }

        private void Update()
        {
            if (!cam) return;

            // Mouse-only control
            if (Input.GetMouseButtonDown(0))
            {
                // no other drag running
                if (s_active == null)
                    TryMouseBegin(Input.mousePosition);
            }

            if (isDragging && jointTrans)
            {
                // Move kinematic anchor in front of camera
                jointTrans.position = cam.transform.position + cam.transform.forward * holdDistance;
                DrawRope();
            }

            if (isDragging && s_active == this && Input.GetMouseButtonUp(0))
            {
                MouseEnd();
            }
        }

        private void TryMouseBegin(Vector3 mouseScreen)
        {
            var ray = cam.ScreenPointToRay(mouseScreen);
            if (!RaycastFiltered(ray, out var hit, mouseReach)) return;

            // Start only if THIS component was clicked
            if (!hit.transform.TryGetComponent(out DragableComponent me) || me != this) return;
            if (!hit.rigidbody) return;

            s_active = this;

            attachedRb  = hit.rigidbody;
            attachedCol = hit.collider;

            var worldAttach = hit.point + hit.normal * 0.005f; // tiny offset for nicer line
            attachmentLocalOnTarget = attachedRb.transform.InverseTransformPoint(worldAttach);

            jointTrans = CreateAttachmentJoint(attachedRb, worldAttach);

            isDragging = true;
            if (lineRenderer) lineRenderer.positionCount = 2;
        }

        private void MouseEnd()
        {
            isDragging = false;
            DestroyRope();

            if (jointTrans != null)
            {
                var go = jointTrans.gameObject;
                jointTrans = null;
                if (go) Destroy(go);
            }

            attachedRb  = null;
            attachedCol = null;

            if (s_active == this) s_active = null;
        }

        // ===== Joint / rope =====
        private Transform CreateAttachmentJoint(Rigidbody targetRb, Vector3 attachmentPosition)
        {
            GameObject go = new GameObject("Attachment Point");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.transform.position = attachmentPosition;

            var kinematicRb = go.AddComponent<Rigidbody>();
            kinematicRb.isKinematic = true;

            var joint = go.AddComponent<ConfigurableJoint>();
            joint.connectedBody = targetRb;
            joint.configuredInWorldSpace = true;

            joint.xDrive = NewJointDrive(spring, damping);
            joint.yDrive = NewJointDrive(spring, damping);
            joint.zDrive = NewJointDrive(spring, damping);
            joint.slerpDrive = NewJointDrive(spring, damping);
            joint.rotationDriveMode = RotationDriveMode.Slerp;

            return go.transform;
        }

        private JointDrive NewJointDrive(float springVal, float damperVal)
        {
            JointDrive drive = new JointDrive
            {
#pragma warning disable CS0618
                mode = JointDriveMode.Position
#pragma warning restore CS0618
            };
            drive.positionSpring = springVal;
            drive.positionDamper = damperVal;
            drive.maximumForce = Mathf.Infinity;
            return drive;
        }

        private void DrawRope()
        {
            if (!lineRenderer) return;

            Vector3 start = lineRenderLocation ? lineRenderLocation.position : (cam ? cam.transform.position : transform.position);
            Vector3 end = transform.position; 
            if (attachedRb)
            {
                end = attachedRb.transform.TransformPoint(attachmentLocalOnTarget);
                if (attachedCol) end = attachedCol.ClosestPoint(end);
            }

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private void DestroyRope()
        {
            if (lineRenderer) lineRenderer.positionCount = 0;
        }

        private bool RaycastFiltered(Ray ray, out RaycastHit validHit, float maxDist)
        {
            var hits = Physics.RaycastAll(ray, maxDist, ~raycastIgnoreLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider.isTrigger) continue;

                int layer = h.collider.gameObject.layer;
                if (((1 << layer) & raycastIgnoreLayers.value) != 0) continue;

                validHit = h;
                return true;
            }

            validHit = default;
            return false;
        }
    }
}
