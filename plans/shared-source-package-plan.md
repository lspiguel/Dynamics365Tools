# Shared Source-Embedding Package — Plan

A standalone repository delivering shared functionality to the XrmToolBox plugins (PCF Scaffold Orchestrator, Component Snapshot & Drift Tracker, Multi-Solution Import Dependency Pre-Flight) as **embedded source** rather than a referenced binary, following the Scriban approach. Built and versioned independently of every consumer.

> **Scope guard:** This package does not modify, depend on, or alter D365ContextExporter. If D365ContextExporter ever consumes this package, that is a separate, later decision made on its own terms — not part of this work.

---

## Decision

Given XrmToolBox delivery complexity (store distribution, native-dependency and assembly-loading friction) and the genuine uncertainty about *which* shared functionality will actually materialize, do **not** ship a shared runtime assembly that each plugin references as a NuGet binary dependency. Instead, use the **Scriban approach: a package that embeds source**, so the shared code is compiled *into* each plugin rather than carried as a separate assembly.

## Why this fits

- **No extra runtime assembly to distribute** alongside each plugin — the shared code becomes part of each plugin's own assembly. This sidesteps the XrmToolBox store / assembly-loading friction that a shared binary dependency would introduce.
- **No versioning lockstep across plugins** — each plugin pins the source-package version it embeds and upgrades on its own schedule. One plugin can move ahead without forcing rebuilds of the others.
- **Honest about uncertainty** — since it's not yet clear what shared functionality will exist, source embedding lets shared code stay small and grow organically, with each consumer free to diverge if its needs differ, rather than committing early to a binary API contract.

## How Scriban does it (the pattern to follow)

Scriban ships a companion source package that adds `.cs` files into the consuming project as compile items at build time (typically `internal` so they don't leak into the consumer's public surface), with an MSBuild target wiring the sources in. The mechanics to replicate:

- A package whose content is **source files added as `Compile` items**, not a referenced DLL.
- Sources compiled as **`internal`** into the consumer, so each plugin gets its own private copy with no public-API coupling.
- Standard NuGet versioning on the *source* package, but each plugin embeds and advances independently.

### Implementation specifics to nail down

- Ship the `.cs` files under the package's `contentFiles` (or a `build`/`buildTransitive` MSBuild target that injects them as `<Compile>` items) so they compile into the consumer.
- Mark the package `DevelopmentDependency` so it doesn't flow as a transitive runtime dependency.
- Namespace and `internal` visibility chosen so two consumers embedding the same sources never collide and nothing leaks publicly.
- Decide on a root namespace convention for the embedded types that won't clash with consumer code.

## What goes in the shared source package (candidates, not commitments)

Kept deliberately minimal until real use justifies more:

- **Component-type model**: type-code ↔ name mapping, and `objectid` / polymorphic-lookup resolution helpers. Needed by the Pre-Flight plugin now, and plausibly by the Snapshot plugin's metadata handling.
- **Component enumeration**: reading components from a solution and/or environment in a consistent shape. Useful to both Snapshot and Pre-Flight.
- **Canonicalization**: XML/metadata canonicalize-then-hash helpers — used by the Snapshot plugin; potentially useful to Pre-Flight for comparing component definitions.

Let real usage pull functionality into the shared package rather than pushing it there speculatively. **If only one plugin ever needs a given helper, it stays in that plugin.** The package earns each addition by being needed in more than one place.

## Consumers

- PCF Scaffold Orchestrator — light/optional consumer (metadata reading at most).
- Component Snapshot & Drift Tracker — canonicalization, component enumeration, component-type model.
- Multi-Solution Import Dependency Pre-Flight — component-type model, component enumeration.

Each consumes independently; none is rebuilt because another upgraded.

## Risks / open questions

- Getting the MSBuild/`contentFiles` wiring right so sources compile cleanly into each consumer without manual steps — the main upfront cost; validate with one consumer before the others adopt.
- Avoiding premature generalization — resist moving code here until a second consumer genuinely needs it.
- `internal` embedding means each consumer carries its own copy; that's intended (decoupling), but be deliberate about it so behavior stays consistent across plugins that embed the same version.
