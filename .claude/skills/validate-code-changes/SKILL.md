---
name: validate-code-changes
description: Validate code changes on the current branch — spawns QA + refactoring agents in parallel to check correctness, edge cases, quality. Use before merging.
disable-model-invocation: true
---

Invoking product-manager agent for this task.

You are coordinating a code validation review for the current branch. Your job is to spawn the **qa** and **refactoring-specialist** agents in parallel to validate all changes on this branch.

## Steps

1. **Identify changes**: Run `git diff main...HEAD --stat` to see which files changed, then `git diff main...HEAD` for the full diff. Also run `git log main..HEAD --oneline` to see the commit history.

2. **Spawn agents in parallel**: Launch both agents using the Task tool concurrently:

   - **qa agent** (`subagent_type: qa`): Review all changes on this branch for correctness, edge cases, regressions, and potential bugs. Look at the git diff against main to understand what changed and validate the implementation.

   - **refactoring-specialist agent** (`subagent_type: refactoring-specialist`): Review all changes on this branch for code quality, structure, naming, duplication, and adherence to project patterns. Look at the git diff against main to understand what changed and suggest improvements.

3. **Summarize findings**: After both agents complete, compile a consolidated report with:
   - **QA findings**: Bugs, edge cases, risks (high/medium/low)
   - **Refactoring findings**: Code smells, structural improvements, pattern violations
   - **Recommended actions**: Prioritized list of changes to make before merging

## Context for this request

$ARGUMENTS
