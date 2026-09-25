"""
Plots + statistics for the DDA survival experiment.

Expects (in CSV_DIR), for each difficulty d and seed s:
    {PREFIX}_{d}_seed{s}.csv        training log
    {PREFIX}_{d}_seed{s}_eval.csv   greedy evaluation log
    {PREFIX}_{d}_random.csv         random-policy baseline

Outputs (in OUTPUT_DIR):
    fig1_learning_curves.png   per-condition survival curves (small multiples)
    fig2_cross_condition.png   all conditions on one axis
    fig3_win_rate.png          rolling win rate over training
    fig4_eval_vs_random.png    greedy evaluation vs random baseline (box plots)
    summary_stats.csv          table of numbers for the paper
    stats_report.txt           significance tests
"""
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from pathlib import Path
from scipy.stats import mannwhitneyu, kruskal

# ============= CONFIGURATION =============
CSV_DIR = Path(".")
PREFIX = "final"
DIFFICULTIES = ["easy", "baseline", "hard", "ai"]
LABELS = {"easy": "Easy", "baseline": "Baseline", "hard": "Hard", "ai": "AI Director"}
SEEDS = [42, 43, 44]
TIMEOUT = 120.0
SMOOTH = 25          # rolling window (episodes) for learning curves
WIN_WINDOW = 50      # rolling window for win rate
FINAL_BLOCK = 100    # "final performance" = last N training episodes
OUTPUT_DIR = Path("plots_final")
OUTPUT_DIR.mkdir(exist_ok=True)

# Validated categorical palette, fixed order (colorblind-checked)
COLORS = {"easy": "#2a78d6", "baseline": "#eb6834", "hard": "#1baf7a", "ai": "#eda100"}
INK, INK2, GRID = "#222222", "#666666", "#e6e6e6"

plt.rcParams.update({
    "font.size": 10, "axes.edgecolor": INK2, "axes.labelcolor": INK,
    "xtick.color": INK2, "ytick.color": INK2, "axes.spines.top": False,
    "axes.spines.right": False, "axes.grid": True, "grid.color": GRID,
    "grid.linewidth": 0.8, "legend.frameon": False,
})


# ============= LOAD =============
def read(path):
    if not path.exists():
        print(f"Missing: {path}")
        return None
    df = pd.read_csv(path, sep=";")
    df.columns = df.columns.str.strip().str.lstrip("+")
    df["Won"] = (df["Died"] == 0).astype(int)
    return df


data = {}
for d in DIFFICULTIES:
    train = [read(CSV_DIR / f"{PREFIX}_{d}_seed{s}.csv") for s in SEEDS]
    evals = [read(CSV_DIR / f"{PREFIX}_{d}_seed{s}_eval.csv") for s in SEEDS]
    data[d] = {
        "train": [t for t in train if t is not None],
        "eval": [e for e in evals if e is not None],
        "random": read(CSV_DIR / f"{PREFIX}_{d}_random.csv"),
    }


def curve(dfs, metric, window):
    """Rolling mean per seed -> mean across seeds, plus min/max seed band."""
    n = min(len(df) for df in dfs)
    rolled = np.array([df[metric].iloc[:n].rolling(window, min_periods=1).mean().values
                       for df in dfs])
    ep = dfs[0]["Episode"].iloc[:n].values
    return ep, rolled.mean(0), rolled.min(0), rolled.max(0)


