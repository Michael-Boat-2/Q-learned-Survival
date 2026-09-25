using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Visual-only shooting feedback: turns the agent to face its target and plays a muzzle flash.
/// Put this on the agent ROOT (same object as ReinforcementProblem / NavMeshAgent).
/// ReinforcementProblem.Shoot() calls OnShoot(targetPos). No effect on gameplay or learning.
/// </summary>
public class ShootVisuals : MonoBehaviour
{
    [Header("Facing")]
    [SerializeField] private NavMeshAgent agent;              // auto-found if empty
    [Tooltip("Seconds to keep facing the target after a shot (agent rotation is paused meanwhile).")]
    [SerializeField] private float faceDuration = 0.4f;
    [Tooltip("Degrees per second. 0 = snap instantly.")]
    [SerializeField] private float turnSpeed = 1080f;

    [Header("Muzzle Flash")]
    [Tooltip("Empty child at the tip of the gun barrel. Parent it to the weapon/hand bone so it follows the animation.")]
    [SerializeField] private Transform muzzlePoint;


    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float prefabLifetime = 1f;

    [Header("Extras")]
    [SerializeField] private float cameraShake = 0.1f;        // 0 = off
    [Tooltip("Skip visuals when Time.timeScale is above this (fast training).")]
    [SerializeField] private float maxTimeScaleForVisuals = 5f;

    private Vector3 _faceTarget;
    private float _faceTimer;
   

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
      
    }

    public void OnShoot(Vector3 targetPos)
    {
        if (Time.timeScale > maxTimeScaleForVisuals) return;

        // --- face target ---
        _faceTarget = targetPos;
        _faceTimer = faceDuration;
        if (agent) agent.updateRotation = false;
        if (turnSpeed <= 0f) RotateTowards(float.MaxValue);

  
        if (muzzleFlashPrefab && muzzlePoint)
        {
            var fx = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(fx, prefabLifetime);
        }

      
        if (cameraShake > 0f) CameraFollow.Instance?.Shake(cameraShake);
    }

    private void Update()
    {
        if (_faceTimer > 0f)
        {
            _faceTimer -= Time.deltaTime;
            RotateTowards(turnSpeed * Time.deltaTime);
            if (_faceTimer <= 0f && agent) agent.updateRotation = true;   // hand rotation back to NavMeshAgent
        }

      
       
    }

    private void RotateTowards(float maxDegrees)
    {
        Vector3 dir = _faceTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, maxDegrees);
    }

    private void OnDisable()
    {
        if (agent) agent.updateRotation = true;
    
    }
}
