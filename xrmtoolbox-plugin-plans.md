# XrmToolBox Plugin Plans — Coding-Assistant-Oriented Tooling

Three standalone plugins, each on its own repository and built independently. Audience: D365 CE / Power Platform developers who already have a coding AI assistant (GitHub Copilot, Cursor, Claude Code) and need platform state pushed into, or platform actions taken from, that workflow.

A closing section covers the shared-library approach (Scriban-style source-embedding packages).

> **Scope guard:** None of these plans modify, depend on, or alter D365ContextExporter. Where shared logic is identified, it is delivered as a source-embedding package that each repo consumes independently — no runtime coupling, no cross-repo edits.

---

## Plugin 9 — PCF Control Scaffold Orchestrator

### Purpose

Eliminate the tedious, error-prone wiring of a PCF control's setup by letting the plugin handle the D365-specific parts (publisher, bound column metadata, manifest typing, solution linkage) and leaving the control *logic* to the developer's coding assistant. The plugin is an orchestrator over PAC CLI plus a post-processing layer.

### User flow

1. Select a **publisher** (from the connected environment, or enter prefix/name for a new one).
2. Select a **table**.
3. Select binding mode: a **field** (column) or a **dataset**.
   - If field: pick the column.
   - If dataset: confirm the table is the dataset source.
4. Choose an output folder (must be empty or confirm-overwrite).
5. Plugin runs the orchestration and emits a ready-to-open VS Code workspace.

### Orchestration sequence

1. `pac pcf init` (subprocess) with the appropriate template flag — **field-bound and dataset-bound are two distinct code paths from the start**, not a retrofit. The PAC templates diverge enough (manifest `property` vs. `data-set` elements, `index.ts` signatures) that branching early is cleaner than special-casing later.
2. Plugin post-processes the generated scaffold:
   - Inject the bound column's metadata into `ControlManifest.Input.xml` — for a field control, the `of-type` / `of-type-group` derived from the actual column type (and `usage`/`required` as sensible defaults); for a dataset control, the dataset and a starter set of `property-set` entries from the table's columns.
   - Type the `index.ts` stubs to match (e.g., correct `IInputs`/`IOutputs` expectations for the column type).
3. `pac solution init` (subprocess) using the chosen publisher prefix and name.
4. `pac solution add-reference` (subprocess) to link the PCF project into the solution.
5. Emit `readme.md` with next steps: `npm install`, `npm run build`, `pac pcf push` (or solution build/import), and a note on where the developer fills in control logic.
6. Optionally emit a `copilot-instructions.md` (or `AGENTS.md`) preamble describing the control's binding context, the column type, and the team's PCF conventions — so the coding assistant has grounding when the developer opens the workspace.

### Architecture notes

- **Subprocess decoupling** for all PAC CLI calls, consistent with the decoupled-component preference (mirrors the Python-subprocess pattern used elsewhere). The plugin shells out to `pac`, captures stdout/stderr, and surfaces failures clearly rather than embedding any PAC logic.
- **PAC CLI presence check** at startup: detect `pac` on PATH (and a minimum version), with an actionable "install Power Platform CLI" message if absent. Safe assumption for this audience, but fail loudly rather than silently.
- **Authentication** leverages XrmToolBox's existing connection for metadata reads (column types, table info). PAC CLI calls that need auth use the developer's existing `pac auth` profile — the plugin should *not* handle credentials; if no profile is present, instruct the user to run `pac auth create`.
- **Post-processing is the fragile part.** PAC scaffolds change across CLI versions. Keep the post-processing logic tolerant — locate manifest elements by structure, not by line position — and version-gate where the template shape is known to have changed.

### MVP boundary

