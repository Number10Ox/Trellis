# Trellis Framework — Package API

## State Machine (`Trellis.StateMachine`)

Generic trigger-based state machine parameterized on enum types.

### Types

- **`IState`** — Lifecycle interface: `Enter()`, `Tick(float deltaTime)`, `Exit()`
- **`StateMachine<TState, TTrigger>`** — State registry, transition table, trigger resolution

### Usage

```csharp
using Trellis.StateMachine;

// 1. Define enums for your states and triggers
enum GameState { Init, Playing, Win, Lose }
enum GameTrigger { SceneReady, BaseDestroyed, AllWavesCleared, Restart }

// 2. Create the state machine
var sm = new StateMachine<GameState, GameTrigger>();

// 3. Register states (each implements IState)
sm.AddState(GameState.Init, initState);
sm.AddState(GameState.Playing, playingState);

// 4. Register transitions
sm.AddTransition(GameState.Init, GameTrigger.SceneReady, GameState.Playing);
sm.AddTransition(GameState.Playing, GameTrigger.BaseDestroyed, GameState.Lose);

// 5. Start and tick
sm.Start(GameState.Init);
sm.Tick(deltaTime);       // Resolves pending trigger, then ticks current state

// 6. Fire triggers (resolved on next Tick)
sm.Fire(GameTrigger.SceneReady);

// 7. Listen for transitions
sm.OnStateChanged += (prev, next) => Debug.Log($"{prev} -> {next}");
```

### Behavior

- One pending trigger at a time. Firing again before resolution overwrites.
- Triggers fired during `Enter()` survive to the next `Tick()` (cleared before resolution, not after).
- Transitions call `Exit()` on the old state, then `Enter()` on the new state, then fire `OnStateChanged`.

---

## Scheduling (`Trellis.Scheduling`)

Deterministic ordered system execution.

### Types

- **`ISystem`** — Tick contract: `Tick(float deltaTime)`
- **`SystemScheduler`** — Ticks an ordered `ISystem[]` sequentially

### Usage

```csharp
using Trellis.Scheduling;

var scheduler = new SystemScheduler(new ISystem[]
{
    waveSystem,       // Phase 1
    spawnSystem,      // Phase 1
    movementSystem,   // Phase 2
    projectileSystem, // Phase 2
    damageSystem,     // Phase 2
    economySystem     // Phase 3
});

// In your game loop
scheduler.Tick(Time.deltaTime);
```

Array order determines execution order. Null elements are safely skipped.

---

## Pooling (`Trellis.Pooling`)

Stack-based GameObject pool with pre-allocation and lifecycle callbacks.

### Types

- **`IPoolable`** — Optional MonoBehaviour interface: `OnPoolGet()`, `OnPoolReturn()`
- **`GameObjectPool`** — Pre-allocates instances, acquires/returns with position, caches IPoolable per instance

### Usage

```csharp
using Trellis.Pooling;

// Create a pool
var pool = new GameObjectPool(prefab, initialCapacity: 20, parent: poolParent);

// Acquire (position set before activation — no visual pop)
GameObject obj = pool.Acquire(spawnPosition);

// Return to pool
pool.Return(obj);

// Teardown
pool.Clear();
```

### IPoolable (optional)

```csharp
using Trellis.Pooling;

public class MyComponent : MonoBehaviour, IPoolable
{
    public void OnPoolGet()
    {
        gameObject.SetActive(true);
        // Reset state for reuse
    }

    public void OnPoolReturn()
    {
        gameObject.SetActive(false);
    }
}
```

Components implementing `IPoolable` are cached per instance on first `Acquire` — no repeated `GetComponent` calls.
