using UnityEngine;
using UnityEngine.AI;


public class CharacterAnimator : MonoBehaviour
{
    
    // auto-found in children if empty
    [SerializeField] private Animator animator;         
    [SerializeField] private NavMeshAgent agent;  
   
    // smooths the Speed parameter
    [SerializeField] private float speedDamp = 0.1f;    
    [Tooltip("Turn off during training at high time scale to save CPU.")]
    [SerializeField] private bool animate = true;

    private static readonly int SpeedId  = Animator.StringToHash("Speed");
    private static readonly int ShootId  = Animator.StringToHash("Shoot");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    //private static readonly int HitId    = Animator.StringToHash("Hit");
    //private static readonly int DieId    = Animator.StringToHash("Die");

    private bool _hasSpeed, _hasShoot, _hasAttack, _hasHit, _hasDie;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (animator)
        {
            animator.applyRootMotion = false;   // NavMeshAgent owns movement
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == SpeedId)  _hasSpeed = true;
                if (p.nameHash == ShootId)  _hasShoot = true;
                if (p.nameHash == AttackId) _hasAttack = true;
                //if (p.nameHash == HitId)    _hasHit = true;
                //if (p.nameHash == DieId)    _hasDie = true;
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
    //public void PlayHit()    { if (animate && _hasHit)    animator.SetTrigger(HitId); }
    //public void PlayDie()    { if (animate && _hasDie)    animator.SetTrigger(DieId); }
}
