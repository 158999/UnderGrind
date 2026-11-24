---
name: undergrind-design-review
description: Review UnderGrind GDDs, system proposals, and implementation plans for design completeness, consistency, feasibility, and testability.
---

# UnderGrind design review

Read the canonical GDD in `158999/unity-context/Documentation/UnderGrind_Game_Design_Document.md`, relevant technical guides, current `Documentation/` pages, and actual implementation.

Classify important statements as confirmed, design direction, open decision, or implementation status. The GDD defines intended design; source code and test reports define what exists. Never invent an answer for an open decision.

Review: player purpose and design pillars; actors and state ownership; inputs/outputs/invariants/failure cases; time and persistence; compatibility with current locomotion, inventory, character and UI contracts; responsibilities across runtime/editor code; scenes, prefabs, ScriptableObjects, clips, packages and external dependencies; EditMode/PlayMode/asset/visual acceptance checks; and Italian docs/XML comments that must change.

Avoid duplicate state, generic managers, parallel bootstraps, presentation-owned domain rules, and speculative packages. Return one verdict: APPROVED, APPROVED WITH OPEN DECISIONS, or NEEDS REVISION. Separate blockers from suggestions. A design review is not proof of implementation or test execution.
