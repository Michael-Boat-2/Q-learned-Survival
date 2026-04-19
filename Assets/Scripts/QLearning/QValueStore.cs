using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class QValueStore : MonoBehaviour
    {
        
        private Dictionary<string, float[]> qTable;
        private const int ActionsBuffer = 4;
        
        
        public QValueStore()
        {
            //create qTable indexed by states and action number in buffer
            qTable = new Dictionary<string, float[]>();
            
            InitQTable();
        }
        
        private void InitQTable()
        {
            var allStates = State.GetAllPossibleStates();
            
            foreach (var state in allStates)
            {
                //for each state, init q values of each action
                var key = state.StateString();
                qTable[key] = new float[ActionsBuffer];

                for (var i = 0; i < ActionsBuffer; i++)
                {
                    //initialising all q values to 1
                    qTable[key][i] = 1.0f;
                }
                   
            }
        }
        

        
        public Action GetBestAction(State state)
        {
            var key = state.StateString();
            
            if (!qTable.TryGetValue(key, out var values))
            {
                qTable[key] = new float[ActionsBuffer];
                return new Action(Action.ActionType.Flee);
            }

            var bestIndex = 0;
            var bestValue = values[0];
            
            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] > bestValue)
                {
                    bestValue = values[i];
                    bestIndex = i;
                }
            }
            
            return new Action((Action.ActionType)bestIndex);
        }
        
        

        public float GetQValue(State state, Action action)
        {
            var key = state.StateString();
            
            if (!qTable.ContainsKey(key))
                qTable[key] = new float[ActionsBuffer];
            
            return qTable[key][(int)action.Type];
        }
        
        
        
        public void StoreQValue(State state, Action action, float qValue)
        {
            var key = state.StateString();
            
            if (!qTable.ContainsKey(key))
                qTable[key] = new float[ActionsBuffer];
            
            qTable[key][(int)action.Type] = qValue;
        }
    }
}