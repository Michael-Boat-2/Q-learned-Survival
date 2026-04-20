using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class ZombieSpawner : MonoBehaviour
    {
        
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float baseSpawnInterval = 5f;
        [SerializeField] private int maxZombies = 8;
        
        
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 20f);
        [SerializeField] private float minDistanceFromPlayer = 5f;
        
        [SerializeField] private float spawnRate = 1f;
        [SerializeField] private List<GameObject> activeZombies = new List<GameObject>();
        
        [SerializeField]private Transform player;
        private float _spawnTimer;
        private float _restTimer;
        private bool _isResting;
        
        private void Start()
        {
            if (!player)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
          
        }
        
        private void Update()
        {
            
            if (_isResting)
            {
                _restTimer -= Time.deltaTime;
                
                if (_restTimer <= 0f)
                {
                    _isResting = false;
                }
                
                return;
            }
            
            //Remove Dead Zombies
            activeZombies.RemoveAll(z => z == null);
            
            //Zombies at max capacity
            if (activeZombies.Count >= maxZombies) return;
            
            _spawnTimer -= Time.deltaTime;

            if (!(_spawnTimer <= 0f)) return;
            
            SpawnZombie();
            
            var interval = baseSpawnInterval / spawnRate;
            _spawnTimer = interval < 1f ? 1f : interval;
            
        }
        
        private void SpawnZombie()
        {
            if (!zombiePrefab) { return; }
            
            var spawnPos = GetSpawnPosition();
            
            var zombie = Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
            activeZombies.Add(zombie);
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
                Vector3 randomPos;
                var attempts = 0;
                
                do
                {
                    var x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
                    var z = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
                    randomPos = transform.position + new Vector3(x, 0.5f, z);
                    attempts++;
                }
                while (player && Vector3.Distance(randomPos, player.position) < minDistanceFromPlayer && attempts < 20);
                
                return randomPos;
            }
        }
        
        public void SetSpawnRate(float rate)
        {
            spawnRate = rate;
        }
        
        public void TriggerRestPeriod(float duration)
        {
            _isResting = true;
            _restTimer = duration;

            foreach (var zombie in activeZombies)
            {
                var z = zombie.GetComponent<ZombieAI>();
                z.TakeDamage(100f);
            }
        }
        
        public void SpawnBossZombie(GameObject strongPrefab)
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
                {
                    SpawnZombie();
                }
            }
        }
        
        public float GetZombieDensity()
        {
            return (float)activeZombies.Count/maxZombies;
        }
        
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
        }
    }
}