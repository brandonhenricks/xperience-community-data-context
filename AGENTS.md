# Agents guide for xperience-community-data-context

Purpose
- Give an automated code assistant (Copilot / code agent) concise, actionable context
  and constraints to work effectively in this repository.

## Repository summary

- Type: .NET class library (multi-targeting .NET 8/.NET 9 in CI/tests)
- Purpose: Fluent, strongly-typed query API wrapper for Kentico Xperience query
  builders. Provides three specialized contexts (content items, pages, reusable
  schemas), expression translation pipeline, caching, and post-query processors.

## High-level architecture

- Three-context pattern: `ContentItemContext<T>`, `PageContentContext<T>`,
  `ReusableSchemaContext<T>` all inherit from `BaseDataContext<T, TExecutor>`.
- Expression translation pipeline: `ContentItemQueryExpressionVisitor` + a set
  of processors in `Expressions/Processors/` that transform LINQ expressions into
  Kentico `ContentItemQueryBuilder` fragments.
- Query execution: `Executors/*` contain query executor implementations that
  interact with Kentico `IContentQueryExecutor` and optionally run
  `IContentItemProcessor<T>`-style post-processors.

## Key files (quick jump)

- Source root: src/XperienceCommunity.DataContext/
- DI and builder: [src/XperienceCommunity.DataContext/DependencyInjection.cs](src/XperienceCommunity.DataContext/DependencyInjection.cs)
- Context base: [src/XperienceCommunity.DataContext/Core/BaseDataContext.cs](src/XperienceCommunity.DataContext/Core/BaseDataContext.cs)
- Expression visitor: [src/XperienceCommunity.DataContext/Expressions/Visitors/ContentItemQueryExpressionVisitor.cs](src/XperienceCommunity.DataContext/Expressions/Visitors/ContentItemQueryExpressionVisitor.cs)
- Context implementations: [src/XperienceCommunity.DataContext/Contexts/](src/XperienceCommunity.DataContext/Contexts/)
- Executors: [src/XperienceCommunity.DataContext/Executors/](src/XperienceCommunity.DataContext/Executors/)
- Tests: [tests/XperienceCommunity.DataContext.Tests/](tests/XperienceCommunity.DataContext.Tests/)
- Config builder: [src/XperienceCommunity.DataContext/Configurations/XperienceContextBuilder.cs](src/XperienceCommunity.DataContext/Configurations/XperienceContextBuilder.cs)

Local dev & verification (how an agent should run things)

- Restore, build and run tests locally (run from repository root):

```powershell
dotnet restore
dotnet build tests/XperienceCommunity.DataContext.Tests/XperienceCommunity.DataContext.Tests.csproj
dotnet test tests/XperienceCommunity.DataContext.Tests/XperienceCommunity.DataContext.Tests.csproj
```

- There are VS Code tasks defined: `build`, `publish`, `watch` (see workspace tasks).

### Coding conventions & constraints
- Keep changes minimal and focused; prefer small, well-scoped edits.
- Follow project conventions: implicit usings, nullable reference types enabled,
  async I/O with `CancellationToken` support.
- Do not add unrelated refactors or large formatting changes.

Testing guidance
- Prefer running specific unit tests related to the change before broad runs.
- Tests use NSubstitute for Kentico dependencies; use the `tests/` project
  when creating or validating behavior changes.

Behavior expectations for agents
- When generating code, adapt to existing patterns: processors in
  `Expressions/Processors/`, visitor pattern, and `BaseDataContext` APIs.
- When adding expression support, create a processor in
  `Expressions/Processors/` and register it where the visitor expects.
- When changing DI, update `DependencyInjection` and `XperienceContextBuilder`
  consistently.

What agents can safely do (examples)
- Implement or extend expression processors (add files under
  `Expressions/Processors/` and tests).
- Add small, focused unit tests and run them.
- Fix bugs that are clearly limited in scope to a single file or small set of
  files (ensure tests pass).

What agents must not do
- Do not attempt to run or modify real Kentico production resources or secrets.
- Avoid large, cross-cutting changes without human review (architecture-level
  redesigns, breaking public APIs, or large dependency changes).

Suggested prompt templates for task requests
- Add expression processor:

  "Add support for `X` expression. Create a processor `XProcessor` in
  `src/.../Expressions/Processors/` and update `ContentItemQueryExpressionVisitor`
  registration. Add unit tests under `tests/` verifying translation and
  execution. Keep changes minimal and run the related tests."

- Fix failing test:

  "Investigate failing test `TestName` in `tests/...`. Run that single test,
  fix the root cause in the implementation, and run the tests again. Keep the
  change small and add an additional unit test that prevents regression."

Developer notes (assumptions)
- Kentico-specific runtime types are mocked in tests (`IContentQueryExecutor`,
  `IProgressiveCache`, `IWebsiteChannelContext`). Agents should prefer unit
  test changes that keep those mocks.
- The library targets Kentico Xperience integration; external system calls
  should be mocked in tests and not executed by an automated agent.

If you're unsure
- Ask the human reviewer for a short clarification: aim for one question that
  unlocks progress (e.g., intended public API change, desired behavior,
  preferred naming).
