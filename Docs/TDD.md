# Trellis Framework — Technical Design Document

## 1. Purpose

Trellis is a lightweight, reusable Unity game framework designed to provide the architectural primitives that every Unity project needs but shouldn't have to reinvent. It extracts patterns proven across multiple production projects (CU-Client, LGCore-Client, DTACK-Override) and codifies them as a single framework package.

The framework solves specific, documented pain points (see [CU-Client Pain Points](CU-Client-Pain-Points.md)):
- **Singleton epidemic** → VContainer dependency injection
- **Multiple incompatible event patterns** → Single Event Bus + Reactive Properties
- **Stringly-typed global state** → Typed Store\<T\> with FLUX-inspired unidirectional data flow
- **Boolean flag state management** → Explicit state machines + ILoadObject\<T\>
- **God-object drivers** → Small, focused subsystems composed via DI
- **Rigid HUD architecture** → Independent reactive panels with layout zones
- **No initialization order** → VContainer scope-based boot sequence
- **No structured logging** → Tagged, filterable logging system

### Key Systems

| Tier | Subsystems |
|------|-----------|
| **Tier 1 — Core** | State Machine, Hierarchical State Machine, System Scheduler, Object Pooling, Event Bus, Reactive Properties, State Store |
| **Tier 2 — UI** | UI Router, Panel Manager, Popup System, Toast/Notification |
| **Tier 3 — App Infrastructure** | App Lifecycle, Scene Manager, Structured Logger, Debug Overlay |
| **Tier 4 — Data** | Definition System, Save System, Timers |
| **Tier 5 — Extended** | Audio Manager, Input Abstraction, Localization, Camera System (as needed) |

The first consumer project is **DTACK-Override** (2-player co-op roguelike, Unity 6, VContainer, Netcode for GameObjects, UI Toolkit). **Vineyard** is a secondary future consumer.

---

## 2. Architecture

### Design Principles

- **Lightweight over comprehensive** — Each subsystem solves one problem well. No configuration frameworks, no reflection, no code generation.
- **Plain C# where possible** — State machine, scheduler, event bus, stores, reactive properties, and timers are pure C# with no MonoBehaviour dependency. Only pooling, UI, and scene management require Unity APIs.
- **No LINQ, no allocations on hot paths** — Framework code follows the same performance discipline as game code. See Constraints below.
- **Generic over game-specific** — No assumptions about the consuming project's domain. State machines are parameterized on enums; stores are parameterized on data types; the event bus dispatches any struct event.
- **Composition over inheritance** — Interfaces (`IState`, `ISystem`, `IPoolable`, `IStore`) define contracts. No base classes to inherit from. Abstract base classes acceptable only when they provide genuine shared behavior behind an interface.
- **No game-specific types in framework** — Framework code never references game enums, data structures, or systems. Dependencies flow inward: game code depends on Trellis, never the reverse.
- **VContainer as the spine** — Dependency injection replaces singletons, service locators, and static registries. VContainer is a direct dependency of `Trellis.Runtime`. All framework types are designed for constructor injection.
- **FLUX-inspired data flow** — Unidirectional data flow where applicable. Stores are the single source of truth, modified only through actions. Changes propagate via reactive properties. No bidirectional data binding.

### Assembly Structure

```
Trellis.Runtime.asmdef          → depends on: VContainer
Trellis.Netcode.asmdef          → depends on: Trellis.Runtime, Unity.Netcode.Runtime
Trellis.Tests.Editor.asmdef     → depends on: Trellis.Runtime (pure C# tests, fast)
Trellis.Tests.Netcode.asmdef    → depends on: Trellis.Netcode (Play Mode tests with network)
```

**Rationale:** VContainer is a direct dependency because it replaces singletons as the composition mechanism — every Trellis consumer will use it. Netcode is isolated in a separate assembly because not all consumers need networking, and NGO has significant compile-time and dependency costs.

### VContainer Integration Strategy

VContainer serves three roles in Trellis:

1. **Dependency resolution** — All framework types receive dependencies through constructors. No `static Instance`, no `FindObjectOfType`, no service locators.
2. **Lifetime scoping** — Different `LifetimeScope` subclasses define different application contexts (boot, gameplay, UI). Scope disposal tears down all registrations cleanly.
3. **Initialization sequencing** — `IStartable` and `IAsyncStartable` replace StepFSM/GameLoader patterns. VContainer's resolution order IS the initialization order. Different boot profiles are different scope configurations.

**Boot pattern (Option A):** The consuming project defines `LifetimeScope` subclasses. Trellis provides registerable modules (e.g., `TrellisCoreScopeModule`, `TrellisUIScopeModule`) that consumers install into their scopes. No framework-level boot controller.

```csharp
// Consumer code — not part of Trellis
public class GameplayScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Trellis modules
        builder.Register<EventBus>(Lifetime.Scoped);
        builder.Register<TrellisLogger>(Lifetime.Singleton);

        // Game-specific registrations
        builder.Register<CreepStore>(Lifetime.Scoped);
        builder.Register<SpawnSystem>(Lifetime.Scoped).As<ISystem>();
    }
}
```

### Subsystem Overview

Each subsystem is described below with its purpose, key types, design decisions, and relationship to other subsystems.

---

#### 2.1 State Machine (`Trellis.StateMachine`)

**Status:** Implemented

A trigger-based state machine parameterized on two enum types: `TState` (state identifiers) and `TTrigger` (transition triggers).

**Key types:** `StateMachine<TState, TTrigger>`, `IState`

**Key design decisions:**
- **One pending trigger per tick** — Simplifies reasoning about state transitions. No queue, no priority system. If two triggers fire before resolution, the last one wins (with a warning).
- **Deferred trigger resolution** — Triggers are resolved at the start of `Tick()`, not when `Fire()` is called. This prevents re-entrant state transitions and makes the execution model predictable.
- **Triggers fired during Enter() survive** — The pending trigger is cleared *before* resolution, so a trigger fired during `Enter()` is preserved for the next `Tick()`.
- **Transition table, not per-state handlers** — Transitions are registered externally (`AddTransition`), keeping states unaware of the overall flow graph.
- **Struct + Enum constraints** — `where TState : struct, Enum` and `where TTrigger : struct, Enum` prevent boxing and ensure value-type semantics. Uses `EqualityComparer<T>.Default` for dictionary lookups.

---

#### 2.2 Hierarchical State Machine (`Trellis.StateMachine`)

**Status:** Implemented

Extends the flat state machine with nested sub-states. A parent state can contain its own `StateMachine<TState, TTrigger>` as a child, enabling complex flow control without a single massive state graph.

**Key types:** `HierarchicalStateMachine<TState, TTrigger>`, `IHierarchicalState`

