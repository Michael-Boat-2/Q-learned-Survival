using System;
using UnityEngine;

namespace QLearning
{
    
    
    public class Pickup : MonoBehaviour
    {
        
        [SerializeField] private PickupType type;
        [SerializeField] private float healthAmount = 30f;
        [SerializeField] private int ammoAmount = 3;
        [SerializeField] private float lifetime = 15f;
        
        
        private Vector3 _startPosition;
        private float _spawnTime;
        private PlayerModel _playerModel;
        
        public void Init(PickupType pickupType)
        {
            type = pickupType;
            _startPosition = transform.position;
            _spawnTime = Time.time;

            _playerModel = FindFirstObjectByType<PlayerModel>();
        }
        
        private void Update()
        {
            
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
            if (!agent) return;
            
            switch (type)
            {
                case PickupType.Health:
                    agent.Heal(healthAmount);
                    break;
                case PickupType.Ammo:
                    agent.AddAmmo(ammoAmount);
                    break;
                default:
                    agent.Heal(healthAmount);
                    break;
            }
            
         
            if (_playerModel)
            {
                _playerModel.UpdatePickups();
            }
            
            Destroy(gameObject);
        }
    }
}