using System;
using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class PlayerModel : MonoBehaviour
    {

        [SerializeField] private ReinforcementProblem agent;
        
        
        [SerializeField] private ZombieSpawner spawner;
        
        //behavioral features
        public float HealthTrend{get;set;}
        public float DamagePerMin { get; set; } //damage taken per min
        public float EnemyDensity{get;set;}
        public int SuccessStreak{get;set;}
        public int FailStreak { get; set; }
    
        
        //Player state definition
        
        //Stress indicates player is worried about losing
        public float Stress {get; set;}
        
        //indicates player is using tools available
        public float Engagement {get; set;}
        
        //fatigue from trying to survive for too long
        public float Fatigue {get; set;}
        
        
        private float _lastHealth;
        private float _currentHealth;
        private const float DeltaTime = 3f;
        private float _healthTimer = 0f;
        
        
        private float _damageThisMinute;
        private float _dmgMinuteTimer;
        private const float ZombieDamage = 20f;

        private float _survivalTime;
        private int _hitsTaken;


        private void Start()
        {
            _lastHealth = agent.GetHealth();

        }


        private void Update()
        {
            
            _healthTimer  += Time.deltaTime;
            
            _survivalTime += Time.deltaTime;
            
            _dmgMinuteTimer += Time.deltaTime;
            

            if (_healthTimer >= DeltaTime)
            {
                _currentHealth = agent.GetHealth();
                
                //health trend is change in health over time, director can use this
                HealthTrend = (_currentHealth - _lastHealth) / DeltaTime;
                _lastHealth = _currentHealth;
                _healthTimer = 0f;
            }

            if (_dmgMinuteTimer >= 15f)
            {
                //Sims run for 30 secs, x4 for expected damage
                _damageThisMinute = _hitsTaken * ZombieDamage * 4;
                _dmgMinuteTimer = 0f;
                _hitsTaken = 0;
            }
            
            
            
            UpdateStress();
            UpdateEngagement();
            UpdateFatigue();
            
            UpdateEnemyDensity();
            
        }
        
        
        //Track Damage, called from RL Agent on hit rather than update
        public void UpdateHits()
        {
            //Get damage per minute
            _hitsTaken++;
            FailStreak++;
            SuccessStreak = 0;
        }


        public void UpdateKills()
        {
            SuccessStreak++;
            FailStreak = 0;
        }

        public void UpdatePickups()
        {
            SuccessStreak++;
            FailStreak = 0;
        }
        
        
        //Enemy density rather than encounter time
        private void UpdateEnemyDensity()
        {
            //Use spawner to count
            EnemyDensity = spawner.GetZombieDensity();
        }
    

        

        private void UpdateStress()
        {
            var healthWeight = 1f - agent.GetHealthRatio();
            
            var ammoWeight = 1f - agent.GetAmmo()/6;

            var enemyWeight = 1f * EnemyDensity;
            
            Stress = (healthWeight * 0.3f + ammoWeight * 0.3f + enemyWeight * 0.4f );
        }

        private void UpdateEngagement()
        {
            var ammoWeight = 1f - agent.GetAmmo()/6;

            var survivalWeight = Mathf.Min(1f, _survivalTime / 30f);
            
            var successWeight = Mathf.Min(1f, SuccessStreak / 5f);
            
            Engagement = survivalWeight * 0.3f + ammoWeight * 0.2f + successWeight * 0.5f;
            
        }

        private void UpdateFatigue()
        {
            var survivalWeight =  Mathf.Min(1f, _survivalTime / 30f);
            
            var stressWeight = Mathf.Min(1f, Stress / 5f);
            
            Fatigue = stressWeight + 0.5f +  survivalWeight;
            
        }


        public void Clear()
        {
            _survivalTime = 0;
            _damageThisMinute = 0;
            _hitsTaken = 0;

            _dmgMinuteTimer = 0;
            _healthTimer = 0;
            
            SuccessStreak = 0;
            FailStreak = 0;
            
            _lastHealth = agent.GetHealth();
            
        }
        

    }
}