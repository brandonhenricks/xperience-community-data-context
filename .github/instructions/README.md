# XperienceCommunity.DataContext - Documentation Index

This directory contains comprehensive AI-assisted development documentation for the XperienceCommunity.DataContext library. These files serve as context for AI coding assistants and as reference materials for human developers.

---

## 📚 Documentation Files

### 1. [ARCHITECTURE.instructions.md](ARCHITECTURE.instructions.md)
**Purpose**: Architectural patterns, layers, and design decisions

**Key Topics**:
- Three-context pattern (ContentItem, PageContent, ReusableSchema)
- Layered architecture (Abstractions, Core, Contexts, Expressions, etc.)
- Expression processing pipeline (Visitor + Strategy patterns)
- Data flow diagrams (query execution, caching)
- Design patterns (Template Method, Factory, Decorator, etc.)
- Architectural risks and anti-patterns
- Improvement opportunities

**When to Read**: Understanding overall system design, extending architecture, evaluating design decisions

---

### 2. [CODE_EXEMPLARS.instructions.md](CODE_EXEMPLARS.instructions.md)
**Purpose**: Reference implementations demonstrating best practices

**Key Topics**:
- Clean architecture examples (`BaseDataContext`)
- Visitor pattern implementation (`ContentItemQueryExpressionVisitor`)
- Defensive programming (`BinaryExpressionProcessor`)
- Fluent builder pattern (`XperienceContextBuilder`)
- Dependency injection patterns
- Async/await best practices
- Testing patterns (AAA, mocking)
- Exception handling
- Diagnostics attributes
- Performance optimizations

**When to Read**: Writing new code, reviewing code, learning patterns, establishing conventions

---

### 3. [CODING_STANDARDS.instructions.md](CODING_STANDARDS.instructions.md)
**Purpose**: Coding conventions, style guidelines, and quality standards

**Key Topics**:
- Naming conventions (PascalCase, camelCase, _privateFields)
- Code organization (namespace structure, member order)
- Formatting rules (indentation, braces, whitespace)
- Modern C# features (pattern matching, nullable types, etc.)
- Async/await rules
- Error handling patterns
- Nullability guidelines
- Comments and documentation
- Dependency injection standards
- Performance considerations
- Testing standards

**When to Read**: Before writing code, during code reviews, when onboarding new contributors

---

### 4. [PROJECT_FOLDER_STRUCTURE.instructions.md](PROJECT_FOLDER_STRUCTURE.instructions.md)
**Purpose**: Complete directory tree with annotations

**Key Topics**:
- Full folder structure (40+ directories)
- File naming conventions
- Folder organization principles
- Build artifacts and ignored files
- Navigation tips
- Multi-targeting structure
- Central Package Management layout
- Test organization

**When to Read**: Navigating the codebase, adding new files, understanding project layout

---

### 5. [TECHNOLOGY_STACK.instructions.md](TECHNOLOGY_STACK.instructions.md)
**Purpose**: Technologies, frameworks, libraries, and tools

