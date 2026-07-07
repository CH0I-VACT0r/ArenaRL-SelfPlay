# Experiment 02 - Asymmetric Reward Shaping
**Date:** 2026-07-07

---

## Goal

Improve combat behavior through **reward engineering** rather than simple skill balancing.

Experiment 01 demonstrated that increasing attack range, damage, and angle alone failed to significantly change agent behavior. This experiment introduces **class-specific asymmetric reward functions** together with PPO optimization improvements to encourage each class to learn its intended combat role.

---

# Motivation

The first experiment revealed several limitations.

- Warrior frequently spammed melee attacks with extremely low hit rates.
- Nearly half of the matches ended in timeouts.
- Both classes shared an identical reward function despite having fundamentally different combat styles.
- Increasing raw skill statistics did not substantially improve policy quality.

Instead of continuing to adjust skill parameters, this experiment redesigns the optimization objective through reward shaping.

---

# Asymmetric Reward Design

Unlike Experiment 01, both agents no longer share the same reward function.

Each class receives additional rewards based on behaviors consistent with its intended gameplay identity.

---

## Shared Reward

The reward for successful attacks was increased by **5×**.

| Reward | Experiment 01 | Experiment 02 |
|---------|--------------:|--------------:|
| Damage Reward | 0.01 × Damage | **0.05 × Damage** |

The stronger reward signal encourages the policy to prioritize successful attacks over passive movement.

---

## Warrior Reward

The Warrior is encouraged to become an aggressive melee fighter.

### Close-Range Pressure

| Condition | Reward |
|-----------|-------:|
| Distance ≤ 2.5 | +0.0005 every step |
| Distance ≥ 5.0 | -0.0002 every step |

Remaining close to the opponent continuously provides positive reinforcement, while disengaging is penalized.

---

### Combo Reward

To encourage continuous pressure, an additional reward is granted when another successful attack lands within **1.5 seconds** after the previous hit.

| Condition | Reward |
|-----------|-------:|
| Consecutive Hit (within 1.5 sec) | +0.2 |

This discourages hit-and-run behavior and promotes sustained melee engagement.

---

## Mage Reward

The Mage receives the opposite objective.

### Distance Maintenance

| Condition | Reward |
|-----------|-------:|
| Distance ≥ 4.0 | +0.0005 every step |
| Distance ≤ 2.0 | -0.0002 every step |

The Mage therefore learns to maximize spacing while searching for opportunities to cast high-damage ranged skills.

---

# PPO Optimization

The redesigned reward function substantially increased reward variance.

To maintain stable PPO optimization, several training hyperparameters were modified.

---

## PPO Objective

The policy is optimized using PPO's clipped surrogate objective.

Policy Ratio

r = π_new(a|s) / π_old(a|s)

Objective

L = E[min(r × A,
          clip(r, 1-ε, 1+ε) × A)]

where

- r : probability ratio between the new and old policy
- A : estimated advantage
- ε : clipping threshold

The clipping operation prevents excessively large policy updates and improves optimization stability during policy improvement.

---

## Increased Reward Variance

Unlike Experiment 01, the reward function now contains

- continuous distance rewards
- combo rewards
- larger attack rewards
- class-specific objectives

These changes significantly increase the variance of the return.

Higher reward variance naturally produces larger policy gradients, making optimization more unstable.

---

## Larger Batch Size

| Parameter | Before | After |
|-----------|--------|-------|
| Batch Size | 1024 | **2048** |

A larger batch estimates the policy gradient using more trajectories before each optimization step.

Gradient Estimate

g = (1 / N) Σ ∇L_i

Using more samples reduces gradient variance and produces smoother policy updates.

---

## Gradient Clipping

Gradient clipping was enabled to prevent unstable parameter updates.

| Parameter | Value |
|-----------|------:|
| max_norm | **0.5** |

Even with a larger batch size, high reward variance occasionally generated excessively large gradients.

Before each optimization step, gradient clipping is applied.

If

||g|| > c

then

g ← g × (c / ||g||)

where

c = 0.5

This limits the L2 norm of the gradient while preserving its direction.

Unlike PPO's policy clipping, gradient clipping directly stabilizes the optimizer by preventing excessively large parameter updates.

---

## Why Both PPO Clipping and Gradient Clipping?

PPO clipping constrains the policy ratio during optimization, preventing the policy from changing too aggressively.

However, it does not directly limit the magnitude of gradients computed during backpropagation.

After introducing asymmetric rewards and increasing attack rewards by five times, the optimization process produced significantly larger gradients.

Therefore, gradient clipping was additionally introduced to stabilize parameter updates while retaining PPO's policy clipping mechanism.

The two techniques complement each other:

- PPO Clipping → stabilizes policy updates
- Gradient Clipping → stabilizes optimizer updates

---

## Hardware Consideration

Training was performed on a GPU with **6 GB VRAM**.

Increasing the batch size beyond **2048** exceeded available GPU memory.

Therefore,

- Batch Size = 2048
- Gradient Clipping = 0.5

were selected as the largest stable configuration that balanced optimization stability and hardware constraints.

---

# Training Result

