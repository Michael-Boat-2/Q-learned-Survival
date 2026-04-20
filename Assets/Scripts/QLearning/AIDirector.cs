using System;
using UnityEngine;

namespace QLearning
{
    public class AIDirector : MonoBehaviour
    {
        
        
        public enum Difficulty
        {
            Easy,
            Baseline,
            Hard,
            AI
        }
        
        public Difficulty Level {get;set;}
        
        
        //Maintain Tension Curve
        [SerializeField]private float targetTension = 0.7f;
        [SerializeField]private float currentTension;

        //Monitor Player State on Model
        [SerializeField] private PlayerModel player;
        [SerializeField] private ZombieSpawner zSpawner;
        [SerializeField] private PickupSpawner pSpawner;

        [SerializeField] private ReinforcementProblem agent;
        

        //Adjust Environment Parameters
        

        //Spawn Rate of Enemies
        [SerializeField] private float spawnRate;
        
        //Pickups available
        [SerializeField] private float pickupRate;
        
        //Modify Reward Scaling
        [SerializeField] private float rewardScaler;


        //spawn boss
        [SerializeField] private bool hasScriptedEvent = false;
        [SerializeField] private float eventTiming;
        [SerializeField] private GameObject bossZombie;

        private float _eventTimer;
        private float _episodeTimer;

        private bool _isRestTriggered;
        

        
        //Director Actions

        //adjust spawn rate
        //trigger rests


        //Introduce Scripted Events


        private void Start()
        {
            
        }


        private void Update()
        {

            
            _episodeTimer += Time.deltaTime;
            
            
            GetTension();
            
            //Switch Difficulty
            switch (Level)
            {
                case Difficulty.Easy:

                    break;
                
                case Difficulty.Baseline:
                    break;
                
                case Difficulty.Hard:
                    break;
                
                case Difficulty.AI:
                    break;
                
            }
            
            //Change Game Settings Accordingly
            
            
            if (hasScriptedEvent)
            {
                _eventTimer += Time.deltaTime;
                
                //See if can perform
                
                
            }
            
            
        }

        public float GetTension()
        {
            return currentTension;
        }

        public float GetSpawnRate()
        {
            return spawnRate;
        }


        private void CheckTension()
        {

            //Player Models defines factors that contribute to tension 
            var playerStress = player.Stress;
            var playerFatigue = player.Fatigue;
            var playerEngagement = player.Engagement;
            
            //A balance of player stress and engagement
            currentTension = playerStress * 0.7f +  playerFatigue * 0.1f + playerEngagement * 0.2f;
                
        }


        private void AdjustTension()
        {
            
        }
        
        
        
        public void Clear()
        {
            _episodeTimer = 0f;
            _eventTimer = 0f;
            _restTriggered = false;
            spawnRateMultiplier = 1f;
            pickupRateMultiplier = 1f;
            rewardScaleMultiplier = 1f;
        }
        
        
        
        
        
        
    }
}