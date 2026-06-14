# PCF Control Scaffold Orchestrator — Plugin Plan

A standalone XrmToolBox plugin on its own repository, built independently. Audience: D365 CE / Power Platform developers who already have a coding AI assistant (GitHub Copilot, Cursor, Claude Code) and want the platform-specific PCF wiring handled for them so the assistant can focus on control logic.

> **Scope guard:** This plugin does not modify, depend on, or alter D365ContextExporter. Any shared logic is consumed as a source-embedding package (see the shared-package plan), compiled into this plugin independently — no runtime coupling.

---

## Purpose

Eliminate the tedious, error-prone wiring of a PCF control's setup by letting the plugin handle the D365-specific parts (publisher, bound column metadata, manifest typing, solution linkage) and leaving the control *logic* to the developer's coding assistant. The plugin is an orchestrator over PAC CLI plus a post-processing layer.

## User flow

1. Select a **publisher** (from the connected environment, or enter prefix/name for a new one).
2. Select a **table**.
3. Select binding mode: a **field** (column) or a **dataset**.
   - If field: pick the column.
   - If dataset: confirm the table is the dataset source.
4. Choose an output folder (must be empty or confirm-overwrite).
5. Plugin runs the orchestration and emits a ready-to-open VS Code workspace.

## Orchestration sequence

1. `pac pcf init` (subprocess) with the appropriate template flag — **field-bound and dataset-bound are two distinct code paths from the start**, not a retrofit. The PAC templates diverge enough (manifest `property` vs. `data-set` elements, `index.ts` signatures) that branching early is cleaner than special-casing later.
2. Plugin post-processes the generated scaffold:
   - Inject the bound column's metadata into `ControlManifest.Input.xml` — for a field control, the `of-type` / `of-type-group` derived from the actual column type (and `usage`/`required` as sensible defaults); for a dataset control, the dataset and a starter set of `property-set` entries from the table's columns.
   - Type the `index.ts` stubs to match (e.g., correct `IInputs`/`IOutputs` expectations for the column type).
3. `pac solution init` (subprocess) using the chosen publisher prefix and name.
4. `pac solution add-reference` (subprocess) to link the PCF project into the solution.
5. Emit `readme.md` with next steps: `npm install`, `npm run build`, `pac pcf push` (or solution build/import), and a note on where the developer fills in control logic.
6. Optionally emit a `copilot-instructions.md` (or `AGENTS.md`) preamble describing the control's binding context, the column type, and the team's PCF conventions — so the coding assistant has grounding when the developer opens the workspace.

## Architecture notes

- **Subprocess decoupling** for all PAC CLI calls, consistent with the decoupled-component preference (mirrors the Python-subprocess pattern used elsewhere). The plugin shells out to `pac`, captures stdout/stderr, and surfaces failures clearly rather than embedding any PAC logic.
- **PAC CLI presence check** at startup: detect `pac` on PATH (and a minimum version), with an actionable "install Power Platform CLI" message if absent. Safe assumption for this audience, but fail loudly rather than silently.
- **Authentication** leverages XrmToolBox's existing connection for metadata reads (column types, table info). PAC CLI calls that need auth use the developer's existing `pac auth` profile — the plugin should *not* handle credentials; if no profile is present, instruct the user to run `pac auth create`.
- **Post-processing is the fragile part.** PAC scaffolds change across CLI versions. Keep the post-processing logic tolerant — locate manifest elements by structure, not by line position — and version-gate where the template shape is known to have changed.

## MVP boundary

- Field-bound and dataset-bound both supported (they're the two core paths; supporting only one undercuts the value).
- New-publisher creation can be deferred — v1 can require selecting an existing publisher, with new-publisher entry as a fast follow.
- No build/push automation in v1 — emit the workspace and the readme; the developer drives build/push. Keeps the plugin out of long-running processes and environment-mutation territory.

## Risks / open questions

- PAC CLI version drift breaking post-processing — mitigate with structural manifest edits and a tested-version note in the readme.
- Dataset control `property-set` generation: how many columns to seed by default? Probably the primary column plus a small, configurable set, rather than every column — decide from your own PCF conventions.

## Shared-package touchpoints

This plugin is largely self-contained (PAC orchestration + manifest post-processing are specific to it). It may consume the shared source package for table/column **metadata reading** in a consistent shape, but has no hard dependency on it. If the only metadata reader it needs is trivial, keep it local.
