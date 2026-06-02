<!--
Sync Impact Report
- Version change: N/A (template) -> 1.0.0
- Modified principles:
	- Template Principle 1 -> I. Code Quality Is Non-Negotiable
	- Template Principle 2 -> II. Testing Is a Delivery Gate
	- Template Principle 3 -> III. UX Consistency Is Product Quality
	- Template Principle 4 -> IV. Performance Budgets Are Requirements
	- Template Principle 5 -> V. Observable and Safe Evolution
- Added sections:
	- Engineering Standards and Constraints
	- Delivery Workflow and Quality Gates
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

### I. Code Quality Is Non-Negotiable
All production code MUST pass linting, static analysis, and peer review before merge.
Complex functions MUST be refactored when readability or maintainability degrades, and
public behavior changes MUST be documented in the related spec or task artifact.
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

### V. Observable and Safe Evolution
Behavioral changes MUST include structured logging and diagnostics sufficient for root
cause analysis. Breaking changes to APIs, contracts, or user flows MUST be called out in
specs and release notes with migration guidance when applicable.
Rationale: Observability and explicit change management enable safe, continuous delivery.

## Engineering Standards and Constraints

- Define quality and performance acceptance criteria in `spec.md` and `plan.md` before
	implementation.
- Treat unresolved quality, UX, or performance risks as blockers for release readiness.
- Keep architecture and implementation decisions traceable to user stories and success
	criteria.
- When requirements are unclear, document assumptions explicitly and resolve them before
	coding high-risk areas.

## Delivery Workflow and Quality Gates

1. Planning MUST define quality, testing, UX consistency, and performance gates.
2. Implementation MUST satisfy failing-then-passing automated tests for changed behavior.
3. Review MUST verify coding standards, UX consistency checks, and performance evidence.
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

**Version**: 1.0.0 | **Ratified**: 2026-06-02 | **Last Amended**: 2026-06-02
