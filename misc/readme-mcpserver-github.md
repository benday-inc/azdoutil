## MCP Server (AI assistant integration)
azdoutil can run as a [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server so an AI assistant (GitHub Copilot, Claude, etc.) can answer delivery questions in plain language — "how long does stuff usually take?", "when will these 10 items be done?", "what's stuck?" — by calling azdoutil's flow metrics calculations directly.

Start the server with:

`azdoutil mcp-server`

The command takes no arguments and communicates over stdio. Connection details come from your stored azdoutil configurations (see [Getting Started](#getting-started)): a tool call can name a configuration explicitly, otherwise the server uses the `AZDO_CONFIG_NAME` environment variable if it's set, and falls back to your default configuration. Because azdoutil is a global .NET tool, the server works per-machine — register it once at user scope and it's available everywhere.

### Setting it up in your AI client

Run `azdoutil mcp-config` to print ready-to-paste configuration for every supported client, or let azdoutil register the server for you at user (per-machine) scope:

```
azdoutil mcp-config --install                       # Claude Code, default configuration
azdoutil mcp-config --install --config myconfig      # Claude Code, using "myconfig"
azdoutil mcp-config --install --client vscode --config myconfig
azdoutil mcp-config --uninstall                     # remove from Claude Code
azdoutil mcp-config --config myconfig               # just print instructions, change nothing
```

**Claude Code (CLI)** — or register it yourself (omit `-e AZDO_CONFIG_NAME=...` to use the default configuration):

```
claude mcp add azdoutil -s user -e AZDO_CONFIG_NAME=myconfig -- azdoutil mcp-server
```

**Claude Desktop (GUI)** — open **Settings → Developer → Edit Config** and add under `mcpServers` (restart Claude Desktop afterward). The file lives at `%APPDATA%\Claude\claude_desktop_config.json` (Windows) or `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS):

```json
{
  "mcpServers": {
    "azdoutil": {
      "command": "azdoutil",
      "args": ["mcp-server"],
      "env": { "AZDO_CONFIG_NAME": "myconfig" }
    }
  }
}
```

**VS Code (GitHub Copilot)** — run the **MCP: Open User Configuration** command (for all workspaces) or create `.vscode/mcp.json` (for one workspace) and add under `servers`, then open Copilot Chat in **Agent** mode:

```json
{
  "servers": {
    "azdoutil": {
      "type": "stdio",
      "command": "azdoutil",
      "args": ["mcp-server"],
      "env": { "AZDO_CONFIG_NAME": "myconfig" }
    }
  }
}
```

**Visual Studio 2022 (17.14+) / Visual Studio 2026 (GitHub Copilot)** — create `%USERPROFILE%\.mcp.json` (all solutions) or `<solutiondir>\.mcp.json` (one solution) with the same `servers` entry as VS Code above, then open Copilot Chat, choose **Agent**, and enable the azdoutil tools.

**Cursor** — add the same entry as Claude Desktop (under `mcpServers`) to `~/.cursor/mcp.json`.

### Getting the assistant to actually use the tools ("routing")
You don't manually route a question to an MCP server — the assistant chooses tools based on their **descriptions**, which is why azdoutil's tools are named and described in outcome language (`get_aging_work`, "what's stuck?"). The server also sends startup **instructions** telling the client when to reach for these tools. To make routing reliable:

- **Ask in plain language that matches a tool's job**: "How long do work items usually take in *ProjectX*?", "When will these 12 items be done?", "What's stuck in *ProjectX* right now?" The assistant maps these to `get_typical_delivery_window`, `forecast_completion_date`, and `get_aging_work`.
- **Name the tool or server when you want to be explicit**: "Use the azdoutil `get_project_summary` tool for *ProjectX*." In VS Code / Visual Studio Agent mode you can also select the tools with the tools (wrench) icon; in Claude Code run `/mcp` to see the server and its tools.
- **Bias routing per project** by adding a line to your `CLAUDE.md` / project instructions, e.g. *"For Azure DevOps delivery questions (cycle time, throughput, forecasts, aging work), use the azdoutil MCP tools."*
- **Tell it which connection** if you have several configs: "…using the `myconfig` configuration", or set `AZDO_CONFIG_NAME` so it doesn't have to ask.

### Available tools
| Tool | What it answers |
| --- | --- |
| `get_typical_delivery_window` | "How long does stuff usually take?" — cycle time percentiles (50th/85th/95th). |
| `get_throughput` | "How much are we getting done?" — throughput and cycle time over a date range. |
| `forecast_completion_date` | "When will these N items be done?" — Monte Carlo forecast of weeks needed. |
| `forecast_items_in_timeframe` | "How much can we get done in N weeks?" — Monte Carlo forecast of item counts. |
| `get_aging_work` | "What's stuck?" — in-progress items aging beyond the typical delivery window. |
| `get_project_summary` | "How's the project going?" — combined throughput, delivery window, and aging headlines. |
| `list_configurations` | "What are you connected to?" — the Azure DevOps configurations azdoutil knows about (never returns tokens). |

These **read-only context tools** help the assistant discover the right project/team/query names (e.g. to feed the flow-metrics tools) without needing a second MCP server:

| Tool | What it answers |
| --- | --- |
| `list_team_projects` | "What projects are there?" — team projects in the org/collection. |
| `get_project_info` | Details for one project (id, URL, state, process). |
| `list_teams` | Teams in a project (find the exact team name for team-scoped flow metrics). |
| `list_process_templates` | Process templates available in the org (Scrum, Agile, Basic, inherited). |
| `get_work_item_types` | Work item types in a project (PBI, Bug, Task, …). |
| `get_work_item_type_states` | Workflow states for a work item type (New → Done). |
| `list_work_item_queries` | Saved work item queries in a project. |
| `run_work_item_query` | Run a saved query by name and return the matching items. |
| `list_git_repositories` | Git repositories in a project. |
| `analyze_repository` | Build-readiness analysis of a repo (languages/build files) without cloning it. |

And a discovery tool so the assistant can fall back to the command line for anything not (yet) exposed as a tool:

| Tool | What it does |
| --- | --- |
| `discover_cli_commands` | Searches the full azdoutil command catalog and returns matching commands with their arguments and an example command line. When you ask for an Azure DevOps task that has no dedicated tool, the assistant can use this to tell you the exact `azdoutil …` command to run — and because it knows which commands are already tools, it won't send you to the CLI unnecessarily. The catalog is generated from the same command metadata as `azdoutil --json`, so it always matches the installed version. |

> **Note:** the MCP server is purely additive — every existing CLI command continues to work exactly as before. The action tools above are all read-only; commands that create or change Azure DevOps state are intentionally not exposed as MCP tools yet, but `discover_cli_commands` still surfaces them so you can run them from the command line.
