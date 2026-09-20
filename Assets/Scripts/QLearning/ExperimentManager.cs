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
                foreach (var seed in seeds)
                {
                    runNumber++;
                    string diffName = difficulty.ToString().ToLower();
                    currentRun = $"{diffName}_seed{seed}";

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

                    Debug.Log($"=== Completed {currentRun} ===");
                }
            }

            Debug.Log("=== ALL EXPERIMENTS COMPLETE ===");
            currentRun = "DONE";
        }
    }
}