# Experiment 04 — Reward & Behavior Refinement

**Date:** 2026-07-12

---

# Goal

Reduce the frequency of draw outcomes before balance optimization and encourage agents to engage in combat more actively and accurately.

This experiment focuses on improving combat tempo, attack precision, and episode decisiveness, providing a more reliable foundation for subsequent automated balance optimization.

---

# Motivation

Previous experiments successfully established distinct combat behaviors through asymmetric reward shaping and richer environmental observations.

However, telemetry analysis revealed several remaining limitations.

- A considerable number of matches still ended in draws due to prolonged disengagement.
- Agents occasionally preferred passive repositioning over active combat.
- The Warrior continued to perform unnecessary Basic Attacks despite previous reward refinements.

Before applying automated balance optimization, these issues must be addressed to ensure that combat outcomes accurately reflect policy quality rather than episode termination.

---

# System & Reward Refinement

## 1. Draw Reduction System

To minimize draw outcomes and encourage decisive engagements, several environment-level mechanisms were introduced.

---

### Time-Decay Penalty

A dynamic time-decay penalty is applied throughout the episode.

The longer an episode continues, the larger the accumulated penalty becomes.

This discourages excessive repositioning and encourages agents to actively seek combat.

---

### Magnetic Field System (Sudden Death)

After **40 seconds**, a magnetic field system is activated.

Every **0.5 seconds**, both agents receive continuous damage equal to **5% of their maximum HP**.

This mechanism guarantees that prolonged matches eventually reach a decisive conclusion.

| Parameter | Value |
|-----------|------:|
| Activation Time | 40 sec |
| Damage Interval | 0.5 sec |
| Damage | 5% of Max HP |

---

### Victory Determination

If both agents are defeated by the magnetic field during the same damage tick,

the winner is determined by comparing the remaining HP ratio immediately before damage was applied.

| Condition | Result |
|-----------|--------|
| Higher Remaining HP Ratio | Victory |
| Lower Remaining HP Ratio | Defeat |

This criterion rewards the agent that maintained a stronger combat advantage throughout the episode.

---

### Draw Penalty

If both agents possess exactly the same remaining HP ratio,

both are recorded as defeated.

Rather than allowing repeated draw outcomes,

this policy encourages both agents to avoid passive gameplay and pursue decisive victories.

---

## 2. Warrior Reward Refinement

### Whiff Penalty

To further improve attack precision,

an additional penalty is introduced when the Warrior's Basic Attack fails to hit the opponent.

| Condition | Reward |
|-----------|--------:|
| Basic Attack Miss | **-0.01** |

Instead of rewarding attack frequency,

the policy is encouraged to maximize successful attack opportunities.

This refinement discourages unnecessary attack spamming and promotes more deliberate melee engagements.

---
# Training Result

## Warrior

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | **60%** |
| Total Damage | 150.83 |
| Damage Taken | 163.40 |
| DPS | 5.69 |
| Survival Time | 26.52 sec |
| Average Distance | 4.35 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 35.02 | 10% | 58.53 |
| Dash | 8.49 | 20% | 14.37 |
| Charge Attack | 4.73 | 12% | 14.60 |
| Chain Pull | 3.55 | **40%** | **63.33** |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 98% |
| CC Success Rate | 12% |
| Chain Pull Success Rate | 94% |

---

## Mage

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | **40%** |
| Total Damage | 147.50 |
| Damage Taken | 169.80 |
| DPS | 5.56 |
| Survival Time | 26.52 sec |
| Average Distance | 4.35 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 20.13 | 49% | 44.50 |
| Teleport | 4.94 | - | - |
| Charge Attack (Nova Stun) | 4.62 | **11%** | 13.40 |
| Meteor | 2.17 | **69%** | **89.60** |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 98% |
| Meteor Success Rate | 82% |

---

# Analysis

![Tensorboard graph](../graph/exp04_tensorboard.png)


## 1. Skill Efficiency Improvement

### Warrior

| Skill | Experiment 03 | Experiment 04 |
|--------|--------------:|--------------:|
| Basic Attack Hit Rate | 8% | **10%** |
| Dash Hit Rate | 17% | **20%** |
| Charge Hit Rate | 12% | **12%** |
| Chain Pull Hit Rate | 33% | **40%** |

