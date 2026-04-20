using UnityEngine;

namespace QLearning
{
    public class Pickup : MonoBehaviour
    {
        public enum PickupType
        {
            Health,
            Ammo
        }
        
        [SerializeField] private PickupType type;
        [SerializeField] private float healthAmount = 30f;
        [SerializeField] private int ammoAmount = 3;
        [SerializeField] private float lifetime = 15f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.5f;
        
        private Vector3 _startPosition;
        private float _spawnTime;
        private PlayerModel _playerModel;
        
        public void Initialize(PickupType pickupType)
        {
            type = pickupType;
            _startPosition = transform.position;
            _spawnTime = Time.time;
            
            // Find player model for tracking
            _playerModel = FindObjectOfType<PlayerModel>();
        }
        
        private void Update()
        {
            // Bobbing animation
            float newY = _startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Rotate slowly
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            
            // Despawn after lifetime
            if (Time.time - _spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            var agent = other.GetComponent<ReinforcementProblem>();
            if (agent == null) return;
            
            switch (type)
            {
                case PickupType.Health:
                    agent.Heal(healthAmount);
                    break;
                case PickupType.Ammo:
                    agent.AddAmmo(ammoAmount);
                    break;
            }
            
            // Notify player model for engagement tracking
            if (_playerModel != null)
            {
                _playerModel.RegisterPickupCollected();
            }
            
            Destroy(gameObject);
        }
    }
}