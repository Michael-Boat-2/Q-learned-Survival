using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QLearning;

/// <summary>
/// Simple HUD for watching the agent. Read-only: never changes gameplay.
/// Every UI reference is optional - leave a slot empty to hide that element.
/// </summary>
public class AgentHUD : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private ReinforcementProblem problem;
    [SerializeField] private QLearnAgent agent;

    [Header("Health")]
    [Tooltip("UI Image with Image Type = Filled (Horizontal).")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Gradient healthColour;          // right = full, left = empty
    [SerializeField] private float fillLerpSpeed = 8f;

    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Color ammoNormal = Color.white;
    [SerializeField] private Color ammoLow = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color ammoEmpty = new Color(1f, 0.25f, 0.25f);

    [Header("Episode")]
    [SerializeField] private TMP_Text survivalText;          // "45.2 / 120 s"
    [SerializeField] private Image survivalFill;             // optional progress bar to timeout
    [SerializeField] private TMP_Text episodeText;           // "Episode 37 / 100"
    [SerializeField] private TMP_Text winRateText;           // "Win rate 22% (8)"
    [SerializeField] private TMP_Text modeText;              // "Evaluation"

    private float _shownHealth = 1f;

    private void Reset()
    {
        // sensible default gradient: red -> yellow -> green
        healthColour = new Gradient();
        healthColour.SetKeys(
            new[] { new GradientColorKey(new Color(0.9f, 0.2f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.8f, 0.2f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.85f, 0.35f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
    }

    private void Awake()
    {
        if (!problem) problem = FindFirstObjectByType<ReinforcementProblem>();
        if (!agent) agent = FindFirstObjectByType<QLearnAgent>();
        if (healthColour == null) Reset();
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;   // smooth even at high time scale

        if (problem)
        {
            // --- Health ---
            float hp = problem.GetHealthRatio();
            _shownHealth = Mathf.Lerp(_shownHealth, hp, 1f - Mathf.Exp(-fillLerpSpeed * dt));
            if (Mathf.Abs(_shownHealth - hp) > 0.5f) _shownHealth = hp;   // snap on respawn
            if (healthFill)
            {
                healthFill.fillAmount = _shownHealth;
                healthFill.color = healthColour.Evaluate(_shownHealth);
            }
            if (healthText)
                healthText.text = $"{Mathf.CeilToInt(problem.GetHealth())} / {Mathf.CeilToInt(problem.MaxHealth)}";

            // --- Ammo ---
            if (ammoText)
            {
                int ammo = Mathf.RoundToInt(problem.GetAmmo());
                ammoText.text = $"{ammo} / {problem.MaxAmmo}";
                ammoText.color = ammo == 0 ? ammoEmpty : (ammo <= 2 ? ammoLow : ammoNormal);
            }
        }

        if (agent)
        {
            float t = agent.EpisodeTime;
            float max = Mathf.Max(agent.EpisodeTimeout, 0.01f);
            if (survivalText) survivalText.text = $"{t:0.0} / {max:0} s";
            if (survivalFill) survivalFill.fillAmount = Mathf.Clamp01(t / max);

            if (episodeText)
                episodeText.text = $"Episode {Mathf.Min(agent.EpisodesCompleted + 1, agent.TotalEpisodes)} / {agent.TotalEpisodes}";

            if (winRateText)
                winRateText.text = agent.EpisodesCompleted > 0
                    ? $"Win rate {agent.WinRate * 100f:0}%  ({agent.Wins}/{agent.EpisodesCompleted})"
                    : "Win rate --";

            if (modeText) modeText.text = agent.Mode;
        }
    }
}
