# Experiment 05 — Automated Balance Optimization using Optuna

**Date:** 2026-07-14

---

# Goal

Previous experiments focused on improving combat behavior through reward engineering and observation refinement.

This experiment shifts the focus from **policy optimization** to **automatic gameplay balance optimization**.

Rather than manually adjusting character statistics, an automated optimization pipeline is introduced to discover HP and damage configurations that minimize the win-rate gap between the Warrior and Mage while preserving decisive combat outcomes.

---

# Motivation

Although previous experiments successfully established distinct combat identities, gameplay balancing still relied on manual parameter tuning.

Manual balancing presents several limitations.

- Large parameter spaces are difficult to explore exhaustively.
- HP and damage parameters interact nonlinearly.
- Manual balancing is subjective and time-consuming.
- Small parameter changes may produce unexpected gameplay outcomes.

To overcome these limitations, **Optuna** is integrated into the evaluation pipeline to automatically search for balanced gameplay parameters using trained reinforcement learning agents.

---

# Optimization Pipeline

For each trial, Optuna samples a new combination of gameplay parameters, launches multiple self-play simulations, evaluates the combat results, computes a balance loss, and updates its search strategy.

---

# Search Space

The optimizer searches the following parameter ranges.

| Parameter | Search Range |
|-----------|-------------:|
| Warrior HP | 150 ~ 300 |
| Mage HP | 100 ~ 250 |
| Warrior Damage Multiplier | 0.50 ~ 1.50 |
| Mage Damage Multiplier | 0.50 ~ 1.50 |

To avoid insignificant parameter differences, HP values are searched in increments of **5**, while damage multipliers are searched in increments of **0.05**.

---

# Objective Function

The optimization objective minimizes the difference between the Warrior and Mage win rates while penalizing timeout episodes.

```
Loss = |Warrior Win Rate − Mage Win Rate| + 0.5 × Timeout Rate
```

where

- Smaller loss indicates better balance.
- Timeout episodes receive an additional penalty.
- A perfectly balanced configuration corresponds to **Loss = 0.0**.

---

# Optimization Configuration

| Item | Value |
|------|------:|
| Optimization Algorithm | Optuna (TPE) |
| PPO Model | ArenaPPO_Ep1400.pt |
| Episodes per Trial | 30 |
| Number of Trials | 50 |
| Unity Time Scale | 10× |
| Observation Dimension | 30 |

Each candidate parameter set is evaluated through **30 independent self-play episodes**.

The trained PPO agents are used only for inference, ensuring consistent policy evaluation throughout the optimization process.

---

# Optimization Result

Among all evaluated parameter combinations, Optuna discovered the following configuration.

| Parameter | Best Value |
|-----------|-----------:|
| Warrior HP | **240** |
| Mage HP | **100** |
| Warrior Damage Multiplier | **0.80** |
| Mage Damage Multiplier | **0.95** |

Resulting combat statistics:

| Metric | Value |
|--------|------:|
| Warrior Wins | **15** |
| Mage Wins | **15** |
| Timeout | **0** |
| Loss | **0.000** |

This configuration achieved identical win rates for both classes while completely eliminating timeout episodes.

---

# Trial Analysis

During optimization, Optuna evaluated a wide range of gameplay configurations.

Several representative trials are summarized below.

| Trial | Result |
|-------|---------|
| High Warrior Damage | Warrior overwhelmingly dominant |
| High Mage Damage | Mage overwhelmingly dominant |
| Similar HP / Damage | Moderate balance |
| **Trial 23** | **Perfect balance (Loss = 0.000)** |

Trial 23 produced the lowest loss among all evaluated candidates.

| Parameter | Value |
|-----------|------:|
| Warrior HP | **240** |
| Mage HP | **100** |
| Warrior Damage | **0.80** |
| Mage Damage | **0.95** |

Win Rate

```
Warrior : 50%

Mage : 50%
```

This demonstrates that balanced gameplay does not necessarily require symmetric character statistics.

Instead, balance emerges through appropriate compensation between survivability and offensive capability.

---

# Analysis

## 1. Automatic Balance Discovery

Optuna successfully identified gameplay parameters that produced identical win rates without requiring manual tuning.

Rather than relying on designer intuition, the optimizer systematically explored the parameter space and converged toward the minimum-loss configuration.

---

## 2. Asymmetric Compensation

Interestingly, the optimal balance was achieved using highly asymmetric statistics.

The Warrior received

- Higher HP
- Lower Damage

while the Mage received

- Lower HP
- Higher Damage

This indicates that gameplay balance is achieved through compensation between survivability and damage output rather than identical numerical values.

---

## 3. Reliable Evaluation after Draw Suppression

Experiment 04 significantly reduced draw outcomes through the introduction of the Time-Decay Penalty and Magnetic Field System.

As a result,

```
Timeout = 0
```

for the optimal configuration.

Consequently, Optuna evaluated only meaningful combat outcomes instead of episode terminations caused by inactivity.

This substantially improved the reliability of automated balance optimization.

---

## 4. Reinforcement Learning as an Automated Playtester

Instead of using handcrafted evaluation heuristics, trained PPO agents functioned as automated playtesters.

For every candidate parameter configuration, the agents repeatedly competed against each other, generating objective telemetry for balance evaluation.

This demonstrates that reinforcement learning agents can be integrated into an AI-assisted game balancing pipeline capable of replacing a significant portion of manual playtesting.

---

# Conclusion

Experiment 05 demonstrates that reinforcement learning agents can be utilized not only for gameplay optimization but also for **automated balance evaluation**.

By integrating PPO self-play, telemetry analysis, Unity Environment Parameters, and Optuna, gameplay parameters were optimized without manual intervention.

The discovered parameter configuration achieved identical win rates while completely eliminating timeout episodes, providing objective evidence that AI can effectively support gameplay balancing.

This experiment extends reinforcement learning beyond policy learning and presents a practical framework for AI-assisted balance optimization in game development.

---

# Future Work

Future work includes

- Multi-objective Bayesian optimization
- Elo-based balance evaluation
- Multi-persona balance optimization
- Automatic patch recommendation
- AI-generated balance patch notes
- Large-scale live-service telemetry optimization