using System.Collections.Generic;
using ProductsPlease.Interactions;
using UnityEngine;
using UnityEngine.Events;

namespace ProductsPlease.Managers
{
    public class BeltManager : MonoBehaviour
    {
        [Header("References")] [Tooltip("Where items appear.")]
        public Transform spawnPoint;

        public GameObject productPrefab;
        public Transform beltForwardRef;
        public DayRuntimeGenerator generator;

        [Header("Belt Settings")] 
        public bool isMoving = true;
        public bool dayStarted = true;
        public float beltSpeed = 2.0f;
        public float beltAcceleration = 8.0f;

        [Header("Spawn Settings")] 
        public int maxProductsPerDay = 200;
        public float spawnInterval = 15f;
        public Vector3 spawnCheckHalfExtents = new Vector3(0.25f, 0.25f, 0.25f);
        public LayerMask itemLayerMask;

        [Header("Jitter/Variation")]
        public float spawnYawJitter = 20f;
        public float initialForwardImpulse = 0.5f;

        [Header("Events")]
        public UnityEvent onDayStart;
        public UnityEvent onAllProductsSpawned;
        public UnityEvent onBeltStarted;
        public UnityEvent onBeltStopped;


        DayParamsData currentDay;
        float lastSpawnTime;
        public int spawnedThisDay;

        public bool AllSpawned => spawnedThisDay >= maxProductsPerDay;
        public bool IsMoving => isMoving;
        public Vector3 BeltForward => (beltForwardRef ? beltForwardRef.forward : transform.forward).normalized;

        void Awake()
        {
            if (!generator) generator = GetComponent<DayRuntimeGenerator>();
        }

        void Update()
        {
            if (!isMoving) return;
            if (!dayStarted) return;
            // Try spawn on cadence
            if (!AllSpawned && Time.time - lastSpawnTime >= spawnInterval)
            {
                if (CanSpawnAtPoint())
                {
                    SpawnOneProduct();
                    lastSpawnTime = Time.time;

                    if (AllSpawned)
                        onAllProductsSpawned?.Invoke();
                }
            }
        }

        public void BeginDay(DayParamsData day)
        {
            currentDay = day;
            spawnedThisDay = 0;
            lastSpawnTime = -999f; // so it spawns immediately
            onDayStart?.Invoke();
        }

        public void StartBelt()
        {
            isMoving = true;
            onBeltStarted?.Invoke();
        }

        public void StopBelt()
        {
            isMoving = false;
            onBeltStopped?.Invoke();
        }

        public bool ForceSpawn()
        {
            if (AllSpawned) return false;
            if (!CanSpawnAtPoint()) return false;

            SpawnOneProduct();
            lastSpawnTime = Time.time;
            if (AllSpawned) onAllProductsSpawned?.Invoke();
            return true;
        }

        // --------- Internals ---------

        bool CanSpawnAtPoint()
        {
            if (!spawnPoint || !productPrefab) return false;

            var center = spawnPoint.position;
            var half = spawnCheckHalfExtents;
            var rot = spawnPoint.rotation;

            bool occupied = Physics.CheckBox(center, half, rot, itemLayerMask, QueryTriggerInteraction.Ignore);
            return !occupied;
        }

        void SpawnOneProduct()
        {
            if (AllSpawned) return;

            // Generate item data
            ItemData data = generator ? generator.GenerateNewRandomProduct() : ScriptableObject.CreateInstance<ItemData>();

            // Instantiate prefab
            Quaternion rot = Quaternion.Euler(0f, spawnPoint.rotation.eulerAngles.y + Random.Range(-spawnYawJitter, spawnYawJitter), 0f);
            GameObject go = Instantiate(productPrefab, spawnPoint.position, rot);

            // Inject data
            var itemComp = go.GetComponent<Item>();
            if (itemComp) itemComp.Init(data);

            // Align to belt direction (optional)
            go.transform.forward = BeltForward;

            // Small initial impulse so it settles onto the belt
            var rb = go.GetComponent<Rigidbody>();
            if (rb && initialForwardImpulse > 0f)
            {
                rb.AddForce(BeltForward * initialForwardImpulse, ForceMode.VelocityChange);
            }

            spawnedThisDay++;
        }

        // Expose values for conveyor sections
        public float GetBeltAcceleration() => beltAcceleration;
        public float GetBeltSpeed() => beltSpeed;

        // Visualize spawn occupancy box
        void OnDrawGizmosSelected()
        {
            if (!spawnPoint) return;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, spawnCheckHalfExtents * 2f);
        }
    }
}