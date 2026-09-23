using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Serialization;
using System.Text;
using System.IO;

namespace QLearning
{
    public class QLearnAgent:MonoBehaviour
    {
        
        [Header("Control")]
        [SerializeField] private bool controlledByExperimentManager = false;
        
        //Inference Mode
        [SerializeField] private bool loadSavedModel = false;
        [SerializeField] private string modelFileName = "trained";
        [SerializeField] private string csvTrainedName = "trained";
        
        
        //Director and player
        [SerializeField] private PlayerModel playerModel;
        [SerializeField] private AIDirector aiDirector;
        
        
        //needs a reinforcement problem
        [SerializeField]private ReinforcementProblem problem;
        
        //iterations
        //[SerializeField] private int iterations = 1000;
        //private int _currentIteration = 0;
        [SerializeField] private int totalEpisodes = 800;
        
        [SerializeField] private float alpha = 0.1f;
        [SerializeField] private float gamma = 0.8f;
        [SerializeField] private float rho = 0.1f;
        [SerializeField] private float startingRho = 0.1f;
        //[SerializeField] private float nu = 0.01f;
        
        //training
        [SerializeField] private bool isTraining = true;
        [SerializeField] private float decisionPeriod = 0.2f;
         private float _decisionTimer = 0f;
         
        [SerializeField] private float episodeTimeout = 30f;

        [SerializeField] private float timeScale;
        
        private float _episodeTimer = 0f;
        private int _episodesCompleted = 0;
        private float _totalEpisodeRewards = 0f;
        
        
        //reference to store for Q values
        [SerializeField]private QValueStore store;
        
        
        //Analysis
        [SerializeField] private int logInterval = 25;
        private StringBuilder trainingLog = new StringBuilder();
        
        
        
        //Current state
        //private State currentState;
        private bool isDead = false;
        
        //pending update tracking for Q-learning
        private State _lastState;
        private Action _lastAction;
        private bool _pendingUpdate = false;
        
        
        //spawners
        [SerializeField] private ZombieSpawner zombieSpawner;
        [SerializeField] private PickupSpawner pickupSpawner;
        


        private void Start()
        {
            
            Time.timeScale = timeScale;


            if (loadSavedModel)
            {
                
                //Watch in normal timescale
                Time.timeScale = 1f;
                
                //load an old model 
                store.LoadFromFile(modelFileName);
                isTraining = false;

                //No exploration, just exploitation
                rho = 0f;
                Debug.Log("Running a trained inference Model");

                return;
                
            }
            
            if (controlledByExperimentManager)
            {
                Debug.Log("QLearnAgent waiting for ExperimentManager");
                return;   // Manager will call BeginTraining()
            }
            
            
            if (problem)
            {
                StartNewEpisode();
            }
        }



        private void Update()
        {
            if (!isTraining) return;
            
            _episodeTimer += Time.deltaTime;
            _decisionTimer += Time.deltaTime;
            
            //Check if training has completed
            if (_episodesCompleted >= totalEpisodes)
            {
                Debug.Log("Training completed");
                isTraining = false;
                
                //Store information
                StoreTrainingData();
                return;
            }
            
            //end after 30 seconds, or when player dies
            if (problem.IsDead() || _episodeTimer >= episodeTimeout)
            {
                EndEpisode();
                return;
            }
            
            //Learn
            if (_decisionTimer >= decisionPeriod)
            {
                _decisionTimer = 0f;
                QLearning();
            }
        }
        
        public bool IsTraining() => isTraining;

