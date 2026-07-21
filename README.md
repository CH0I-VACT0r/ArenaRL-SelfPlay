# ArenaRL-SelfPlay

**ArenaRL-SelfPlay: A Telemetry-Driven Game Balance Optimization Framework using Self-Play Reinforcement Learning**

Warrior and Mage agents learn competitive PvP combat through a custom PPO implementation, self-play training, and telemetry-driven analysis.

---

# Project Overview

This project investigates how reinforcement learning can be applied not only to train competitive game AI, but also to automatically optimize gameplay balance.

Instead of relying solely on designer intuition, self-play agents are used as automated playtesters while telemetry-driven multi-objective optimization searches for balanced gameplay configurations.

The framework combines:

- Custom PPO
- Self-Play Reinforcement Learning
- Gameplay Telemetry
- Optuna Multi-Objective Optimization
- Domain Randomization

The project is organized as a sequence of iterative experiments, where each stage incrementally improves the reinforcement learning framework, gameplay telemetry, and automated balance optimization pipeline.

---

# Highlights

- Custom PPO implementation in PyTorch
- Unity ML-Agents Low-Level API
- Self-Play PvP Arena
- Telemetry Side Channel
- Domain Randomization
- Multi-objective Balance Optimization
- 8-Dimensional Parameter Search
- Automated Gameplay Analysis

---

# Architecture

<p align="center">
<img src="Docs/architecture/overall_pipeline.png" width="1600">
</p>

The training pipeline consists of:

- Unity 2D Arena Environment
- Self-Play PvP Simulation
- PyTorch PPO Training
- Telemetry Collection
- Gameplay Analysis
- Telemetry-driven Balance Optimization

---
# Game System

The self-play environment is designed to resemble a lightweight action RPG combat system rather than a simple reinforcement learning benchmark.

## Play Rules

- 1 vs 1 Arena
- Warrior vs Mage
- Maximum Episode Length: 60 seconds
- Sudden Death after 40 seconds
- Simultaneous Action Execution

---

## Warrior

Designed as an aggressive melee fighter.

Skills

1. Basic Attack
2. Dash
3. Charge Slash
4. Chain Pull (Crowd Control)

Characteristics

- High close-range pressure
- Combo-oriented gameplay
- Pull-and-finish combat style

---

## Mage

Designed as a ranged control specialist.

Skills

1. Basic Attack
2. Dash
3. AoE Stun
4. Meteor

Characteristics

- Long-range kiting
- Defensive repositioning
- Area control

---

## Combat Mechanics

- Skill Cooldown
- Charge Skills
- Crowd Control
- Projectile System
- Hazard Telegraph
- Magnetic Field (Sudden Death)
- Continuous Damage Zone

---

# Action Space

<p align="center">
<img src="Docs/architecture/action_space.png" width="900">
</p>

The agent outputs **two discrete actions every frame**.

### Branch 0 — Movement (9)

- Stop
- Up
- Down
- Left
- Right
- Up-Left
- Up-Right
- Down-Left
- Down-Right

### Branch 1 — Skill (5)

- Do Nothing
- Skill 1
- Skill 2
- Skill 3
- Skill 4

This Multi-Discrete action space enables simultaneous movement and skill execution.

---

# Observation Space

Each agent observes a 31-dimensional state vector.

The observation includes

- Self Status
- Enemy Status
- Relative Distance
- Relative Velocity
- Cooldown State
- Crowd Control State
- Hazard Position
- Hazard Velocity
- Telegraph Information

---
# Reward Design

The reward function evolved throughout multiple experiments.

Major reward components include

- Damage Reward
- Combo Reward
- Distance Reward
- Aim Alignment Reward
- Action Cost
- Whiff Penalty
- Hazard Avoidance Reward

---

# Demo

<p align="center">
<img src="Docs/demo/demo1.gif" width="600">
</p>

The animation above shows the first self-play training environment between Warrior and Mage.

---

# Current Features

## Environment

- Warrior vs Mage Arena
- 1 vs 1 Self-Play
- 60-second Episodes

---

## Custom PPO

- PyTorch Implementation
- Actor-Critic Network
- Multi-Discrete Policy
- Entropy Bonus
- Replay Buffer

---

## Self-Play

- Symmetric Arena
- Continuous Policy Updates
- Custom Reward Functions

---

## Telemetry System

The project automatically records gameplay statistics during training.

Examples include:

- Win Rate
- Damage Dealt
- Damage Taken
- DPS
- Skill Usage
- Skill Hit Rate
- Survival Time
- Average Distance
- Charge Success
- CC Success

Telemetry data is later analyzed to guide reward tuning and game balancing.

