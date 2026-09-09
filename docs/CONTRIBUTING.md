# Contributing to BarengIn

This is the working agreement for our team. It exists so that nobody has to guess where to branch from, what to call things, or when a piece of work counts as finished. Read it once properly; after that you will mostly need the two tables and the Definition of Done.

**Who does what**

| Name | Role |
| --- | --- |
| Altaf Parves Shua Ilham | Software Architect / Project Manager |
| Calvin | Backend |
| M Dimas Dwi Ananda | Frontend |
| Diffie Alfierie Iswanto | Frontend |

---

## 1. Branching model

We use three levels of branch, and each has one job.

**`main`** is protected and always demo-ready. If a lecturer or tutor clones this repo at a random moment, `main` must build and run. Every module submission gets tagged here, so `main` is also our submission history. Nobody pushes to `main` directly.

**`develop`** is the integration branch. It is where finished work lands and where features meet each other for the first time. It is also protected, and also never receives direct commits.

**Feature branches** are where you actually work. They start from `develop`, hold one focused piece of work, and go back into `develop` through a pull request.

```
main        ────●───────────────────●──────────  (protected, tagged per module)
                 ╲                 ╱
develop     ──────●───●───●───●───●────────────  (protected, integration)
                   ╲   ╲   ╲   ╱
feature            ●    ●   ●─●                  (your work lives here)
```

So the flow for any task is: branch from latest `develop` → commit → push → open a PR into `develop` → get one approval → merge. Periodically we merge `develop` into `main` and tag it for a module submission.

Direct commits to `main` or `develop` are blocked by branch protection. This is not a trust problem; it is so that every change has a reviewer who has seen it, which is exactly what the course asks us to demonstrate.

## 2. Branch naming

```
<type>/<area>/<short-kebab-description>
```

**Types**

| Type | Use it for |
| --- | --- |
| `feature` | New functionality |
| `fix` | Fixing something that is broken |
| `chore` | Tooling, config, dependencies, repo housekeeping |
| `docs` | Documentation only |

**Areas**

| Area | Meaning |
| --- | --- |
| `be` | Backend: domain, application, infrastructure |
| `fe` | Frontend |
| `api` | API surface: controllers, DTOs, Swagger contracts |
| `infra` | CI, deployment, repo configuration |
| `docs` | Documentation and course deliverables |

The description is lowercase, hyphen-separated, and short enough to read in a branch list. Aim for three to five words.

Examples:

```
feature/be/ride-matching-score
fix/api/request-expiry-timezone
docs/module-2/use-case-diagram
```

## 3. The three-day branch rule

Every branch starts from the latest `develop`, should represent no more than about three days of work, and gets rebased onto `develop` daily while it is open:

```bash
git checkout develop && git pull
git checkout feature/be/ride-matching-score
git rebase develop
```

**Why this matters more than it looks like it does.** A branch that lives for two weeks is not just an old branch, it is a branch that was written against a version of the codebase that no longer exists. Every day it stays open, the gap between it and `develop` widens, and the merge at the end stops being a merge and becomes a small archaeology project. This is the single most reliable way student projects fall apart: everyone works in isolation for a month, everything works individually, and then in week 12 nothing integrates and there is no time left to fix it.

Rebasing daily turns that one catastrophic conflict into a handful of small ones you resolve while you still remember the code. If a task genuinely cannot fit in three days, split it into pieces that can — an interface and a stub first, then the implementation — rather than letting one branch run long.

## 4. Commit messages: Conventional Commits

```
<type>(<scope>): <description>
```

The type is one of `feat`, `fix`, `chore`, `docs`, `refactor`, `test`. The scope is the part of the system you touched. The description is imperative mood ("add", not "added"), lowercase, and no full stop at the end.

Three real examples:

```
feat(matching): add detour distance to score
fix(api): correct request expiry timezone
docs(module-2): add use case diagram
```

**Why we are strict about this.** The progress report is written directly from `git log`. A log full of `update`, `fix bug`, and `asdf` produces a progress report that has to be reconstructed from memory, badly, the night before it is due. A log of well-formed Conventional Commits produces most of that report for free, and also lets any of us answer "when did this behaviour change and why" in about ten seconds.

Write the commit message for the person reading the log in week 14. That person is you.

## 5. Pull requests

Every PR into `develop` needs **one approval** before it merges.

**Who reviews what.** Backend PRs go to Altaf. Frontend PRs are cross-reviewed between Dimas and Diffie, with Altaf as the tiebreak if they disagree or if one of them is unavailable. `CODEOWNERS` requests the right reviewer automatically for changes under `BarengIn.Domain/` and `BarengIn.Application/`.

Fill in the PR template properly. The Summary and How to test sections are what make a review take ten minutes instead of an hour: if the reviewer has to guess how to exercise your change, they will either ask you and wait, or approve without really checking. Neither is good.

Reviewing is a real task, not a rubber stamp. Pull the branch, run it, and confirm the change does what the description says. If you approve something you did not actually try, you own the bug with the author.

**Branches are never deleted after merge.** Do not tick the delete-branch box, and do not tidy up old branches later. Module 1 is assessed partly on visible branch history, and a deleted branch takes its history off the network graph. Branch protection has deletions disabled on `main` and `develop`, but feature branches rely on us simply not doing it.

## 6. Definition of Done (PRD 12.3)

A task is not done because the code works on your machine. It is done when all of these are true:

- [ ] Code is merged to `develop` through an approved pull request
- [ ] **Backend:** the endpoint is visible in Swagger and returns the correct response
- [ ] **Frontend:** the flow works end to end against the deployed API
- [ ] Unit tests exist for any calculation logic (matching scores, impact/emission figures, distance and fare maths)
- [ ] No console errors
- [ ] README or `/docs` is updated if the setup steps changed

That last one is the one people skip. If your change means a teammate has to run a new command, set a new variable, or install something to get the project running, the README change is part of your task, not a follow-up. The test is simple: could a teammate get this running from the README alone, without messaging you?

## 7. Secrets

**Never commit a secret.** No `.env` files, no connection strings, no API keys, not even temporarily and not even in a commit you plan to amend away. This repository is public, and a secret that reaches GitHub is compromised the moment it is pushed — rewriting history afterwards does not un-leak it.

How we handle configuration instead:

- `appsettings.Development.json` is gitignored. It holds your local PostgreSQL connection string and stays on your machine.
- `appsettings.Example.json` **is** committed. It lists every key the application needs, with placeholder values. When you add a new setting, add it here too — this file is how teammates know what to configure.
- For anything genuinely secret, use .NET user secrets, which stores values outside the repository entirely:

  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=barengin;Username=..."
  ```

- Frontend environment files follow the same pattern: `.env` is ignored, `.env.example` is committed.

Before you push, look at your own diff. `git diff --staged` takes five seconds and catches almost every accidental secret.

If a secret does get committed, say so immediately in the group chat. Rotating a leaked key quickly is a small problem; a leaked key nobody mentioned is a much larger one.
