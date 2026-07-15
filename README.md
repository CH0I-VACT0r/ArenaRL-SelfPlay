# ArenaRL-SelfPlay

**AI-assisted Game Balancing Framework using Self-Play Reinforcement Learning**

Warrior and Mage agents learn competitive PvP combat through a custom PPO implementation, self-play training, and telemetry-driven analysis.

---

# Project Overview

This project explores how reinforcement learning can be applied to competitive PvP combat in RPG games.

Unlike traditional scripted AI, two heterogeneous agents (Warrior and Mage) continuously improve by fighting against each other in a Unity arena using Self-Play.

The project is built from scratch using **PyTorch PPO**, **Unity ML-Agents Low-Level API**, and a custom telemetry pipeline for gameplay analysis.

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
- Future Balance Recommendation System

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

Each agent observes a 30-dimensional state vector.

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

- Warrior HP
- Mage HP
- Warrior Damage Multiplier
- Mage Damage Multiplier

### Objective

Loss = |Warrior Win Rate − Mage Win Rate| + 0.5 × Timeout Rate

The PPO agents act as automated playtesters,

allowing hundreds of balance candidates to be evaluated without human intervention.

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

---

# Results

| Metric | Result |
|---------|---------:|
| PPO Training | Stable |
| Self-Play | Warrior vs Mage |
| Observation Size | 30 |
| Action Space | Multi-Discrete (9 × 5) |
| Telemetry Metrics | 10+ |
| Reward Iterations | 4 |
| Balance Optimization | Optuna |
| Best Balance | 50% vs 50% Win Rate |

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

---

# Key Contributions

- Implemented a custom PPO framework using PyTorch.
- Designed a multi-discrete action space for simultaneous movement and skill execution.
- Developed asymmetric reward functions for heterogeneous agents.
- Built a telemetry pipeline for automated gameplay analysis.
- Integrated Optuna to automatically optimize gameplay balance through self-play.
- Demonstrated AI-assisted game balancing without manual parameter tuning.

---
