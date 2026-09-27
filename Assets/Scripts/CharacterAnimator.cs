using UnityEngine;
using UnityEngine.AI;


public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;         
    [SerializeField] private NavMeshAgent agent;  
    
    [SerializeField] private float speedDamp = 0.1f;    
    [SerializeField] private bool animate = true;

    private static readonly int SpeedId  = Animator.StringToHash("Speed");
    private static readonly int ShootId  = Animator.StringToHash("Shoot");
    private static readonly int AttackId = Animator.StringToHash("Attack");

    private bool _hasSpeed;
    private bool _hasShoot;
    private bool _hasAttack;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (animator)
        {
            
            animator.applyRootMotion = false;   
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == SpeedId)  _hasSpeed = true;
                if (p.nameHash == ShootId)  _hasShoot = true;
                if (p.nameHash == AttackId) _hasAttack = true;
             
            }
        }
        if (animator) animator.enabled = animate;
    }

    private void Update()
    {
        if (!animate || !animator || !agent || !_hasSpeed) return;
        float maxSpeed = Mathf.Max(agent.speed, 0.01f);
        float speed01 = Mathf.Clamp01(agent.velocity.magnitude / maxSpeed);
        animator.SetFloat(SpeedId, speed01, speedDamp, Time.deltaTime);
    }

    public void PlayShoot()  { if (animate && _hasShoot)  animator.SetTrigger(ShootId); }
    public void PlayAttack() { if (animate && _hasAttack) animator.SetTrigger(AttackId); }

}
