using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;


namespace QLearning
{
    public class ReinforcementProblem : MonoBehaviour
    {
        
        [Header("Agent Stats")]
        [SerializeField] private NavMeshAgent navAgent;
        
        [SerializeField] private Transform agentTransform;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private int maxAmmo = 6;
      
        [SerializeField] private GameObject shootDisplay;
     
        [SerializeField]private float shotRange;
        [SerializeField]private float fireRate;
        
        [Header("Agent Discrete Thresholds")]
        
        //Health threshold
        [SerializeField] private float criticalHealth;
        [SerializeField] private float hurtHealth;
        
        //Distance threshold
        [SerializeField] private float nearDistance;
        [SerializeField] private float mediumDistance;


        [Header("Rewards")]
        //Rewards
        [SerializeField]
        private float survivalReward;
        [SerializeField] private float healthGainReward;
        [SerializeField] private float healthLossPenalty;
        [SerializeField] private float ammoPickupReward;
        [SerializeField] private float killReward;
        [SerializeField] private float deathPenalty;


        [Header("Player Model")]
        [SerializeField] private PlayerModel player;
        
        private Vector3 startPosition;
        
        private float shotDamage = 100f;
        private bool zombieKilled;
        private float fireTimer = 0f;
        
        
        private float prevHealth;
        private int prevAmmo;
        
        private GameObject nearestZombie;
        private GameObject nearestPickup;

        private float rewardScale = 1f;
        
        private void Start()
        {
            startPosition = agentTransform.position;
            prevHealth = currentHealth;
            prevAmmo = currentAmmo;
        }


        private void Update()
        {
            if(fireTimer > 0)
             fireTimer -= Time.deltaTime;
        }
        
        
        
        //Returns state based on discrete vals of variables
        public State GetCurrentState()
        {
            var zombieDist = GetZombieDistanceCategory();
            var ammo = GetAmmoCategory();
            var health = GetHealthCategory();
            
            return new State(zombieDist, ammo, health);
        }
        
        
        public static State GetRandomState()
        {
            var zombieDist = Random.Range(0, 3);
            var ammo = Random.Range(0, 3);
            var health = Random.Range(0, 3);
            
            return new State(zombieDist, ammo, health);
        }
        
        
        public List<Action> GetAvailableActions(State state)
        {
            var actions = new List<Action>();
            
            actions.Add(new Action(Action.ActionType.HoldPosition));
            actions.Add(new Action(Action.ActionType.Flee));
            actions.Add(new Action(Action.ActionType.MoveToPickup));
            
            // Shoot action requires ammo, an enemy and fire rate control
            if (currentAmmo > 0 && FindNearestZombie() && fireTimer <= 0)
                actions.Add(new Action(Action.ActionType.Shoot));
            
            return actions;
        }
        
        
        public (float reward, State newState) TakeActions(State state, Action action)
        {
            prevHealth = currentHealth;
            prevAmmo = currentAmmo;
            zombieKilled = false;
            
            //Take action
            ExecuteAction(action);
            
            var reward = CalculateReward();
            State newState = GetCurrentState();
            
            return (reward, newState);
        }
        
        
        
        private void ExecuteAction(Action action)
        {
            switch (action.Type)
            {
                
                case Action.ActionType.HoldPosition:
                    //will do nothing and stand
                    Move(this.transform.position);
                    break;
                
                
                case Action.ActionType.Flee:
                    var enemy = FindNearestZombie();
                    if (enemy)
                    {
                        Vector3 direction = (agentTransform.position - enemy.transform.position).normalized;
                        Move(agentTransform.position + direction * mediumDistance);
                    }
                    break;
                    
                case Action.ActionType.MoveToPickup:
                    var pickUp = FindNearestPickup();
                    if(pickUp)
                        Move(pickUp.transform.position);
                    break;
                    
                case Action.ActionType.Shoot:
                    if (currentAmmo > 0)
                    {
                        var target = FindNearestZombie();
                        if(target)
                         Shoot(target);
                        currentAmmo--;
                        fireTimer = fireRate;
                    }
                    break;
            }
        }
        
        
        private void Move(Vector3 target)
        {
            
            navAgent.SetDestination(target);
            
            
            
            /*Vector3 direction = (target - agentTransform.position).normalized;
            Vector3 oldPosition = agentTransform.position;
            
            agentTransform.position += direction * moveSpeed * Time.deltaTime;*/
            
        }
        
        
        
