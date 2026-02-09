# Claude Code Rules

## Environment

- **Unity Editor path:** `/Applications/Unity/Hub/Editor/6000.3.5f1/Unity.app`
- **Unity CLI:** `/Applications/Unity/Hub/Editor/6000.3.5f1/Unity.app/Contents/MacOS/Unity`

## Session Start

At the start of each session, check for and read the following files if they exist:
- `Docs/TDD.md` -- technical design document with architecture, constraints, and subsystem design
- `Docs/Architecture-Diagrams.md` -- visual companion with Mermaid diagrams for all subsystems

## Write-Back Before Session End or Context Risk

Context compaction loses in-session details. To survive it, **write back to docs before losing context**:

1. **After every significant change** (deliverable completed, architecture change, new subsystem):
   - Update `Docs/TDD.md` to reflect architectural changes, new types, or updated constraints
   - Update `Docs/Architecture-Diagrams.md` if structural changes were made (new classes, changed relationships)

2. **When the user mentions compaction risk** (e.g., "99% context", "running out of context"):
   - Immediately write back ALL pending state to docs before doing anything else
   - Prioritize TDD.md (it's the session-start file and single source of truth)

3. **Rule: docs must always be current enough that a fresh session reading only the Session Start files can pick up where we left off.** If something is only in conversation context and not in a doc, it's at risk.

4. **Proactive context management:** Your context window will be automatically compacted as it approaches its limit, allowing you to continue working indefinitely. Do not stop tasks early due to context budget concerns. As you approach the context limit, proactively write back all pending state to docs before the window refreshes. Always complete tasks fully rather than stopping early.

---

## Unity C# Coding Style

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `SystemScheduler`, `GameObjectPool` |
| Methods (public & private) | PascalCase | `AddState()`, `Acquire()` |
| Private fields | camelCase | `private float blendTimer;` |
| Serialized private fields | camelCase with attribute | `[SerializeField] private float moveSpeed;` |
| Properties | PascalCase | `public TState CurrentStateId => currentStateId;` |
| Constants | UPPER_SNAKE_CASE | `private const int HASH_PRIME = 397;` |
| Enums | PascalCase type and values | `enum PoolGrowthMode { Fixed, Dynamic }` |
| Interfaces | I-prefix, PascalCase | `IState`, `ISystem`, `IPoolable` |
| Generic type parameters | T-prefix, PascalCase | `TState`, `TTrigger` |

### Method Naming by Intent

Methods fall into two categories based on their intent:

- **Builders** — Return an object or value. Named with a **noun** or **noun phrase** that describes what is produced. Must not change the state of the owning object. Examples: `NearestTarget(...)`, `ParsedConfig(...)`.
- **Manipulators** — Perform an action that changes state. Named with a **verb** or **verb-noun phrase** describing the action. Generally return `void` or a status indicator (`bool`, enum), not the object being operated on. Examples: `AddState(...)`, `MarkForRemoval(...)`, `Reset()`, `Acquire(...)`.

Avoid generic `Get`/`Set` prefixes. Exceptions: standard C# idioms (`TryGetValue` pattern), and trivial value-type property wrappers.

### Formatting

- **Brace style**: Allman (opening brace on its own line)
- **Indentation**: 4 spaces (no tabs)
- **Blank lines**: One blank line between members
- **`[SerializeField]`**: On the same line as the field declaration
- **No `#region`**: Do not use `#region`/`#endregion` blocks

### Code Organization

- **Namespaces**: All Trellis framework code uses `Trellis.*` namespaces (`Trellis.StateMachine`, `Trellis.Scheduling`, `Trellis.Pooling`). Starter/demo project code does not use the `Trellis` namespace.
- **Folder structure**: Subsystem-based organization under `Trellis/Runtime/`
- **Using statements**: System first, then UnityEngine, then Trellis namespaces
- **Member order in classes**: Fields, then lifecycle methods (`Awake`/`Start`/`Update`/`OnDestroy`), then public methods, then private methods

### Field & Access Patterns

- Prefer `[SerializeField] private` over `public` fields for editor-exposed data
- Use expression-bodied properties for read-only access: `public TState CurrentStateId => currentStateId;`
- Use `private` by default; only expose what is needed
- **Properties over public fields** — Expose data through properties, not raw public fields. Exception: pure data containers (structs or classes that exist solely to hold data with no behavior) may use public fields.

### Component Reference Patterns

- **Avoid `GetComponent` in hot paths** — `GetComponent<T>()` is a runtime lookup. Never call it per-frame, in Update loops, or inside high-frequency iteration (pool Acquire/Return). Treat any `GetComponent` call in a loop as a performance bug.
- **Cache once in Awake** — When a serialized reference isn't practical, call `GetComponent` once during `Awake`/`OnEnable` and store the result in a field. Never re-fetch what you already have.
- **Use `TryGetComponent` for fallible lookups** — When the component may legitimately be absent, use `TryGetComponent` (avoids exceptions and is clearer intent than null-checking `GetComponent`).
- **Cache in pooling infrastructure** — Object pools that call `TryGetComponent` on Acquire/Return should cache the component reference per instance (e.g., in a `Dictionary<GameObject, IPoolable>`) to avoid repeated lookups across the object's pooled lifetime.
- **Constructor injection for pure C# objects** — Framework classes receive their dependencies through constructors, not through runtime lookups.
- **Constructor/method parameter limits** — 0–3 parameters: good. 4–5: acceptable, but verify the method isn't mixing concerns. 6+: design smell — group related parameters into small bundle types. Bundle by concept, not by count.

### Code Clarity

- Self-documenting method names preferred over comments
- Minimal inline comments; use only where logic is non-obvious
- **XML doc comments (`///`) on public API** — Trellis is a library consumed by other assemblies. Public types and members should have XML doc comments describing their contract.
- Use string interpolation: `Debug.LogError($"Failed to load: {assetName}");`
- Use `new()` target-typed syntax: `private Dictionary<string, int> lookup = new();`
- **No magic numbers** — Define named constants for numeric literals that carry domain meaning. Trivially obvious values (0, 1, -1, dividing/multiplying by 2) are exempt.

### Error Handling

- Null-check serialized dependencies in `Awake()` with `Debug.LogError` and `enabled = false`
- Use `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` consistently
- Prefer early returns over deep nesting
- Use `ArgumentNullException` / `ArgumentOutOfRangeException` for constructor parameter validation in framework classes

### Event Patterns

- Use C# `event Action<T>` or custom delegates for system-level events
- Use `?.Invoke()` for safe invocation
- Avoid UnityEvents in code-driven systems (reserve for designer-facing inspector hookups)

### Performance Rules

- **No LINQ in runtime code** - Do not use `System.Linq` in any runtime (non-editor-tool) code. LINQ causes hidden allocations and GC pressure. Use explicit loops, arrays, and manual collection operations instead.
- **Minimize GC allocations** - Every `GC.Alloc` in a hot path is a bug. Specific rules:
  - **Boxing** - Never pass value types to `object` parameters. Avoid non-generic collections (`ArrayList`, `Hashtable`). Use generic collections and interfaces. Use `EqualityComparer<T>.Default` for generic equality comparisons on value types.
  - **Object pooling** - Pool frequently created/destroyed objects. Design systems for clean teardown and rebuild without residual allocations.
  - **Pre-allocate collections** - Size lists/arrays upfront. Reuse with `Clear()` instead of `new`. No `new List<T>()` in per-frame code.
  - **Cache references** - Cache results of `GetComponent<T>()`, `FindObjectOfType<T>()`, and similar lookups. Never call them in Update or hot loops.
  - **Strings** - Avoid string concatenation (`+`) in hot paths; it allocates. Use `StringBuilder` or pre-built strings. Be cautious with `Debug.Log` string interpolation in loops.
  - **Coroutine yields** - Cache `WaitForSeconds` and other yield instruction instances in fields. `yield return new WaitForSeconds()` allocates every call.
  - **foreach** - Safe on arrays and `List<T>`. Avoid `foreach` on non-generic or custom `IEnumerable` implementations that allocate enumerators.
  - **Lambdas/closures** - Acceptable for setup, configuration, and infrequent callbacks. Avoid creating new lambdas with captures in per-frame or high-frequency code paths. Cache delegates in fields when a callback with captures is needed on a hot path.
  - **Structs over classes** - Prefer structs for hot-path data to avoid heap allocation. Be mindful of struct size and copying costs.

---

## Architectural Principles

- **Lightweight over comprehensive** — Each subsystem solves one problem well. No configuration frameworks, no DI containers, no reflection.
- **Plain C# where possible** — State machine and scheduler are pure C# with no MonoBehaviour dependency. Only pooling requires Unity APIs.
- **Generic over domain-specific** — No assumptions about consuming project's domain. State machine is parameterized on enums; scheduler takes any `ISystem`.
- **Composition over inheritance** — Interfaces (`IState`, `ISystem`, `IPoolable`) define contracts. No base classes to inherit from. Abstract base classes acceptable only when they provide genuine shared behavior behind an interface.
- **No game-specific types in framework** — Framework code never references game-specific enums, data structures, or systems. Dependencies flow inward: game code depends on Trellis, never the reverse.

---

## Package Development Conventions

- **Assembly definitions** — All framework code lives under `Trellis.Runtime.asmdef`. Tests under `Trellis.Tests.Editor.asmdef`. No code outside of assembly definitions.
- **Namespace convention** — All framework types use `Trellis.*` namespaces. One namespace per subsystem folder.
- **Versioning** — Follow semantic versioning in `package.json`. Breaking changes increment the minor version during 0.x development.
- **No game-specific dependencies** — Framework `package.json` must have zero dependencies on game-specific packages. Unity module dependencies are acceptable.
- **`autoReferenced: true`** — The runtime assembly auto-references into consuming projects. No manual asmdef wiring needed.

---

## Testing Strategy

Tests are required for every deliverable. Tests must be written alongside feature implementation, not deferred.

### Test Types

- **Edit Mode Tests** (`Trellis/Tests/Editor/`): Unit tests for pure logic that does not require a running scene. State machine, scheduler, and data validation tests.
- **Play Mode Tests** (future): Integration tests requiring MonoBehaviour lifecycle. Pool tests fall in this category.

### Per-Deliverable Test Requirements

Each deliverable must include:
1. **Happy-path tests** verifying the acceptance criteria are met
2. **Edge case analysis** documented in the test file or as comments
3. **Coverage review** before signing off — confirm all acceptance criteria have corresponding test assertions

### Test Conventions

- Test class naming: `[SubsystemName]Tests` (e.g., `StateMachineTests`, `SystemSchedulerTests`)
- Test method naming: `MethodOrBehavior_Condition_ExpectedResult` (e.g., `Fire_TriggerResolvedOnNextTick`, `Tick_NullElementsSkipped`)
- Use `[SetUp]` / `[TearDown]` for shared test fixtures
- Use `Assert` (NUnit) for assertions
- Use `[UnityTest]` for Play Mode coroutine-based tests
- Use `[Test]` for synchronous Edit Mode tests
- Framework tests define their own test enums and stub implementations — no dependency on any game project

### Plan Review Checklist

Before presenting a plan for user approval, perform a critical self-review checking:
- **No game-specific coupling**: New code does not reference any consuming project's types.
- **Generic type constraints**: New generic parameters use `struct, Enum` or appropriate constraints. No boxing on hot paths.
- **Test plan completeness**: Every AC has at least one test. Edge cases identified.
- **Constructor/dependency wiring**: All new dependencies are threaded through constructors. No runtime lookups for pure C# objects.
- **Consistency with TDD.md**: Plan aligns with documented architecture. New patterns are justified, not accidental divergence.
- **Namespace discipline**: All new types are in the correct `Trellis.*` namespace.
- **Performance**: No LINQ, no per-frame allocations, no GetComponent in hot paths.

### Deliverable Sign-Off Checklist

**Trigger:** When the user says "signed off", "sign off", "ready for next story", or similar completion phrases, immediately run through ALL uncompleted checklist items below before confirming completion. Do not wait for the user to ask — proactively execute every step that hasn't been done yet in the current story.

Before a deliverable is considered complete:
- All acceptance criteria have passing tests
- Edge cases identified and tested or documented as out of scope
- No regressions in previously passing tests
- TDD.md updated to reflect any architectural changes made during implementation
- Architecture-Diagrams.md updated to reflect any structural changes (new classes, changed relationships, modified sequences)
- README.md updated if any changes to other markdown documentation files affect it
- Critical code review: systematic pass through all new/modified production files checking for bugs, event lifecycle issues (subscribe/unsubscribe), null safety, and adherence to architectural constraints (no LINQ, no GetComponent in hot paths, no game-specific coupling)
