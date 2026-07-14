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

This enables AI-assisted game balancing without manual parameter tuning.

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
| PPO Training | ✅ Stable |
| Self-Play | ✅ Warrior vs Mage |
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

# Key Contributions

- Implemented a custom PPO framework using PyTorch.
- Designed a multi-discrete action space for simultaneous movement and skill execution.
- Developed asymmetric reward functions for heterogeneous agents.
- Built a telemetry pipeline for automated gameplay analysis.
- Integrated Optuna to automatically optimize gameplay balance through self-play.
- Demonstrated AI-assisted game balancing without manual parameter tuning.

---
