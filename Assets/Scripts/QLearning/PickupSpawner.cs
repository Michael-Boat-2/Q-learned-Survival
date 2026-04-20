using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class PickupSpawner : MonoBehaviour
    {
        public enum PickupType
        {
            Health,
            Ammo
        }
        
        [Header("Prefabs")]
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private GameObject ammoPickupPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 8f;
        [SerializeField] private int maxPickups = 4;
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(15f, 15f);
        
        [Header("Runtime")]
        [SerializeField] private float spawnMultiplier = 1f;
        [SerializeField] private List<GameObject> activePickups = new List<GameObject>();
        
        private float _spawnTimer;
        private Transform _player;
        
        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        private void Update()
        {
            // Clean up collected pickups
            activePickups.RemoveAll(p => p == null);
            
            if (activePickups.Count >= maxPickups) return;
            
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                // Randomly choose pickup type (60% health, 40% ammo)
                PickupType type = Random.value < 0.6f ? PickupType.Health : PickupType.Ammo;
                SpawnPickup(type);
                
                float interval = baseSpawnInterval / spawnMultiplier;
                _spawnTimer = Mathf.Max(2f, interval);
            }
        }
        
        private void SpawnPickup(PickupType type)
        {
            GameObject prefab = type == PickupType.Health ? healthPickupPrefab : ammoPickupPrefab;
            
            if (prefab == null)
            {
                Debug.LogWarning($"PickupSpawner: No prefab for {type}!");
                return;
            }
            
            Vector3 spawnPos = GetSpawnPosition();
            GameObject pickup = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Configure the pickup
            var pickupScript = pickup.GetComponent<Pickup>();
            if (pickupScript != null)
            {
                pickupScript.Initialize(type);
            }
            
            activePickups.Add(pickup);
        }
        
        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                return point.position;
            }
            else
            {
                float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                float z = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                return transform.position + new Vector3(x, 0f, z);
            }
        }
        
        public void SetSpawnMultiplier(float multiplier)
        {
            spawnMultiplier = Mathf.Max(0.1f, multiplier);
        }
        
        public void SpawnHealthPack()
        {
            SpawnPickup(PickupType.Health);
        }
        
        public void SpawnAmmoPack()
        {
            SpawnPickup(PickupType.Ammo);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        }
    }
}