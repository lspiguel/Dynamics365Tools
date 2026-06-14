# Multi-Solution Import Dependency Pre-Flight — Plugin Plan

A standalone XrmToolBox plugin on its own repository, built independently. Audience: D365 CE / Power Platform developers and release engineers staging multi-solution imports who want to catch order-dependent dependency gaps before running the import.

> **Scope guard:** This plugin does not modify, depend on, or alter D365ContextExporter. Any shared logic is consumed as a source-embedding package (see the shared-package plan), compiled into this plugin independently — no runtime coupling.

---

## Purpose

Prevent the specific, painful, common failure of a staged multi-solution import that fails several solutions deep because a later solution needed something an even-later solution was going to provide. Given an **ordered list** of solution files and a target environment, statically check — *order-sensitively* — whether each solution's required components will be satisfied at the point it is imported.

## Core algorithm

1. For each solution file in the ordered list, read its component manifest (`solution.xml` / `customizations.xml` inside the zip) to extract both **components provided** and **components required** (dependencies). Managed and unmanaged both expose this, though dependency declarations differ in completeness.
2. Initialize `available_at_target` from the **current target environment** components.
3. Walk the import order. For each solution, in sequence:
   - Check its **required** components against `available_at_target` **as of that point** — i.e., the target's existing components *plus* everything provided by solutions earlier in the order.
   - Flag a requirement only if satisfied by neither.
   - Then accumulate that solution's **provided** components into `available_at_target` before moving to the next.

The correctness insight: **a missing component at the target is not an error if an earlier solution in the sequence provides it.** The check is order-sensitive and accumulative — this is what makes it better than import-and-hope.

## Notes carried from the design discussion (read before building)

- **Dependency extraction completeness.** The `MissingDependencies` Dataverse computes at import time draws on more than what's statically declared in `customizations.xml` — some dependencies resolve only server-side. Static parsing of the solution zip catches the majority (entity / attribute / relationship / form references) but will not perfectly replicate the platform's own dependency calculation. **Be honest in the output** that this is a *pre-flight heuristic that catches the common cases*, not a guarantee the import will succeed.
- **Why not just call `RetrieveMissingComponents`?** That message evaluates one solution against the *current* target state. To respect import order with it, you'd have to actually stage the imports — which defeats the pre-flight purpose. Static parsing + accumulation is the right model; scope the promise honestly. (It *can* optionally be used as a cross-check for the first solution against the untouched target.)
- **Provided-vs-required granularity.** A solution may provide an entity but require a *specific attribute* on an entity that an earlier solution provides only partially. The accumulation set therefore can't be "entity X exists" — it needs **component-level granularity** (this attribute, this form, this relationship), matching how Dataverse keys dependencies by `componenttype` + `objectid`. This is the same polymorphic-`objectid` terrain encountered elsewhere; the component-type mapping work carries over and is a shared-package candidate.

## User flow

1. Add solution files (zip) and set their **import order** (drag-to-order or explicit numbering).
2. Connect to / confirm the target environment.
3. Run pre-flight.
4. Output: per-solution report of satisfied vs. flagged requirements, with the flagged ones naming the missing component (type + identifier) and confirming none of the earlier solutions in the order provide it.

## Architecture notes

- Solution-zip parsing is the heart of v1 — read `customizations.xml` / `solution.xml` from each zip, extract provided and required component sets at component-level granularity.
- Target environment read via XrmToolBox's existing connection (to seed `available_at_target`).
- No environment mutation — this is read-only pre-flight. The plugin never imports anything.
- Component-type mapping (type code ↔ name, `objectid` resolution) is shared-package material.

## Output framing

Make the heuristic nature explicit in the report header: this catches common, statically-declared dependency gaps in import order; it is not a substitute for the platform's own import-time dependency calculation.

## MVP boundary

- Static parsing + accumulation against a live target, read-only.
- Per-solution satisfied/flagged report.
- Optional `RetrieveMissingComponents` cross-check (first solution vs. untouched target) can be deferred if it complicates v1.

## Risks / open questions

- Completeness of static dependency extraction (above) — the known, accepted limitation.
- Managed vs. unmanaged dependency-declaration differences — verify both are parsed correctly with real solution files of each type.

## Shared-package touchpoints

Candidates this plugin may consume from the shared source package: the **component-type model** (type-code ↔ name mapping, `objectid` / polymorphic-lookup resolution) and **component enumeration** (reading provided/required components in a consistent shape). Consume only what real use justifies.
