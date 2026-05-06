# CHOP-019: Remove Empty Foundation Project

## Priority: 🟢 P3 — Minor
## Type: Code Cleanup
## Component: Native/Foundation

---

## Problem Statement

The Foundation project contains only:
```csharp
// ExampleClass.cs
public class ExampleClass { }
```

This is an empty placeholder project that:
- Adds build time to CI
- Creates a DLL that's deployed but contains nothing useful
- Confuses new contributors looking at the project structure
- Adds unnecessary solution complexity

## Solution Design

1. Remove `Native/Foundation/` directory
2. Remove from solution file references
3. Remove any project references to Foundation from other .csproj files

Or alternatively, if this is intended for future use, add a `// TODO` explaining its purpose and mark it as not built in CI.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Foundation/` | **DELETE** entire directory |
| Any .sln files referencing Foundation | Remove project reference |
| Any .csproj files referencing Foundation | Remove ProjectReference |

---

## Acceptance Criteria

- [ ] Foundation project removed (or documented with clear intent)
- [ ] No build errors in remaining projects
- [ ] Solution compiles and tests pass

---

## Testing Specification

#### Test 1: Solution Builds Without Foundation
```
GIVEN Foundation project removed from solution
WHEN dotnet build is run
THEN all other projects build successfully
```

#### Test 2: No Orphan References
```
GIVEN Foundation removed
WHEN scanning all .csproj and .sln files for "Foundation"
THEN no references remain
```

---

## Definition of Done

- [ ] Directory removed
- [ ] Solution references cleaned
- [ ] CI passes