---

## Automated Balance Optimization

The trained PPO agents are also used as automated playtesters.

By integrating Optuna with Unity Environment Parameters, gameplay statistics such as HP and damage multipliers are automatically optimized through repeated self-play simulations.

### Optimization Variables

- HP
- Damage
- Movement Speed
- Cooldown Multiplier

### Optimization Objectives

The optimization objective is defined as a weighted multi-objective loss:

Loss =
0.35 × Win Rate Gap
+ 0.20 × Survival Penalty
+ 0.15 × Timeout Penalty
+ 0.10 × Hit Rate Penalty
+ 0.10 × Distance Penalty
+ 0.10 × DPS Penalty

The optimizer minimizes a weighted multi-objective loss constructed from gameplay telemetry collected during self-play.

---

# Experiment Log

Development is organized as a series of research experiments.

| No. | Experiment | Status |
|----:|------------|--------|
| 01 | [Initial Self-Play Training](Docs/experiments/01_initial_selfplay.md) | Complete |
| 02 | [Asymmetric Reward Functions](Docs/experiments/02_asymmetric_reward_shaping.md) | Complete |
| 03 | [Reward Refinement](Docs/experiments/03_reward_refinement.md) | Complete |
| 04 | [Behavior Refinement](Docs/experiments/04_behavior_refinement.md) | Complete |
| 05 | [Balance Optimization](Docs/experiments/05_balance_optimization.md) | Complete |
| 06 | [Multi Objecitve Optimization](Docs/experiments/06_multi_objective_optimization.md) | Complete |
| 07 | [Telemetry Driven Optimization](Docs/experiments/07_telemetry_driven_optimization.md) | Complete |
| 08 | [Expanded Search Space](Docs/experiments/08_expanded_search_space.md) | Complete |

---

# Results

| Metric | Result |
|---------|---------:|
| PPO Training Episodes | 7000 |
| Self-Play Matches | 10,000+ |
| Observation Size | 31 |
| Action Space | Multi-Discrete (9 × 5) |
| Telemetry Metrics | 10+ |
| Balance Parameters | 8 |
| Optimization Method | Optuna |
| Best Win Rate Gap | 0.00 |
| Lowest Integrated Loss | 0.1668 |
| Best Balance | 50 : 50 Win Rate |

---

# Tech Stack

| Category | Technology |
|----------|------------|
| Engine | Unity 2022 |
| Language | C#, Python |
| RL Framework | PyTorch |
| Algorithm | PPO (Custom Implementation) |
| Communication | Unity ML-Agents Low-Level API |
| Optimization | Optuna |
| Data Analysis | TensorBoard |
| Version Control | Git |

---

# Design Philosophy

Instead of designing stronger AI,

this project focuses on designing better gameplay through AI.

Self-play agents are treated as automated playtesters.

Telemetry generated from thousands of simulated matches is analyzed to

- improve reward functions,
- discover undesirable behaviors,
- evaluate game balance,
- recommend parameter adjustments.

The long-term objective is an AI-assisted game balancing framework for live-service games.

Rather than treating balance as a collection of handcrafted parameters, this project formulates game balancing as an optimization problem driven by gameplay telemetry.

The framework explores how reinforcement learning can assist designers by automatically searching for parameter configurations while revealing unintended emergent strategies.

Ultimately, the goal is not to replace human designers, but to provide quantitative evidence that supports balance decisions through large-scale automated gameplay simulation.

---

# Key Contributions

- Implemented a custom PPO framework for asymmetric PvP self-play.
- Designed a telemetry-driven gameplay analysis pipeline using Unity Side Channels.
- Developed a multi-objective balance optimization framework using Optuna.
- Expanded the optimization space from four to eight gameplay parameters.
- Investigated the relationship between optimization objectives and emergent gameplay.
- Demonstrated that increasing optimization dimensionality alone does not necessarily alter emergent combat behavior.
- Showed how reward weighting influences balance outcomes through telemetry-guided analysis.
- Demonstrated how telemetry can be integrated into reinforcement learning-based game balance optimization through Unity ML-Agents Side Channels.
  
---

# Research Findings

Throughout eight iterative experiments, several observations emerged.

- Reward shaping significantly influences combat behavior.
- Multi-objective optimization produces more realistic gameplay than win-rate optimization alone.
- Expanding the optimization search space improves flexibility but does not necessarily change emergent strategies.
- Agents repeatedly converged toward kite-oriented combat, suggesting that environment dynamics dominated parameter optimization.
- Effective AI-assisted game balancing requires not only parameter optimization but also careful design of reward functions and combat environments.
