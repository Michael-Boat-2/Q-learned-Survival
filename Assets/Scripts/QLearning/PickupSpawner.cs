using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

namespace QLearning
{
    
    
    public class PickupSpawner : MonoBehaviour
    {
        
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private GameObject ammoPickupPrefab;
        
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 10f;
        [SerializeField] private int maxPickups = 4;
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(15f, 15f);

        [SerializeField] private float minFromPlayer = 4f;
        [SerializeField] private float maxFromPlayer = 12f;
        [SerializeField] private float minFromZombies = 4f;
        
        
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
            
            var type = Random.value < 0.5f ? PickupType.Health : PickupType.Ammo;
                
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
            for (int i = 0; i < 15; i++)   // try a few candidates
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(minFromPlayer, maxFromPlayer);
                Vector3 c = player.position + new Vector3(r.x, 0, r.y);
                if (!NavMesh.SamplePosition(c, out var hit, 2f, NavMesh.AllAreas)) continue;

                bool safe = true;
                foreach (var z in GameObject.FindGameObjectsWithTag("Zombie"))
                    if (Vector3.Distance(hit.position, z.transform.position) < minFromZombies) { safe = false; break; }
                if (safe) return hit.position + Vector3.up * 0.5f;
            }
            return transform.position;   // fallback: arena centre
        }
        
        public void SetSpawnRate(float rate)
        {
            spawnRate = rate;
        }
        
        public void ClearAllPickups()
        {
            foreach (var pickup in activePickups)
            {
                if (pickup != null) Destroy(pickup);
            }
            activePickups.Clear();
            _spawnTimer = 0f;
        }
        
        
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        }
    }
}