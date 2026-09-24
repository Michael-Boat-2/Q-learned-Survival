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
        //[SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private int maxAmmo = 6;
      
        //[SerializeField] private GameObject shootDisplay;
     
        [SerializeField]private float shotRange;
        [SerializeField]private float fireRate;
        
        [Header("Hit Response")]
        [SerializeField] private float invulnDuration = 0.75f;
        [SerializeField] private float knockbackDistance = 2f;
        private float invulnTimer;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 8f;
        [SerializeField] private float dashDuration = 0.4f;
        [SerializeField] private float dashCooldown = 5f;
        [SerializeField] private float dashInvuln = 0.3f;
        [SerializeField] private float dashAcceleration = 100f;
        private float dashTimer;
        private float dashCooldownTimer;
        private float baseSpeed;
        private float baseAcceleration;
        
        
        [Header("Agent Discrete Thresholds")]
        
        //Health threshold
        [SerializeField] private float criticalHealth;
        [SerializeField] private float hurtHealth;
        
        //Distance threshold
        [SerializeField] private float nearDistance;
        [SerializeField] private float mediumDistance;
        
        [Header("Wall Sensing")]
        [SerializeField] private float wallMargin = 3f;

        [Header("Pickup Sensing")]
        [SerializeField] private float pickupNearDistance = 8f;

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
        //private GameObject nearestPickup;

        //private float rewardScale = 1f;
        
        //Episode Stats
        public int DamageEvents { get; private set; }
        public float DamageTaken { get; private set; }
        public int ShotsFired { get; private set; }
        public int Kills { get; private set; }
        public int HealthPickups { get; private set; }
        public int AmmoPickups { get; private set; }
        // -1 = never hit
        public float TimeToFirstHit { get; private set; } = -1f;  
        public int MaxAlerted { get; private set; }
        public int DashesUsed { get; private set; }

        private float _episodeTime;
        
        
        
        private void Start()
        {
            startPosition = agentTransform.position;
            baseSpeed = navAgent.speed;
            baseAcceleration = navAgent.acceleration;
            prevHealth = currentHealth;
            prevAmmo = currentAmmo;
        }


        private void Update()
        {
            if(fireTimer > 0) fireTimer -= Time.deltaTime;
            if (invulnTimer > 0) invulnTimer -= Time.deltaTime;
            if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

            if (dashTimer > 0)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0) EndDash();
            }
            
            
            _episodeTime += Time.deltaTime;
        }
        
        
        
        //Returns state based on discrete vals of variables
        public State GetCurrentState()
        {
            MaxAlerted = Mathf.Max(MaxAlerted, CountAlerted()); 
            
            var zombieDist = GetZombieDistanceCategory();
            var ammo = GetAmmoCategory();
            var health = GetHealthCategory();
            var pickupDist = GetPickupCategory();
            
            return new State(zombieDist, ammo, health, pickupDist);
        }
        
        
        public static State GetRandomState()
        {
            var zombieDist = Random.Range(0, 3);
            var ammo = Random.Range(0, 3);
            var health = Random.Range(0, 3);
            var pickupDist = Random.Range(0, 3);
            
            
            return new State(zombieDist, ammo, health,  pickupDist);
        }
        
        
        public List<Action> GetAvailableActions(State state)
        {
            var actions = new List<Action>();
            actions.Add(new Action(Action.ActionType.HoldPosition));

            //nearest alerted zombies
            var z = FindNearestZombie(alertedOnly: true);
            float d = z ? Vector3.Distance(agentTransform.position, z.transform.position) : float.MaxValue;
            
            //Will not flee if not close enough 
            if(d < mediumDistance)
                actions.Add(new Action(Action.ActionType.Flee));

            // Dash: emergency escape, only when threatened and off cooldown
            if (d < mediumDistance && dashCooldownTimer <= 0 && dashTimer <= 0)
                actions.Add(new Action(Action.ActionType.Dash));
              
            //Only move to pickups if they exist
            if(FindNearestPickup())
                actions.Add(new Action(Action.ActionType.MoveToPickup));
            
            // Shoot action requires ammo, an enemy and fire rate control, prevents shot wasting
            if (currentAmmo > 0 && d < shotRange && fireTimer <= 0)
                actions.Add(new Action(Action.ActionType.Shoot));
            
            return actions;
        }

        // reward accumulated since the last snapshot
        public float ComputeIntervalReward()
        {
            return CalculateReward();
        }

        //reset baselines so next interval measures from now
        public void SnapshotForReward()
        {
            prevHealth = currentHealth;
            prevAmmo = currentAmmo;
            zombieKilled = false;
        }
        
        
        
        
        //Execute action
        public void ExecuteAction(Action action)
        {
            switch (action.Type)
            {
                
                case Action.ActionType.HoldPosition:
                    //will do nothing and stand
                    Move(this.transform.position);
                    break;
                
                
                case Action.ActionType.Flee:
                    
                    Move(FindSafestPoint());
                    break;
                    
                case Action.ActionType.MoveToPickup:
                    
                    var pickup = FindNearestPickup();
                    
                    //Move to the best pickup found
                    Move(pickup.transform.position);
                    break;
                    
                case Action.ActionType.Shoot:
                    if (currentAmmo > 0)
                    {
                        var target = FindNearestZombie();
                        
                        //shoot in range
                        /*if (target && Vector3.Distance(agentTransform.position, target.transform.position) < shotRange)
                        {
                            Shoot(target);
                            currentAmmo--;
                        }*/
                        
                        //shoot out
                        if (target)
                        {
                            Shoot(target);
                            currentAmmo--;
                            
                            ShotsFired++;
                            
                        }

                        fireTimer = fireRate;
                    }
                    break;

                case Action.ActionType.Dash:
                    StartDash();
                    break;
            }
        }
        
        
        private void StartDash()
        {
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            invulnTimer = Mathf.Max(invulnTimer, dashInvuln);
            DashesUsed++;

            navAgent.speed = dashSpeed;
            navAgent.acceleration = dashAcceleration;
            Move(FindSafestPoint());
        }

        private void EndDash()
        {
            dashTimer = 0f;
            if (baseSpeed <= 0f) return; // Start() hasn't cached the base values yet
            navAgent.speed = baseSpeed;
            navAgent.acceleration = baseAcceleration;
        }


        private void Move(Vector3 target)
        {
            navAgent.SetDestination(target);
        }

        private Vector3 FindSafestPoint()
        {
            var zombies = GameObject.FindGameObjectsWithTag("Zombie");
            Vector3 pos = agentTransform.position;
            Vector3 best = pos;
            
            float bestScore = float.MinValue;
            
            
            for (int i = 0; i < 8; i++)   // test 8 directions around the agent
            {
                float ang = i * 45f * Mathf.Deg2Rad;
                Vector3 target = pos + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * mediumDistance;

                // clip to walls
                if (NavMesh.Raycast(pos, target, out NavMeshHit hit, NavMesh.AllAreas))
                    target = hit.position;

                // score = distance to the closest zombie from that point
                float minD = float.MaxValue;
                foreach (var z in zombies)
                    minD = Mathf.Min(minD, Vector3.Distance(target, z.transform.position));

                // prefer points that actually move
                float score = minD + 0.2f * Vector3.Distance(pos, target);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }
            
            return best;
        }
        
        
        private int GetPickupCategory()
        {
            // Find the nearest pickup
            
            var p = FindBestPickup();   
            if (!p) return 0;           // none on map
            float d = Vector3.Distance(agentTransform.position, p.transform.position);
            return d < pickupNearDistance ? 1 : 2;   // 1 = near, 2 = far
        }
        
        
        

        private GameObject FindBestPickup()
        {
            var pickups = GameObject.FindGameObjectsWithTag("Pickup");
            
            GameObject best = null;
            float bestScore = float.MinValue;
            
            
            float healthNeed = 1f - currentHealth/maxHealth;
            
            float ammoNeed = 1f - (float)currentAmmo/maxAmmo;


            foreach (var p in pickups)
            {
                
                var pu = p.GetComponent<Pickup>();
                float need = pu.Type == PickupType.Health ? healthNeed : ammoNeed;
                
                
                float dist =  Vector3.Distance(agentTransform.position, p.transform.position);
                
                // if needed and close, this is the target that will win
                float score = need * 10f - dist;


                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }


            }
            
            return best;
            
        }
        
        
        
        private void Shoot(GameObject target)
        {
            //Alerts
            foreach (var z in GameObject.FindGameObjectsWithTag("Zombie"))
                if (Vector3.Distance(agentTransform.position, z.transform.position) < 12f)
                    z.GetComponent<ZombieAI>()?.Alert();
            
            //Stops
            navAgent.SetDestination(transform.position);
            
            float distance = Vector3.Distance(agentTransform.position, target.transform.position);
            
            // stochastic hit chance based on distance
            float hitChance = distance < nearDistance ? 0.95f
                : Mathf.Lerp(0.9f, 0.35f, (distance - nearDistance) / (shotRange - nearDistance));
            
            if (distance < shotRange && Random.value < hitChance)
            {
                
                var zombie = target.GetComponent<ZombieAI>();
                if (zombie)
                {
                    zombie.TakeDamage(shotDamage);
                    if (zombie.IsDead())
                    {
                        zombieKilled = true;
                        Kills++;
                    }
                       
                }
                
            }
        }


    
        
        
        //Reward Function
        private float CalculateReward()
        {
            float reward = 0f;
            
            // Gain base reward for surviving
            reward += survivalReward;
            
            // Health Rewards for gains, Penalty for loss
            float healthChange = currentHealth - prevHealth;

            if (healthChange < 0)
            {
                //scaling health change penalty
                reward -= healthLossPenalty * (-healthChange/maxHealth);
                
            }
            else if (healthChange > 0)
            {
                reward += healthGainReward;
            }
            
            // Reward for gaining ammo
            if (currentAmmo > prevAmmo) reward += ammoPickupReward;
            
            // Reward for killing a zombie
            if (zombieKilled) reward += killReward;
            
            // Death penalty
            if (currentHealth <= 0) reward -= deathPenalty;
            
            // no more reward scaling
            return reward;
            
        }
        
        
        
        private int GetZombieDistanceCategory()
        {
            nearestZombie = FindNearestZombie(alertedOnly:true);
            
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
        
        
        
        // 1 if the agent is within wallMargin of the NavMesh edge (walls), else 0
        private int GetNearWallCategory()
        {
            if (NavMesh.FindClosestEdge(agentTransform.position, out NavMeshHit hit, NavMesh.AllAreas))
                return hit.distance < wallMargin ? 1 : 0;
            return 0;
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
        
       
        
        private GameObject FindNearestZombie(bool alertedOnly = false)
        {
            var zombies = GameObject.FindGameObjectsWithTag("Zombie");
            
            GameObject nearest = null;
            var minDist = float.MaxValue;
            
            foreach (var z in zombies)
            {

                if (alertedOnly)
                {
                    var ai = z.GetComponent<ZombieAI>();
                    if(!ai || !ai.IsAlerted()) continue;
                }
                
              
                
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
        
      
        
        public void TakeDamage(float damage, Vector3 sourcePos)
        {
            
            if (invulnTimer > 0f || IsDead()) return;
            
            if (TimeToFirstHit < 0f) TimeToFirstHit = _episodeTime;
            
            player.UpdateHits();
            
            float applied = Mathf.Min(damage, currentHealth);
            currentHealth -= applied;

            DamageEvents++;
            DamageTaken += applied;
            
            invulnTimer = invulnDuration;
            Knockback(sourcePos);
            
        }
        
        
        private void Knockback(Vector3 sourcePos)
        {
            Vector3 dir = agentTransform.position - sourcePos;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitSphere;
            dir.y = 0;
            dir.Normalize();

            Vector3 from = agentTransform.position;
            Vector3 to = from + dir * knockbackDistance;

            // NavMesh.Raycast stops at walls/edges, so a cornered agent is pushed only as far as it can go
            if (NavMesh.Raycast(from, to, out NavMeshHit hit, NavMesh.AllAreas))
                to = hit.position;

            navAgent.Warp(to);
            navAgent.ResetPath();   // cancel the current move; the agent picks a new action next tick
        }
        
        private int CountAlerted()
        {
            int n = 0;
            foreach (var z in GameObject.FindGameObjectsWithTag("Zombie"))
            {
                var ai = z.GetComponent<ZombieAI>();
                if (ai && ai.IsAlerted()) n++;
            }
            return n;
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

            HealthPickups++;

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
            
            
            AmmoPickups++;
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
            fireTimer = 0f;
            invulnTimer = 0f;
            EndDash();
            dashCooldownTimer = 0f;
            DashesUsed = 0;
            
            _episodeTime = 0f;
            TimeToFirstHit = -1f;
            MaxAlerted = 0;

            //set back to start position
            navAgent.Warp(startPosition);
            
            DamageEvents = 0; DamageTaken = 0f; ShotsFired = 0; HealthPickups = 0; AmmoPickups = 0;
            Kills = 0;
            
        }
        
        
        private void OnDrawGizmosSelected()
        {
            if (!agentTransform) return;

            /*// Density radius — cyan wire sphere
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(agentTransform.position, 9f);*/

            // Near threshold — green wire sphere
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(agentTransform.position, nearDistance);

            // Medium threshold — yellow wire sphere
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(agentTransform.position, mediumDistance);

            // Shot range — red wire sphere (optional, helps see where shooting works)
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(agentTransform.position, shotRange);
        }
        
     
    }
}