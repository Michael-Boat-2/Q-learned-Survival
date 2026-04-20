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
        //[SerializeField] private float nu = 0.01f;
        
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
        
        //reference to store for Q values
        [SerializeField]private QValueStore store;
        
        
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
                Debug.Log("Training completed");
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
                QLearning();
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
         
            StartNewEpisode();
        }
        
        private void QLearning()
        {
           
            
            //has a current state
            State state = problem.GetCurrentState();
            Action action = new Action();
            
            
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
            _currentIteration++;
            
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
        
   
        
        
    }
}