- Field-bound and dataset-bound both supported (they're the two core paths; supporting only one undercuts the value).
- New-publisher creation can be deferred — v1 can require selecting an existing publisher, with new-publisher entry as a fast follow.
- No build/push automation in v1 — emit the workspace and the readme; the developer drives build/push. Keeps the plugin out of long-running processes and environment-mutation territory.

### Risks / open questions

- PAC CLI version drift breaking post-processing — mitigate with structural manifest edits and a tested-version note in the readme.
- Dataset control `property-set` generation: how many columns to seed by default? Probably the primary column plus a small, configurable set, rather than every column.

---

## Plugin A — Component Snapshot & Drift Tracker (MVP, folder-output)

### Purpose

Capture a point-in-time snapshot of an environment's components — with canonical hashes and change-attribution metadata — to a clean folder. The developer uses their own git (or any diff tool) to compare snapshots over time. This is deliberately an **MVP**: the design expects to be adjusted from real-world use, so the first version stays narrow and does not embed git.

### Why no git (this version)

Snapshots are written to a folder that is **explicitly emptied at the start of each run**. The user points two runs at two folders (or commits between runs in their own repo) and diffs them however they like. This removes all git-integration complexity (executable detection, LibGit2Sharp native binaries, commit/branch logic) from v1 and lets real usage reveal what's actually needed before any VCS coupling is added.

### User flow

1. Select component types to snapshot (forms, processes, SiteMap, table/column metadata, etc. — start with a curated subset).
2. Optionally scope to a solution or set of components.
3. Choose an output folder.
4. **Plugin empties the folder** (with an explicit confirmation prompt naming the folder and warning that contents will be deleted).
5. Plugin queries, canonicalizes, hashes, and writes the snapshot.

### Output shape

- **One file per component** rather than a monolithic file — so the user's own git/diff tooling produces clean, file-level diffs.
  - e.g. `forms/account/main-form.xml`, `processes/lead-qualification.json`, `tables/account/metadata.json`.
- A root **`snapshot.json` manifest** keyed by component, holding: component type, logical name/id, `modifiedby`, `modifiedon`, `createdby`, `createdon`, and the canonical hash.

The two signals the user gets by diffing two snapshot folders:
- **Content diff** (their git/diff tool over the per-component files): *what* changed.
- **Manifest diff** (hashes + attribution in `snapshot.json`): *who* changed it and *when* — the question a plain content diff can't answer, and the thing that makes this more than a file compare during a "why did this break" investigation.

### Canonicalization (the critical correctness piece)

Raw Dataverse XML hashing is fragile: attributes get reordered, volatile IDs (`formid`, regenerated GUIDs) and embedded version/timestamp attributes are injected, and whitespace is rewritten between retrievals. Two functionally identical components can otherwise hash differently across environments or even across exports.

So before hashing, **canonicalize**:
- Parse the XML, strip volatile attributes (regenerating IDs, embedded timestamps/version stamps), sort attributes, normalize whitespace, then hash.
- For non-XML components (column metadata), hash a **canonical projection** of the fields you actually care about, not the whole `AttributeMetadata` blob.

This is the same class of canonicalization problem seen in transformation work elsewhere; the logic is a candidate for the shared source-embedding package (see final section).

### Architecture notes

- Authentication via XrmToolBox's existing connection.
- The "empty the folder" step is destructive — guard it: name the exact folder, require explicit confirmation, and refuse to operate on a path that looks unsafe (drive root, non-empty system folders).
- Keep the component-type coverage curated and explicit in v1; breadth can grow as usage shows what matters.

### MVP boundary

- No git, no remotes, no commit — folder output only.
- Curated component-type subset, not exhaustive coverage.
- Compare/diff is the user's responsibility (their git, their diff tool) — the plugin only *produces* snapshots in v1. A built-in "compare two snapshot folders" convenience view is a candidate fast-follow once the snapshot format has stabilized from real use.

### Risks / open questions

- Canonicalization completeness — getting the volatile-attribute strip list right will take iteration; expected, hence MVP framing.
- Which component types earn their place in v1 vs. add noise — decide from the first real snapshots.

---

## Plugin B — Multi-Solution Import Dependency Pre-Flight

### Purpose

Prevent the specific, painful, common failure of a staged multi-solution import that fails several solutions deep because a later solution needed something an even-later solution was going to provide. Given an **ordered list** of solution files and a target environment, statically check — *order-sensitively* — whether each solution's required components will be satisfied at the point it is imported.

### Core algorithm

1. For each solution file in the ordered list, read its component manifest (`solution.xml` / `customizations.xml` inside the zip) to extract both **components provided** and **components required** (dependencies). Managed and unmanaged both expose this, though dependency declarations differ in completeness.
2. Initialize `available_at_target` from the **current target environment** components.
3. Walk the import order. For each solution, in sequence:
   - Check its **required** components against `available_at_target` **as of that point** — i.e., the target's existing components *plus* everything provided by solutions earlier in the order.
   - Flag a requirement only if satisfied by neither.
   - Then accumulate that solution's **provided** components into `available_at_target` before moving to the next.

The correctness insight (carried over from the design discussion): **a missing component at the target is not an error if an earlier solution in the sequence provides it.** The check is order-sensitive and accumulative — this is what makes it better than import-and-hope.

### Notes carried from the design discussion (read before building)

- **Dependency extraction completeness.** The `MissingDependencies` Dataverse computes at import time draws on more than what's statically declared in `customizations.xml` — some dependencies resolve only server-side. Static parsing of the solution zip catches the majority (entity / attribute / relationship / form references) but will not perfectly replicate the platform's own dependency calculation. **Be honest in the output** that this is a *pre-flight heuristic that catches the common cases*, not a guarantee the import will succeed.
- **Why not just call `RetrieveMissingComponents`?** That message evaluates one solution against the *current* target state. To respect import order with it, you'd have to actually stage the imports — which defeats the pre-flight purpose. Static parsing + accumulation is the right model; scope the promise honestly. (It *can* optionally be used as a cross-check for the first solution against the untouched target.)
- **Provided-vs-required granularity.** A solution may provide an entity but require a *specific attribute* on an entity that an earlier solution provides only partially. The accumulation set therefore can't be "entity X exists" — it needs **component-level granularity** (this attribute, this form, this relationship), matching how Dataverse keys dependencies by `componenttype` + `objectid`. This is the same polymorphic-`objectid` terrain encountered elsewhere; the component-type mapping work carries over and is a shared-package candidate.

### User flow

1. Add solution files (zip) and set their **import order** (drag-to-order or explicit numbering).
2. Connect to / confirm the target environment.
3. Run pre-flight.
4. Output: per-solution report of satisfied vs. flagged requirements, with the flagged ones naming the missing component (type + identifier) and confirming none of the earlier solutions in the order provide it.

### Architecture notes

- Solution-zip parsing is the heart of v1 — read `customizations.xml` / `solution.xml` from each zip, extract provided and required component sets at component-level granularity.
- Target environment read via XrmToolBox's existing connection (to seed `available_at_target`).
- No environment mutation — this is read-only pre-flight. The plugin never imports anything.
- Component-type mapping (type code ↔ name, `objectid` resolution) is shared-package material.

### Output framing

Make the heuristic nature explicit in the report header: this catches common, statically-declared dependency gaps in import order; it is not a substitute for the platform's own import-time dependency calculation.

### Risks / open questions

- Completeness of static dependency extraction (above) — the known, accepted limitation.
- Managed vs. unmanaged dependency-declaration differences — verify both are parsed correctly with real solution files of each type.

---

## Shared libraries — Scriban-style source embedding

### Decision

Given XrmToolBox delivery complexity (store distribution, native-dependency friction) and the genuine uncertainty about *which* shared functionality will actually materialize, do **not** ship a shared runtime assembly that each plugin references as a NuGet binary dependency. Instead, use the **Scriban approach: a package that embeds source**, so the shared code is compiled *into* each plugin rather than carried as a separate assembly.

### Why this fits

- **No extra runtime assembly to distribute** alongside each plugin — the shared code becomes part of each plugin's own assembly. This sidesteps the XrmToolBox store / assembly-loading friction that a shared binary dependency would introduce.
- **No versioning lockstep across plugins** — each plugin pins the source-package version it embeds and upgrades on its own schedule. One plugin can move ahead without forcing rebuilds of the others.
- **Honest about uncertainty** — since it's not yet clear what shared functionality will exist, source embedding lets shared code stay small and grow organically, with each consumer free to diverge if its needs differ, rather than committing early to a binary API contract.

### How Scriban does it (the pattern to follow)

Scriban ships a companion package (`Scriban.Signed` / the `*.Source`-style mechanism) that adds `.cs` files into the consuming project as compile items at build time (typically `internal` so they don't leak into the consumer's public surface), with an MSBuild target wiring the sources in. The mechanics to replicate:

- A package whose content is **source files added as `Compile` items**, not a referenced DLL.
- Sources compiled as **`internal`** into the consumer, so each plugin gets its own private copy with no public-API coupling.
- Standard NuGet versioning on the *source* package, but each plugin embeds and advances independently.

### What goes in the shared source package (candidates, not commitments)

Kept deliberately minimal until real use justifies more:

- **Component-type model**: type-code ↔ name mapping, and `objectid` / polymorphic-lookup resolution helpers. Needed by Plugin B now, and plausibly by Plugin A's metadata handling.
- **Component enumeration**: reading components from a solution and/or environment in a consistent shape.
- **Canonicalization**: XML/metadata canonicalize-then-hash helpers — used by Plugin A; potentially useful to B for comparing component definitions.

Let real usage pull functionality into the shared package rather than pushing it there speculatively. If only one plugin ever needs a given helper, it stays in that plugin.

> **Reminder:** D365ContextExporter is explicitly out of scope. Although it may have overlapping needs (component-type mapping, canonicalization), nothing in these plans modifies it or makes it depend on the shared package. If it ever consumes the same source package, that is a separate, later decision made on its own terms — not part of this work.
