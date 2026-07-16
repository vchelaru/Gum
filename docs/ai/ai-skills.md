# AI Skills

A **skill** is a small, self-contained document that teaches an AI assistant how to work in a particular area. Tools such as Claude Code load a skill automatically when its description matches what you are doing, giving the assistant durable, concept-level context it would otherwise lack — for example, how Gum's file formats are structured, how the layout system behaves, or when to reach for a Forms control versus a raw visual.

Gum ships a set of **consumer-facing skills** you can drop into your own project so your assistant understands Gum without you having to explain it every time.

## Getting the skills

The skills live in the [`gum-skills/`](../../gum-skills) folder of the Gum repository. To use them, copy the skills you want into your project's `.claude/skills/` folder (or your machine-wide `~/.claude/skills/` folder so they apply everywhere).

A good starting set is the overview skill plus whichever topical skills match your work — for example layout, Forms controls, and the file format. Each skill is a folder containing a `SKILL.md` file with the standard frontmatter your assistant uses to decide when the skill is relevant.

{% hint style="info" %}
The `gum-skills/` folder is being populated incrementally. If a skill you expect is not there yet, check the folder for the current set. These consumer skills are distinct from the engine-internal skills under Gum's own `.claude/skills/`, which describe how to work _on_ Gum rather than _with_ it.
{% endhint %}

## Skills, the MCP server, and GumCli

The three AI pieces reinforce each other:

* **Skills** give your assistant durable context that loads automatically and shapes _what_ it writes.
* The [MCP Documentation Server](mcp-server.md) lets it look up authoritative, current details on demand.
* [GumCli](gumcli-for-agents.md) lets it generate code, validate the project, and verify the result.

Using all three together gives an assistant the best chance of producing correct Gum UI on the first try.
