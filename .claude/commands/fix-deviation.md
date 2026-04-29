# /project:fix-deviation

Structured process for resolving a detected deviation between the codebase and source of truth documents. Run this when `/project:check-sot` reports a deviation, or when you spot one during development.

## What a deviation is

Any difference between:
- What a `docs/architecture/*.md` document specifies
- What the actual code in `src/` or `tests/` does

Deviations are never silently ignored, patched around, or worked with. They are always surfaced and resolved.

## Step 1 — Characterise the deviation

Identify:
1. Which source of truth document is violated
2. What the document specifies (quote it exactly)
3. What the code does instead
4. Whether the deviation is intentional or accidental

## Step 2 — Assess impact

Determine:
- Is this deviation already live (committed) or still in-progress work?
- Does it affect other parts of the system (e.g. a naming deviation might mean tests are not being discovered)?
- Is it a Critical (breaks functionality), Major (breaks architecture contract), or Minor (style/naming) deviation?

## Step 3 — Present resolution options

Always present both options to the developer:

```
⚠️ DEVIATION DETECTED

Document:  docs/architecture/{XX-name.md}
Section:   {relevant section heading}
Expected:  "{exact quote from document}"
Found:     "{what the code actually does}"
Severity:  Critical | Major | Minor
Impact:    {what this breaks or risks}

Options:
  A) Fix the code — revert the code to match the document
     Changes needed: {list of files and what changes}

  B) Update the document — accept this as an intentional architectural change
     ⚠️  This means the planning decision is being revised.
     Document update needed: {what to change in which doc}
     Downstream impact: {what else might be affected by this change}

Which do you want to do? (A or B)
```

## Step 4A — If fixing the code

1. Make the minimum change to bring the code into conformance
2. Do not refactor or improve anything else while fixing the deviation — single-purpose change
3. Run tests to verify nothing is broken by the fix
4. Run `/project:check-sot` in the affected area to verify the deviation is resolved
5. Confirm with developer before moving on

## Step 4B — If updating the document

This requires explicit developer approval before any document is modified.

Once approved:
1. Update the relevant `docs/architecture/*.md` document
2. Check for downstream impacts — does this change affect other documents?
   - Example: changing a field name in ToolDefinition affects 01-tool-registry.md, 04-processors.md, and CLAUDE.md
3. Update all affected documents in the same change
4. Update `CLAUDE.md` if the change affects the never-do list, naming conventions, or quick reference tables
5. Run `/project:check-sot` to verify the updated documents are now consistent with the code
6. Document the change in `docs/architecture/00-overview.md` under an ADR (Architecture Decision Record) section

## Step 5 — Verify resolution

After either fix path:
```bash
dotnet build
dotnet test
```

Both must pass. Then confirm the deviation no longer appears in `/project:check-sot`.

## Escalation

If the deviation reveals that a fundamental architectural assumption is wrong (e.g. a library cannot do what was assumed, or a .NET WASM constraint prevents the designed approach):

1. Stop all related work immediately
2. Document the constraint clearly
3. Present alternative approaches with tradeoffs
4. Update the relevant SoT document only after the developer has chosen an alternative
5. Check all downstream documents for cascading changes

## What never happens

- A deviation is never silently accepted and worked around
- A source of truth document is never updated without the developer explicitly choosing option B
- New feature work never proceeds while a Critical deviation exists
- The phrase "we'll fix it later" is never used for Critical or Major deviations
