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

        [Header("Need-Weighted Spawning")]
        [SerializeField] private bool needWeighted = true;
        // base weight per type; lower = need matters more (0.5 -> up to 75/25 split)
        [SerializeField] private float baseTypeWeight = 0.5f;
        [SerializeField] private ReinforcementProblem agent;
        
        private void Start()
        {
            if (!player)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

            if (!agent)
            {
                agent = FindFirstObjectByType<ReinforcementProblem>();
            }
           
        }
        
        private void Update()
        {
            // Remove collected pickups
            activePickups.RemoveAll(pickUp => pickUp == null);
            
            if (activePickups.Count >= maxPickups) return;
            
            _spawnTimer -= Time.deltaTime;

            if (!(_spawnTimer <= 0f)) return;
            
            var type = ChoosePickupType();
                
            SpawnPickup(type);
                
            var interval = baseSpawnInterval / spawnRate;
            _spawnTimer = interval < 2f ? 2f : interval;
        }
        
        // Health vs ammo chosen by the agent's current need (falls back to 50/50)
        private PickupType ChoosePickupType()
        {
            if (!needWeighted || !agent)
                return Random.value < 0.5f ? PickupType.Health : PickupType.Ammo;

            float healthW = baseTypeWeight + (1f - agent.GetHealthRatio());
            float ammoW = baseTypeWeight + (1f - agent.GetAmmoRatio());

            float pHealth = healthW / (healthW + ammoW);
            return Random.value < pHealth ? PickupType.Health : PickupType.Ammo;
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