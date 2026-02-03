# Trellis Framework — Architecture Diagrams

## Package Dependency

```
┌─────────────────────────────┐
│  Your Game Project          │
│  (Assembly-CSharp)          │
│                             │
│  - Defines enums            │
│  - Implements IState        │
│  - Implements ISystem       │
│  - Implements IPoolable     │
│                             │
│  ┌───────────────────────┐  │
│  │ Packages/manifest.json│  │
│  │ "com.trellis.framework│  │
│  │  : file:../../Trellis"│  │
│  └───────────┬───────────┘  │
└──────────────┼──────────────┘
               │ references
               ▼
┌─────────────────────────────┐
│  Trellis.Runtime            │
│  (com.trellis.framework)    │
│                             │
│  StateMachine<TState,TTrig> │
│  SystemScheduler            │
│  GameObjectPool             │
│  IState, ISystem, IPoolable │
└─────────────────────────────┘
```

## State Machine Lifecycle

```
             Fire(trigger)
                 │
                 ▼
         ┌──────────────┐
         │pendingTrigger │ (stored, not resolved)
         └──────┬───────┘
                │
    ────────────┼──────────── Tick() boundary
                │
                ▼
         ┌──────────────┐
         │Clear pending  │
         │before resolve │
         └──────┬───────┘
                │
                ▼
    ┌───────────────────────┐
    │ Look up transition    │
    │ (currentState,trigger)│
    │ → destination         │
    └───────────┬───────────┘
                │
         found? │
        ┌───────┴────────┐
        │ yes            │ no → LogWarning, skip
        ▼                │
  currentState.Exit()    │
        │                │
        ▼                │
  currentState = dest    │
  currentState.Enter()   │
        │                │
        ▼                │
  OnStateChanged?.Invoke │
        │                │
        ├────────────────┘
        ▼
  currentState.Tick(dt)
```

**Key detail:** `pendingTrigger` is cleared *before* `ResolveTrigger` runs. This means if `Enter()` calls `Fire()`, the new trigger is preserved for the next `Tick()` rather than being wiped.

## System Scheduler Tick Order

```
SystemScheduler.Tick(deltaTime)
    │
    ├── systems[0].Tick(dt)   ← e.g., Phase 1 systems
    ├── systems[1].Tick(dt)
    ├── systems[2].Tick(dt)   ← e.g., Phase 2 systems
    ├── systems[3].Tick(dt)
    ├── systems[4].Tick(dt)
    └── systems[5].Tick(dt)   ← e.g., Phase 3 systems

    Array index = execution order. Deterministic.
    null entries safely skipped.
```

## Object Pool Lifecycle

```
  Construction
       │
       ▼
  Pre-allocate N instances
  (Instantiate, SetActive(false), cache IPoolable)
       │
       ▼
  ┌─────────────────────────────────────┐
  │              Pool Stack             │
  │  [instance_0, instance_1, ... N-1]  │
  └──────────────┬──────────────────────┘
                 │
    Acquire(pos) │                    Return(obj)
        ┌────────┘                        │
        ▼                                 ▼
  Pop from stack                    Push to stack
  (or Instantiate if empty)         │
        │                           │
        ▼                           ▼
  transform.position = pos    IPoolable.OnPoolReturn()
        │                     (deactivate, reset)
        ▼
  IPoolable.OnPoolGet()
  (activate, init)
        │
        ▼
  Return GameObject
```
