
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path
from scipy.stats import mannwhitneyu


CSV_DIR = Path(".")
OUTPUT_DIR = Path("plots_final")
OUTPUT_DIR.mkdir(exist_ok=True)
CONDITIONS = {"baseline": "Baseline", "ai": "AI Director"}
SEEDS = [42, 43, 44]
SMOOTH, WIN_WINDOW, FINAL_BLOCK, TIMEOUT = 25, 50, 100, 120.0


SCHEDULES = {
    "final": "Decaying ε (1.0 → 0.02)",
    "fixed_exploration": "Fixed ε",          
}

COLORS = {"final": "#2a78d6", "fixed_exploration": "#eb6834"}
INK, INK2, GRID = "#222222", "#666666", "#e6e6e6"

plt.rcParams.update({
    "font.size": 10, "axes.edgecolor": INK2, "axes.labelcolor": INK,
    "xtick.color": INK2, "ytick.color": INK2, "axes.spines.top": False,
    "axes.spines.right": False, "axes.grid": True, "grid.color": GRID,
    "grid.linewidth": 0.8, "legend.frameon": False,
})


def read(path):
    if not path.exists():
        print(f"Missing: {path}")
        return None
    df = pd.read_csv(path, sep=";")
    df.columns = df.columns.str.strip().str.lstrip("+")
    df["Won"] = (df["Died"] == 0).astype(int)
    df["Pickups"] = df["HealthPickups"] + df["AmmoPickups"]
    return df


data = {}
for c in CONDITIONS:
    for p in SCHEDULES:
        tr = [read(CSV_DIR / f"{p}_{c}_seed{s}.csv") for s in SEEDS]
        ev = [read(CSV_DIR / f"{p}_{c}_seed{s}_eval.csv") for s in SEEDS]
        data[(c, p)] = {"train": [t for t in tr if t is not None],
                        "eval": [e for e in ev if e is not None]}


def curve(dfs, metric, window):
    n = min(len(df) for df in dfs)
    rolled = np.array([df[metric].iloc[:n].rolling(window, min_periods=1).mean().values for df in dfs])
    return dfs[0]["Episode"].iloc[:n].values, rolled.mean(0), rolled.min(0), rolled.max(0)


def draw(ax, c, metric, window, scale=1.0, label_ends=True):
    for p, name in SCHEDULES.items():
        dfs = data[(c, p)]["train"]
        if not dfs:
            continue
        ep, m, lo, hi = curve(dfs, metric, window)
        ax.fill_between(ep, lo * scale, hi * scale, color=COLORS[p], alpha=0.18, linewidth=0)
        ax.plot(ep, m * scale, color=COLORS[p], linewidth=2, label=name)
        if label_ends:
            ax.text(ep[-1] + 8, m[-1] * scale, name.split(" (")[0], color=INK, fontsize=8, va="center")
    ax.set_xlim(0, 690 if label_ends else None)
    ax.set_xlabel("Episode")

#survival
fig, axes = plt.subplots(1, 2, figsize=(12, 4.8), sharey=True)
for ax, (c, cname) in zip(axes, CONDITIONS.items()):
    draw(ax, c, "SurvivalTime", SMOOTH)
    ax.axhline(TIMEOUT, color=INK2, linestyle=":", linewidth=1)
    ax.set_ylim(0, 130)
    ax.set_title(cname, loc="left", color=INK, fontweight="bold")
axes[0].set_ylabel(f"Survival time (s), {SMOOTH}-ep rolling mean")
axes[0].legend(loc="lower right", fontsize=8)
fig.suptitle("Effect of exploration schedule on learning", color=INK, fontsize=13, x=0.01, ha="left")
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "figE1_exploration_survival.png", dpi=200)
plt.close(fig)

#winrate
fig, axes = plt.subplots(1, 2, figsize=(12, 4.8), sharey=True)
for ax, (c, cname) in zip(axes, CONDITIONS.items()):
    draw(ax, c, "Won", WIN_WINDOW, scale=100)
    ax.set_ylim(0, 60)
    ax.set_title(cname, loc="left", color=INK, fontweight="bold")
axes[0].set_ylabel(f"Win rate (%), {WIN_WINDOW}-ep rolling")
axes[0].legend(loc="upper left", fontsize=8)
fig.suptitle("Win rate by exploration schedule", color=INK, fontsize=13, x=0.01, ha="left")
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "figE2_exploration_winrate.png", dpi=200)
plt.close(fig)

#behavior
METRICS = [("Kills", "Kills"), ("Pickups", "Pickups"),
           ("DamageTaken", "Damage taken"), ("DashesUsed", "Dashes")]
fig, axes = plt.subplots(2, 4, figsize=(16, 7.5), sharex=True)
for r, (c, cname) in enumerate(CONDITIONS.items()):
    for k, (metric, title) in enumerate(METRICS):
        ax = axes[r, k]
        draw(ax, c, metric, SMOOTH, label_ends=False)
        ax.set_xlim(0, 600)
        if r == 0:
            ax.set_title(title, loc="left", color=INK)
        if k == 0:
            ax.set_ylabel(f"{cname}\n{SMOOTH}-ep rolling mean", color=INK)
        if r == 0:
            ax.set_xlabel("")
handles, labels = axes[0, 0].get_legend_handles_labels()
fig.legend(handles, labels, loc="upper right", ncol=2, fontsize=9)
fig.suptitle("Behaviour by exploration schedule", color=INK, fontsize=13, x=0.01, ha="left")
fig.tight_layout(rect=(0, 0, 1, 0.95))
fig.savefig(OUTPUT_DIR / "figE3_exploration_behaviour.png", dpi=200)
plt.close(fig)

#greedy
fig, ax = plt.subplots(figsize=(9, 5))
boxes, pos, cols, ticks = [], [], [], []
for i, (c, cname) in enumerate(CONDITIONS.items()):
    for j, p in enumerate(SCHEDULES):
        ev = data[(c, p)]["eval"]
        if ev:
            boxes.append(pd.concat(ev)["SurvivalTime"].values)
            pos.append(i * 3 + j); cols.append(COLORS[p])
    ticks.append(i * 3 + 0.5)
bp = ax.boxplot(boxes, positions=pos, widths=0.7, patch_artist=True,
                medianprops=dict(color=INK, linewidth=1.5),
                flierprops=dict(marker="o", markersize=3, markerfacecolor=INK2, markeredgecolor="none"))
for patch, col in zip(bp["boxes"], cols):
    patch.set_facecolor(col); patch.set_edgecolor(INK2)
ax.set_xticks(ticks); ax.set_xticklabels(CONDITIONS.values())
ax.axhline(TIMEOUT, color=INK2, linestyle=":", linewidth=1)
ax.set_ylim(0, 130); ax.set_ylabel("Survival time (s)")
ax.grid(axis="x", visible=False)
from matplotlib.patches import Patch
ax.legend([Patch(facecolor=COLORS[p], edgecolor=INK2) for p in SCHEDULES], SCHEDULES.values(),
          loc="lower right", fontsize=8)
ax.set_title("Greedy evaluation by exploration schedule", loc="left", color=INK)
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "figE4_exploration_eval.png", dpi=200)
plt.close(fig)
      
