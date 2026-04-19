using UnityEngine;
using System.Collections.Generic;

namespace QLearning
{
    public class QValueStore
    {

        // Dictionary mapping state string to array of Q-values per action
        private Dictionary<string, float[]> qTable;
        private const int NUM_ACTIONS = 4;
        
        
        public QValueStore()
        {
            qTable = new Dictionary<string, float[]>();
            InitializeQTable();
        }
        
        private void InitializeQTable()
        {
            // Pre-populate with all 27 possible states
            var allStates = State.GetAllPossibleStates();
            foreach (var state in allStates)
            {
                string key = state.ToString();
                qTable[key] = new float[NUM_ACTIONS];
                // Initialize with small random values to break ties
                for (int i = 0; i < NUM_ACTIONS; i++)
                    qTable[key][i] = Random.Range(-0.1f, 0.1f);
            }
        }
        


        public Action GetBestAction(State state)
        {
            string key = state.ToString();
            
            if (!qTable.ContainsKey(key))
            {
                qTable[key] = new float[NUM_ACTIONS];
                return new Action(Action.ActionType.MoveToEnemy);
            }
            
            float[] values = qTable[key];
            int bestIndex = 0;
            float bestValue = values[0];
            
            for (int i = 1; i < values.Length; i++)
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
            string key = state.ToString();
            
            if (!qTable.ContainsKey(key))
                qTable[key] = new float[NUM_ACTIONS];
            
            return qTable[key][(int)action.Type];
        }
        public void StoreQValue(State state, Action action, float qValue)
        {
            string key = state.ToString();
            
            if (!qTable.ContainsKey(key))
                qTable[key] = new float[NUM_ACTIONS];
            
            qTable[key][(int)action.Type] = qValue;
        }
    }
}