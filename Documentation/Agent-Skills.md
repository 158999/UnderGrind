# Agent skills installed for UnderGrind

Project-scoped Codex skills are stored in `.codex/skills/`. Read root `AGENTS.md` first; local architecture, GDD, asset-safety, and verification rules take precedence.

## Engineering and gameplay
- `unity-developer` — Unity 6, URP, C#, performance, assets, input, physics, animation.
- `game-development` — C#/Unity gameplay architecture and implementation patterns.
- `game-developer` — gameplay systems, state machines, physics, profiling, performance.
- `level-design` — spatial layout, blockout, player flow, pacing, playtesting.

## Characters and assets
- `3d-modeling` — game-ready meshes, topology, UVs, DCC workflow.
- `blender-modeling` — Blender modeling and mesh workflows.
- `build-rigged-game-assets` — skeleton, mesh partitions, collision contracts, asset validation.
- `asset-audit` — asset naming, references, formats, size budgets, orphan checks.

## Presentation and quality
- `game-ui-design` — HUD, inventory, menus, controller navigation, accessibility.
- `uloop-screenshot` — Unity capture workflow when external uLoop MCP is connected.
- `visual-testing-advanced` — screenshot comparison and regression workflow, especially for browser-based documentation UI.
- `undergrind-visual-qa` — project-specific in-game visual checks and evidence rules.

## Documentation and design
- `code-documentation` — architecture, API, guides, and developer documentation.
- `csharp-docs` — XML documentation conventions.
- `undergrind-design-review` — GDD/system proposal review against canonical project sources.

## Testing
- `undergrind-unity-testing` — operational adapter for existing standalone/EditMode/PlayMode scripts.
- The upstream `uloop-run-tests` entry was not installed because it has no reproducible pinned install. The local adapter uses the already versioned scripts in `Tools/Testing/`.

Skills are agent guidance, not Unity runtime packages: they do not add game dependencies or change gameplay by themselves. Screenshot capture remains conditional on a compatible uLoop MCP; otherwise visual QA must be reported as not run or manually evidenced.
