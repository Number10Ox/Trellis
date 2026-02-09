# Snapshot — Write Back All Context to Docs

The user is at risk of context compaction or ending a session. **Immediately write back all in-session state to docs.**

Do NOT ask questions. Do NOT do any other work first. Write back now.

## Write-Back Checklist

For each file, compare what's in the doc vs what's true in the current conversation. Update anything that's stale.

### 1. `Docs/TDD.md` (HIGHEST PRIORITY)
- Current deliverable progress — which deliverable are we on? What's done, what's next?
- Architecture section — did any subsystem designs change?
- Key types — were any interfaces, classes, or enums added/modified?
- Constraints — did any design constraints change?
- Deliverable acceptance criteria — were any checked off or modified?

### 2. `Docs/Architecture-Diagrams.md`
- Were any new classes or subsystems added?
- Did any relationships or sequences change?
- Do existing diagrams still accurately reflect the code?

### 3. `README.md`
- Did any changes affect the framework's public-facing documentation?
- (Usually stable — only update if subsystem inventory or usage patterns changed)

## Output

After updating, print a short summary:
```
Snapshot complete. Updated:
- TDD.md: [what changed]
- Architecture-Diagrams.md: [what changed / no changes]
- README.md: [what changed / no changes]

Safe to compact or end session.
```

## Rules

- Be fast. The user is running out of context.
- Don't add speculative content — only write back what was actually decided or built.
- If nothing changed for a file, skip it and note "no changes needed."
- Commit nothing — just update the docs. The user will commit when ready.
