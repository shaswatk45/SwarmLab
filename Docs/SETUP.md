# SwarmLab — Foundation setup

This is the Phase 2 foundation: a working swarm core with **two experiments**
(flocking + rendezvous), mouse interaction, and a one-click scene builder.

## Step 1 — Create the Unity project
In Unity Hub:
1. **New project** → template **Universal 3D** (URP). (If URP isn't there, plain **3D** also works — the code auto-detects the render pipeline.)
2. Name it `SwarmLab`.
3. **Location: NOT inside OneDrive.** Use something like `C:\Users\91953\SwarmLab`.
   OneDrive tries to sync Unity's `Library` folder and corrupts projects.
4. Create project.

## Step 2 — Add these scripts
Copy the `Assets` folder from `swarmlab/foundation/` into your new project,
merging with the project's existing `Assets` folder. (I can do this for you with one
command once you tell me the project path.)

## Step 3 — One input setting
`Edit → Project Settings → Player → Other Settings → Active Input Handling` →
set to **Both** (or **Input Manager (Old)**). Unity restarts. This lets the simple
keyboard/mouse controls work.

## Step 4 — Build the scene
Top menu: **SwarmLab → Build Lab Scene**. It creates the ground, camera, light,
the swarm object and a glowing material automatically.

## Step 5 — Play
Press **Play**.
- `1` = Flocking   `2` = Rendezvous   `R` = Respawn
- **Left-drag** = attract the swarm   **Right-drag** = repel

Change `Agent Count` on the **Swarm** object in the Inspector to push it to 2000+.

## What each script does (for your viva)
| File | Role |
|------|------|
| `Core/SwarmManager.cs` | Owns agent data, runs the sim loop, GPU-instanced rendering |
| `Core/SpatialHash.cs` | Fast neighbour lookup — the reason it scales to 1000+ |
| `Behaviours/ISwarmBehaviour.cs` | Common interface; each experiment is a plug-in module |
| `Behaviours/FlockingBehaviour.cs` | Boids: separation + alignment + cohesion |
| `Behaviours/RendezvousBehaviour.cs` | Consensus: leaderless convergence |
| `Interaction/SwarmInteractor.cs` | Mouse attract/repel, experiment switching, HUD |
| `Editor/SwarmSceneBuilder.cs` | The **SwarmLab → Build Lab Scene** menu |

## Next phases (not built yet — matches the progress report)
- Coverage, foraging, formation experiments
- Metrics dashboard + CSV export
- Fault injection (kill %, jam comms), challenge mode, versus mode
- XR Device Simulator layer, neon polish, `.exe` build
