"""
Generates the performance charts embedded in the optimization PR.

Data sources (committed in this repo):
  - Native/Benchmarks/BASELINE.md                      (V1.0 / V2.0 / V2.1 dispatch numbers)
  - Native/Benchmarks/.../PromiseSourcePoolingBenchmarks-report-github.md (pooled vs new)
  - Native/Messages/Tests/AsyncDispatchAllocationProbe.cs (this PR: 360 B -> ~0 B warm)

Re-run:  python docs/pr-assets/gen_charts.py
"""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from pathlib import Path

OUT = Path(__file__).parent
plt.rcParams.update({
    "font.size": 11,
    "axes.titlesize": 14,
    "axes.titleweight": "bold",
    "figure.dpi": 140,
    "axes.spines.top": False,
    "axes.spines.right": False,
    "axes.grid": True,
    "grid.alpha": 0.25,
    "grid.linestyle": "--",
})

# Brand-ish palette
C_V1, C_V2, C_V21 = "#9aa7b1", "#5b8def", "#28a745"
C_BEFORE, C_AFTER = "#e3625d", "#28a745"


def label_bars(ax, bars, fmt="{:.0f}", suffix=""):
    for b in bars:
        h = b.get_height()
        ax.annotate(f"{fmt.format(h)}{suffix}", (b.get_x() + b.get_width() / 2, h),
                    ha="center", va="bottom", fontsize=9, xytext=(0, 2),
                    textcoords="offset points")


# ---------------------------------------------------------------- Chart 1
# Per-dispatch allocation across versions.
def chart_alloc_per_dispatch():
    labels = ["TryHandle\n(sync)", "Handle\n(sync)", "TryHandleAsync\n(sync)",
              "HandleAsync\n(sync)", "Multicast\n5 handlers"]
    v1 = [312, 560, 312, 560, 344]
    v2 = [272, 480, 272, 480, 272]
    v21 = [0, 0, 0, 0, 272]

    x = np.arange(len(labels))
    w = 0.27
    fig, ax = plt.subplots(figsize=(10, 5.2))
    b1 = ax.bar(x - w, v1, w, label="v1.0 (baseline)", color=C_V1)
    b2 = ax.bar(x, v2, w, label="v2.0", color=C_V2)
    b3 = ax.bar(x + w, v21, w, label="v2.1 (this PR series)", color=C_V21)
    label_bars(ax, b1, suffix=" B"); label_bars(ax, b2, suffix=" B"); label_bars(ax, b3, suffix=" B")
    ax.set_ylabel("Allocated bytes per dispatch")
    ax.set_title("Per-dispatch heap allocation — lower is better")
    ax.set_xticks(x); ax.set_xticklabels(labels)
    ax.legend(frameon=False, ncol=3, loc="upper center", bbox_to_anchor=(0.5, -0.12))
    ax.set_ylim(0, 640)
    fig.text(0.5, 0.005, "Sync handler paths reach 0 B (zero-allocation fast path). "
             "Source: Native/Benchmarks/BASELINE.md", ha="center", fontsize=8, color="#666")
    fig.tight_layout(rect=(0, 0.06, 1, 1))
    fig.savefig(OUT / "alloc_per_dispatch.png", bbox_inches="tight")
    plt.close(fig)


# ---------------------------------------------------------------- Chart 2
# Dispatch latency across versions.
def chart_latency():
    labels = ["TryHandle\n(sync)", "Handle\n(sync)", "TryHandleAsync\n(sync)",
              "HandleAsync\n(sync)", "Multicast\n5 handlers"]
    v1 = [52, 65, 55, 70, 92]
    v21 = [38, 40, 39, 45, 62]
    impr = [(1 - a / b) * 100 for a, b in zip(v21, v1)]

    x = np.arange(len(labels))
    w = 0.38
    fig, ax = plt.subplots(figsize=(10, 5.2))
    b1 = ax.bar(x - w / 2, v1, w, label="v1.0 (baseline)", color=C_V1)
    b2 = ax.bar(x + w / 2, v21, w, label="v2.1", color=C_V21)
    label_bars(ax, b1, suffix=" ns"); label_bars(ax, b2, suffix=" ns")
    for xi, im, top in zip(x, impr, v1):
        ax.annotate(f"-{im:.0f}%", (xi, top + 6), ha="center", color=C_AFTER,
                    fontweight="bold", fontsize=9)
    ax.set_ylabel("Mean latency (ns)")
    ax.set_title("Dispatch latency — lower is better")
    ax.set_xticks(x); ax.set_xticklabels(labels)
    ax.legend(frameon=False, ncol=2, loc="upper center", bbox_to_anchor=(0.5, -0.12))
    ax.set_ylim(0, 110)
    fig.text(0.5, 0.005, "Source: Native/Benchmarks/BASELINE.md (.NET 9, X64 RyuJIT AVX2)",
             ha="center", fontsize=8, color="#666")
    fig.tight_layout(rect=(0, 0.06, 1, 1))
    fig.savefig(OUT / "latency_per_dispatch.png", bbox_inches="tight")
    plt.close(fig)


