# SwarmLab — VR Autonomous Robot Swarm Laboratory

An interactive, VR-ready laboratory built in **Unity** for experimenting with
**autonomous robot swarms**. Run six swarm algorithms on 1000+ agents in real time,
command and disturb the swarm, break it to prove it is resilient, and export the
results as data — a hands-on way to learn how simple local rules produce intelligent
group behaviour.

> VR/AR Lab experiential-learning project. Desktop-first and built on Unity's XR
> Interaction Toolkit, so it runs without a headset today and is ready for one later.

---

## Experiments

| # | Experiment | Idea | Real-world use |
|---|------------|------|----------------|
| 1 | **Flocking (Boids)** | Separation, alignment, cohesion | Drone shows, crowd/traffic modelling |
| 2 | **Rendezvous (Consensus)** | Leaderless convergence to one point | Robots regrouping; sensor sync |
| 3 | **Area Coverage** | Disperse and explore; coverage % | Mapping, surveying, recon, cleaning |
| 4 | **Foraging (Pheromone)** | Stigmergy: trails between nest and food | Warehouse logistics, route optimisation |
| 5 | **Formation Control** | Assemble into sphere / ring / grid / heart | Satellite constellations, light shows |
| 6 | **Search & Rescue** | Find scattered survivors; mission timer | Disaster response |

## Features

- **1000+ agents in real time** via a spatial-hash neighbour search and GPU-instanced rendering.
- **Live metrics dashboard** — active agents, FPS, connectivity, and a per-experiment metric.
- **Fault injection** — kill 30% of the swarm, or jam communications, to show robustness.
- **Obstacles** the whole swarm navigates around; drop or clear them live.
- **CSV data export** — every run can be saved and plotted (coverage over time, rescue time, connectivity vs. jamming).
- **Neon lab environment** — glowing grid floor, bloom, fog, boundary ring, cinematic orbit camera.
- **Clickable UI** plus keyboard shortcuts; the whole demo can be driven from on-screen buttons.

## Controls

| Key | Action |
|-----|--------|
| `1`–`6` | Switch experiment |
| `F` | Morph shape (Formation) |
| `K` | Kill 30% of the swarm |
| `J` | Jam / un-jam communications |
| `R` | Reset the swarm |
| `E` | Export metrics to CSV |
| `B` / `N` | Drop obstacle at cursor / clear obstacles |
| `O` | Toggle auto-orbit |
| Left-drag / Right-drag | Attract / repel the swarm |
| Middle-drag / Scroll | Rotate / zoom the camera |

A printable cheat-sheet is in [`Docs/`](Docs).

## Run it

1. Open the project in **Unity 6 LTS** (built with 6000.6).
2. Open the `SampleScene` (or any scene) and press **Play** — the lab builds itself automatically.
3. Use the dashboard buttons or the keys above.

No manual scene setup is needed: a runtime bootstrapper creates the camera, lights,
environment, swarm and UI on Play.

## Project structure

```
Assets/Scripts/
  Core/          SwarmManager, SpatialHash, bootstrap, environment, camera, data logger, scenario
  Behaviours/    SwarmBehaviour base + the six experiments
  Interaction/   SwarmInteractor (input) + LabUI (dashboard)
  Editor/        One-click scene builder (optional)
Docs/            Objectives, tools & tech, progress report, controls cheat-sheet (PDF)
```

## Tech

Unity 6 (URP) · C# · XR Interaction Toolkit (VR-ready) · new Input System ·
GPU instancing · URP post-processing (bloom/vignette/color grading).

## Status

Core simulation, six experiments, metrics, fault injection, obstacles, data export and
the neon UI are implemented and running. Planned next: challenge/versus modes, an
RL-trained policy, the XR Device Simulator layer, and a standalone build.
