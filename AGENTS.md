# Agent notes for BarengIn

This file is for any AI coding agent working in this repo (Claude Code, Cursor, Codex, etc.),
so different sessions and different tools stay consistent with each other and with the team.
It's a map to the real sources of truth, not a replacement for them — when in doubt, open the
file it points to.

## Project snapshot

BarengIn (aka NebengKampus) is a student carpooling PWA for UGM students, built for a UGM
Junior Project (OOP practicum) course. Backend is ASP.NET Core Web API on .NET 8, C#, in a
strict layered architecture: **Domain → Application → Infrastructure → Api**. Frontend is
React 18 + TypeScript (Vite). Full context: [README.md](README.md).

Team: Altaf — Software Architect/PM · Calvin — Backend · Dimas & Diffie — Frontend. See
[docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for who reviews what.

## The one rule that's easy to break

**`BarengIn.Domain` (`src/BarengIn.Domain`) must stay 100% dependency-free.** No
`<ProjectReference>`, no EF Core/Npgsql, no external NuGet package of any kind — plain C#/BCL
only (`Guid`, `DateTime`, `TimeSpan`, `List<T>`, etc. are fine). This is enforced by CI. If a
task in Domain seems to need a repository, a hashing library, or a lookup across many entities,
it almost certainly belongs in Application instead — see the next section for how we handled
that exact situation.

## Where things stand

- `BarengIn.Domain` v1 is implemented, straight from the Module 3 class diagram (see
  `README.md`, "Module 3 — Class Design"): enums, `GeoPoint`/`Money` value objects, and the
  `User`/`Passenger`/`Driver`/`Admin`/`Vehicle`/`Trip`/`RideRequest`/`ClassSchedule`/`Faculty`/
  `EmissionFactor` entities.
- `BarengIn.Application`, `BarengIn.Infrastructure`, `BarengIn.Api`, and the frontend are still
  skeletons (empty folders with `.gitkeep`) — not yet built out.
- Two patterns were established while building Domain v1 and should be **followed, not
  reinvented**, by anyone extending it:
  1. A method whose diagram signature needs something Domain can't have (password hashing, a
     repository query, a lookup by id) throws `NotImplementedException` with a message naming
     what's missing and which layer resolves it.
  2. A composition/aggregation relationship in the diagram that has no backing attribute gets a
     private `List<T>` plus a small, diagram-external accessor method (e.g.
     `Driver.RecordPublishedTrip`) to populate it, and a public `IReadOnlyList<T>` to read it.

  Full reasoning and consequences: [docs/decisions/0001-domain-layer-infrastructure-boundary.md](docs/decisions/0001-domain-layer-infrastructure-boundary.md).

## Conventions

Branching, commit messages, PR review, and the Definition of Done are all defined in
[docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) — don't duplicate them here, that file is the
source of truth. In short: branch from latest `develop`, Conventional Commits, PR into `develop`
with one approval.

## Architecture decisions

Significant technical decisions are recorded as ADRs in `docs/decisions/`. Check there before
making an architectural choice that isn't obvious from the code, and add a new numbered ADR
when you make one yourself.
