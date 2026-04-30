# Claude Working Workflow

This document governs how Claude and the developer work together on this codebase. Read it at the start of every session alongside `CLAUDE.md`.

---

## Branch Strategy

- `main` is always releasable — never commit or push directly to it
- One branch per milestone — name describes the deliverable, not any milestone number
  > Examples: `feat/shared-data-tools`, `feat/image-tools`, `feat/pdf-manipulation`, `feat/doc-conversion`, `feat/seo-release`
- Create the milestone branch at the very start of that milestone's work; merge to `main` via PR when all sections are complete and CI passes
- No sub-branches per section — all section work stays on the milestone branch

---

## Session Start Protocol

Before writing a single line of code:

1. State which milestone branch and which PROGRESS.md section we are about to work on
2. Run `git status` — confirm correct branch, clean working tree, no leftover uncommitted changes from a previous session
3. Run `git log --oneline -5` — orient against recent commits
4. Read the SoT doc(s) for this section (listed at the top of each PROGRESS.md section)
5. Check for any unresolved deviations before proceeding

---

## Working Through a Section

- Complete one PROGRESS.md section at a time — do not start the next section until the current one is committed
- Only touch files that belong to the current section's scope; if a change is needed outside that scope, note it explicitly rather than silently folding it in
- If an unexpected deviation from a SoT document is found mid-section, stop and follow the Deviation Protocol below before continuing

### Checkbox State Convention

PROGRESS.md checkboxes use three states to make progress traceable at a glance:

| State | Meaning |
|---|---|
| `- [ ] Item description` | Not yet started |
| `- [~] Item description` | In progress — work has begun but is not confirmed complete |
| `- [x] Item description` | Complete — built, compiles, and verified |

**When starting a section:** mark each item `[~]` as soon as active work begins on it.

**When completing an item:** change `[~]` to `[x]` only after the item is built and confirmed working (build passes, or test passes if applicable). Never mark `[x]` speculatively.

**Optional detail in brackets:** for items where the completion condition is non-obvious, add a short parenthetical after the description:

```
- [x] Add `FormatDetector` (magic bytes + ZIP disambiguation only — text heuristics in next step)
- [~] Implement `JobDispatcher` (created JobRecord + dispatch loop; SSE writes pending)
```

This keeps the checklist honest and makes partial progress visible across sessions.

---

## Commit Protocol

**Claude never commits or pushes.** After each section is complete, Claude will:

1. Run `git status` to show all changed files
2. Stage only the files relevant to the completed section — provide explicit `git add <file>` commands, never `git add .` or `git add -A`
3. Provide the exact `git commit -m "..."` command ready to copy-paste

**Never add a `Co-Authored-By` trailer** to commit messages. Commit authorship belongs to the developer only.

### Commit Message Format

```
<type>(<scope>): <subject>
```

**Types**

| Type | When |
|---|---|
| `feat` | New working code — a section deliverable |
| `fix` | Bug or regression correction |
| `refactor` | Restructuring with no behaviour change |
| `test` | Adding or updating tests only |
| `docs` | Documentation, PROGRESS.md updates |
| `chore` | Build config, CI, project files, packages |

**Scope** — the layer or component being changed, e.g.:
`shared`, `api`, `client`, `detection`, `registry`, `processors`, `ui`, `tests`, `ci`, `docker`

**Subject rules**
- Imperative present tense ("add", "implement", "fix", not "added" or "fixes")
- ≤ 72 characters
- No trailing period
- No milestone numbers, phase names, sprint references, or ticket numbers

**Good examples**
```
feat(shared): add ToolDefinition record and ToolRegistry query API
feat(detection): implement three-pass FormatDetector with WASM parity
feat(api): wire job dispatch, SSE progress stream, and health endpoints
feat(client): add SseClient JS bridge and typed event deserialisation
fix(processors): detect password-protected PDFs before processing begins
test(detection): cover magic bytes, ZIP disambiguation, and text heuristics
chore(ci): add build and test workflow with coverlet coverage artifact
docs(progress): mark shared-types section complete
```

**Never include** milestone labels (M1, M2…), phase numbers, or "implement milestone X" anywhere in the message.

---

## Bug Fix Protocol

If a bug is found after a section commit — whether discovered during the next section or by a test failure:

- Fix it immediately before starting new section work
- Commit the fix as a standalone `fix(<scope>): …` commit
- Do not bundle the fix into an unrelated section's commit

---

## Deviation Protocol

If code exists that conflicts with a SoT architecture document:

- Stop — do not write new code that works around the deviation
- Report using the exact format from `CLAUDE.md` ("⚠️ DEVIATION DETECTED")
- Wait for explicit developer direction before writing anything further

---

## Pre-Commit Checklist

Run through this before providing any `git commit` command:

- [ ] `git status` shows only files in scope for the completed section
- [ ] No file uses a name that doesn't match its type name exactly
- [ ] No hardcoded rate limit values, timeout values, or size limits — all from config
- [ ] No sensitive values staged (`.env`, connection strings, access keys)
- [ ] No `toolOptions` values appear in any log call
- [ ] PROGRESS.md checkboxes for the completed section are marked `[x]`
- [ ] `dotnet build` exits 0 (warnings are errors — no yellow output)

---

## Test Gate

- Run tests relevant to the completed section before providing the commit command
- If any test fails, fix it before providing the commit command — never provide a commit command for broken code
- Integration tests that require unavailable external services (R2, LibreOffice) may be deferred, but this must be stated explicitly with a follow-up task noted

---

## PROGRESS.md as the Session Log

- At session start: read PROGRESS.md to know exactly where work left off
- During work: mark checkboxes `[x]` as each item is completed
- `PROGRESS.md` + `git log --oneline` together should tell the complete story of what is done and what remains

---

## Pull Request Protocol

When a milestone branch is ready to merge:

- All PROGRESS.md checkboxes for the milestone are `[x]`
- CI passes on the branch
- Claude provides the `gh pr create` command; the developer runs it
- PR title follows the same commit subject rules — no milestone numbers
- PR body summarises what was built and lists the manual verification steps from the PROGRESS.md final QA checklist
