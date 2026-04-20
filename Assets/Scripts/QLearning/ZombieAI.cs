using UnityEngine;
using UnityEngine.AI;

namespace QLearning
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
    
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Transform targetAgent;

        [SerializeField] private float health = 100f;
        
        [SerializeField] private float attackDistance;
        [SerializeField] private float attackDamage;
        [SerializeField]private float attackCooldown = 1.5f;

        [SerializeField] private float rotationSpeed = 5f;
     
        private Animator _animator;
        private float attackCooldownTimer = 0;
     
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            
        }



        private void FixedUpdate()
        {
        
            if( attackCooldownTimer > 0f )
            {
                attackCooldownTimer -= Time.fixedDeltaTime;
            }
            else if(attackCooldownTimer <= 0f && navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = false;
            }


            if (!targetAgent) return;
        
            var distance = Vector3.Distance(transform.position, targetAgent.transform.position);

            if (distance < attackDistance)
            {
                Attack();
            }
            else
            {
                Chase();
            }
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
        
        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") && attackCooldownTimer <= 0)
            {
                var rp = collision.gameObject.GetComponent<ReinforcementProblem>();
                if (rp != null)
                {
                    rp.TakeDamage(attackDamage);
                    attackCooldownTimer = attackCooldown;
                }
            }
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
        
        public void TakeDamage(float amount)
        {
            health -= amount;
            if (!(health <= 0)) return;
            
            var playerModel = GameObject.FindFirstObjectByType<PlayerModel>();
            if (playerModel)
            {
                playerModel.UpdateKills();
            }
                
            Destroy(gameObject);

        }
        
        
        public bool IsDead()
        {
            return health <= 0;
        }

    
    
    
    
    
    }
}