**Key design decisions:**
- **Composition, not inheritance** — `HierarchicalStateMachine` wraps `StateMachine` rather than extending it. A hierarchical state implements `IHierarchicalState` which extends `IState` with a `ChildMachine` property.
- **Tick propagation** — When the parent machine ticks a hierarchical state, the state's `Tick()` also ticks its child machine. Triggers in the child machine are resolved within the child. Only explicit "bubble-up" triggers (via a delegate) can affect the parent machine.
- **Same enum constraints** — Child machines can use different enum types than the parent, enabling localized state/trigger vocabularies per sub-state.
- **Pure C#** — No MonoBehaviour dependency. Same deferred-trigger, transition-table design as the flat machine.

---

#### 2.3 System Scheduler (`Trellis.Scheduling`)

**Status:** Implemented

An ordered array of `ISystem` implementations ticked sequentially each frame.

**Key types:** `SystemScheduler`, `ISystem`

**Key design decisions:**
- **Array-based, not list-based** — Systems are registered at construction time. No runtime add/remove. Deterministic order, no collection mutation during iteration.
- **Null-safe** — Null elements in the array are silently skipped.
- **No phase abstraction** — "Phases" are a convention of the consuming project, not a framework concept. Systems in positions 0-2 might be "Phase 1" by convention.

---

#### 2.4 Object Pooling (`Trellis.Pooling`)

**Status:** Implemented

A stack-based pre-allocated pool for GameObjects with per-instance `IPoolable` component caching.

**Key types:** `GameObjectPool`, `IPoolable`, `PoolManager`

**Key design decisions:**
- **Position before activation** — `Acquire(Vector3 position)` sets transform position before calling `OnPoolGet()`, preventing one-frame visual pop.
- **Cached IPoolable lookup** — `TryGetComponent<IPoolable>` is called once per instance and cached. No per-acquire component lookups.
- **On-demand growth with warning** — If exhausted, new instances are created with `Debug.LogWarning`.
- **PoolManager** — A registry of named pools, keyed by prefab or string ID. Consumers request pools by key rather than managing individual `GameObjectPool` instances. Integrates with VContainer for lifecycle management. Provides `ReturnAll()` for clean teardown on scope disposal.

---

#### 2.5 Event Bus (`Trellis.Events`)

**Status:** Implemented

A typed, zero-allocation (when possible) event bus for decoupled communication between systems.

**Key types:** `EventBus`, `IEventSubscription`

**Key design decisions:**
- **Struct events** — Events are value types. `EventBus.Publish<T>(T evt) where T : struct` dispatches to all subscribers of type `T`. No boxing when the subscriber list is generic.
- **Ordered dispatch** — Subscribers are stored in a `List<Action<T>>`, dispatched in subscription order (FIFO). Not a `HashSet` — ordering is deterministic and matches subscription sequence. This is a deliberate choice informed by the FLUX reference's analysis of race conditions in unordered dispatch.
- **Subscription lifecycle** — `Subscribe<T>(Action<T>)` returns an `IEventSubscription` (disposable). Subscribers are responsible for disposing subscriptions (typically in `OnDestroy` or scope teardown). VContainer scope disposal triggers cleanup.
- **No global static bus** — The `EventBus` is a regular class instance, scoped through VContainer. Different scopes can have different buses (e.g., gameplay bus vs UI bus).
- **Deferred unsubscribe** — Unsubscribing during dispatch is safe. Removals are deferred to after the current dispatch completes, preventing collection-modified-during-iteration errors.
- **No async, no awaitable** — Events are synchronous. Async event handling introduces ordering ambiguity that conflicts with deterministic dispatch.

---

#### 2.6 Reactive Properties (`Trellis.Reactive`)

**Status:** Implemented

Observable value wrappers that notify subscribers when their value changes. The core primitive for FLUX-inspired data binding.

**Key types:** `Observable<T>`, `ReadOnlyObservable<T>`, `IObservable<T>`

**Key design decisions:**
- **Queue-based notification** — When a value changes, notifications are queued and dispatched after the set completes. This prevents cascading re-entrancy where setting A triggers a handler that sets B which triggers a handler that sets A. This is the "Set_Variant" fix documented in the FLUX architecture reference.
- **Equality check before notify** — `Observable<T>.Value` only queues a notification if the new value differs from the current value (using `EqualityComparer<T>.Default`). Prevents spurious notifications.
- **ReadOnlyObservable\<T\>** — A read-only wrapper exposing only `Value` (get) and subscription. Stores expose `ReadOnlyObservable<T>` properties while keeping the writable `Observable<T>` private. This enforces single-writer discipline at the API level.
- **No initial notification on subscribe** — Subscribing does not immediately fire with the current value. The subscriber reads `.Value` if it needs the current state. This avoids "event storm on construction" and keeps behavior predictable.
- **BindTo() extension** — Convenience method for UIToolkit binding: `store.Health.BindTo(label, (l, v) => l.text = v.ToString())`. Returns a disposable subscription.

---

#### 2.7 State Store (`Trellis.Stores`)

**Status:** Implemented

FLUX-inspired typed data stores that serve as the single source of truth for application state.

**Key types:** `Store<T>`, `StoreActions<T>`, `ILoadObject<T>`, `LoadState`

**Key design decisions:**
- **Single-writer principle** — Each `Store<T>` is modified only through its corresponding `StoreActions<T>`. Consumers read from the store; only the designated action class writes to it. This prevents the bidirectional data flow problems seen in CU-Client.
- **Store\<T\> exposes ReadOnlyObservable** — The store's state is an `Observable<T>` internally, exposed as `ReadOnlyObservable<T>` publicly. Subscribers react to changes; they cannot directly mutate the store.
- **ILoadObject\<T\>** — Wraps a value with its loading state: `LoadState` enum (`None`, `Reading`, `Writing`, `Error`) plus an optional error message. This replaces the boolean-flag pattern (`isLoading`, `hasData`, `hasError`) with a single state machine. Consuming projects use `ILoadObject<T>` for any data that involves async operations (network requests, file I/O, scene loads).
- **T is a plain data type** — Store state is a struct or class with no behavior. Systems operate on the data; the store just holds it.
- **Scoped through VContainer** — Stores are registered in the appropriate `LifetimeScope`. Scope disposal clears all subscriptions. No static state.
- **Reset()** — Stores provide a `Reset()` method that restores state to its initial value and notifies subscribers. Used for game restart / session teardown.

```csharp
// Framework types
public class Store<T>
{
    private readonly Observable<T> state;
    public ReadOnlyObservable<T> State { get; }
    // Only StoreActions<T> calls these (enforced by convention, not access modifier)
    internal void SetState(T newState);
    public void Reset();
}

public class StoreActions<T>
{
    private readonly Store<T> store;
    protected void UpdateState(Func<T, T> updater);
    protected void SetState(T newState);
}
```