## Warrior

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | **32%** |
| Total Damage | 129.56 |
| Damage Taken | 151.62 |
| DPS | 4.79 |
| Survival Time | 27.06 sec |
| Average Distance | 5.16 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 36.01 | 7% | 49.69 |
| Dash | 12.55 | 15% | 16.93 |
| Charge Attack | 6.93 | 10% | 20.72 |
| Chain Pull | 4.56 | **30%** | **77.40** |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 92% |
| CC Success Rate | 10% |
| Chain Pull Success Rate | 79% |

---

## Mage

### Overall Statistics

| Metric | Value |
|--------|------:|
| Win Rate | **27%** |
| Total Damage | 110.70 |
| Damage Taken | 94.60 |
| DPS | 5.60 |
| Survival Time | 19.76 sec |
| Average Distance | 4.98 |

### Skill Telemetry

| Skill | Cast Count | Hit Rate | Avg Skill Damage |
|------|-----------:|----------:|-----------------:|
| Basic Attack | 22.69 | 42% | 48.67 |
| Teleport | 7.32 | - | - |
| Charge Attack | 6.70 | 8% | 15.85 |
| Meteor | 2.34 | **50%** | **111.11** |

#### Additional Metrics

| Metric | Value |
|--------|------:|
| Charge Success Rate | 92% |
| Meteor Success Rate | 60% |

---

# Analysis

## 1. Combat Meta Shift

The most significant outcome of this experiment is the complete reversal of combat dominance.

| Experiment | Warrior | Mage |
|------------|---------:|------:|
| Experiment 01 | 27% | 32% |
| Experiment 02 | **32%** | **27%** |

The asymmetric reward successfully transformed the Warrior into an aggressive melee fighter capable of consistently closing distance and applying combat pressure.

---

## 2. Emergence of Class Identity

The redesigned reward functions encouraged each class to learn behaviors aligned with its intended gameplay role.

### Warrior

- Maintains close-range pressure.
- Frequently chains consecutive attacks.
- Aggressively pursues melee engagements.

### Mage

- Maintains spacing from opponents.
- Relies on ranged burst damage.
- Uses Teleport to reposition before casting Meteor.

This result demonstrates that reward shaping has a substantially greater influence on learned behavior than simply increasing skill statistics.

---

## 3. High-Impact Skills

The current combat meta has become highly dependent on one key ability for each class.

### Warrior — Chain Pull

- Hit Rate: **30%**
- Average Damage: **77.40**

Chain Pull became the Warrior's primary engagement tool by forcing opponents into melee range.

---

### Mage — Meteor

- Hit Rate: **50%**
- Average Damage: **111.11**

Although Meteor is cast only **2.34 times** per episode on average, it remains the single highest-impact ability in the entire combat system.

---

## 4. Remaining Problems

Despite the overall improvement, several issues remain.

### Warrior

Basic Attack is still heavily overused.

- Average Cast Count: **36.01**
- Hit Rate: **7%**

The agent continues to spam melee attacks once it reaches close range.

---

### Mage

The Charge Attack (Nova Stun) remains ineffective.

- Hit Rate: **8%**

The Mage rarely finds opportunities to successfully use close-range crowd-control against an aggressively approaching Warrior.

---

# Conclusion

Experiment 02 demonstrates that **behavior shaping through asymmetric reward functions is significantly more effective than direct skill balancing.**

Instead of relying solely on modifications to weapon parameters, redefining the optimization objective enabled each agent to learn behaviors consistent with its intended combat role.

The Warrior evolved into an aggressive close-range fighter that actively maintains pressure, while the Mage adopted a spacing-oriented strategy focused on ranged burst damage.

However, telemetry analysis also revealed that several behaviors remain suboptimal. The Warrior continues to overuse Basic Attack despite its low hit rate, and the Mage rarely utilizes its close-range defensive abilities effectively.

These findings suggest that the current reward function successfully establishes combat identity, but still lacks mechanisms that encourage **precision, efficiency, and context-aware decision making**.

Rather than proceeding directly to gameplay balancing, the next iteration will further refine the reward structure to improve policy quality before evaluating overall game balance.

---

# Next Experiment

The next iteration focuses on **reward refinement through telemetry-driven behavior analysis**.

Rather than modifying gameplay balance, the objective is to eliminate inefficient behaviors identified during Experiment 02 and improve policy quality before performing balance evaluation.

Future work includes:

- Penalizing inefficient attack attempts to reduce Warrior attack spamming.
- Introducing conditional rewards for successful defensive actions under close-range situations.
- Improving attack precision instead of simply increasing attack frequency.
- Refining reward sparsity to better align optimization with desired combat behaviors.
- Evaluating behavioral changes using telemetry statistics and Elo progression.

The long-term objective is to obtain policies that exhibit stable, efficient, and interpretable combat behavior, providing a reliable foundation for subsequent automated balance recommendation experiments.

---

# Lessons Learned

Experiment 02 provided several important insights.

- Reward engineering is substantially more effective than direct parameter tuning.
- Class-specific reward functions naturally induce class-specific combat identities.
- Increasing reward magnitude requires corresponding optimization adjustments.
- Larger batch sizes and gradient clipping successfully stabilized PPO training under increased reward variance.
- Telemetry-driven analysis continues to provide valuable guidance for subsequent reward design.
