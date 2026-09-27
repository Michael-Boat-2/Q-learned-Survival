using UnityEngine;
using UnityEngine.AI;


public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private NavMeshAgent targetAgent;  
    [SerializeField] private float targetHeightOffset = 1f;

    [Header("Framing")]
    [SerializeField] private float distance = 14f;
    [SerializeField, Range(10f, 89f)] private float pitch = 55f; 
    [SerializeField] private float yaw = 0f;                      

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.25f;
    [SerializeField] private float lookAhead = 2f;        
    [SerializeField] private float lookAheadSmoothTime = 0.5f;
  
    
    [SerializeField] private float snapDistance = 8f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Zoom")]
    [SerializeField] private bool allowZoom = true;
    [SerializeField] private float zoomSpeed = 4f
        ;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Orbit")]
    [SerializeField] private bool allowManualOrbit = true;  
    [SerializeField] private float orbitSpeed = 60f;       
    [SerializeField] private bool autoOrbit;         
    [SerializeField] private float autoOrbitSpeed = 6f;

    [Header("Shake")]
    [SerializeField] private float maxShakeOffset = 0.5f;
    [SerializeField] private float shakeDecay = 1.5f;        

    private Vector3 _focus;
    private Vector3 _focusVelocity;
    private Vector3 _lookAheadOffset;
    private Vector3 _lookAheadVelocity;
    private Vector3 _lastTargetPos;
    private float _targetDistance;
    private float _trauma;                                 

    private float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        Instance = this;
        _targetDistance = distance;
    }

    private void Start()
    {
        if (target && !targetAgent) targetAgent = target.GetComponent<NavMeshAgent>();
        SnapToTarget();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetAgent = newTarget ? newTarget.GetComponent<NavMeshAgent>() : null;
        SnapToTarget();
    }

  
    public void Shake(float amount)
    {
        _trauma = Mathf.Clamp01(_trauma + amount);
    }

    private void SnapToTarget()
    {
        if (!target) return;
        _focus = target.position + Vector3.up * targetHeightOffset;
        _focusVelocity = Vector3.zero;
        _lookAheadOffset = Vector3.zero;
        _lookAheadVelocity = Vector3.zero;
        _lastTargetPos = target.position;
        ApplyTransform(Vector3.zero);
    }

    private void LateUpdate()
    {
        if (!target) return;
        float dt = Dt;
        if (dt <= 0f) return;

        if (Vector3.Distance(target.position, _lastTargetPos) > snapDistance)
            SnapToTarget();
        _lastTargetPos = target.position;

        HandleInput(dt);

        // Look ahead in the direction of travel
        Vector3 vel = targetAgent ? targetAgent.velocity : Vector3.zero;
        vel.y = 0f;
        float speedRef = targetAgent && targetAgent.speed > 0.01f ? targetAgent.speed : 1f;
        Vector3 desiredAhead = Vector3.ClampMagnitude(vel / speedRef, 1f) * lookAhead;
        _lookAheadOffset = Vector3.SmoothDamp(_lookAheadOffset, desiredAhead, ref _lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, dt);
        
        
        Vector3 desiredFocus = target.position + Vector3.up * targetHeightOffset + _lookAheadOffset;
        _focus = Vector3.SmoothDamp(_focus, desiredFocus, ref _focusVelocity, followSmoothTime, Mathf.Infinity, dt);

        // Smooth zoom
        distance = Mathf.Lerp(distance, _targetDistance, 1f - Mathf.Exp(-10f * dt));

        
        // Shake with trauma
        Vector3 shake = Vector3.zero;
        if (_trauma > 0f)
        {
            float s = _trauma * _trauma * maxShakeOffset;
            float t = Time.unscaledTime * 25f;
            shake = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * (2f * s);
            _trauma = Mathf.Max(0f, _trauma - shakeDecay * dt);
        }

        ApplyTransform(shake);
    }

    private void HandleInput(float dt)
    {
        if (allowZoom)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        if (allowManualOrbit)
        {
            if (Input.GetKey(KeyCode.Q)) yaw -= orbitSpeed * dt;
            if (Input.GetKey(KeyCode.E)) yaw += orbitSpeed * dt;
        }

        if (autoOrbit) yaw += autoOrbitSpeed * dt;
    }

    private void ApplyTransform(Vector3 shake)
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = _focus - rot * Vector3.forward * distance;
        transform.SetPositionAndRotation(pos + shake, rot);
    }
}
