using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    
    
    public class PickupSpawner : MonoBehaviour
    {
        
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private GameObject ammoPickupPrefab;
        
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 8f;
        [SerializeField] private int maxPickups = 4;
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(15f, 15f);
        
        
        [SerializeField] private float spawnRate = 1f;
        [SerializeField] private List<GameObject> activePickups = new List<GameObject>();
        
        private float _spawnTimer;
        [SerializeField]private Transform player;
        
        private void Start()
        {
            if (!player)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
           
        }
        
        private void Update()
        {
            // Remove collected pickups
            activePickups.RemoveAll(pickUp => pickUp == null);
            
            if (activePickups.Count >= maxPickups) return;
            
            _spawnTimer -= Time.deltaTime;

            if (!(_spawnTimer <= 0f)) return;
            
            var type = Random.value < 0.6f ? PickupType.Health : PickupType.Ammo;
                
            SpawnPickup(type);
                
            var interval = baseSpawnInterval / spawnRate;
            _spawnTimer = interval < 2f ? 2f : interval;
        }
        
        private void SpawnPickup(PickupType type)
        {

            GameObject prefab;
            if (type == PickupType.Health)
            {
                prefab = healthPickupPrefab;
            }
            else
            {
                prefab = ammoPickupPrefab;
            }
            
            
            if (!prefab) return;
         
            var spawnPos = GetSpawnPosition();
            var pickup = Instantiate(prefab, spawnPos, Quaternion.identity);
            
          
            var pickupScript = pickup.GetComponent<Pickup>();
            
            if (pickupScript)
            {
                pickupScript.Init(type);
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
                return transform.position + new Vector3(x, 1f, z);
            }
        }
        
        public void SetSpawnRate(float rate)
        {
            spawnRate = rate;
        }
        
        
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        }
    }
}