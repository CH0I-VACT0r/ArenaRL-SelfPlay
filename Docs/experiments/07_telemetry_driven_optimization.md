# EP07 — Telemetry-Driven Multi-Objective Balance Optimization

**Date:** 2026-07-20

---
## Objective

EP06 optimized balance using only win rate and survival time.

However, actual game balance cannot be fully represented by these two metrics alone.

In EP07, the optimization process was extended to incorporate **combat telemetry**, allowing balance evaluation to consider actual gameplay quality rather than only match outcomes.

Additionally, Domain Randomization was introduced during training so that agents could learn robust combat behaviors under varying gameplay conditions.

---

# Motivation

Previous experiments optimized only:

- Win Rate
- Survival Time

This often produced balanced win rates while still allowing undesirable gameplay such as:

- excessively short fights
- unrealistic combat distances
- inaccurate skill usage
- abnormal DPS patterns

To better approximate designer intentions, optimization was expanded into a **multi-objective problem** driven by gameplay telemetry.

---

# Step 1 — Domain Randomization Training

Before optimization, the balance parameters discovered in EP06 were fixed:

| Parameter | Value |
|-----------|------:|
| Warrior HP | 250 |
| Mage HP | 230 |
| Warrior Damage Multiplier | 0.80 |
| Mage Damage Multiplier | 1.05 |

During every episode reset, gameplay variables were randomized.

Randomized parameters:

- Warrior Movement Speed
- Mage Movement Speed
- Warrior Skill Cooldown
- Mage Skill Cooldown

Each parameter was randomly sampled within ±20% of its default value.

This forced the PPO agents to learn policies that remained effective under changing combat conditions instead of memorizing a single environment.

---

# Step 2 — Telemetry Side Channel

A custom Unity ML-Agents Side Channel was implemented to transmit combat telemetry directly from Unity to Python.

Pipeline:

Unity Episode

↓

TelemetryManager

↓

Telemetry Side Channel (JSON)

↓

Python Telemetry Channel

↓

Optuna Objective Function

Each completed episode transmitted the following combat statistics:

- Warrior Hit Rate
- Mage Hit Rate
- Average Combat Distance
- Survival Time
- Warrior DPS
- Mage DPS

Unlike previous experiments that relied only on rewards and win rates, optimization now had access to detailed gameplay statistics.

---

# Step 3 — Expanded Observation Space

Because agents now had to adapt to randomized gameplay conditions, the observation space was expanded.

Observation Dimension

30 -> 31

The additional observation allows agents to better respond to dynamically changing combat environments during Domain Randomization training.

---

# Step 4 — Multi-Objective Optimization

The Optuna objective function was redesigned to optimize multiple gameplay metrics simultaneously.

Optimization targets:

- Win Rate Gap
- Survival Time
- Timeout Rate
- Hit Rate
- Average Distance
- DPS

Target gameplay values:

| Metric | Target |
|---------|--------|
| Win Rate | 50 : 50 |
| Survival Time | 20 s |
| Average Distance | 3.2 |
| Hit Rate | 30 % |
| DPS | 6.5 |

Each telemetry metric was converted into a normalized penalty based on the distance from its desired target.

The final optimization objective became a weighted sum of all penalties.

```
Loss =
0.35 × Win Rate Gap
+ 0.20 × Survival Penalty
+ 0.15 × Timeout Penalty
+ 0.10 × Hit Rate Penalty
+ 0.10 × Distance Penalty
+ 0.10 × DPS Penalty
```

This formulation encouraged the optimizer to search for gameplay that was not only statistically fair but also exhibited desirable combat characteristics.

---

# Optimization Result

After 50 Optuna trials, the following balance parameters achieved the lowest integrated loss.

| Parameter | Value |
|-----------|------:|
| Warrior HP | **235** |
| Mage HP | **250** |
| Warrior Damage Multiplier | **1.50** |
| Mage Damage Multiplier | **1.50** |

Compared to EP06, the optimizer selected a different balance configuration because optimization now considered combat quality rather than only match outcomes.