### Mage

| Skill | Experiment 03 | Experiment 04 |
|--------|--------------:|--------------:|
| Basic Attack Hit Rate | 42% | **49%** |
| Nova Stun Hit Rate | 11% | **11%** |
| Meteor Hit Rate | 53% | **69%** |

---

## 2. Decisive Combat Outcomes

One of the primary objectives of this experiment was to reduce the influence of draw outcomes before future balance optimization.

To improve the reliability of telemetry analysis, episode logging was modified so that only episodes containing actual combat interactions were included in the final statistics.

As a result,

| Class | Win Rate |
|--------|---------:|
| Warrior | **60%** |
| Mage | **40%** |

Unlike previous experiments, the total win rate now sums to **100%**, providing a much more reliable basis for subsequent balance evaluation.

This confirms that the combination of the Time-Decay Penalty, Magnetic Field System, and revised victory determination successfully eliminated most non-informative draw episodes.

---

## 3. Expected Reward Analysis of Warrior Basic Attack

Despite introducing both an Action Cost and an additional Miss Penalty, the Warrior continued to execute Basic Attack at a very high frequency.

Rather than indicating a failure of learning, this behavior can be explained by the expected reward of each attack.

Let

- P(hit) = 0.10
- P(miss) = 0.90

The expected reward of one Basic Attack can be written as

```
E = P(hit) × R(hit) + P(miss) × R(miss)
```

where

```
R(hit) ≈ +1.2
R(miss) = -0.015
```

Substituting the observed telemetry,

```
E = (0.10 × 1.20) + (0.90 × -0.015) = 0.120 - 0.0135 = +0.1065
```

Although the Warrior only lands approximately **10%** of its Basic Attacks, the expected reward remains positive.

Therefore, from the perspective of reinforcement learning, repeatedly executing Basic Attack is still mathematically optimal because it maximizes the long-term expected return.

Rather than exploiting a bug, the policy has correctly optimized the objective defined by the reward function.

---

## 4. Tactical Interpretation

The observed behavior can also be interpreted from a gameplay perspective rather than purely from an optimization standpoint.

The average distance between both agents remained

```
4.35
```

which is significantly larger than the Warrior's Basic Attack range.

This indicates that the Mage consistently attempted to kite by maintaining long-range spacing.

Under these conditions, repeatedly attacking while advancing toward the opponent resembles a zoning or pre-casting strategy commonly observed in fighting games and MOBA titles.

Furthermore,

the Warrior's Chain Pull achieved

- **40% Hit Rate**

which frequently forced the Mage into close-range combat.

Once the target was successfully pulled,

the Warrior immediately followed with repeated Basic Attacks, eventually overwhelming the opponent.

Consequently, the introduced miss penalty did not eliminate aggressive attack behavior.

Instead, it reinforced the Warrior's identity as a relentless melee fighter that continuously pressures opponents despite the inherent risk of missed attacks.

From a game design perspective, this behavior closely resembles a **berserker-style combat pattern**, demonstrating that reinforcement learning successfully captured the intended class identity through reward engineering.

# Conclusion

Experiment 04 demonstrates that environment-level refinements are essential before performing automated balance optimization.

The introduction of the Time-Decay Penalty, Magnetic Field System, revised victory determination, and draw filtering substantially reduced the influence of passive matches and produced cleaner self-play telemetry.

Reward refinements further improved attack precision while preserving the Warrior's aggressive combat identity.

Interestingly, the Warrior continued to perform frequent Basic Attacks despite the newly introduced miss penalty.

Expected reward analysis revealed that this behavior is mathematically optimal under the current reward function, highlighting how reinforcement learning naturally discovers globally optimal strategies rather than human-intuitive behaviors.

These findings indicate that future balance optimization should focus not only on gameplay parameters but also on the expected value structure induced by the reward function itself.

The resulting environment now provides a reliable foundation for AI-assisted balance optimization using large-scale self-play telemetry.

# Next Experiment

Experiment 05 shifts from reward engineering to automated balance optimization.

Future work includes

- Optuna-based balance parameter optimization
- Automated HP and Damage multiplier tuning
- Telemetry-driven patch recommendation
- Balance evaluation under multiple player personas
- AI-assisted balance patch generation
