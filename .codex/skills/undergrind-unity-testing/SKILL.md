---
name: undergrind-unity-testing
description: Run and interpret UnderGrind's existing standalone, Unity EditMode, and PlayMode test pipelines.
---

# UnderGrind Unity testing

Read `Documentation/Testing.md` and the scripts in `Tools/Testing/` before changing test orchestration. Use the existing runner; do not create a parallel one.

- Standalone: `./Tools/Testing/run-tests.ps1`
- Unity EditMode: `./Tools/Testing/run-unity-tests.ps1 -Platform EditMode`
- Unity PlayMode: `./Tools/Testing/run-unity-tests.ps1 -Platform PlayMode`
- Both Unity platforms: `./Tools/Testing/run-unity-tests.ps1 -Platform All`
- Full orchestration: `./Tools/Testing/run-all-tests.ps1` (inspect parameters first).

Inspect reports and logs under `TestResults/`. Report discovery, passed/failed/skipped/inconclusive counts, compiler diagnostics, timeout, exit code, and missing evidence. A source test is not an executed test; standalone success is not Unity compilation; EditMode is not PlayMode; headless tests are not visual proof. Add PlayMode coverage for input, CharacterController, physics, Animator, lifecycle, scenes, or UI. Do not commit generated output unless explicitly required.

The upstream `uloop-run-tests` catalog entry is not reproducibly installable. Existing UnderGrind scripts are the operational replacement. A connected uLoop MCP may supplement but never replace their reports.