---

# Discussion

Although the optimizer successfully minimized the integrated objective, analysis of the resulting combat telemetry revealed several interesting behavioral characteristics.

## Result Analysis

Among the 50 Optuna trials, **Trial 21** achieved the lowest integrated loss.

| Metric | Result |
|---------|--------|
| Integrated Loss | **0.1567** |
| Win Rate Gap | **3%** |
| Average Survival Time | **21.3 s** |

The optimizer therefore succeeded in simultaneously satisfying the two highest-priority objectives:

- Near-equal win rate
- Approximately 20-second combat duration

---

## Parameter Changes

The optimal balance parameters differed noticeably from those found in EP06.

| Parameter | Value |
|-----------|------:|
| Warrior HP | **235** |
| Mage HP | **250** |
| Warrior Damage Multiplier | **1.50** |
| Mage Damage Multiplier | **1.50** |

Both agents converged to the maximum allowable damage multiplier while HP values shifted only moderately.

---

## Telemetry Analysis

Although the integrated loss was minimized, several telemetry metrics still deviated from their desired targets.

| Metric | Target | Trial 21 |
|---------|-------:|---------:|
| Survival Time | 20.0 s | **21.3 s** |
| Average Distance | 3.2 | **4.3** |
| DPS | 6.5 | **10.9** |

The optimizer successfully matched combat duration while producing combat dynamics substantially different from the intended gameplay.

---

## Emergent Combat Meta

The telemetry suggests that the learned agents developed a new combat strategy rather than simply increasing combat efficiency.

Despite both damage multipliers reaching **1.5×**, average survival time remained close to the 20-second target.

This indicates that the agents compensated for higher damage by changing their behavior.

Observed characteristics include:

- Maintaining larger combat distances
- Waiting for skill cooldowns
- Delivering burst damage instead of continuous melee exchanges
- Repositioning immediately after attacking

Instead of maximizing close-range DPS, the agents naturally converged toward a **kite-and-burst combat style**.

---

## Reward Hacking Interpretation

This behavior can be interpreted as a form of **reward hacking**.

The optimization objective assigned the highest weights to:

- Win-rate equality
- Survival time

while average distance and DPS contributed relatively small penalties.

Consequently, Optuna discovered a parameter configuration that minimized the weighted objective without matching every designer-intended gameplay characteristic.

Rather than reproducing the desired combat style, the optimizer identified an alternative equilibrium that achieved a lower mathematical loss.

---

## Implications

EP07 demonstrates that multi-objective optimization successfully balances multiple gameplay objectives simultaneously.

However, it also reveals an important limitation.

Even when optimization converges, the resulting gameplay may differ significantly from the designer's expectations.

This experiment highlights the importance of carefully designing objective functions and weighting telemetry metrics when applying reinforcement learning to automated game balancing.

The optimization target ultimately determines not only **whether** the game is balanced, but also **how** that balance is achieved.

---

# Outcome

EP07 represents the transition from **Outcome-Based Optimization** to **Gameplay-Aware Optimization**.

Key improvements include:

- Domain Randomization training
- Telemetry Side Channel integration
- Real-time Unity → Python telemetry communication
- Multi-objective Optuna optimization
- Gameplay quality incorporated into balance evaluation
- Robust policy learning under randomized combat conditions

Rather than optimizing solely for equal win rates, the balance search now accounts for how the battle is actually played.

---

# Conclusion

EP07 establishes a telemetry-driven balance optimization framework that combines reinforcement learning, domain randomization, and multi-objective optimization.

This enables future experiments to optimize combat using designer-defined gameplay objectives instead of relying only on win/loss statistics, providing a more practical foundation for automated game balance tuning.

More importantly, EP07 demonstrates that optimizing a weighted objective does not necessarily produce the gameplay designers originally intended.

Instead, the optimizer may discover alternative combat strategies that minimize the mathematical objective while exhibiting entirely different gameplay characteristics.

This observation motivates future work on richer telemetry objectives and more expressive designer-defined balance metrics.