# ---------------------------------------------------------------- Chart 3
# Promise-source pooling: allocation per 100K ops, new vs pooled.
def chart_pooling():
    labels = ["TryHandle\nSource", "TryHandleAsync\nSource", "Handle\nSource",
              "HandleAsync\nSource"]
    new_mb = [16.0, 15.2, 15.2, 15.2]   # MB per 100K ops (from *-report-github.md)
    pooled_mb = [0.000003, 0.000003, 0.000003, 0.000003]

    x = np.arange(len(labels))
    w = 0.38
    fig, ax = plt.subplots(figsize=(10, 5.2))
    b1 = ax.bar(x - w / 2, new_mb, w, label="non-pooled (new each call)", color=C_BEFORE)
    b2 = ax.bar(x + w / 2, pooled_mb, w, label="pooled", color=C_AFTER)
    for b, v in zip(b1, new_mb):
        ax.annotate(f"{v:.1f} MB", (b.get_x() + b.get_width() / 2, v), ha="center",
                    va="bottom", fontsize=9, xytext=(0, 2), textcoords="offset points")
    for b in b2:
        ax.annotate("~0 B", (b.get_x() + b.get_width() / 2, 0.4), ha="center",
                    va="bottom", fontsize=9, color=C_AFTER, fontweight="bold")
    ax.set_ylabel("Heap allocated per 100,000 dispatches (MB)")
    ax.set_title("Promise-source pooling — garbage produced per 100K ops")
    ax.set_xticks(x); ax.set_xticklabels(labels)
    ax.legend(frameon=False, ncol=2, loc="upper center", bbox_to_anchor=(0.5, -0.12))
    ax.set_ylim(0, 18)
    fig.text(0.5, 0.005, "Pooling removes ~152 B/op of Gen0 garbage. "
             "Source: PromiseSourcePoolingBenchmarks-report-github.md", ha="center",
             fontsize=8, color="#666")
    fig.tight_layout(rect=(0, 0.06, 1, 1))
    fig.savefig(OUT / "pooling_alloc.png", bbox_inches="tight")
    plt.close(fig)


# ---------------------------------------------------------------- Chart 4
# This PR's specific contribution: async-fallback source reclaim + steady-state GC.
def chart_pr_contribution():
    fig, (axl, axr) = plt.subplots(1, 2, figsize=(11, 4.8))

    # Left: per-dispatch source footprint on the async-fallback path.
    bars = axl.bar(["before\n(GC'd each\ndispatch)", "after\n(pooled,\nwarm)"],
                   [360, 0.5], color=[C_BEFORE, C_AFTER], width=0.55)
    axl.annotate("360 B", (0, 360), ha="center", va="bottom", fontsize=11,
                 fontweight="bold", xytext=(0, 2), textcoords="offset points")
    axl.annotate("~0 B", (1, 0.5), ha="center", va="bottom", fontsize=11,
                 fontweight="bold", color=C_AFTER, xytext=(0, 2), textcoords="offset points")
    axl.set_ylabel("Bytes per async-fallback dispatch")
    axl.set_title("This PR: SequentialHandlingPromiseSource")
    axl.set_ylim(0, 420)

    # Right: steady-state garbage at sustained dispatch rates (async-fallback path).
    rates = [60, 240, 1000]            # dispatches/sec (e.g., 60 Hz game loop and above)
    before_kb = [r * 360 / 1024 for r in rates]
    after_kb = [0 for _ in rates]
    x = np.arange(len(rates)); w = 0.38
    bb = axr.bar(x - w / 2, before_kb, w, label="before", color=C_BEFORE)
    axr.bar(x + w / 2, after_kb, w, label="after (pooled)", color=C_AFTER)
    label_bars(axr, bb, fmt="{:.1f}", suffix=" KB/s")
    axr.set_xticks(x); axr.set_xticklabels([f"{r}/s" for r in rates])
    axr.set_ylabel("Steady-state Gen0 garbage (KB/s)")
    axr.set_title("Sustained async-fallback dispatch")
    axr.legend(frameon=False, loc="upper left")
    axr.set_ylim(0, max(before_kb) * 1.25)

    fig.suptitle("Async-fallback dispatch: pooling reclaims the 360 B source",
                 fontsize=14, fontweight="bold")
    fig.text(0.5, 0.005, "Footprint measured by AsyncDispatchAllocationProbe; warm rent+return "
             "asserted <=16 B/op. 'After' steady-state is 0 once the pool is warm.",
             ha="center", fontsize=8, color="#666")
    fig.tight_layout(rect=(0, 0.05, 1, 0.95))
    fig.savefig(OUT / "pr_contribution.png", bbox_inches="tight")
    plt.close(fig)


if __name__ == "__main__":
    chart_alloc_per_dispatch()
    chart_latency()
    chart_pooling()
    chart_pr_contribution()
    print("charts written to", OUT)
