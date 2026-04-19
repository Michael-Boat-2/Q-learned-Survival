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
        [SerializeField] private float alpha = 0.1f;
        [SerializeField] private float gamma = 0.9f;
        [SerializeField] private float rho = 0.2f;
        [SerializeField] private float nu = 0.01f;
        
        
        //training
        [SerializeField] private bool isTraining = true;
        [SerializeField] private float decisionInterval = 0.2f;
        [SerializeField] private float episodeTimeout = 30f;
        
        //debugging
        [SerializeField] private bool logProgress = true;
        [SerializeField] private int logInterval = 100;
        
        
        //store for Q values
        public QValueStore Store = new QValueStore();
        
        
            
        //Control var
        private int currentIteration = 0;
        private float decisionTimer = 0f;
        private float episodeTimer = 0f;
        private int episodesCompleted = 0;
        private float totalRewardThisEpisode = 0f;
        
        private State currentState;
        private bool isDead = false;


        private void Start()
        {
            if (problem)
            {
                //Start new episode
                StartNewEpisode();
            }
        }



        private void Update()
        {
            if (!isTraining) return;
            if (currentIteration >= iterations)
            {
                Debug.Log("TRAINING COMPLETE!");
                isTraining = false;
                //Store.SaveToFile("trained_qtable.json");
                return;
            }
            
            episodeTimer += Time.deltaTime;
            
            // Check episode end conditions
            if (problem.IsDead() || episodeTimer >= episodeTimeout)
            {
                EndEpisode();
                return;
            }
            
            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
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
            totalRewardThisEpisode += reward;
            
            // 5. Q-Learning update
            float oldQ = Store.GetQValue(currentState, action);
            float maxFutureQ = Store.GetQValue(newState, Store.GetBestAction(newState));
            float newQ = (1 - alpha) * oldQ + alpha * (reward + gamma * maxFutureQ);
            
            Store.StoreQValue(currentState, action, newQ);
            
            // 6. Update state
            currentState = newState;
            currentIteration++;
            
            // 7. Log progress
            if (logProgress && currentIteration % logInterval == 0)
            {
                Debug.Log($"Iteration {currentIteration}/{iterations} | " +
                          $"Episode {episodesCompleted} | " +
                          $"Epsilon: {epsilon:F3} | " +
                          $"State: {currentState}");
            }
        }
        
        private void StartNewEpisode()
        {
            problem.ResetAgent();
            episodeTimer = 0f;
            totalRewardThisEpisode = 0f;
            currentState = problem.GetCurrentState();
            isDead = false;
        }
        
        
        private void EndEpisode()
        {
            episodesCompleted++;
            
            if (logProgress)
            {
                Debug.Log($"Episode {episodesCompleted} Ended | " +
                          $"Survival: {episodeTimer:F1}s | " +
                          $"Reward: {totalRewardThisEpisode:F2}");
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