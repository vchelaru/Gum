# Using Gum with AI

If you build game UI with the help of an AI assistant — such as Claude Code, Cursor, GitHub Copilot, or any other LLM-based coding tool — Gum gives your assistant several ways to produce correct, up-to-date output instead of relying on whatever it happened to learn during training.

This section is the single home for that story. It is aimed at **developers using an AI assistant to build a Gum UI**, not at contributors working on Gum itself.

## Why this matters

Game UI frameworks change, and an AI assistant's training data is always somewhat stale. Left on its own, an assistant will guess at file formats, invent properties that do not exist, and produce layout that does not behave the way you expect. Gum closes that gap with three complementary pieces:

* A **live documentation server** your assistant can query on demand, so its answers come from the current docs rather than memory.
* A **command-line tool** (`gumcli`) the assistant can drive to generate code, validate projects, and render screenshots to verify its own work.
* A set of **drop-in skills** that give the assistant concept-level guidance on how Gum projects are structured.

## The pieces

* [MCP Documentation Server](mcp-server.md) — connect your AI assistant directly to Gum's documentation so it can search and read authoritative guidance live.
* [GumCli for Agents](gumcli-for-agents.md) — a recipe-oriented walkthrough of the commands an agent uses most: generating code, checking for errors, and capturing screenshots for self-verification.
* [AI Skills](ai-skills.md) — reusable, consumer-facing skills you can drop into your project so your assistant understands Gum's file formats, layout system, and Forms controls.

{% hint style="info" %}
These pieces are complementary, not exclusive. The MCP server keeps your assistant's knowledge current, the skills give it durable context that loads automatically, and `gumcli` lets it act on a project and check the result.
{% endhint %}