        private void Shoot(GameObject target)
        {
            //Stops
            navAgent.SetDestination(transform.position);
            
            float distance = Vector3.Distance(agentTransform.position, target.transform.position);
            if (distance < shotRange)
            {
                var zombie = target.GetComponent<ZombieAI>();
                if (zombie)
                {
                    zombie.TakeDamage(shotDamage);
                    if (zombie.IsDead())
                    {
                        zombieKilled = true;
                    }
                       
                }
            }
        }


        public void SetRewardScale(float scale)
        {
            rewardScale = scale < 0.1f ? 0.1f : scale;
        }
        
        
        //Reward Function
        private float CalculateReward()
        {
            float reward = 0f;
            
            // Gain base reward for surviving
            reward += survivalReward;
            
            // Health Rewards for gains, Penalty for loss
            float HealthChange = currentHealth - prevHealth;

            if (HealthChange < 0)
            {
                reward -= healthLossPenalty;
                
            }
            else if (HealthChange > 0)
            {
                reward += healthGainReward;
            }
            
            // Reward for gaining ammo
            if (currentAmmo > prevAmmo) reward += ammoPickupReward;
            
            // Reward for killing a zombie
            if (zombieKilled) reward += killReward;
            
            // Death penalty
            if (currentHealth <= 0) reward -= deathPenalty;
            
            //director scales reward
            return reward * rewardScale; ;
            
        }
        
        
        
        private int GetZombieDistanceCategory()
        {
            nearestZombie = FindNearestZombie();
            
            if (!nearestZombie)
            {
                // No Zombies Nearby, or far
                return 2; 
            }
            
            var dist = Vector3.Distance(agentTransform.position, nearestZombie.transform.position);
            
            if (dist < nearDistance)
            {
                // Near
                return 0;     
            }

            if (dist < mediumDistance)
            {
                //At a Medium Distance
                return 1;
            }
            
            // Zombies Too Far
            return 2;                               
        }
        
        private int GetAmmoCategory()
        {

            if (currentAmmo == 0)
            {
                //Out of Ammo
                return 0;
            }

            if (currentAmmo <= 2)
            {
                //Low on Ammo
                return 1;
            }
            
            //High Ammo
            return 2;
            
        }
        
        private int GetHealthCategory()
        {
            
            if (currentHealth/maxHealth < criticalHealth)
            {
                //Health is Critical
                return 0;
            }

            if (currentHealth/maxHealth < hurtHealth)
            {
                //Agent is hurt
                return 1;
            }
            
            //Heart if full
            return 2;
            
        }
        
       
        
        private GameObject FindNearestZombie()
        {
            var zombies = GameObject.FindGameObjectsWithTag("Zombie");
            
            GameObject nearest = null;
            var minDist = float.MaxValue;
            
            foreach (var z in zombies)
            {
                var dist = Vector3.Distance(agentTransform.position, z.transform.position);
                if (!(dist < minDist)) continue;
                minDist = dist;
                nearest = z;
            }
            
            return nearest;
        }
        
        private GameObject FindNearestPickup()
        {
            var pickups = GameObject.FindGameObjectsWithTag("Pickup");
            GameObject nearest = null;
            var minDist = float.MaxValue;
            
            foreach (var pickup in pickups)
            {
                var dist = Vector3.Distance(agentTransform.position, pickup.transform.position);
                if (!(dist < minDist)) continue;
                minDist = dist;
                nearest = pickup;
            }
            
            return nearest;
        }


        public float GetHealth()
        {
            return currentHealth;
        }
        

        public float GetHealthRatio()
        {
            return currentHealth/maxHealth;
        }

        public float GetAmmo()
        {
            return currentAmmo;
        }

        public float GetAmmoRatio()
        {
            return (float)currentAmmo/maxAmmo;
        }
        
        public void TakeDamage(float damage)
        {
            
            player.UpdateHits();
            
            if (currentHealth - damage <= 0)
            {
                currentHealth = 0;
            }
            else
            {
                currentHealth -= damage;
            }
        }
        
        public void Heal(float amount)
        {

            if (currentHealth + amount > maxHealth)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth += amount;
            }
            
        }
        
        public void AddAmmo(int amount)
        {
            if (currentAmmo + amount > maxAmmo)
            {
                currentAmmo = maxAmmo;
            }
            else
            {
                currentAmmo += amount;
            }
        }

        public bool IsDead()
        {
            return currentHealth <= 0;
        } 
        
        
        public void ResetAgent()
        {
            currentHealth = maxHealth;
            currentAmmo = maxAmmo;
            prevHealth = currentHealth;
            prevAmmo = currentAmmo;
            zombieKilled = false;
            
            agentTransform.position = startPosition;
        }
        
     
    }
}