# ============= FIG 1: PER-CONDITION LEARNING CURVES =============
fig, axes = plt.subplots(2, 2, figsize=(11, 7.5), sharex=True, sharey=True)
for ax, d in zip(axes.flat, DIFFICULTIES):
    c = COLORS[d]
    if data[d]["train"]:
        ep, m, lo, hi = curve(data[d]["train"], "SurvivalTime", SMOOTH)
        ax.fill_between(ep, lo, hi, color=c, alpha=0.2, linewidth=0, label="Seed range")
        ax.plot(ep, m, color=c, linewidth=2, label=f"Q-learning (mean of {len(data[d]['train'])} seeds)")
    if data[d]["random"] is not None:
        rm = data[d]["random"]["SurvivalTime"].median()
        ax.axhline(rm, color=INK2, linestyle="--", linewidth=1.2, label=f"Random policy median ({rm:.0f}s)")
    ax.axhline(TIMEOUT, color=INK2, linestyle=":", linewidth=1)
    ax.text(5, TIMEOUT + 2, "timeout (win)", color=INK2, fontsize=8)
    ax.set_title(LABELS[d], color=INK, loc="left", fontweight="bold")
    ax.set_ylim(0, 130)
    ax.legend(loc="lower right", fontsize=8)
for ax in axes[1]:
    ax.set_xlabel("Episode")
for ax in axes[:, 0]:
    ax.set_ylabel(f"Survival time (s), {SMOOTH}-ep rolling mean")
fig.suptitle("Learning curves by difficulty condition", color=INK, fontsize=13, x=0.01, ha="left")
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "fig1_learning_curves.png", dpi=200)
plt.close(fig)


# ============= FIG 2: CROSS-CONDITION =============
fig, ax = plt.subplots(figsize=(10, 5.5))
for d in DIFFICULTIES:
    if not data[d]["train"]:
        continue
    ep, m, lo, hi = curve(data[d]["train"], "SurvivalTime", SMOOTH)
    ax.fill_between(ep, lo, hi, color=COLORS[d], alpha=0.15, linewidth=0)
    ax.plot(ep, m, color=COLORS[d], linewidth=2, label=LABELS[d])
    ax.text(ep[-1] + 8, m[-1], LABELS[d], color=INK, fontsize=9, va="center")  # direct label
ax.axhline(TIMEOUT, color=INK2, linestyle=":", linewidth=1)
ax.set_xlim(0, ep[-1] * 1.13)
ax.set_ylim(0, 130)
ax.set_xlabel("Episode")
ax.set_ylabel(f"Survival time (s), {SMOOTH}-ep rolling mean")
ax.set_title("Survival time across conditions (mean of seeds, band = seed range)",
             loc="left", color=INK)
ax.legend(loc="lower right")
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "fig2_cross_condition.png", dpi=200)
plt.close(fig)


# ============= FIG 3: ROLLING WIN RATE =============
fig, ax = plt.subplots(figsize=(10, 5.5))
for d in DIFFICULTIES:
    if not data[d]["train"]:
        continue
    ep, m, lo, hi = curve(data[d]["train"], "Won", WIN_WINDOW)
    ax.fill_between(ep, lo * 100, hi * 100, color=COLORS[d], alpha=0.15, linewidth=0)
    ax.plot(ep, m * 100, color=COLORS[d], linewidth=2, label=LABELS[d])
    ax.text(ep[-1] + 8, m[-1] * 100, LABELS[d], color=INK, fontsize=9, va="center")
ax.set_xlim(0, ep[-1] * 1.13)
ax.set_ylim(0, 105)
ax.set_xlabel("Episode")
ax.set_ylabel(f"Win rate (%), {WIN_WINDOW}-ep rolling")
ax.set_title("Win rate (survived to timeout) during training", loc="left", color=INK)
ax.legend(loc="upper left")
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "fig3_win_rate.png", dpi=200)
plt.close(fig)


# ============= FIG 4: GREEDY EVAL vs RANDOM =============
fig, ax = plt.subplots(figsize=(10, 5.5))
positions, boxes, colors, ticks, ticklabels = [], [], [], [], []
for i, d in enumerate(DIFFICULTIES):
    base = i * 3
    r = data[d]["random"]
    e = pd.concat(data[d]["eval"]) if data[d]["eval"] else None
    if r is not None:
        positions.append(base); boxes.append(r["SurvivalTime"].values); colors.append("#cfcfcf")
    if e is not None:
        positions.append(base + 1); boxes.append(e["SurvivalTime"].values); colors.append(COLORS[d])
    ticks.append(base + 0.5); ticklabels.append(LABELS[d])