---

#### 2.8 UI Router (`Trellis.UI`)

**Status:** Implemented (routing logic). UI Toolkit rendering layer pending.

A deep-link-capable router for managing which UI panels are visible, built natively on UI Toolkit.

**Key types:** `UIRouter`, `Route`, `IRoutable`, `LayoutZone`

**Key design decisions:**
- **Named layout zones** — The screen is divided into zones: `Top`, `Bottom`, `Left`, `Right`, `Center`, `Overlay`. Panels register into zones, not into a central controller. Multiple panels can occupy the same zone (stacked). This solves CU-Client's rigid top/bottom-only HUD layout.
- **Route-based navigation** — Routes are string paths (e.g., `/menu`, `/gameplay/inventory`, `/settings/audio`). The router resolves a route to a set of panels to display. Deep linking support means any route can be navigated to directly (e.g., from a push notification or debug command).
- **IRoutable** — Interface for panels that participate in routing. `OnRouteEnter(RouteContext)` and `OnRouteExit()` provide lifecycle hooks.
- **No panel creation** — The router activates/deactivates panels; it does not create them. Panels are pre-registered (via VContainer or manual registration) and the router shows/hides them based on the current route.
- **Route parameters** — Routes support parameters: `/inventory?itemId=42`. `RouteContext` provides parameter access.
- **History stack** — Optional back-navigation support. `Router.Back()` returns to the previous route.

---

#### 2.9 Panel Manager (`Trellis.UI`)

**Status:** Implemented (logic). UI Toolkit rendering layer pending.

Manages the lifecycle and layering of UI Toolkit panels within layout zones.

**Key types:** `PanelManager`, `IPanel`, `PanelDescriptor`

**Key design decisions:**
- **Zone-based registration** — Each panel declares its target zone via `PanelDescriptor`. The panel manager places it within the zone's visual hierarchy.
- **Sort order within zones** — Panels within a zone have a sort order. Higher sort order renders on top.
- **Independent panel lifecycle** — Each panel manages its own data binding (subscribing to stores/observables). No central "update all panels" call. Panels are reactive.
- **Lazy instantiation** — Panel UXML/USS assets are loaded on first show, not at registration time. Reduces startup cost.

---

#### 2.10 Popup System (`Trellis.UI`)

**Status:** Implemented (logic). UI Toolkit rendering layer pending.

Modal and semi-modal popup management with queue support.

**Key types:** `PopupManager`, `IPopup`, `PopupRequest`, `PopupResult`

**Key design decisions:**
- **Queue-based display** — Multiple popup requests are queued. Only one modal popup displays at a time. When dismissed, the next in queue appears.
- **PopupRequest/PopupResult** — Request-response pattern. `PopupManager.Show<T>(PopupRequest)` returns a result (or callback). The popup implementation decides what result to produce (e.g., Confirm/Cancel).
- **Overlay zone** — Popups render in the `Overlay` layout zone, above all other panels.
- **Backdrop/dimming** — Optional configurable backdrop that blocks input to panels behind the popup.
- **Non-modal option** — `PopupRequest.Modal = false` allows the popup to coexist with normal UI interaction.

---

#### 2.11 Toast / Notification (`Trellis.UI`)

**Status:** Implemented (logic). UI Toolkit rendering layer pending.

Transient notification messages that auto-dismiss after a configurable duration.

**Key types:** `ToastManager`, `ToastRequest`

**Key design decisions:**
- **Fire and forget** — `ToastManager.Show(ToastRequest)` displays a notification. No result, no callback.
- **Configurable position and duration** — Toast anchor position (top, bottom, center) and display duration are per-request.
- **Queue with max visible** — Multiple toasts can be visible simultaneously (configurable max). New toasts push older ones up/down.
- **No blocking** — Toasts never block input or interrupt gameplay.

---

#### 2.12 App Lifecycle (`Trellis.App`)

