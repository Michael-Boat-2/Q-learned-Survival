using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System.Text;
using System.IO;

namespace QLearning
{
    public class QLearnAgent:MonoBehaviour
    {
        
        
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
        [SerializeField] private int totalEpisodes = 500;
        
        [SerializeField] private float alpha = 0.3f;
        [SerializeField] private float gamma = 0.75f;
        [SerializeField] private float rho = 0.1f;
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
        private State currentState;
        private bool isDead = false;
        


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
        
        
        private void StartNewEpisode()
        {
            problem.ResetAgent();
            FindFirstObjectByType<ZombieSpawner>()?.ClearAllZombies();
            _episodeTimer = 0f;
            _totalEpisodeRewards = 0f;
            currentState = problem.GetCurrentState();
            isDead = false;
            
            
            playerModel.Clear();
            aiDirector.Clear();
            
        }
        
        
        private void EndEpisode()
        {
            _episodesCompleted++;
            
            //Log information about training
            //Record data at intervals
            if (_episodesCompleted % logInterval == 0)
            {
                trainingLog.AppendLine($"{_episodesCompleted}];{_episodeTimer:F1};{_totalEpisodeRewards:F2}");
                Debug.Log($"Episode: {_episodesCompleted}, Survival Time: {_episodeTimer:F1}, Rewards Gained: {_totalEpisodeRewards:F2}");
            }
            
            
            
            StartNewEpisode();
        }
        
        private void QLearning()
        {
            
            //has a current state
            State state = problem.GetCurrentState();
            Action action;
            
            
            //list of available actions based on state
            List<Action> actions = problem.GetAvailableActions(state);
                
            //use a random action this time?
            if (Random.value < rho)
            {
                action = OneOf(actions);
            }
            else
            {
                //or use best action available
                action = store.GetBestAction(state);
            }

            //perform action and retrieve the reward and new state
            var (reward, newState) = problem.TakeActions(state, action);
            _totalEpisodeRewards += reward;
                
            //Get the current q from store
            float Q = store.GetQValue(state, action);
                
            //get the q of the best action from the new state
            float maxQ = store.GetQValue(newState, store.GetBestAction(newState));
                
            //Perform the q learning
            Q = (1 - alpha) * Q + alpha * (reward + gamma * maxQ);
                
            //Store the new Q value
            store.StoreQValue(state,action, Q);
                
            //update state
            currentState = newState;
            //_currentIteration++;
            
        }
        
        
        /*//QLearning updates store
        private void QLearningIter(ReinforcementProblem prob,int iter,float a,float g,float r,float n)
        {
            //starting state
            State state = ReinforcementProblem.GetRandomState();
            Action action = new Action();
            
            

            //Repeat
            for (int i = 0; i < iter; i++)
            {

                //if random between 1 and 0 is less than nu
                if (Random.value < n)
                {
                    state = ReinforcementProblem.GetRandomState();
                }
                
                //list of available actions based on state
                List<Action> actions = prob.GetAvailableActions(state);
                
                
                //use a random action this time?
                if (Random.value < r)
                {
                    action = OneOf(actions);
                }
                else
                {
                    //or use best action available
                    action = store.GetBestAction(state);
                }
                
                //perform action and retrieve the reward and new state
                var (reward, newState) = prob.TakeActions(state, action);
                
                //Get the current q from store
                float Q = store.GetQValue(state, action);
                
                //get the q of the best action from the new state
                float maxQ = store.GetQValue(newState, store.GetBestAction(newState));
                
                //Perform the q learning
                Q = (1 - a) * Q + a * (reward + g * maxQ);
                
                //Store the new Q value
                store.StoreQValue(state,action, Q);
                
                //update state
                state = newState;

            }
            
        }*/
        
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
                string header = "Episode;SurvivalTime;TotalReward \n";

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