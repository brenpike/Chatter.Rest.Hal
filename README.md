# Codex Review Loop Agent Framework Files

This package regenerates the uploaded SemVer-aware agent framework with the Codex PR review feedback loop added.

## Layout

```text
/
├─ CLAUDE.md
├─ AGENTS.md
├─ agent-system-policy.md
├─ branching-pr-workflow.md
├─ pr-review-remediation-loop.md
├─ docs/
│  └─ versioning.md
└─ .claude/
   ├─ agents/
   │  ├─ orchestrator.md
   │  ├─ planner.md
   │  ├─ coder.md
   │  └─ designer.md
   └─ skills/
      ├─ create-working-branch/
      │  └─ SKILL.md
      ├─ checkpoint-commit/
      │  └─ SKILL.md
      ├─ open-plan-pr/
      │  └─ SKILL.md
      ├─ request-codex-review/
      │  └─ SKILL.md
      └─ remediate-codex-review/
         ├─ SKILL.md
         └─ github-pr-review-graphql.md
```
