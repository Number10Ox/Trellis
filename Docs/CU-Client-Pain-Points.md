# CU-Client Architecture Pain Points

This document catalogs architectural issues discovered in the CU-Client codebase. These pain points directly informed Trellis's design — each subsystem exists in part to prevent one or more of these problems from recurring.

---

## 1. Singleton Epidemic

**117+ `Instance` references** in the HUD subsystem alone. Nearly every system, controller, and manager is accessed via static `Instance` properties.

**Why it hurts:**
- Untestable — can't substitute fakes without reflection hacks
- Implicit coupling — any class can reach any other class at any time
- Initialization order is undefined — race conditions on who constructs first
- Prevents multiple instances (e.g., two players, multiple scenes)

**Trellis answer:** VContainer dependency injection. Zero `static Instance` properties anywhere in the framework. All dependencies are constructor-injected or scope-resolved.

---

## 2. Three Coexisting Event Patterns

The codebase uses three incompatible event systems simultaneously:

1. **Static events** in `HudEvents.cs` — global, no lifecycle management
2. **Instance events** on controllers — per-object, but accessed through singletons anyway
3. **`GameContextSystem.OnContextChanged`** — fires a single event for ALL context changes, requiring receivers to filter by type

**Why it hurts:**
- No single pattern to learn or enforce
- Static events are never unsubscribed — memory leaks and ghost handlers
- `OnContextChanged` is a firehose — every listener gets every change, leading to unnecessary work and subtle bugs when filtering logic is wrong

**Trellis answer:** A single `EventBus` with typed events, ordered dispatch, and explicit subscribe/unsubscribe lifecycle. Reactive `Observable<T>` properties for state-change notification that target specific values, not a global firehose.

---

## 3. Stringly-Typed Pseudo-Store

`GameContextSystem` uses `StaticTypeLookup<T>` — a pattern based on static generic dictionaries that acts as a global type-indexed key-value store.

**Why it hurts:**
- Effectively a global mutable dictionary accessible from anywhere
- No change tracking per key — only one `OnContextChanged` event for everything
- Static generics are singleton-shaped — can't scope to a lifetime or test context
- Type safety is an illusion — the "lookup" succeeds or fails silently at runtime

**Trellis answer:** Typed `Store<T>` with `StoreActions` following FLUX single-writer principle. Each store owns its data, publishes granular change events via `Observable<T>`, and is scoped through VContainer.

---

## 4. Boolean Flag State Management

State scattered across controllers as individual boolean fields (`isOpen`, `isAnimating`, `isLoading`, `hasData`). Complex conditional logic checks combinations of these flags.

**Why it hurts:**
- Combinatorial explosion — N booleans = 2^N possible states, most of which are invalid
- No single source of truth for "what state is this UI in?"
- Race conditions when multiple flags are set in sequence
- Debugging requires mentally reconstructing state from scattered fields

**Trellis answer:** Explicit state machines (`StateMachine<TState, TTrigger>`) for flow control. `ILoadObject<T>` for operation state tracking (None/Reading/Writing/Error) instead of boolean flags.

---

## 5. God-Object Drivers

The Driver Pattern (`IXSystemDriver → XSystemDriverDefault → XSystem`) was meant to separate interface from implementation, but some drivers grew unchecked. `DefinitionSystemDriver` alone is **75KB** — thousands of lines in a single file.

**Why it hurts:**
- Impossible to understand, review, or safely modify
- Single-responsibility principle abandoned — one class does everything related to its domain
- Merge conflicts on every change
- Testing requires constructing an enormous object graph

**Trellis answer:** Small, focused subsystems. VContainer scopes naturally break large systems into composable pieces. Framework classes stay small because they solve one problem each.

---

## 6. Two State Machine Implementations

The project contains two incompatible state machine patterns:

1. **Animator-based** — states driven by Unity Animator with string-based triggers
2. **HFSM-based** — typed hierarchical state machine with proper Enter/Exit lifecycle

**Why it hurts:**
- Two mental models for the same concept
- Animator-based FSMs are stringly-typed and require Unity editor setup
- No shared interface — code that works with one can't work with the other
- The Animator approach leaks presentation concerns into logic

**Trellis answer:** One state machine implementation (`StateMachine<TState, TTrigger>`) with enum-based type safety. Hierarchical extension for nested state needs. Pure C#, no Unity Animator dependency.

---

## 7. Bidirectional Data Flow in Directories

Data "directories" (registry/container classes) serve as both **data containers** and **event publishers**. They hold data AND notify subscribers of changes, creating circular data flow.

**Why it hurts:**
- Impossible to trace data flow direction — changes can trigger reactions that trigger more changes
- Cascading update storms when one change triggers a chain of reactions
- No clear ownership — who is the authoritative source of this data?
- Debugging requires tracing through event chains to find the original cause

**Trellis answer:** FLUX-inspired unidirectional data flow. `Store<T>` is read-only to consumers. Only `StoreActions` can write. Changes flow in one direction: Action → Store → Observable → UI. Queue-based notification prevents cascading re-entrancy.

---

## 8. HUD Architecture Problems

The HUD system suffered from two specific issues:

**Rigid layout:** Originally built for top and bottom bars only. When left and right elements were needed, the system couldn't accommodate them cleanly — bolted-on layout rather than a flexible zone system.

**Central controller:** A single HUD controller managed all elements, creating a bottleneck. Every new element required modifying the controller. Dependencies between elements flowed through this central point.

**Trellis answer:** Named layout zones (top, bottom, left, right, overlay) that panels register into independently. No central HUD controller. Each panel binds to its own observable state and manages its own lifecycle.

---

## 9. No Clear Initialization Order

The combination of singletons, static events, and driver pattern created an initialization web with no deterministic order. Systems access other systems' `Instance` properties during their own initialization, creating implicit ordering requirements that aren't documented or enforced.

**Why it hurts:**
- `NullReferenceException` on startup when access order changes
- Circular dependencies between systems that initialize each other
- Alternative startup flows (skip tutorial, jump to specific scene) are nearly impossible
- The `StepFSM`/`GameLoader` was introduced to manage this, but itself became complex and rigid — hard to create alternative boot sequences

**Trellis answer:** VContainer `LifetimeScope` resolution IS the initialization sequence (Option A). Different boot profiles are different scope configurations. `IAsyncStartable` for ordered async initialization. No StepFSM framework needed.

---

## 10. Recognized but Unfixed Rot

**50+ TODO/HACK comments** scattered throughout the codebase — evidence that developers recognized problems but couldn't address them due to the cost of changing tightly coupled systems.

**Why it hurts:**
- Each hack is a landmine for future developers
- The comments indicate known architectural debt that compounded over time
- Fixing any single issue requires understanding (and potentially changing) many interconnected systems

**Trellis answer:** This is a systemic problem, not a single fix. Trellis addresses it by providing clean architectural primitives (DI, event bus, stores, state machines) from the start, so consuming projects don't need to invent their own — and the hacks that follow when homegrown solutions don't scale.
