# Trellis Framework — Technical Design Document

## 1. Purpose

Trellis is a lightweight Unity game framework extracted from production game code. It provides three core subsystems that are commonly needed across Unity projects:

1. **State Machine** — Generic, trigger-based flow control
2. **System Scheduler** — Deterministic ordered system execution
3. **Object Pooling** — Pre-allocated GameObject reuse with lifecycle callbacks

The framework is intentionally minimal. Each subsystem is proven in production and solves one problem well. Growth happens by extracting patterns that prove useful across multiple projects, not by speculating on future needs.

## 2. Architecture

### Design Principles

- **Lightweight over comprehensive** — Each subsystem is a few files, not a library. No configuration frameworks, no dependency injection containers, no reflection.
- **Plain C# where possible** — State machine and scheduler are pure C# classes with no MonoBehaviour dependency. Only the pooling system requires Unity APIs (GameObject instantiation).
- **No LINQ, no allocations on hot paths** — Framework code follows the same performance discipline as game code. See constraints below.
- **Generic over game-specific** — No assumptions about what states, triggers, or systems a consuming project will define. The state machine is parameterized on enums; the scheduler takes any `ISystem` implementation.
- **Composition over inheritance** — Interfaces (`IState`, `ISystem`, `IPoolable`) define contracts. No base classes to inherit from.

### Subsystem Design

#### State Machine (`Trellis.StateMachine`)

A trigger-based state machine parameterized on two enum types: `TState` (state identifiers) and `TTrigger` (transition triggers).

**Key design decisions:**
- **One pending trigger per tick** — Simplifies reasoning about state transitions. No queue, no priority system. If two triggers fire before resolution, the last one wins (with a warning).
- **Deferred trigger resolution** — Triggers are resolved at the start of `Tick()`, not when `Fire()` is called. This prevents re-entrant state transitions and makes the execution model predictable.
- **Triggers fired during Enter() survive** — The pending trigger is cleared *before* resolution, so a trigger fired during `Enter()` (e.g., an init state that immediately validates and transitions) is preserved for the next `Tick()`.
- **Transition table, not per-state handlers** — Transitions are registered externally (`AddTransition`), keeping states unaware of the overall flow graph.

#### System Scheduler (`Trellis.Scheduling`)

An ordered array of `ISystem` implementations ticked sequentially each frame.

**Key design decisions:**
- **Array-based, not list-based** — Systems are registered at construction time. No runtime add/remove. This enforces deterministic order and avoids collection mutation during iteration.
- **Null-safe** — Null elements in the array are silently skipped, allowing placeholder slots.
- **No phase abstraction** — The scheduler ticks systems in array order. "Phases" are a convention of the consuming project (e.g., systems 0-2 are "phase 1"), not a framework concept.

#### Object Pooling (`Trellis.Pooling`)

A stack-based pre-allocated pool for GameObjects with per-instance `IPoolable` component caching.

**Key design decisions:**
- **Position before activation** — `Acquire(Vector3 position)` sets the transform position before calling `OnPoolGet()`, preventing one-frame visual pop at the old position.
- **Cached IPoolable lookup** — `TryGetComponent<IPoolable>` is called once per instance (on creation or first on-demand instantiation) and cached in a dictionary. No per-acquire component lookups.
- **On-demand growth with warning** — If the pool is exhausted, new instances are created with a `Debug.LogWarning`. This keeps the game running while flagging the capacity issue.

## 3. Constraints

- **No LINQ in runtime code** — `System.Linq` causes hidden allocations. Use explicit loops.
- **No GetComponent in hot paths** — Cache component references. Never call `GetComponent` per-frame.
- **No MonoBehaviour inheritance for pure logic** — State machine, scheduler, and their interfaces are plain C#. Only pooling touches Unity APIs.
- **No game-specific types** — Framework code never references game enums, data structures, or systems. Dependencies flow inward: game code depends on Trellis, never the reverse.
- **Struct constraints on generics** — `StateMachine<TState, TTrigger>` constrains to `struct, Enum` to avoid boxing and ensure value-type semantics.

## 4. Package Structure

```
Trellis/
├── package.json                    # com.trellis.framework
├── Runtime/
│   ├── Trellis.Runtime.asmdef
│   ├── Pooling/
│   │   ├── GameObjectPool.cs
│   │   └── IPoolable.cs
│   ├── Scheduling/
│   │   ├── SystemScheduler.cs
│   │   └── ISystem.cs
│   └── StateMachine/
│       ├── StateMachine.cs
│       └── IState.cs
└── Tests/
    └── Editor/
        ├── Trellis.Tests.Editor.asmdef
        ├── StateMachineTests.cs
        └── SystemSchedulerTests.cs
```

## 5. Testing Strategy

- **Edit Mode tests** for state machine and scheduler (pure C#, no scene required)
- **Pool tests deferred** — `GameObjectPool` requires `GameObject.Instantiate` which needs Play Mode. The pool code is unchanged from its production origin and is battle-tested there.
- Test naming: `MethodOrBehavior_Condition_ExpectedResult`
- Framework tests define their own test enums and stub implementations — no dependency on any game project
