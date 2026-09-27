using System.Collections;
using UnityEngine;

namespace QLearning
{
    public class ExperimentManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QLearnAgent agent;
        [SerializeField] private QValueStore store;
        [SerializeField] private AIDirector director;

        [Header("Experiment Setup")]
        [SerializeField] private int[] seeds = { 42, 43, 44 };
        [SerializeField] private AIDirector.Difficulty[] difficulties = {
            AIDirector.Difficulty.Easy,
            AIDirector.Difficulty.Baseline,
            AIDirector.Difficulty.Hard,
            AIDirector.Difficulty.AI
        };

        [Header("Run Phases")]
        [SerializeField] private string filePrefix = "final";
        // once per difficulty
        [SerializeField] private bool runRandomBaseline = true;   
        [SerializeField] private int randomEpisodes = 100;
        // after each training run
        [SerializeField] private bool runEvaluation = true;  
        // eval uses a different seed than training
        [SerializeField] private int evalSeedOffset = 1000;   
        
        
        [Header("Timing")]
        // Real-time seconds to wait after each run completes before starting the next
        [SerializeField] private float postRunDelaySeconds = 3f;
        
        [Header("Status")]
        [SerializeField] private string currentRun = "";
        [SerializeField] private int runNumber = 0;
        [SerializeField] private int totalRuns = 0;

        private void Start()
        {
            StartCoroutine(RunAllExperiments());
        }

        private IEnumerator RunAllExperiments()
        {
            // Wait a frame so other Start() methods complete
            yield return null;

            totalRuns = difficulties.Length * seeds.Length;

            foreach (var difficulty in difficulties)
            {
                string diffName = difficulty.ToString().ToLower();

                // Random-policy baseline for this difficulty
                if (runRandomBaseline)
                {
                    currentRun = $"{filePrefix}_{diffName}_random";
                    Debug.Log($" Random baseline: {currentRun} ");
                    store.ResetQTable();
                    Random.InitState(seeds.Length > 0 ? seeds[0] : 42);
                    director.Level = difficulty;
                    agent.BeginRandomBaseline(currentRun, randomEpisodes);
                    while (agent.IsTraining()) yield return null;
                    yield return new WaitForSecondsRealtime(postRunDelaySeconds);
                }

                foreach (var seed in seeds)
                {
                    runNumber++;
                    currentRun = $"{filePrefix}_{diffName}_seed{seed}";

                    string csvName   = currentRun;    
                    string modelName = currentRun;    

                    Debug.Log($"Run {runNumber}/{totalRuns}: {currentRun} ");

                    // reset environment and our Q-table
                    store.ResetQTable();

                    // seed the random source for our Unity
                    Random.InitState(seed);

                    // configure director and filenames
                    director.Level = difficulty;
                    agent.SetRunNames(csvName, modelName);

                    // begin training
                    agent.BeginTraining();

                    // wait until QLearnAgent reports completion
                    while (agent.IsTraining())
                    {
                        yield return null;
                    }

                    // yield for enough time for file writing
                    yield return new WaitForSecondsRealtime(postRunDelaySeconds);

                    // greedy evaluation of the Q-table just trained
                    if (runEvaluation)
                    {
                        Random.InitState(seed + evalSeedOffset);
                        director.Level = difficulty;
                        agent.BeginEvaluation(modelName, currentRun + "_eval");
                        while (agent.IsTraining()) yield return null;
                        yield return new WaitForSecondsRealtime(postRunDelaySeconds);
                    }

                    Debug.Log($" Completed {currentRun}");
                }
            }

            Debug.Log("End of Experiments");
            currentRun = "DONE";
        }
    }
}