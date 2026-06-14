# Component Snapshot & Drift Tracker — Plugin Plan (MVP)

A standalone XrmToolBox plugin on its own repository, built independently. Audience: D365 CE / Power Platform developers who already have a coding AI assistant and their own version-control / diff tooling, and want point-in-time snapshots of an environment's components to track change over time.

> **Scope guard:** This plugin does not modify, depend on, or alter D365ContextExporter. Any shared logic is consumed as a source-embedding package (see the shared-package plan), compiled into this plugin independently — no runtime coupling.

---

## Purpose

Capture a point-in-time snapshot of an environment's components — with canonical hashes and change-attribution metadata — to a clean folder. The developer uses their own git (or any diff tool) to compare snapshots over time. This is deliberately an **MVP**: the design expects to be adjusted from real-world use, so the first version stays narrow and does not embed git.

## Why no git (this version)

Snapshots are written to a folder that is **explicitly emptied at the start of each run**. The user points two runs at two folders (or commits between runs in their own repo) and diffs them however they like. This removes all git-integration complexity (executable detection, native binaries, commit/branch logic) from v1 and lets real usage reveal what's actually needed before any VCS coupling is added.

## User flow

1. Select component types to snapshot (forms, processes, SiteMap, table/column metadata, etc. — start with a curated subset).
2. Optionally scope to a solution or set of components.
3. Choose an output folder.
4. **Plugin empties the folder** (with an explicit confirmation prompt naming the folder and warning that contents will be deleted).
5. Plugin queries, canonicalizes, hashes, and writes the snapshot.

## Output shape

- **One file per component** rather than a monolithic file — so the user's own git/diff tooling produces clean, file-level diffs.
  - e.g. `forms/account/main-form.xml`, `processes/lead-qualification.json`, `tables/account/metadata.json`.
- A root **`snapshot.json` manifest** keyed by component, holding: component type, logical name/id, `modifiedby`, `modifiedon`, `createdby`, `createdon`, and the canonical hash.

The two signals the user gets by diffing two snapshot folders:
- **Content diff** (their git/diff tool over the per-component files): *what* changed.
- **Manifest diff** (hashes + attribution in `snapshot.json`): *who* changed it and *when* — the question a plain content diff can't answer, and the thing that makes this more than a file compare during a "why did this break" investigation.

## Canonicalization (the critical correctness piece)

Raw Dataverse XML hashing is fragile: attributes get reordered, volatile IDs (`formid`, regenerated GUIDs) and embedded version/timestamp attributes are injected, and whitespace is rewritten between retrievals. Two functionally identical components can otherwise hash differently across environments or even across exports.

So before hashing, **canonicalize**:
- Parse the XML, strip volatile attributes (regenerating IDs, embedded timestamps/version stamps), sort attributes, normalize whitespace, then hash.
- For non-XML components (column metadata), hash a **canonical projection** of the fields you actually care about, not the whole `AttributeMetadata` blob.

The canonicalize-then-hash logic is a candidate for the shared source-embedding package (see the shared-package plan).

## Architecture notes

- Authentication via XrmToolBox's existing connection.
- The "empty the folder" step is destructive — guard it: name the exact folder, require explicit confirmation, and refuse to operate on a path that looks unsafe (drive root, non-empty system folders).
- Keep the component-type coverage curated and explicit in v1; breadth can grow as usage shows what matters.

## MVP boundary

- No git, no remotes, no commit — folder output only.
- Curated component-type subset, not exhaustive coverage.
- Compare/diff is the user's responsibility (their git, their diff tool) — the plugin only *produces* snapshots in v1. A built-in "compare two snapshot folders" convenience view is a candidate fast-follow once the snapshot format has stabilized from real use.

## Risks / open questions

- Canonicalization completeness — getting the volatile-attribute strip list right will take iteration; expected, hence MVP framing.
- Which component types earn their place in v1 vs. add noise — decide from the first real snapshots.

## Shared-package touchpoints

Candidates this plugin may consume from the shared source package: **canonicalization** (XML/metadata canonicalize-then-hash), **component enumeration** (reading components from a solution/environment in a consistent shape), and the **component-type model** (type-code ↔ name). Consume only what real use justifies; helpers needed by this plugin alone stay local.