        public void SetRunNames(string csvName, string modelName)
        {
            csvTrainedName = csvName;
            modelFileName = modelName;
        }
        
        
        public void BeginTraining()
        {
            // Reset per-run state
            rho = startingRho;
            
            _episodesCompleted = 0;
            _episodeTimer = 0f;
            _decisionTimer = 0f;
            _totalEpisodeRewards = 0f;
            
            trainingLog.Clear();
            isTraining = true;
    
            Debug.Log($"QLearnAgent: Beginning training run");
            StartNewEpisode();
        }
        
        
        private void StartNewEpisode()
        {
            //resetting problem adequately
            
            problem.ResetAgent();
            
            zombieSpawner?.ClearAllZombies();
            pickupSpawner?.ClearAllPickups();
            
            _episodeTimer = 0f;
            _totalEpisodeRewards = 0f;
            //currentState = problem.GetCurrentState();
            isDead = false;
            _pendingUpdate = false;
            
            
            playerModel.Clear();
            aiDirector.Clear();
            
        }
        
        
        private void EndEpisode()
        {
            
            // applying terminal Q-update for the last action
            if (_pendingUpdate)
            {
                float terminalReward = problem.ComputeIntervalReward();
                _totalEpisodeRewards += terminalReward;

                float oldQ = store.GetQValue(_lastState, _lastAction);
                // Terminal state: no future, so no gamma * maxNextQ term
                float newQ = (1 - alpha) * oldQ + alpha * terminalReward;
                store.StoreQValue(_lastState, _lastAction, newQ);

                _pendingUpdate = false;
            }
            
            _episodesCompleted++;
            
            // epsilon(rho) decay
            rho = Mathf.Max(0.01f, rho * 0.995f);
            
            
            bool died = problem.IsDead();
            
            
            //Log information about training
            //Record data at intervals
           
            //appends every episode
            trainingLog.AppendLine(string.Join(";",
                _episodesCompleted,
                _episodeTimer.ToString("F1", CultureInfo.InvariantCulture),
                _totalEpisodeRewards.ToString("F2", CultureInfo.InvariantCulture),
                died ? 1 : 0,
                problem.DamageEvents,
                problem.DamageTaken.ToString("F0", CultureInfo.InvariantCulture),
                problem.ShotsFired,
                problem.Kills));
            
            
            //logs to console in intervals
            if (_episodesCompleted % logInterval == 0)
                Debug.Log($"Ep {_episodesCompleted} | t={_episodeTimer:F1}s died={died} " +
                          $"hits={problem.DamageEvents} dmg={problem.DamageTaken:F0} " +
                          $"shots={problem.ShotsFired} kills={problem.Kills}");
        
            


            StartNewEpisode();
        }
        
        private void QLearning()
        {
            
            //observe current state S_t
            State state = problem.GetCurrentState();
            //Action action;
            
            // compute reward accumulated reward from t-1 to t, for action at t - 1
            float intervalReward = problem.ComputeIntervalReward();
            _totalEpisodeRewards += intervalReward;
            
            
            //apply pending q-update
            if (_pendingUpdate)
            {
                float oldQ = store.GetQValue(_lastState, _lastAction);
                float maxNextQ = store.GetQValue(state, store.GetBestAction(state));
                float newQ = (1 - alpha) * oldQ + alpha * (intervalReward + gamma * maxNextQ);
                store.StoreQValue(_lastState, _lastAction, newQ);
            }
            
            //snapshot baselines before choosing/executing to correctly measure effects of action to take
            problem.SnapshotForReward();
            
            
            //chose action a_t using current state
            
            //list of available actions based on state
            List<Action> actions = problem.GetAvailableActions(state);
            
            //use a random action this time?    //or use best action available
            Action action = (Random.value < rho) ? OneOf(actions) : store.GetBestAction(state);
                
            //execute actions
            problem.ExecuteAction(action);
            
            //save for the next update cycle
            _lastState = state;
            _lastAction = action;
            _pendingUpdate = true;
            
        }
        
      
        
        private Action OneOf(List<Action> actions)
        {
            if (actions == null || actions.Count == 0)
                return new Action(Action.ActionType.Flee);
                
            return actions[Random.Range(0, actions.Count)];
        }

        private void StoreTrainingData()
        {
            if (trainingLog.Length > 0)
            {
                string path = Path.Combine(Application.streamingAssetsPath, csvTrainedName + ".csv");
                string header = "Episode;SurvivalTime;TotalReward;Died;DamageEvents;DamageTaken;ShotsFired;Kills\n";

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                
                File.WriteAllText(path, header + trainingLog.ToString());
                Debug.Log($"Stored at {path}");
                
                
                store.SaveToFile(modelFileName);
                

            }
        }
        
   
        
        
    }
}