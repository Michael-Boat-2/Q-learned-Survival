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
        [SerializeField] private bool runRandomBaseline = true;   // once per difficulty
        [SerializeField] private int randomEpisodes = 100;
        [SerializeField] private bool runEvaluation = true;       // after each training run
        [SerializeField] private int evalSeedOffset = 1000;       // eval uses a different seed than training

        [Header("Timing")]
        [Tooltip("Real-time seconds to wait after each run completes before starting the next.")]
        [SerializeField] private float postRunDelaySeconds = 3f;

        [Header("Status (read-only)")]
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

                // 0. Random-policy baseline for this difficulty
                if (runRandomBaseline)
                {
                    currentRun = $"{filePrefix}_{diffName}_random";
                    Debug.Log($"=== Random baseline: {currentRun} ===");
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

                    string csvName   = currentRun;      // e.g. baseline_seed42
                    string modelName = currentRun;      // e.g. baseline_seed42

                    Debug.Log($"=== Run {runNumber}/{totalRuns}: {currentRun} ===");

                    // 1. Reset environment + Q-table
                    store.ResetQTable();

                    // 2. Seed Unity's random source for reproducibility
                    Random.InitState(seed);

                    // 3. Configure director and filenames
                    director.Level = difficulty;
                    agent.SetRunNames(csvName, modelName);

                    // 4. Begin training
                    agent.BeginTraining();

                    // 5. Wait until QLearnAgent reports completion
                    while (agent.IsTraining())
                    {
                        yield return null;
                    }

                    // 6. Give time for file write + Unity cleanup
                    yield return new WaitForSecondsRealtime(postRunDelaySeconds);

                    // 7. Greedy evaluation of the Q-table just trained
                    if (runEvaluation)
                    {
                        Random.InitState(seed + evalSeedOffset);
                        director.Level = difficulty;
                        agent.BeginEvaluation(modelName, currentRun + "_eval");
                        while (agent.IsTraining()) yield return null;
                        yield return new WaitForSecondsRealtime(postRunDelaySeconds);
                    }

                    Debug.Log($"=== Completed {currentRun} ===");
                }
            }

            Debug.Log("=== ALL EXPERIMENTS COMPLETE ===");
            currentRun = "DONE";
        }
    }
}