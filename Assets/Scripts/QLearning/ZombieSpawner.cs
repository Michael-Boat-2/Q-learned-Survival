using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 5f;
        [SerializeField] private int maxZombies = 10;
        
        [Header("Spawn Area (if no spawn points)")]
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 20f);
        [SerializeField] private float minDistanceFromPlayer = 10f;
        
        [Header("Runtime")]
        [SerializeField] private float spawnMultiplier = 1f;
        [SerializeField] private List<GameObject> activeZombies = new List<GameObject>();
        
        private Transform _player;
        private float _spawnTimer;
        private float _restTimer;
        private bool _isResting;
        
        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        private void Update()
        {
            // Handle rest period
            if (_isResting)
            {
                _restTimer -= Time.deltaTime;
                if (_restTimer <= 0f)
                    _isResting = false;
                return;
            }
            
            // Clean up dead zombies from list
            activeZombies.RemoveAll(z => z == null);
            
            // Don't spawn if at max capacity
            if (activeZombies.Count >= maxZombies) return;
            
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnZombie();
                float interval = baseSpawnInterval / spawnMultiplier;
                _spawnTimer = Mathf.Max(1f, interval);
            }
        }
        
        private void SpawnZombie()
        {
            if (zombiePrefab == null)
            {
                Debug.LogWarning("ZombieSpawner: No zombie prefab assigned!");
                return;
            }
            
            Vector3 spawnPos = GetSpawnPosition();
            
            GameObject zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
            activeZombies.Add(zombie);
        }
        
        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                // Use predefined spawn points
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                return point.position;
            }
            else
            {
                // Random position within area
                Vector3 randomPos;
                int attempts = 0;
                
                do
                {
                    float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                    float z = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                    randomPos = transform.position + new Vector3(x, 0f, z);
                    attempts++;
                }
                while (_player != null && 
                       Vector3.Distance(randomPos, _player.position) < minDistanceFromPlayer && 
                       attempts < 20);
                
                return randomPos;
            }
        }
        
        public void SetSpawnMultiplier(float multiplier)
        {
            spawnMultiplier = Mathf.Max(0.1f, multiplier);
        }
        
        public void TriggerRestPeriod(float duration)
        {
            _isResting = true;
            _restTimer = duration;
        }
        
        public void SpawnStrongEnemy(GameObject strongPrefab)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject strong = Instantiate(strongPrefab, spawnPos, Quaternion.identity);
            activeZombies.Add(strong);
        }
        
        public void SpawnWave(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (activeZombies.Count < maxZombies)
                    SpawnZombie();
            }
        }
        
        public int GetActiveZombieCount() => activeZombies.Count;
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        }
    }
}