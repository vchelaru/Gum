# Gum Repository Guidelines

## Agent Workflow

For every task, invoke the appropriate agent from `.claude/agents/` before proceeding. The agent's instructions provide guidelines for how the task should be performed.

Available agents:
- **coder** — Writing or modifying code (not unit tests) for new features or bugs
- **qa** — Testing, reviewing changes, writing unit tests, and verifying correctness
- **refactoring-specialist** — Refactoring and improving code structure
- **docs-writer** — Writing or updating documentation
- **product-manager** — Breaking down tasks and tracking progress
- **security-auditor** — Security reviews and vulnerability assessments

Select the agent that best matches the task at hand. For tasks that span multiple concerns (e.g., implement a feature and write tests), invoke the relevant agents in sequence.
