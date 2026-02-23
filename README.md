# Trellis

A lightweight Unity game framework providing architectural primitives for state management, system scheduling, object pooling, reactive data flow, UI management, and application infrastructure. Built on VContainer for dependency injection.

## Subsystems

| Tier | Subsystem | Namespace | Status |
|------|-----------|-----------|--------|
| **Core** | State Machine | `Trellis.StateMachine` | Implemented |
| **Core** | Hierarchical State Machine | `Trellis.StateMachine` | Implemented |
| **Core** | System Scheduler | `Trellis.Scheduling` | Implemented |
| **Core** | Object Pooling | `Trellis.Pooling` | Implemented |
| **Core** | Event Bus | `Trellis.Events` | Implemented |
| **Core** | Reactive Properties | `Trellis.Reactive` | Implemented |
| **Core** | State Store | `Trellis.Stores` | Implemented |
| **Core** | Timers | `Trellis.Timing` | Implemented |
| **App** | Structured Logger | `Trellis.Logging` | Implemented |
| **UI** | UI Router | `Trellis.UI` | Planned |
| **UI** | Panel Manager | `Trellis.UI` | Planned |
| **UI** | Popup System | `Trellis.UI` | Planned |
| **UI** | Toast / Notification | `Trellis.UI` | Planned |
| **App** | App Lifecycle | `Trellis.App` | Planned |
| **App** | Scene Manager | `Trellis.Scenes` | Planned |
| **App** | Debug Overlay | `Trellis.Debugging` | Planned |
| **Data** | Definition System | `Trellis.Data` | Planned |
| **Data** | Save System | `Trellis.Data` | Planned |

## Architecture

Trellis follows a FLUX-inspired unidirectional data flow pattern:

```
User Input → StoreActions<T> → Store<T> → Observable<T> → UI Panels / Systems
```

- **VContainer** provides dependency injection — no singletons, no service locators
- **Stores** are the single source of truth, modified only through actions
- **Observable\<T\>** properties notify subscribers reactively
- **EventBus** handles decoupled system-to-system communication
- **UI Toolkit** native for all framework UI (router, panels, popups, toasts)

## Assembly Structure

```
Trellis.Runtime.asmdef          → VContainer dependency
Trellis.Netcode.asmdef          → Trellis.Runtime + Unity.Netcode.Runtime
Trellis.Tests.Editor.asmdef     → Edit Mode tests (pure C#, fast)
Trellis.Tests.Netcode.asmdef    → Play Mode tests (network)
```

## Importing into Your Project

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.trellis.framework": "file:../relative/path/to/Trellis"
  }
}
```

The `Trellis.Runtime` assembly is auto-referenced. Ensure VContainer is also installed in your project.

## Repository Structure

```
Trellis/              # Unity package (com.trellis.framework)
  Runtime/            # Framework code
  Tests/Editor/       # Edit Mode tests
  Tests/Netcode/      # Play Mode tests (future)
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

- [Technical Design Document](Docs/TDD.md) — Full architecture, subsystem design, and deliverables
- [Architecture Diagrams](Docs/Architecture-Diagrams.md) — Mermaid diagrams for all subsystems
- [CU-Client Pain Points](Docs/CU-Client-Pain-Points.md) — Problems Trellis is designed to solve

## Target Unity Version

Unity 6000.3 (6.3) or later.
