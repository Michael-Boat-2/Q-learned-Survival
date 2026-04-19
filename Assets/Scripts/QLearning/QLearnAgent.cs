using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace QLearning
{
    public class QLearnAgent:MonoBehaviour
    {
        
        //needs a reinforcement problem
        [SerializeField]private ReinforcementProblem problem;
        
        //iterations
        [SerializeField] private int iterations = 1000;
        private int _currentIteration = 0;
        
        [SerializeField] private float alpha = 0.3f;
        [SerializeField] private float gamma = 0.75f;
        [SerializeField] private float rho = 0.1f;
        [SerializeField] private float nu = 0.01f;
        
        
        //training
        [SerializeField] private bool isTraining = true;
        [SerializeField] private float decisionPeriod = 0.2f;
         private float _decisionTimer = 0f;
         
        [SerializeField] private float episodeTimeout = 30f;
        private float _episodeTimer = 0f;
        private int _episodesCompleted = 0;
        private float _totalEpisodeRewards = 0f;
        
        
        //debugging
        [SerializeField] private bool logProgress = true;
        [SerializeField] private int logInterval = 100;
        
        
        //store for Q values
        public QValueStore Store = new QValueStore();
        
        
        //Current state
        private State currentState;
        private bool isDead = false;


        private void Start()
        {
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
            if (_currentIteration >= iterations)
            {
                Debug.Log("TRAINING COMPLETE!");
                isTraining = false;
                
                //Store information somehow
                return;
            }
            
            //end after 30 seconds, or when player dies
            //TODO have problem is Dead call End Episode
            if (problem.IsDead() || _episodeTimer >= episodeTimeout)
            {
                EndEpisode();
                return;
            }
            
            //Learn
            if (_decisionTimer >= decisionPeriod)
            {
                _decisionTimer = 0f;
                QLearningStep();
            }
        }
        
        
        private void QLearningStep()
        {
            // 1. Get current state
            currentState = problem.GetCurrentState();
            
            // 2. Random restart (nu)
            if (Random.value < restartProbability)
            {
                StartNewEpisode();
                return;
            }
            
            // 3. Choose action (epsilon-greedy)
            Action action;
            List<Action> availableActions = problem.GetAvailableActions(currentState);
            
            if (Random.value < epsilon || availableActions.Count == 0)
            {
                // Explore: random action
                action = OneOf(availableActions);
            }
            else
            {
                // Exploit: best known action
                action = Store.GetBestAction(currentState);
            }
            
            // 4. Take action and get reward + new state
            var (reward, newState) = problem.TakeActions(currentState, action);
            _totalEpisodeRewards += reward;
            
            // 5. Q-Learning update
            float oldQ = Store.GetQValue(currentState, action);
            float maxFutureQ = Store.GetQValue(newState, Store.GetBestAction(newState));
            float newQ = (1 - alpha) * oldQ + alpha * (reward + gamma * maxFutureQ);
            
            Store.StoreQValue(currentState, action, newQ);
            
            // 6. Update state
            currentState = newState;
            _currentIteration++;
            
            // 7. Log progress
            if (logProgress && _currentIteration % logInterval == 0)
            {
                Debug.Log($"Iteration {_currentIteration}/{iterations} | " +
                          $"Episode {_episodesCompleted} | " +
                          $"Epsilon: {epsilon:F3} | " +
                          $"State: {currentState}");
            }
        }
        
        private void StartNewEpisode()
        {
            problem.ResetAgent();
            _episodeTimer = 0f;
            _totalEpisodeRewards = 0f;
            currentState = problem.GetCurrentState();
            isDead = false;
        }
        
        
        private void EndEpisode()
        {
            _episodesCompleted++;
            
            if (logProgress)
            {
                Debug.Log($"Episode {_episodesCompleted} Ended | " +
                          $"Survival: {_episodeTimer:F1}s | " +
                          $"Reward: {_totalEpisodeRewards:F2}");
            }
            
            // Decay epsilon
            epsilon = Mathf.Max(0.05f, epsilon * 0.995f);
            
            StartNewEpisode();
        }
        
        
        
        
        //QLearning updates store
        private void QLearning(ReinforcementProblem prob,int iter,float a,float g,float r,float n)
        {
            //starting state
            State state = prob.GetRandomState();
            Action action = new Action();
            

            //Repeat
            for (int i = 0; i < iter; i++)
            {

                //if random between 1 and 0 is less than nu
                if (Random.value < n)
                {
                    state = prob.GetRandomState();
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
                    action = Store.GetBestAction(state);
                }
                
                //perform action and retrieve the reward and new state
                var (reward, newState) = prob.TakeActions(state, action);
                
                //Get the current q from store
                float Q = Store.GetQValue(state, action);
                
                //get the q of the best action from the new state
                float maxQ = Store.GetQValue(newState, Store.GetBestAction(newState));
                
                //Perform the q learning
                Q = (1 - a) * Q + a * (reward + g * maxQ);
                
                //Store the new Q value
                Store.StoreQValue(state,action, Q);
                
                //update state
                state = newState;

            }
            
        }
        
        private Action OneOf(List<Action> actions)
        {
            if (actions == null || actions.Count == 0)
                return new Action(Action.ActionType.MoveToEnemy);
                
            return actions[Random.Range(0, actions.Count)];
        }
        
        //Need methods to train publicly
        
        
    }
}