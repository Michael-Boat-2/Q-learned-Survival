import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path

# ============= CONFIGURATION =============
CSV_DIR = Path(".")
DIFFICULTIES = ["easy", "baseline", "hard", "ai"]
SEEDS = [42, 43, 44]
OUTPUT_DIR = Path("plots")
OUTPUT_DIR.mkdir(exist_ok=True)

COLORS = {
    "easy": "#2ca02c",
    "baseline": "#1f77b4",
    "hard": "#d62728",
    "ai": "#9467bd",
}

# ============= LOAD =============
def load_difficulty(difficulty):
    dfs = []
    for seed in SEEDS:
        filepath = CSV_DIR / f"{difficulty}_seed{seed}.csv"
        if not filepath.exists():
            print(f"Missing: {filepath}")
            continue
        df = pd.read_csv(filepath, sep=";")
        df.columns = df.columns.str.strip()
        dfs.append(df)
    return dfs

all_data = {d: load_difficulty(d) for d in DIFFICULTIES}

# ============= AGGREGATION HELPER =============
def aggregate(difficulty, metric):
    """Return (episodes, mean, std) across seeds."""
    dfs = all_data[difficulty]
    if not dfs:
        return None, None, None
    episodes = dfs[0]["Episode"].values
    values = np.array([df[metric].values for df in dfs])
    return episodes, values.mean(axis=0), values.std(axis=0)

# ============= PLOT 1: PER-DIFFICULTY (mean ± std) =============
print("Generating per-difficulty plots...")

for difficulty in DIFFICULTIES:
    if not all_data[difficulty]:
        continue

    color = COLORS[difficulty]
    fig, axes = plt.subplots(1, 2, figsize=(14, 5))

    # --- Survival Time ---
    ep, mean, std = aggregate(difficulty, "SurvivalTime")
    axes[0].plot(ep, mean, color=color, linewidth=2, label="Mean (3 seeds)")
    axes[0].fill_between(ep, mean - std, mean + std, color=color, alpha=0.25, label="± Std")
    axes[0].set_xlabel("Episode")
    axes[0].set_ylabel("Survival Time (s)")
    axes[0].set_title(f"{difficulty.capitalize()} — Survival Time (mean ± std)")
    axes[0].legend()
    axes[0].grid(True, alpha=0.3)
    axes[0].set_ylim(0, 32)

    # --- Total Reward ---
    ep, mean, std = aggregate(difficulty, "TotalReward")
    axes[1].plot(ep, mean, color=color, linewidth=2, label="Mean (3 seeds)")
    axes[1].fill_between(ep, mean - std, mean + std, color=color, alpha=0.25, label="± Std")
    axes[1].set_xlabel("Episode")
    axes[1].set_ylabel("Total Reward")
    axes[1].set_title(f"{difficulty.capitalize()} — Total Reward (mean ± std)")
    axes[1].legend()
    axes[1].grid(True, alpha=0.3)

    plt.tight_layout()
    plt.savefig(OUTPUT_DIR / f"per_{difficulty}.png", dpi=150)
    plt.close()
    print(f"  Saved per_{difficulty}.png")

# ============= PLOT 2: CROSS-CONDITION (mean ± std) =============
print("Generating cross-condition plot...")

fig, axes = plt.subplots(1, 2, figsize=(15, 6))

for difficulty in DIFFICULTIES:
    color = COLORS[difficulty]

    ep, mean, std = aggregate(difficulty, "SurvivalTime")
    if ep is not None:
        axes[0].plot(ep, mean, label=difficulty.capitalize(), color=color, linewidth=2)
        axes[0].fill_between(ep, mean - std, mean + std, color=color, alpha=0.2)

    ep, mean, std = aggregate(difficulty, "TotalReward")
    if ep is not None:
        axes[1].plot(ep, mean, label=difficulty.capitalize(), color=color, linewidth=2)
        axes[1].fill_between(ep, mean - std, mean + std, color=color, alpha=0.2)

axes[0].set_xlabel("Episode")
axes[0].set_ylabel("Survival Time (s)")
axes[0].set_title("Survival Time — All Conditions (mean ± std)")
axes[0].legend()
axes[0].grid(True, alpha=0.3)

axes[1].set_xlabel("Episode")
axes[1].set_ylabel("Total Reward")
axes[1].set_title("Total Reward — All Conditions (mean ± std)")
axes[1].legend()
axes[1].grid(True, alpha=0.3)

plt.tight_layout()
plt.savefig(OUTPUT_DIR / "cross_condition.png", dpi=150)
plt.close()
print("  Saved cross_condition.png")



print(f"\nAll plots saved to: {OUTPUT_DIR.resolve()}")