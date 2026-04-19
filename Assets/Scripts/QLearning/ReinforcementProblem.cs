using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class ReinforcementProblem : MonoBehaviour
    {
        [Header("Agent References")]
        [SerializeField] private Transform agentTransform;
        [SerializeField] private float moveSpeed = 5f;
        
        [Header("Agent Stats")]
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private int maxAmmo = 6;
        
        [Header("Distance Thresholds")]
        [SerializeField] private float nearDistance = 5f;
        [SerializeField] private float mediumDistance = 12f;
        
        [Header("Health Thresholds")]
        [SerializeField] private float criticalHealth = 30f;
        [SerializeField] private float hurtHealth = 70f;
        
        [Header("Rewards")]
        [SerializeField] private float survivalReward = 0.01f;
        [SerializeField] private float healthGainReward = 0.5f;
        [SerializeField] private float healthLossPenalty = 0.3f;
        [SerializeField] private float ammoPickupReward = 0.4f;
        [SerializeField] private float killReward = 1.0f;
        [SerializeField] private float deathPenalty = 5.0f;
        
        // Tracking variables
        private float lastHealth;
        private int lastAmmo;
        private bool enemyKilledThisStep;
        private GameObject nearestEnemy;
        private GameObject nearestPickup;
        
        private void Start()
        {
            lastHealth = currentHealth;
            lastAmmo = currentAmmo;
        }
        
        // ===== CORE RL METHODS =====
        
        public State GetCurrentState()
        {
            int enemyDist = GetEnemyDistanceCategory();
            int ammo = GetAmmoCategory();
            int health = GetHealthCategory();
            
            return new State(enemyDist, ammo, health);
        }
        
        public State GetRandomState()
        {
            return new State(
                Random.Range(0, 3),
                Random.Range(0, 3),
                Random.Range(0, 3)
            );
        }
        
        public List<Action> GetAvailableActions(State state)
        {
            var actions = new List<Action>();
            
            // All movement actions are always available
            actions.Add(new Action(Action.ActionType.MoveToEnemy));
            actions.Add(new Action(Action.ActionType.FleeFromEnemy));
            actions.Add(new Action(Action.ActionType.MoveToPickup));
            
            // Shoot only available if ammo > 0 and enemy exists
            if (currentAmmo > 0 && FindNearestEnemy() != null)
                actions.Add(new Action(Action.ActionType.Shoot));
            
            return actions;
        }
        
        public (float reward, State newState) TakeActions(State state, Action action)
        {
            // Store previous values for reward calculation
            lastHealth = currentHealth;
            lastAmmo = currentAmmo;
            enemyKilledThisStep = false;
            
            // Execute the action
            ExecuteAction(action);
            
            // Calculate reward based on what happened
            float reward = CalculateReward();
            
            // Get the new state after action execution
            State newState = GetCurrentState();
            
            return (reward, newState);
        }
        
        // ===== ACTION EXECUTION =====
        
        private void ExecuteAction(Action action)
        {
            switch (action.Type)
            {
                case Action.ActionType.MoveToEnemy:
                    MoveToward(FindNearestEnemy());
                    break;
                    
                case Action.ActionType.FleeFromEnemy:
                    GameObject enemy = FindNearestEnemy();
                    if (enemy != null)
                    {
                        Vector3 awayDir = (agentTransform.position - enemy.transform.position).normalized;
                        MoveToward(agentTransform.position + awayDir * 10f);
                    }
                    break;
                    
                case Action.ActionType.MoveToPickup:
                    MoveToward(FindNearestPickup());
                    break;
                    
                case Action.ActionType.Shoot:
                    if (currentAmmo > 0)
                    {
                        ShootAt(FindNearestEnemy());
                        currentAmmo--;
                    }
                    break;
            }
        }
        
        private void MoveToward(GameObject target)
        {
            if (target == null) return;
            MoveToward(target.transform.position);
        }
        
        private void MoveToward(Vector3 target)
        {
            Vector3 dir = (target - agentTransform.position).normalized;
            agentTransform.position += dir * moveSpeed * Time.fixedDeltaTime;
        }
        
        private void ShootAt(GameObject target)
        {
            if (target == null) return;
            
            // Simple hit-scan shooting
            float distance = Vector3.Distance(agentTransform.position, target.transform.position);
            if (distance < 15f)
            {
                var zombie = target.GetComponent<ZombieAI>();
                if (zombie != null)
                {
                    zombie.TakeDamage(34f);
                    if (zombie.IsDead())
                    {
                        //zombie was killed
                        enemyKilledThisStep = true;
                    }
                       
                }
            }
        }
        
        // ===== REWARD CALCULATION =====
        
        private float CalculateReward()
        {
            float reward = 0f;
            
            // Small survival reward for staying alive
            reward += survivalReward;
            
            // Health changes
            float healthDelta = currentHealth - lastHealth;
            if (healthDelta > 0) reward += healthGainReward;
            if (healthDelta < 0) reward -= healthLossPenalty;
            
            // Ammo pickup
            if (currentAmmo > lastAmmo) reward += ammoPickupReward;
            
            // Kill reward
            if (enemyKilledThisStep) reward += killReward;
            
            // Death penalty (applied separately when health reaches zero)
            if (currentHealth <= 0) reward -= deathPenalty;
            
            return reward;
        }
        
        // ===== STATE CATEGORIZATION =====
        
        private int GetEnemyDistanceCategory()
        {
            nearestEnemy = FindNearestEnemy();
            if (nearestEnemy == null) return 2; // Far/None
            
            float dist = Vector3.Distance(agentTransform.position, nearestEnemy.transform.position);
            if (dist < nearDistance) return 0;      // Near
            if (dist < mediumDistance) return 1;    // Medium
            return 2;                               // Far
        }
        
        private int GetAmmoCategory()
        {
            if (currentAmmo == 0) return 0;         // Empty
            if (currentAmmo <= 2) return 1;         // Low
            return 2;                               // Has Ammo
        }
        
        private int GetHealthCategory()
        {
            if (currentHealth < criticalHealth) return 0;   // Critical
            if (currentHealth < hurtHealth) return 1;       // Hurt
            return 2;                                       // Healthy
        }
        
        // ===== HELPER METHODS =====
        
        private GameObject FindNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(agentTransform.position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }
            return nearest;
        }
        
        private GameObject FindNearestPickup()
        {
            GameObject[] pickups = GameObject.FindGameObjectsWithTag("Pickup");
            GameObject nearest = null;
            float minDist = float.MaxValue;
            
            foreach (var pickup in pickups)
            {
                float dist = Vector3.Distance(agentTransform.position, pickup.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = pickup;
                }
            }
            return nearest;
        }
        
        // ===== PUBLIC METHODS FOR GAME SYSTEMS =====
        
        public void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
        }
        
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        public void AddAmmo(int amount)
        {
            currentAmmo = Mathf.Min(maxAmmo, currentAmmo + amount);
        }
        
        public bool IsDead() => currentHealth <= 0;
        
        public void ResetAgent()
        {
            currentHealth = maxHealth;
            currentAmmo = maxAmmo;
            lastHealth = currentHealth;
            lastAmmo = currentAmmo;
            enemyKilledThisStep = false;
            
            // Reset position
            agentTransform.position = Vector3.zero;
        }
        
        public float GetHealth() => currentHealth;
        public int GetAmmo() => currentAmmo;
    }
}