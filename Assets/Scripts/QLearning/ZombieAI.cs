using UnityEngine;
using UnityEngine.AI;

namespace QLearning
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
    
        [Header("Zombie Properties")]
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Transform targetAgent;

        [SerializeField] private float health;
        
        [SerializeField] private float attackDistance = 1.3f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField]private float attackCooldown = 1f;
        [SerializeField] private float rotationSpeed = 5f;
        
        
        [Header("Detection")]
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private float wanderSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 3.3f;
        [SerializeField] private float wanderRadius = 6f;
        private bool _alerted;
        private float _wanderTimer;
        
        
        //Reinforcement Target
        private ReinforcementProblem _targetRP;
     
        private Animator _animator;
        private float attackCooldownTimer = 0;
     
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            
            var player = GameObject.FindGameObjectWithTag("Player");
            targetAgent = player.transform;
            _targetRP = player.GetComponent<ReinforcementProblem>();
            
            
            targetAgent = GameObject.FindGameObjectWithTag("Player").transform;
            
            
            //Zombie must be able to physically get within attack range
            navMeshAgent.stoppingDistance = attackDistance * 0.8f;
            
        }



        private void FixedUpdate()
        {
        
            
            if(!targetAgent) return;
            
            if( attackCooldownTimer > 0f )  attackCooldownTimer -= Time.fixedDeltaTime; 
            
            
            float dist = FlatDistance(transform.position, targetAgent.position);
            if (!_alerted && dist < detectionRadius) Alert();

            if (_alerted)
            {
                if (dist <= attackDistance && attackCooldownTimer <= 0f && _targetRP)
                {
                    _targetRP.TakeDamage(attackDamage, transform.position);
                    attackCooldownTimer = attackCooldown;
                }

                Chase();
            }

            else Wander();
            
            
        }

        private void Wander()
        {
            
            navMeshAgent.speed = wanderSpeed;
            _wanderTimer -= Time.fixedDeltaTime;
            if (_wanderTimer > 0f && navMeshAgent.remainingDistance > 0.5f) return;
            
            
            Vector3 p = transform.position + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas))
                navMeshAgent.SetDestination(hit.position);
            
            _wanderTimer = Random.Range(2f, 4f);
            
        }


        public void Alert()
        {
            _alerted = true; 
            navMeshAgent.speed = chaseSpeed;
        }

        public bool IsAlerted()
        {
            return _alerted;
        }

        private void Attack()
        {
        
            if( attackCooldownTimer > 0f || !_animator )
            {
                return;
            }

            navMeshAgent.isStopped = true;
        
            attackCooldownTimer = attackCooldown;
        
        }
 

        private void Chase()
        {
            //keep destination and disable all else
            navMeshAgent.SetDestination(targetAgent.transform.position);
        }

    
        private void Face(Vector3 destination)
        {
            Vector3 lookPos = destination - transform.position;
            lookPos.y = 0;
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed );  
        }
        
        public void TakeDamage(float amount, bool countAsPlayerKill = true)
        {
            health -= amount;
            if (!(health <= 0)) return;

            if (countAsPlayerKill)
            {
                var playerModel = GameObject.FindFirstObjectByType<PlayerModel>();
                if (playerModel)  playerModel.UpdateKills();
            }
         
            Destroy(gameObject);

        }
        
        
        public bool IsDead()
        {
            return health <= 0;
        }

    
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0; b.y = 0;
            return Vector3.Distance(a, b);
        }
    
    
    
    
    }
}