**Key Topics**:
- .NET platform (8.0, 9.0, C# latest)
- Core dependencies (Kentico Xperience 30.3.1+, CSharpFunctionalExtensions)
- Development dependencies (Source Link, Logging)
- Testing stack (xUnit, NSubstitute, Coverlet)
- Build & packaging (CPM, lock files)
- CI/CD technologies (GitHub Actions)
- Diagnostics & telemetry (OpenTelemetry, performance counters)
- Version constraints
- Future technology considerations

**When to Read**: Setting up dev environment, updating dependencies, understanding integrations

---

### 6. [UNIT_TESTS.instructions.md](UNIT_TESTS.instructions.md)
**Purpose**: Testing strategy, patterns, and coverage goals

**Key Topics**:
- Testing framework (xUnit + NSubstitute)
- Test project structure
- Test naming convention (`Method_Scenario_ExpectedOutcome`)
- AAA pattern (Arrange, Act, Assert)
- Mocking strategy with NSubstitute
- Test categories (contexts, processors, executors, builders)
- Coverage goals (80% target, 75% current)
- Running tests (CLI, VS, VS Code)
- CI/CD integration
- Test data management
- Best practices (do's and don'ts)
- Coverage gaps and recommendations

**When to Read**: Writing tests, reviewing test coverage, improving test quality

---

### 7. [WORKFLOW_ANALYSIS.instructions.md](WORKFLOW_ANALYSIS.instructions.md)
**Purpose**: Developer workflows, CI/CD pipelines, release management

**Key Topics**:
- Local development workflow
- Feature development process
- Bug fix workflow
- CI/CD pipeline (Build & Test, Release)
- Pull request workflow
- Release management (versioning, process)
- Automation opportunities (dependency updates, coverage reporting, etc.)
- Identified bottlenecks
- Workflow metrics and KPIs
- Best practices for contributors and maintainers

**When to Read**: Understanding processes, contributing to the project, improving workflows, managing releases

---

## 🎯 Quick Start by Role

### For New Contributors
1. Start with [PROJECT_FOLDER_STRUCTURE.instructions.md](PROJECT_FOLDER_STRUCTURE.instructions.md) - Understand the layout
2. Read [CODING_STANDARDS.instructions.md](CODING_STANDARDS.instructions.md) - Learn conventions
3. Review [CODE_EXEMPLARS.instructions.md](CODE_EXEMPLARS.instructions.md) - See best practices
4. Check [WORKFLOW_ANALYSIS.instructions.md](WORKFLOW_ANALYSIS.instructions.md) - Understand processes

### For Architects/Designers
1. Start with [ARCHITECTURE.instructions.md](ARCHITECTURE.instructions.md) - Understand design
2. Review [CODE_EXEMPLARS.instructions.md](CODE_EXEMPLARS.instructions.md) - See patterns
3. Check [TECHNOLOGY_STACK.instructions.md](TECHNOLOGY_STACK.instructions.md) - Understand constraints

### For QA/Testers
1. Start with [UNIT_TESTS.instructions.md](UNIT_TESTS.instructions.md) - Learn testing strategy
2. Review [WORKFLOW_ANALYSIS.instructions.md](WORKFLOW_ANALYSIS.instructions.md) - Understand CI/CD
3. Check [TECHNOLOGY_STACK.instructions.md](TECHNOLOGY_STACK.instructions.md) - Know test tools

### For DevOps/Release Managers
1. Start with [WORKFLOW_ANALYSIS.instructions.md](WORKFLOW_ANALYSIS.instructions.md) - Understand pipelines
2. Review [TECHNOLOGY_STACK.instructions.md](TECHNOLOGY_STACK.instructions.md) - Know tools
3. Check [ARCHITECTURE.instructions.md](ARCHITECTURE.instructions.md) - Understand dependencies

### For AI Coding Assistants
**Load all files as context** - These documents provide comprehensive project knowledge for accurate code generation, review, and suggestions.

---

## 📊 Documentation Statistics

| Metric | Value |
|--------|-------|
| Total Documentation Files | 7 |
| Total Pages (estimated) | ~100+ |
| Total Words (estimated) | ~25,000+ |
| Topics Covered | 100+ |
| Code Examples | 150+ |
| Coverage | Architecture, Code Quality, Testing, Workflows |

---

## 🔄 Maintenance

### Review Schedule
- **ARCHITECTURE.instructions.md**: Annually or with major changes
- **CODE_EXEMPLARS.instructions.md**: Quarterly
- **CODING_STANDARDS.instructions.md**: Quarterly or when adding contributors
- **PROJECT_FOLDER_STRUCTURE.instructions.md**: As structure changes
- **TECHNOLOGY_STACK.instructions.md**: Quarterly (after dependency updates)
- **UNIT_TESTS.instructions.md**: With each PR + quarterly audit
- **WORKFLOW_ANALYSIS.instructions.md**: Quarterly

### Update Triggers
- Major architectural changes
- New patterns introduced
- Technology upgrades
- Process improvements
- Coverage goals achieved/missed
- Team feedback

---

## 🤝 Contributing to Documentation

If you find gaps, errors, or areas for improvement:

1. Create an issue describing the documentation problem
2. Submit a PR with documentation updates
3. Follow the same standards as code contributions
4. Ensure all links and references are valid

---

## 📝 Document Format

All instruction files follow this structure:
- **Markdown format** (`.instructions.md` extension)
- **Clear headings** (hierarchical structure)
- **Code examples** (with syntax highlighting)
- **Tables and lists** (for readability)
- **Metadata footer** (Last Updated, Review Frequency)

---

**Created**: December 28, 2025
**Last Updated**: December 28, 2025
**Maintained By**: Brandon Henricks
**Purpose**: AI-assisted development & human reference