bp = ax.boxplot(boxes, positions=positions, widths=0.7, patch_artist=True, showfliers=True,
                medianprops=dict(color=INK, linewidth=1.5),
                flierprops=dict(marker="o", markersize=3, markerfacecolor=INK2, markeredgecolor="none"))
for patch, c in zip(bp["boxes"], colors):
    patch.set_facecolor(c); patch.set_edgecolor(INK2)
ax.set_xticks(ticks); ax.set_xticklabels(ticklabels)
ax.axhline(TIMEOUT, color=INK2, linestyle=":", linewidth=1)
ax.set_ylim(0, 130)
ax.set_ylabel("Survival time (s)")
ax.set_title("Greedy evaluation (colour) vs random policy (grey)", loc="left", color=INK)
fig.tight_layout()
fig.savefig(OUTPUT_DIR / "fig4_eval_vs_random.png", dpi=200)
plt.close(fig)


# ============= STATS =============
def iqr(x):
    return f"{np.percentile(x, 25):.1f}-{np.percentile(x, 75):.1f}"


rows, report, evals_all = [], [], {}
for d in DIFFICULTIES:
    r = data[d]["random"]
    tr = data[d]["train"]
    ev = data[d]["eval"]
    if r is None or not tr or not ev:
        continue
    last = pd.concat([t.tail(FINAL_BLOCK) for t in tr])
    E = pd.concat(ev)
    evals_all[d] = E["SurvivalTime"].values
    p_rand = mannwhitneyu(E["SurvivalTime"], r["SurvivalTime"], alternative="two-sided").pvalue
    seed_eval_medians = [e["SurvivalTime"].median() for e in ev]
    rows.append({
        "Condition": LABELS[d],
        "Random median (s)": round(r["SurvivalTime"].median(), 1),
        "Random win %": round(r["Won"].mean() * 100, 1),
        f"Train last{FINAL_BLOCK} median (s)": round(last["SurvivalTime"].median(), 1),
        f"Train last{FINAL_BLOCK} IQR": iqr(last["SurvivalTime"]),
        f"Train last{FINAL_BLOCK} win %": round(last["Won"].mean() * 100, 1),
        "Eval median (s)": round(E["SurvivalTime"].median(), 1),
        "Eval IQR": iqr(E["SurvivalTime"]),
        "Eval win %": round(E["Won"].mean() * 100, 1),
        "Eval seed medians": " / ".join(f"{m:.0f}" for m in seed_eval_medians),
        "Eval kills (mean)": round(E["Kills"].mean(), 1),
        "Eval damage taken (mean)": round(E["DamageTaken"].mean(), 1),
        "Eval dashes (mean)": round(E["DashesUsed"].mean(), 1) if "DashesUsed" in E else np.nan,
        "p (eval vs random)": f"{p_rand:.2e}",
        "n eval episodes": len(E),
    })

summary = pd.DataFrame(rows)
summary.to_csv(OUTPUT_DIR / "summary_stats.csv", index=False)

if len(evals_all) > 1:
    h = kruskal(*evals_all.values())
    report.append(f"Kruskal-Wallis across conditions (greedy eval survival): H={h.statistic:.2f}, p={h.pvalue:.2e}")
    if "baseline" in evals_all:
        k = len(evals_all) - 1
        report.append(f"\nPairwise vs Baseline (Mann-Whitney U, Bonferroni x{k}):")
        for d, x in evals_all.items():
            if d == "baseline":
                continue
            p = mannwhitneyu(x, evals_all["baseline"], alternative="two-sided").pvalue
            report.append(f"  {LABELS[d]:<12} p={p:.2e}  adj p={min(1, p * k):.2e}")

text = summary.to_string(index=False) + "\n\n" + "\n".join(report)
(OUTPUT_DIR / "stats_report.txt").write_text(text)
print(text)
print(f"\nAll outputs saved to: {OUTPUT_DIR.resolve()}")