**Status:** Implemented (pure C# core). MonoBehaviour callback wrapper pending.

Application-level lifecycle events and state management.

**Key types:** `AppLifecycleManager`, `AppState`, `IAppLifecycleAware`, `AppPausedEvent`, `AppResumedEvent`, `AppFocusLostEvent`, `AppFocusGainedEvent`, `AppQuittingEvent`

**Key design decisions:**
- **Pure C# core with thin MonoBehaviour wrapper** — `AppLifecycleManager` is a plain C# class that receives notifications via `NotifyPause(bool)`, `NotifyFocus(bool)`, `NotifyQuit()`. A separate MonoBehaviour (consumer-provided) calls these from Unity callbacks.
- **Dual notification** — State changes are surfaced both as typed struct events on the `EventBus` AND as `Observable<AppState>`. Consumers choose their preferred pattern.
- **IAppLifecycleAware** — Interface for systems that need pause/resume hooks. Systems register/unregister with the manager.
- **AppState enum** — `Active`, `Paused`, `Unfocused`, `Quitting`. Focus loss while paused does not change state (pause takes priority).
- **Disposable** — `IDisposable` implementation clears registered systems and stops event dispatch.

---

#### 2.13 Scene Manager (`Trellis.Scenes`)

**Status:** Implemented (pure C# core). Unity SceneManager provider pending.

Scene loading with VContainer scope management and loading state tracking.

**Key types:** `SceneLoader`, `SceneTransition`, `ISceneLoadHandler`, `ISceneLoadProvider`, `SceneLoadedEvent`, `SceneUnloadedEvent`

**Key design decisions:**
- **Pure C# core with provider interface** — `SceneLoader` orchestrates transitions. Actual scene loading is delegated to `ISceneLoadProvider`, which wraps Unity's `SceneManager.LoadSceneAsync`. This makes the orchestration logic fully testable.
- **Additive loading by default** — Scenes load additively. The "base" scene persists. Gameplay scenes load/unload on top.
- **Observable progress** — Load progress tracked as `Observable<float>` (0.0 to 1.0) via `ReadOnlyObservable<float> LoadProgress`. Multi-scene transitions weight progress across scenes.
- **Transition support** — `SceneTransition` describes the load sequence: scenes to unload then scenes to load. Static factory methods `Load()` and `Switch()` for common patterns.
- **Handler interface** — `ISceneLoadHandler` receives `OnSceneLoaded` and `OnSceneUnloading` callbacks. Registered handlers are notified in addition to EventBus events.
- **Guard against concurrent loads** — `LoadScene` and `ExecuteTransition` throw if a load is already in progress. One transition at a time.

---

#### 2.14 Structured Logger (`Trellis.Logging`)

**Status:** Implemented

A tagged, filterable logging system that replaces raw `Debug.Log` usage.

**Key types:** `TrellisLogger`, `LogTag`, `LogLevel`, `ILogSink`

**Key design decisions:**
- **Tagged logs** — Every log call includes one or more tags (e.g., `LogTag.Network`, `LogTag.UI`, `LogTag.Pooling`). Tags are enum-based for zero-allocation filtering.
- **Runtime filtering** — Filters can be set per-tag and per-level at runtime. `TrellisLogger.SetFilter(LogTag.Network, LogLevel.Warning)` suppresses Network logs below Warning. Filtering happens BEFORE string formatting — when a log is filtered out, no string allocation occurs.
- **LogLevel hierarchy** — `Trace`, `Debug`, `Info`, `Warning`, `Error`. Default level is `Info` in builds, `Debug` in editor.
- **ILogSink** — Output destination interface. Default sink writes to `Debug.Log`/`LogWarning`/`LogError`. Additional sinks can write to file, remote server, or in-game debug overlay. Multiple sinks supported simultaneously.
- **Zero-allocation when filtered** — The API uses a check-then-format pattern: `if (logger.IsEnabled(tag, level)) logger.Log(tag, level, $"message {value}")`. The interpolated string is never allocated if the check fails. Convenience methods (`logger.Info(tag, message)`) wrap this pattern.
- **Scoped through VContainer** — `TrellisLogger` is a regular class, typically registered as singleton. No static access.
- **Custom tags** — Consuming projects define their own `LogTag` enum values. Framework provides built-in tags for its own subsystems.

---

#### 2.15 Debug Overlay (`Trellis.Debug`)

**Status:** Implemented (logic). UI Toolkit rendering layer pending.

An in-game debug panel for runtime inspection and control.

**Key types:** `DebugOverlay`, `IDebugSection`, `DebugCommand`

**Key design decisions:**
- **UI Toolkit based** — Renders as an overlay panel, toggled via configurable key binding (default: backtick/tilde).
- **Section-based** — Systems register `IDebugSection` implementations that provide debug UI. Sections are tabbed or collapsible.
- **Built-in sections** — Logger filter controls, pool statistics, event bus subscription count, store state inspector, FPS/memory counters.
- **Debug commands** — Text commands (e.g., `set gold 999`, `goto /menu/settings`) for quick debugging. Extensible via `DebugCommand` registration.
- **Development-only** — The debug overlay should be strippable from release builds via assembly definition constraints or scripting defines.

---

#### 2.16 Definition System (`Trellis.Data`)

**Status:** Implemented

A type-safe registry for ScriptableObject-based game definitions (items, characters, abilities, etc.).

**Key types:** `DefinitionRegistry<TKey, TDef>`, `DefinitionRegistryBuilder<TKey, TDef>`, `IDefinitionSource<TDef>`

**Key design decisions:**
- **Generic registry** — `DefinitionRegistry<TKey, TDef>` maps a key (typically an enum or string ID) to a definition struct/class. Definitions are immutable after loading.
- **IDefinitionSource\<TDef\>** — Interface for loading definitions from different sources (ScriptableObjects, Addressables, JSON). The registry doesn't care where data comes from. Source populates a `List<TDef>` — no IEnumerable.
- **Builder pattern** — `DefinitionRegistryBuilder<TKey, TDef>` takes a key-extraction function (`Func<TDef, TKey>`), validates uniqueness, and produces the immutable registry. Builder is single-use (cannot build twice).
- **Dictionary-based lookup** — `TryGet(TKey key, out TDef definition)` pattern. O(1) lookup by key. Also `Get()` (throws), `Contains()`, `CopyKeysTo()`, `CopyValuesTo()`.
- **Built once at bootstrap** — Like MorlocsTowerDefense's `TurretTypeDirectoryBuilder` pattern. A builder validates and constructs the immutable registry. Runtime code only reads.
- **No Addressables dependency** — The definition system provides interfaces. Addressables loading is implemented by the consuming project via `IDefinitionSource<TDef>`.

---

#### 2.17 Save System (`Trellis.Data`)

**Status:** Implemented

Serialization and persistence of game state.

**Key types:** `SaveManager`, `ISaveSerializer`, `JsonSaveSerializer`, `SaveSlot`, `ISaveable`, `ISaveStorage`

**Key design decisions:**
- **Slot-based** — Multiple save slots supported. Each slot is a named container mapping save keys to serialized byte arrays.
- **ISaveSerializer** — Pluggable serialization. Default: `JsonSaveSerializer` using `JsonUtility`. Consumers can provide MessagePack, binary, or custom serializers.
- **ISaveStorage** — Pluggable storage backend abstraction (`Exists`, `Write`, `Read`, `Delete`). Default implementation uses file system. Consumers provide platform-specific implementations (cloud saves, etc.).
- **ISaveable** — Interface for stores/systems that participate in save/load. Declares a `SaveKey`, `CaptureState()`, and `RestoreState(object)`. SaveManager registers saveables and coordinates bulk save/load.
- **Synchronous core** — Save/load operations are synchronous at the core level. Async wrappers can be layered by consuming projects or via future `ILoadObject<T>` integration.
- **Platform-agnostic** — Storage is behind `ISaveStorage` interface. No direct `Application.persistentDataPath` dependency in the framework.

---

#### 2.18 Timers (`Trellis.Timing`)

**Status:** Implemented

Lightweight timer utilities for delayed and repeating actions.

**Key types:** `Timer`, `TimerManager`, `ITimerHandle`

**Key design decisions:**
- **Pooled timers** — Timer objects are pooled internally. Creating and canceling timers does not allocate.
- **Tick-driven** — `TimerManager` is an `ISystem` that ticks all active timers. No coroutines, no `Invoke`.
- **Handle-based API** — `TimerManager.Schedule(float delay, Action callback)` returns an `ITimerHandle` for cancellation. Handles can be stored and cancelled from anywhere.
- **Repeating timers** — `TimerManager.ScheduleRepeating(float interval, Action callback)` for periodic execution.
- **Pure C#** — No MonoBehaviour dependency. Timers run within the system scheduler's tick.

---

### Data Flow Architecture

Trellis follows a FLUX-inspired unidirectional data flow pattern:

```
User Input / System Event
        │
        ▼
  StoreActions<T>        ← Only writer
        │
        ▼
    Store<T>             ← Single source of truth
        │
        ▼
  Observable<T>          ← Change notification (queue-based)
        │
        ▼
  UI Panels / Systems    ← Reactive consumers (read-only)
```

**Rules:**
1. Stores are the single source of truth for their data domain.
2. Only the corresponding `StoreActions<T>` may write to a store.
3. Changes propagate via `Observable<T>` — never by polling.
4. UI panels bind to observables and re-render on change. They never write directly to stores.
5. User actions produce events (via EventBus) or call StoreActions methods — never mutate stores directly.

### Event Architecture

Two complementary event mechanisms serve different needs:

| Mechanism | Use Case | Characteristics |
|-----------|----------|----------------|
| **EventBus** | Decoupled system-to-system communication | Fire-and-forget, struct events, ordered dispatch, no return value |
| **Observable\<T\>** | State-change notification, UI binding | Value-based, equality-checked, queue-based notification, subscription-based |

**When to use which:**
- "Something happened" (creep killed, wave started, button clicked) → EventBus
- "This value changed" (health updated, coins changed, selection changed) → Observable\<T\>

### UI Architecture

```
Screen Layout:
┌─────────────────────────────────────┐
│              Top Zone               │
├───────┬─────────────────┬───────────┤
│       │                 │           │
│ Left  │     Center      │   Right   │
│ Zone  │      Zone       │   Zone    │
│       │                 │           │
├───────┴─────────────────┴───────────┤
│            Bottom Zone              │
└─────────────────────────────────────┘
         ┌───────────────┐
         │  Overlay Zone │  (popups, toasts, debug)
         │  (above all)  │
         └───────────────┘
```

- **Each panel is independent** — Binds to its own observables, manages its own lifecycle.
- **No central HUD controller** — Panels register into zones. The PanelManager handles layering.
- **UI Toolkit native** — All framework UI uses UXML/USS. No UGUI dependency in framework code.
- **UIRouter drives visibility** — Routes determine which panels are shown. Panels implement `IRoutable` for route lifecycle.

### Boot & Initialization

```
Unity Scene Load
      │
      ▼
RootLifetimeScope (MonoBehaviour)
      │
      ▼
VContainer resolves Trellis singletons
(TrellisLogger, AppLifecycleManager)
      │
      ▼
IStartable / IAsyncStartable implementations run
(ordered by VContainer registration order)
      │
      ▼
Child LifetimeScope (e.g., GameplayScope)
      │
      ▼
VContainer resolves gameplay registrations
(Stores, Systems, EventBus, UI panels)
      │
      ▼
Application running
```

Different boot profiles = different `LifetimeScope` subclasses. Skip the title screen for testing? Use a `TestGameplayScope` that doesn't register title screen systems. No StepFSM, no GameLoader, no boot state machine.

---

## 3. Constraints

- **No LINQ in runtime code** — `System.Linq` causes hidden allocations. Use explicit loops.
- **No GetComponent in hot paths** — Cache component references. Never call `GetComponent` per-frame.
- **No MonoBehaviour inheritance for pure logic** — State machines, schedulers, stores, events, timers, and their interfaces are plain C#. Only pooling, UI, and scene management touch Unity APIs.
- **No game-specific types** — Framework code never references game enums, data structures, or systems.
- **Struct constraints on generics** — State machine type parameters constrain to `struct, Enum`. Store and observable type parameters are unconstrained but documented usage favors value types on hot paths.
- **No static mutable state** — No `static Instance`, no static event buses, no static registries. Everything flows through VContainer.
- **No per-frame allocations in framework code** — Pre-allocate collections, pool delegates where needed, use structs for event data.
- **VContainer is required** — Trellis.Runtime has a hard dependency on VContainer. This is intentional — DI is the composition spine, not an optional feature.

---

## 4. Package Structure

```
Trellis/
├── package.json                          # com.trellis.framework
├── Runtime/
│   ├── Trellis.Runtime.asmdef            # depends on: VContainer
│   ├── StateMachine/
│   │   ├── IState.cs
│   │   ├── StateMachine.cs
│   │   ├── IHierarchicalState.cs
│   │   └── HierarchicalStateMachine.cs
│   ├── Scheduling/
│   │   ├── ISystem.cs
│   │   └── SystemScheduler.cs
│   ├── Pooling/
│   │   ├── IPoolable.cs
│   │   ├── GameObjectPool.cs
│   │   └── PoolManager.cs
│   ├── Events/
│   │   ├── EventBus.cs
│   │   └── IEventSubscription.cs
│   ├── Reactive/
│   │   ├── Observable.cs
│   │   ├── ReadOnlyObservable.cs
│   │   └── IObservable.cs
│   ├── Stores/
│   │   ├── Store.cs
│   │   ├── StoreActions.cs
│   │   ├── ILoadObject.cs
│   │   └── LoadState.cs
│   ├── UI/
│   │   ├── UIRouter.cs
│   │   ├── Route.cs
│   │   ├── IRoutable.cs
│   │   ├── LayoutZone.cs
│   │   ├── PanelManager.cs
│   │   ├── IPanel.cs
│   │   ├── PanelDescriptor.cs
│   │   ├── PopupManager.cs
│   │   ├── IPopup.cs
│   │   ├── PopupRequest.cs
│   │   ├── ToastManager.cs
│   │   └── ToastRequest.cs
│   ├── App/
│   │   ├── AppLifecycleManager.cs
│   │   ├── AppState.cs
│   │   └── IAppLifecycleAware.cs
│   ├── Scenes/
│   │   ├── SceneLoader.cs
│   │   ├── SceneTransition.cs
│   │   ├── ISceneLoadHandler.cs
│   │   └── ISceneLoadProvider.cs
│   ├── Logging/
│   │   ├── TrellisLogger.cs
│   │   ├── LogTag.cs
│   │   ├── LogLevel.cs
│   │   └── ILogSink.cs
│   ├── Debug/
│   │   ├── DebugOverlay.cs
│   │   ├── IDebugSection.cs
│   │   └── DebugCommand.cs
│   ├── Data/
│   │   ├── DefinitionRegistry.cs
│   │   ├── IDefinitionSource.cs
│   │   ├── SaveManager.cs
│   │   ├── ISaveSerializer.cs
│   │   ├── JsonSaveSerializer.cs
│   │   ├── ISaveStorage.cs
│   │   ├── ISaveable.cs
│   │   └── SaveSlot.cs
│   └── Timing/
│       ├── Timer.cs
│       ├── TimerManager.cs
│       └── ITimerHandle.cs
├── Netcode/
│   ├── Trellis.Netcode.asmdef            # depends on: Trellis.Runtime, Unity.Netcode.Runtime
│   └── (future: sparse overlay for server/client truth reconciliation, network-aware stores, synced state)
└── Tests/
    ├── Editor/
    │   ├── Trellis.Tests.Editor.asmdef    # depends on: Trellis.Runtime
    │   ├── StateMachineTests.cs
    │   ├── SystemSchedulerTests.cs
    │   ├── EventBusTests.cs
    │   ├── ObservableTests.cs
    │   ├── StoreTests.cs
    │   ├── TimerTests.cs
    │   └── ... (per-subsystem test files)
    └── Netcode/
        └── Trellis.Tests.Netcode.asmdef   # depends on: Trellis.Netcode (Play Mode)
```

---

## 5. Testing Strategy

Tests are required for every deliverable. Tests must be written alongside feature implementation, not deferred.

### Test Types

- **Edit Mode Tests** (`Trellis/Tests/Editor/`): Unit tests for pure logic. State machine, scheduler, event bus, reactive properties, stores, timers, definition registry, logger. These are the majority of tests.
- **Play Mode Tests** (future, `Trellis/Tests/Runtime/`): Integration tests requiring MonoBehaviour lifecycle. Pool tests, UI tests, scene loading tests.

### Per-Deliverable Requirements

Each deliverable must include:
1. **Happy-path tests** verifying acceptance criteria
2. **Edge case analysis** documented in test files or as comments
3. **Coverage review** before sign-off — all acceptance criteria have corresponding test assertions
4. **Trellis-Starter demo** — A working demo scene in the Trellis-Starter project that exercises the deliverable's framework code in a tangible, runnable context. The demo serves as integration validation, API design proof, and consumer reference code. A deliverable is not complete until its demo runs.

### Conventions

- Test class naming: `[TypeName]Tests` (e.g., `EventBusTests`, `ObservableTests`)
- Test method naming: `MethodOrBehavior_Condition_ExpectedResult`
- Framework tests define their own test enums/stubs — no dependency on any game project
- Use `[SetUp]` / `[TearDown]` for shared fixtures
- Use `Assert` (NUnit) for assertions

---

## 6. Deliverables

Implementation order builds subsystems incrementally. Each deliverable produces **both framework code AND a Trellis-Starter demo** that exercises it in a tangible, runnable context. The demo validates API design, proves integration works, and provides consumer reference code. A deliverable is not complete until its demo runs.

Dependencies flow forward — later deliverables build on earlier ones.

---

### Future: Sparse Overlay for Networked State

**Status:** TODO — Design and implement as part of `Trellis.Netcode`

In a networked game (e.g., DTACK-Override with Netcode for GameObjects), each client must reconcile **server-authoritative truth** with **client-predicted/local truth**. The planned approach is the **sparse overlay** pattern:

- **Server truth** lives in the canonical `Store<T>` instances, updated via network sync.
- **Client truth** (predictions, optimistic updates, local-only state) lives in a thin overlay layer that sits on top of server truth.
- **Reads fall through** — when the overlay has no entry for a given key/field, the read returns the server value. When the overlay has a local override, the local value is returned instead.
- **Reconciliation** — when the server confirms or rejects a prediction, the overlay entry is removed and the store's server value takes over.

This avoids the alternatives of wrapping every model with server/client pairs or duplicating the entire store layer. The overlay is sparse: only the fields that are currently predicted or locally overridden exist in it, keeping memory and complexity proportional to the amount of active prediction rather than the total state size.

**Key design questions to resolve:**
- Granularity: per-store overlay vs per-field overlay vs per-key overlay
- Reconciliation strategy: timestamp-based, sequence-number-based, or server-ack-based
- Integration with `Observable<T>` — should overlay changes trigger the same notification path?
- Whether the overlay is visible to UI (reads always go through overlay) or only to systems

---

### Deliverable 1: VContainer Integration & Assembly Restructure

**Demo: Traffic Light** — A traffic light controlled by StateMachine + SystemScheduler, wired entirely through VContainer. Demonstrates DI-based composition root, scope configuration, and constructor injection of framework types.

> As a framework consumer, Trellis integrates with VContainer so I can use dependency injection instead of singletons.

**Framework Acceptance Criteria:**
- `Trellis.Runtime.asmdef` declares a dependency on VContainer
- `package.json` updated with VContainer as a dependency
- Existing subsystems (StateMachine, SystemScheduler, GameObjectPool) remain functional — no regressions
- A test demonstrates VContainer resolving a Trellis type
- Assembly structure supports the four-assembly plan (Runtime, Netcode, Tests.Editor, Tests.Netcode)
- All existing tests pass

**Demo Acceptance Criteria:**
- Trellis-Starter scene with a traffic light that cycles Green → Yellow → Red via `StateMachine<TState, TTrigger>`
- `SystemScheduler` ticks the state machine
- All wiring done through a `LifetimeScope` subclass — no `new` in MonoBehaviour code, no singletons
- Visual feedback (colored light changes) driven by state machine `OnStateChanged` event

---

### Deliverable 2: Structured Logger

**Demo: Boot Profiles** — Two VContainer boot configurations (Normal and Verbose) that produce different logging output. Demonstrates tag filtering, multiple sinks, and runtime filter adjustment.

> As a framework consumer, I have a tagged logging system so I can filter log output by subsystem and severity.

**Framework Acceptance Criteria:**
- `TrellisLogger` class with `LogLevel` (Trace, Debug, Info, Warning, Error) and `LogTag` support
- Runtime filtering by tag and level — changing a filter immediately affects log output
- Zero string allocation when a log call is filtered out (check-before-format pattern)
- `ILogSink` interface with a default sink that writes to `Debug.Log`/`LogWarning`/`LogError`
- Multiple sinks can be registered simultaneously
- Logger is injectable via VContainer (not static)
- Tests verify filtering, multi-sink dispatch, and zero-allocation behavior

**Demo Acceptance Criteria:**
- Trellis-Starter scene with two boot profiles selectable at startup (Normal: Info+, Verbose: Trace+)
- Multiple systems log with different tags (e.g., `LogTag.Core`, `LogTag.UI`)
- On-screen UI Toolkit panel displays logged messages with tag/level coloring
- Runtime toggle buttons adjust log filters and immediately change visible output

**Depends on:** Deliverable 1 (VContainer)

---

### Deliverable 3: Event Bus + Reactive Properties

**Demo: Signal Board** — A control panel with buttons that publish typed events and reactive labels that update in response. Demonstrates EventBus for fire-and-forget events and Observable\<T\> for state-change binding.

> As a framework consumer, I have a typed event bus for decoupled communication and observable value wrappers for state-change notification.

**Framework Acceptance Criteria (Event Bus):**
- `EventBus` class with `Publish<T>(T)` and `Subscribe<T>(Action<T>)` where T is a struct
- Subscription returns `IEventSubscription` (disposable) for cleanup
- Dispatch order matches subscription order (FIFO)
- Unsubscribing during dispatch is safe (deferred removal)
- Publishing an event with no subscribers is a no-op (no allocation, no error)
- EventBus is injectable via VContainer

**Framework Acceptance Criteria (Reactive Properties):**
- `Observable<T>` with `Value` property that notifies subscribers on change
- `ReadOnlyObservable<T>` exposing read-only access and subscription
- Queue-based notification prevents cascading re-entrancy
- Equality check prevents notification when value hasn't changed
- Subscribing does NOT fire with the current value
- `BindTo()` convenience method for UIToolkit elements (returns disposable)

**Tests:** Verify dispatch order, deferred unsubscribe, zero-subscriber publish, multi-type isolation, notification behavior, re-entrancy safety, equality suppression, and subscription lifecycle.

**Demo Acceptance Criteria:**
- Trellis-Starter scene with a UI Toolkit panel of buttons (e.g., "Damage", "Heal", "Add Gold")
- Buttons publish struct events via EventBus (e.g., `DamageEvent { int amount }`)
- A health `Observable<int>` and gold `Observable<int>` are bound to UI labels via `BindTo()`
- Event log panel shows all published events in order
- Demonstrates both mechanisms working together: events trigger actions, observables update UI

**Depends on:** Deliverable 1 (VContainer)

---

### Deliverable 4: State Store

**Demo: Inventory Store** — An inventory management screen demonstrating FLUX data flow: StoreActions modify an InventoryStore, Observable properties drive the UI, and ILoadObject tracks a simulated async load.

> As a framework consumer, I have FLUX-inspired typed stores for managing application state with single-writer discipline.

**Framework Acceptance Criteria:**
- `Store<T>` holds state as `Observable<T>`, exposes `ReadOnlyObservable<T>` publicly
- `StoreActions<T>` provides `UpdateState(Func<T, T>)` and `SetState(T)` for mutation
- `ILoadObject<T>` wraps a value with `LoadState` (None, Reading, Writing, Error) and optional error message
- `Store.Reset()` restores initial state and notifies subscribers
- Stores are injectable via VContainer
- Tests verify single-writer pattern, reset behavior, ILoadObject state transitions, and Observable integration

**Demo Acceptance Criteria:**
- Trellis-Starter scene with an inventory UI: item list, coin balance, add/remove buttons
- `InventoryStore` holds inventory state; `InventoryActions` is the single writer
- UI panels bind to `ReadOnlyObservable` properties — no direct store mutation from UI
- A "Load Inventory" button demonstrates `ILoadObject<T>` state transitions (None → Reading → None/Error) with loading spinner
- "Reset" button calls `Store.Reset()` and UI immediately reflects initial state

**Depends on:** Deliverable 3 (Event Bus, Reactive Properties)

---

### Deliverable 5: Hierarchical State Machine

**Demo: Character Controller** — A character with top-level states (Idle, Moving, Combat) where Combat contains nested sub-states (Aiming, Firing, Reloading). Demonstrates parent-child tick propagation and trigger isolation.

> As a framework consumer, I can model complex state graphs using nested sub-state machines.

**Framework Acceptance Criteria:**
- `HierarchicalStateMachine<TState, TTrigger>` supports states that contain child state machines
- `IHierarchicalState` extends `IState` with child machine access
- Parent `Tick()` propagates to active child machine
- Child machine triggers do not automatically bubble to parent — explicit bubble-up required via delegate
- Child machines can use different enum types than parent
- Existing flat `StateMachine` tests still pass (no regressions)
- Tests verify parent-child tick propagation, trigger isolation, and bubble-up mechanism

**Demo Acceptance Criteria:**
- Trellis-Starter scene with a character representation (can be simple shapes/sprites)
- Top-level states: Idle, Moving, Combat (flat `StateMachine` for outer states)
- Combat state contains a child machine: Aiming → Firing → Reloading → Aiming
- UI panel shows current parent state AND current child state
- Buttons trigger parent-level transitions (Enter Combat, Exit Combat) and child-level transitions (Fire, Reload)
- Demonstrates that child triggers (Fire) don't affect the parent machine

---

### Deliverable 6: Timers & Pool Manager

**Demo: Bullet Pool** — Spawners fire pooled projectiles on timer intervals. Demonstrates TimerManager scheduling, PoolManager multi-pool registry, and pool statistics.

> As a framework consumer, I have timer utilities and a multi-pool registry for common gameplay patterns.

**Framework Acceptance Criteria:**
- `Timer` and `TimerManager` (implements `ISystem`) for delayed and repeating callbacks
- `TimerManager.Schedule(delay, callback)` returns `ITimerHandle` for cancellation
- `TimerManager.ScheduleRepeating(interval, callback)` for periodic execution
- Timer objects are pooled — create/cancel does not allocate
- `PoolManager` provides a named registry of `GameObjectPool` instances keyed by prefab or string ID
- `PoolManager.ReturnAll()` returns all active objects across all pools
- Existing `GameObjectPool` tests pass (no regressions)
- Tests verify timer scheduling, cancellation, repeating timers, pool registration, and ReturnAll

**Demo Acceptance Criteria:**
- Trellis-Starter scene with 2-3 spawner positions firing projectiles at repeating intervals
- Projectiles are pooled via `PoolManager` (different prefab types = different pools)
- `TimerManager` drives spawn cadence — UI sliders adjust timer intervals in real time
- On-screen pool statistics panel shows: active count, available count, total created per pool
- "Return All" button calls `PoolManager.ReturnAll()` and visually clears all projectiles
- "Cancel Timers" button stops all spawning

---

### Deliverable 7: UI Framework — Router & Panel Manager

**Demo: App Shell** — A multi-screen application with navigation between Home, Settings, and Profile screens. Demonstrates route-based navigation, layout zones, deep linking, and back navigation.

> As a framework consumer, I have a route-based UI system with layout zones for organizing panels.

**Framework Acceptance Criteria:**
- `UIRouter` resolves string routes to panel configurations
- Routes support parameters (`/inventory?itemId=42`) via `RouteContext`
- `LayoutZone` enum defines screen regions (Top, Bottom, Left, Right, Center, Overlay)
- `PanelManager` places panels in zones with sort-order layering
- `IPanel` and `IRoutable` interfaces define panel contracts
- Route history stack with `Router.Back()` support
- Deep linking: any registered route is directly navigable
- Router and PanelManager are injectable via VContainer
- Tests verify route resolution, parameter parsing, zone assignment, back navigation, and panel lifecycle

**Demo Acceptance Criteria:**
- Trellis-Starter scene with a multi-screen app: Home (`/home`), Settings (`/settings`), Profile (`/profile`)
- Navigation bar in Top zone with route buttons
- Content panels in Center zone swap based on current route
- Status bar in Bottom zone persists across all routes
- Settings screen has sub-routes (`/settings/audio`, `/settings/display`) demonstrating nested routing
- Back button navigates route history
- Deep link text field: type a route path and navigate directly to it
- Route parameters demonstrated: `/profile?userId=42` displays the user ID

**Depends on:** Deliverable 3 (Reactive Properties for panel binding)

---

### Deliverable 8: Popup & Toast Systems

**Demo: Extend App Shell** — Adds modal confirmation dialogs and toast notifications to the App Shell demo. Demonstrates popup queuing, modal backdrop, toast positioning, and auto-dismiss.

> As a framework consumer, I have popup and toast systems for modal dialogs and transient notifications.

**Framework Acceptance Criteria:**
- `PopupManager` with queue-based display — one modal popup at a time
- `PopupRequest` / `PopupResult` request-response pattern
- Popups render in the Overlay zone above all panels
- Optional backdrop/dimming for modal popups
- `ToastManager` with fire-and-forget `Show(ToastRequest)`
- Configurable toast position, duration, and max visible count
- Toasts never block input
- Tests verify popup queuing, modal behavior, toast auto-dismiss, and max visible enforcement

**Demo Acceptance Criteria:**
- App Shell demo extended with:
  - "Delete Account" button that shows a modal confirmation popup with Confirm/Cancel
  - "Save Settings" action that shows a success toast ("Settings saved")
  - "Trigger 5 Toasts" button demonstrating queue behavior and max visible limit
  - Multiple popup buttons demonstrating queue (second popup waits for first to dismiss)
  - Toast position toggle (top vs bottom)

**Depends on:** Deliverable 7 (Panel Manager, Layout Zones)

---

### Deliverable 9: App Lifecycle & Scene Manager

**Demo: Extend App Shell** — Adds scene transitions and pause/resume behavior to the App Shell demo. Home, Settings, and a Gameplay scene load/unload additively with progress tracking.

> As a framework consumer, I have application lifecycle management and scene loading with VContainer scope support.

**Framework Acceptance Criteria:**
- `AppLifecycleManager` surfaces pause/focus/quit as EventBus events and Observable state
- `IAppLifecycleAware` interface for systems that need pause/resume hooks
- `SceneLoader` supports additive scene loading with progress tracking via `ILoadObject<float>`
- Each loaded scene can have its own `LifetimeScope` (child of root scope)
- Unloading a scene disposes its VContainer scope
- `SceneTransition` describes load/unload sequences
- Tests verify lifecycle event dispatch, scene scope creation/disposal, and progress tracking

**Demo Acceptance Criteria:**
- App Shell demo extended with:
  - "Play" button loads a Gameplay scene additively (with loading progress bar via `ILoadObject<float>`)
  - Gameplay scene has its own `LifetimeScope` with scene-specific systems
  - "Back to Menu" unloads Gameplay scene and disposes its scope
  - Pause overlay appears when application loses focus (via `AppLifecycleManager`)
  - Event log shows lifecycle events (SceneLoaded, SceneUnloaded, AppPaused, AppResumed)

**Depends on:** Deliverable 3 (Event Bus), Deliverable 4 (ILoadObject)

---

### Deliverable 10: Definition & Save Systems

**Demo: Config Explorer** — Browse game definitions (items, characters) loaded from ScriptableObjects, edit values, and save/load state across sessions. Demonstrates DefinitionRegistry, SaveManager, and store snapshot/restore.

> As a framework consumer, I have a type-safe definition registry and a save/load system for persisting game state.

**Framework Acceptance Criteria:**
- `DefinitionRegistry<TKey, TDef>` maps keys to immutable definition data
- `IDefinitionSource<TDef>` interface for pluggable data loading (SO, Addressables, JSON)
- `TryGet(key, out def)` O(1) lookup
- `SaveManager` with slot-based persistence
- `ISaveSerializer` interface with default JSON implementation
- Async save/load with `ILoadObject<T>` progress tracking
- Store integration: stores implementing `ISaveable<T>` can be snapshotted and restored
- Tests verify registry lookup, save/load round-trip, slot management, and store snapshot/restore

**Demo Acceptance Criteria:**
- Trellis-Starter scene with a definition browser: list of items/characters loaded from ScriptableObjects
- Selecting a definition displays its fields in a detail panel
- An editable "player state" section (inventory, gold) backed by a `Store<T>`
- "Save to Slot 1/2/3" and "Load from Slot 1/2/3" buttons demonstrating slot-based persistence
- Save/load progress shown via `ILoadObject<T>` status
- "Reset" button restores defaults, demonstrating `Store.Reset()` + `SaveManager` interplay

**Depends on:** Deliverable 4 (Store, ILoadObject)

---

### Deliverable 11: Debug Overlay

**Demo: Extend Boot Profiles** — Adds the debug overlay to the Boot Profiles demo with live log filtering, pool stats, event bus inspection, and debug commands.

> As a framework consumer, I have an in-game debug panel for runtime inspection and control.

**Framework Acceptance Criteria:**
- `DebugOverlay` UI Toolkit panel, toggled via configurable key
- `IDebugSection` interface for registering debug UI sections
- Built-in sections: logger filter controls, pool stats, event bus subscriptions, FPS/memory
- `DebugCommand` registration for text-based commands
- Debug overlay is strippable from release builds
- Tests verify section registration, command parsing, and overlay toggle

**Demo Acceptance Criteria:**
- Boot Profiles demo extended with:
  - Backtick key toggles debug overlay
  - Logger section: live log stream with tag/level filter toggles
  - Pool section: shows all registered pools with active/available counts (if pools are active in the scene)
  - FPS/memory section: frames per second and managed heap size
  - Command input: type `set loglevel Core Trace` to change log filter at runtime
  - Overlay renders above all other UI and does not interfere with demo interaction

**Depends on:** Deliverable 2 (Logger), Deliverable 3 (Reactive Properties)

---

### Deliverable 12: Kitchen Sink

**Demo: Kitchen Sink** — All Trellis subsystems integrated in a single demo scene. No new framework code — this deliverable validates that all subsystems compose correctly.

> As a framework consumer, I can see all Trellis subsystems working together in one application.

**Acceptance Criteria:**
- Single Trellis-Starter scene combining elements from all previous demos
- VContainer wires everything: logger, event bus, stores, state machine, timers, pools, UI router, popups, toasts, scene loading, definitions, save system, debug overlay
- A mini-application flow: boot → menu → gameplay (with pooled objects, timers, store-driven UI) → save → load → reset
- Debug overlay accessible throughout
- No regressions in any framework subsystem
- Serves as the comprehensive integration test and consumer reference

**Depends on:** All previous deliverables
