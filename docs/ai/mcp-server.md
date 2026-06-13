# MCP Documentation Server

Gum's documentation is available to AI assistants through a hosted **MCP server**. Connecting your assistant to it lets the assistant search and read the current Gum docs on demand, so its answers reflect the latest guidance instead of stale training data.

## What is MCP?

The **Model Context Protocol (MCP)** is an open standard that lets AI assistants connect to external tools and data sources. An MCP server exposes a set of capabilities — in this case, searching and fetching documentation pages — that a compatible assistant can call while it works.

Most modern AI coding tools support MCP, including Claude Code, Claude Desktop, Cursor, and VS Code.

## Endpoint

The Gum documentation MCP server is hosted at:

```
https://docs.flatredball.com/gum/~gitbook/mcp
```

It exposes tools to **search** the documentation and **fetch** individual pages, so your assistant can look up authoritative answers about layout units, Forms controls, code generation, the file formats, and everything else covered in these docs.

## Connecting your assistant

### Claude Code

Add the server from your terminal:

```
claude mcp add --transport http gum-docs https://docs.flatredball.com/gum/~gitbook/mcp
```

Once added, your assistant can query the Gum docs during any session. Use `claude mcp list` to confirm it is connected.

### Cursor, VS Code, Claude Desktop, and other clients

Most clients are configured with an `mcpServers` block in a JSON settings file. Add an entry pointing at the endpoint:

```json
{
  "mcpServers": {
    "gum-docs": {
      "url": "https://docs.flatredball.com/gum/~gitbook/mcp"
    }
  }
}
```

The exact file location and field names vary by client — check your tool's MCP documentation for where this config lives. Some clients label the remote-HTTP transport explicitly (for example with a `"type": "http"` field).

{% hint style="info" %}
A few clients can only launch MCP servers as local processes (stdio transport) and cannot connect to a remote URL directly. For those, bridge to the hosted server with [`mcp-remote`](https://www.npmjs.com/package/mcp-remote):

```json
{
  "mcpServers": {
    "gum-docs": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "https://docs.flatredball.com/gum/~gitbook/mcp"]
    }
  }
}
```
{% endhint %}

## Using it

Once connected, you do not need to do anything special — just ask your assistant Gum questions as usual. When it needs authoritative information, it can search and read the docs through the server rather than guessing. For tasks that involve writing or generating code, pair this with [GumCli for Agents](gumcli-for-agents.md) so the assistant can also act on a project and verify the result.
