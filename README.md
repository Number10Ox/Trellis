# Trellis

A lightweight Unity game framework providing three core subsystems: a generic trigger-based state machine, a deterministic system scheduler, and a GameObject object pool.

## Subsystems

| Subsystem | Namespace | Purpose |
|-----------|-----------|---------|
| **State Machine** | `Trellis.StateMachine` | Generic trigger-based state machine with transition tables, Enter/Exit/Tick lifecycle, and deferred trigger resolution. |
| **Scheduling** | `Trellis.Scheduling` | Ordered system execution with deterministic tick order. Systems implement `ISystem` and are ticked sequentially. |
| **Pooling** | `Trellis.Pooling` | Stack-based GameObject pool with pre-allocation, IPoolable lifecycle callbacks, and per-instance component caching. |

## Importing into Your Project

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.trellis.framework": "file:../relative/path/to/Trellis"
  }
}
```

The `Trellis.Runtime` assembly is auto-referenced, so all Trellis types are immediately available in your scripts.

## Repository Structure

```
Trellis/              # Unity package (com.trellis.framework)
  Runtime/            # Framework code
  Tests/Editor/       # Edit Mode tests
Trellis-Starter/      # Demo Unity project consuming the package
Docs/                 # Design documentation
```

## Quick Start

```csharp
using Trellis.StateMachine;
using Trellis.Scheduling;

// Define your states and triggers as enums
enum AppState { Loading, Menu, Playing }
enum AppTrigger { Loaded, StartGame, GameOver }

// Wire up the state machine
var sm = new StateMachine<AppState, AppTrigger>();
sm.AddState(AppState.Loading, new LoadingState());
sm.AddState(AppState.Menu, new MenuState());
sm.AddState(AppState.Playing, new PlayingState());

sm.AddTransition(AppState.Loading, AppTrigger.Loaded, AppState.Menu);
sm.AddTransition(AppState.Menu, AppTrigger.StartGame, AppState.Playing);

sm.Start(AppState.Loading);

// Wire up the system scheduler
var scheduler = new SystemScheduler(new ISystem[] { movementSystem, combatSystem });

// In your Update loop
sm.Tick(Time.deltaTime);
scheduler.Tick(Time.deltaTime);
```

## Running Tests

Open `Trellis-Starter` in Unity 6000.3.5f1, then: Window > General > Test Runner > Edit Mode > Run All.

## Documentation

- [Technical Design Document](Docs/TDD.md)
- [Architecture Diagrams](Docs/Architecture-Diagrams.md)

## Target Unity Version

Unity 6000.3 (6.3) or later.
