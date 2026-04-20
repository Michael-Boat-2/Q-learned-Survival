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
                    
                    //More pickups, less spawn
                    spawnRate = 0.5f;
                    pickupRate = 1.5f;
                    rewardScaler = 1.5f;
                    
                    break;
                
                case Difficulty.Baseline:

                    spawnRate = 1f;
                    pickupRate = 1f;
                    rewardScaler = 1f;
                    
                    break;
                
                case Difficulty.Hard:

                    spawnRate = 1.5f;
                    pickupRate = 0.5f;
                    rewardScaler = 0.8f;
                    
                    break;
                
                case Difficulty.AI:

                    var adjThreshold = 0.05f;

                    //if tension is too high
                    if (currentTension - targetTension > adjThreshold)
                    {
                        //Lerp to gradually spawn adjust rate 
                        spawnRate = Mathf.Lerp(spawnRate, 0.5f, 0.1f * Time.deltaTime);
                        pickupRate = 1.2f;
                        
                        //Rest when tension is too high
                        if (currentTension > 0.8f && !_isRestTriggered)
                        {
                            TriggerRest();
                        }
                        
                        
                    }
                    else if (currentTension - targetTension < -adjThreshold)
                    {
                        
                        spawnRate = Mathf.Lerp(spawnRate, 1.5f, 0.1f * Time.deltaTime );
                        pickupRate = 0.8f;
                       

                    }
                    else
                    {
                        spawnRate = Mathf.Lerp(spawnRate, 1f, 0.1f * Time.deltaTime);
                        pickupRate = 1f;
                       
                    }
                    
                    rewardScaler = 0.8f + (1 - currentTension) * 0.4f;
                    
                    break;
                
            }
            
            //Change Game Settings According to difficulty
            ChangeRates();
            
            
            if (hasScriptedEvent)
            {
                _eventTimer += Time.deltaTime;
                
                //See if can perform events to adjust tension
                
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


        private void ChangeRates()
        {
            zSpawner.SetSpawnRate(spawnRate);
            pSpawner.SetSpawnRate(pickupRate);
            agent.SetRewardScale(rewardScaler);
        }


        private void TriggerRest()
        {
            _isRestTriggered = true;
            
            zSpawner.TriggerRestPeriod(5f);
            
        }
       
        
        
        public void Clear()
        {
            _episodeTimer = 0f;
            _eventTimer = 0f;
            
            _isRestTriggered = false;
           
        }
        
        
        
        
        
        
    }
}