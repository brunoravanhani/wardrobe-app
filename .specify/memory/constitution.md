<!--
Sync Impact Report
- Version change: 1.0.0 -> 1.1.0
- Modified principles:
	- I. Code Quality Is Non-Negotiable -> I. Code Quality and Reuse Are Non-Negotiable
	- V. Observable and Safe Evolution -> V. Secure Configuration and Safe Evolution
- Added sections:
	- None
- Removed sections:
	- None
- Templates requiring updates:
	- ✅ .specify/templates/plan-template.md
	- ✅ .specify/templates/spec-template.md
	- ✅ .specify/templates/tasks-template.md
	- ⚠ pending: .specify/templates/commands/*.md (directory not present in this repository)
	- ✅ .github/copilot-instructions.md
- Follow-up TODOs:
	- None
-->

# Virtual Wardrobe Constitution

## Core Principles

### I. Code Quality and Reuse Are Non-Negotiable
All production code MUST pass linting, static analysis, and peer review before merge.
Complex functions MUST be refactored when readability or maintainability degrades, and
public behavior changes MUST be documented in the related spec or task artifact.
Before creating a new component, developers MUST search for an existing reusable
component and extend or compose it when feasible; new components MUST include a brief
justification when reuse is not possible.
Rationale: Consistent quality reduces defects, onboarding time, and long-term maintenance
cost.

### II. Testing Is a Delivery Gate
Every feature MUST include automated tests aligned to risk: unit tests for logic,
integration tests for boundary interactions, and end-to-end or contract tests where user
journeys or interfaces are affected. A change MUST NOT merge unless new tests fail before
implementation and pass after implementation.
Rationale: Enforced test gates prevent regressions and make behavior changes explicit.

### III. UX Consistency Is Product Quality
User-facing changes MUST conform to shared interaction patterns, terminology, and
accessibility requirements. Equivalent actions MUST behave consistently across screens,
error messaging MUST be clear and actionable, and acceptance criteria MUST include at
least one UX validation scenario.
Rationale: Predictable and accessible experiences improve trust and reduce support burden.

### IV. Performance Budgets Are Requirements
Features MUST define measurable performance targets in planning artifacts and validate
them before release. Any change that risks latency, throughput, memory, bundle size, or
render smoothness MUST include profiling evidence and explicit mitigation tasks.
Rationale: Performance is a core user expectation, not a post-release optimization.

### V. Secure Configuration and Safe Evolution
Keys, connection strings, passwords, and equivalent secrets MUST NOT be hardcoded in
source files. They MUST be loaded from environment-backed configuration such as `.env`
or secure runtime configuration files like `appSettings.json` with environment-specific
overrides and secret-management controls.
Behavioral changes MUST include structured logging and diagnostics sufficient for root
cause analysis. Breaking changes to APIs, contracts, or user flows MUST be called out in
specs and release notes with migration guidance when applicable.
Rationale: Secure configuration, observability, and explicit change management enable
safe, continuous delivery.

## Engineering Standards and Constraints

- Define quality and performance acceptance criteria in `spec.md` and `plan.md` before
	implementation.
- Treat unresolved quality, UX, or performance risks as blockers for release readiness.
- Keep architecture and implementation decisions traceable to user stories and success
	criteria.
- Keep secrets outside source code and out of version control; provide example config
	files that contain placeholders only.
- Prefer composition and extension of existing components over net-new components to
	maintain a coherent design system and reduce duplication.
- When requirements are unclear, document assumptions explicitly and resolve them before
	coding high-risk areas.

## Delivery Workflow and Quality Gates

1. Planning MUST define quality, testing, UX consistency, performance, reuse, and secret
	management gates.
2. Implementation MUST satisfy failing-then-passing automated tests for changed behavior.
3. Review MUST verify coding standards, component reuse checks, secure configuration,
	UX consistency checks, and performance evidence.
4. Pre-release validation MUST confirm no unresolved high-severity defects or budget
	 violations.
5. Deployment approval MUST include documented rollback and monitoring expectations.

## Governance

This constitution supersedes conflicting local practices for planning and implementation.
Amendments require: (a) a written proposal, (b) impact assessment on templates and
workflow artifacts, and (c) approval by project maintainers.

Versioning policy:
- MAJOR: Removal or incompatible redefinition of a core principle or governance rule.
- MINOR: Addition of a principle/section or materially expanded mandatory guidance.
- PATCH: Clarifications, wording improvements, and non-semantic refinements.

Compliance review expectations:
- Every plan and pull request MUST include an explicit constitution compliance check.
- Exceptions MUST be time-bound, documented, and approved by maintainers.
- Periodic reviews SHOULD validate that templates and agent guidance remain aligned.

**Version**: 1.1.0 | **Ratified**: 2026-06-02 | **Last Amended**: 2026-06-02
