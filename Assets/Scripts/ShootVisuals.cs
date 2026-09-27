using UnityEngine;
using UnityEngine.AI;

public class ShootVisuals : MonoBehaviour
{
    [Header("Facing")]
    [SerializeField] private NavMeshAgent agent;      
    [SerializeField] private float faceDuration = 0.4f;
    [SerializeField] private float turnSpeed = 1080f;

    [Header("Muzzle Flash")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float prefabLifetime = 1f;

    [Header("Extras")]
    [SerializeField] private float cameraShake = 0.1f;       
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

        // Face the target to shoot
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
            if (_faceTimer <= 0f && agent) agent.updateRotation = true;  
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
