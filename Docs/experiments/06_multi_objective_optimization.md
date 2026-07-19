# Experiment 06 — Multi-Objective Balance Optimization

**Date:** 2026-07-15

---

# Goal

Extend the previous balance optimization framework by replacing the single-objective optimization with a unified multi-objective loss function.

Instead of optimizing only the win-rate difference, this experiment additionally considers combat duration to prevent extremely short and one-sided matches.

The objective is to search for HP and Damage parameters that produce both fair and sufficiently engaging combat.

---

# Motivation

Experiment 05 successfully demonstrated that Optuna can automatically discover balanced HP and Damage parameters.

However, optimizing only the win-rate introduced an important limitation.

Two parameter sets can produce an identical **50:50 win rate**, while resulting in completely different gameplay experiences.

For example,

- extremely short battles,
- one-shot combat,
- or excessively passive matches

may all achieve similar win-rate statistics.

Since game balance depends not only on fairness but also on combat quality, additional gameplay metrics should influence the optimization process.

This experiment introduces a multi-objective optimization framework that incorporates combat duration into the optimization objective.

---

# Multi-Objective Loss Function

The optimization objective is reformulated as a weighted combination of multiple gameplay metrics.

Instead of minimizing only

```
Win Rate Gap
```

the optimizer minimizes

```
Loss = 0.7 × WinRateGap + 0.3 × SurvivalPenalty + 0.5 × TimeoutRate
```

where

- **WinRateGap** measures class fairness.
- **SurvivalPenalty** discourages excessively short battles.
- **TimeoutRate** penalizes matches that fail to reach a decisive outcome.

---

## Win Rate Gap

The primary optimization target remains class fairness.

```
WinRateGap = | WarriorWinRate − MageWinRate |

```

A perfectly balanced match produces

```
WinRateGap = 0
```

---

## Survival Penalty (Hinge Loss)

Instead of forcing combat to last an exact duration,

a hinge loss is adopted.

The optimizer only penalizes matches that finish **too quickly**.

```
SurvivalPenalty = max ( 0, (TargetSteps − AverageSteps) / TargetSteps )

```

where

```
TargetSteps = 750
```

corresponding to approximately

```
15 seconds
```

of gameplay.

If the average episode duration exceeds the threshold,

```
Penalty = 0
```

Therefore,

longer battles are accepted,

while extremely short battles are discouraged.

This design avoids over-constraining combat duration while preserving optimization flexibility.

---

## Timeout Penalty

Episodes reaching the maximum step limit receive an additional penalty.

```
TimeoutPenalty = 0.5 × TimeoutRate
```

Although Experiment 04 already introduced the Magnetic Field System,

this term further discourages optimization toward passive gameplay.

---

# Optimization Variables

Optuna searches over four gameplay parameters.

| Parameter | Search Range |
|-----------|-------------:|
| Warrior HP | 150 ~ 300 |
| Mage HP | 100 ~ 250 |
| Warrior Damage Multiplier | 0.50 ~ 1.50 |
| Mage Damage Multiplier | 0.50 ~ 1.50 |

HP values are searched in increments of **5**,

while damage multipliers are searched in increments of **0.05**.

---

# Optimization Procedure

For every Optuna trial,

1. Sample HP and Damage parameters.
2. Send parameters to Unity through Environment Parameters.
3. Simulate **30 Self-Play episodes**.
4. Collect battle statistics.
5. Compute the multi-objective loss.
6. Return the loss to Optuna.

The parameter set with the minimum loss is selected as the optimal balance candidate.

---

# Training Result

A total of **50 Optuna trials** were executed.

Each trial evaluated

- 30 Self-Play episodes
- PPO inference using the trained Arena agent
- Multi-objective loss evaluation

The optimization successfully identified a parameter configuration that minimized the unified objective while maintaining balanced combat outcomes.

---

## Best Parameter Set

| Parameter | Value |
|-----------|------:|
| Warrior HP | **250** |
| Mage HP | **230** |
| Warrior Damage Multiplier | **0.80** |
| Mage Damage Multiplier | **1.05** |

---

## Interpretation

Compared with the previous experiment, the optimizer no longer searched only for equal win rates.

Instead, it simultaneously considered

- win-rate fairness,
- minimum combat duration,
- timeout suppression,

through a unified objective function.

Interestingly, the optimizer selected a configuration that slightly **reduced Warrior damage** while **slightly increasing Mage damage**, despite giving the Warrior a modest HP advantage.

Rather than simply maximizing one class's numerical advantage, the optimization discovered a parameter combination that compensated for differences in combat style while satisfying the multi-objective loss.

This demonstrates that automatic balance optimization can discover non-intuitive parameter combinations that may be difficult to obtain through manual tuning alone.

--- 

# Discussion

Compared with Experiment 05,

the optimization objective no longer searches solely for equal win rates.

Instead,

the optimizer simultaneously considers

- class fairness,
- combat duration,
- and decisive match completion.

The introduction of hinge loss provides a simple yet effective mechanism for preventing excessively short battles without forcing every episode to converge toward a fixed duration.

This represents the first step toward telemetry-driven game balancing,

where gameplay quality itself becomes part of the optimization objective.

---

# Limitation

Despite the improved objective,

the optimization variables remain limited to

- HP
- Damage Multiplier

Many gameplay characteristics,

including

- movement speed,
- attack range,
- cooldown,
- casting delay,
- crowd-control duration,

remain fixed.

Consequently,

the optimizer can only adjust numerical balance rather than gameplay dynamics.

---

# Conclusion

Experiment 06 extends the previous balance optimization framework into a unified multi-objective optimization problem.

By combining win-rate fairness, combat duration, and timeout penalties into a single objective,

Optuna can search for parameter sets that produce both balanced and engaging gameplay.

This experiment establishes the foundation for the next stage of automated balancing, where telemetry metrics and gameplay parameters will be jointly optimized.

---

# Next Experiment

Experiment 07 will further expand the optimization framework.

Instead of optimizing only HP and Damage,

the search space will include gameplay parameters such as

- Movement Speed
- Skill Cooldown
- Attack Range
- Casting Delay
- Crowd-Control Duration

Additionally,

Domain Randomization will be introduced so that agents learn under continuously changing combat conditions.

Combined with richer telemetry analysis,

this will enable automatic balancing based on actual gameplay behavior rather than simple numerical tuning.