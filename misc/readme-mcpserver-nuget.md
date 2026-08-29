## MCP Server (AI assistant integration)
azdoutil can run as a [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server so an AI assistant (GitHub Copilot, Claude, etc.) can answer delivery questions in plain language — "how long does stuff usually take?", "when will these 10 items be done?", "what's stuck?" — by calling azdoutil's flow metrics calculations directly.

Start it with `azdoutil mcp-server`. Run `azdoutil mcp-config` to print ready-to-paste setup for Claude Code, Claude Desktop, VS Code, Visual Studio 2022/2026, and Cursor — or run `azdoutil mcp-config --install` and let it register the server for you at user (per-machine) scope. See the [GitHub README](https://github.com/benday-inc/azdoutil#mcp-server-ai-assistant-integration) for full setup and routing details.

The tools are all read-only:

| Tool | What it answers |
| --- | --- |
| `get_typical_delivery_window` | "How long does stuff usually take?" — cycle time percentiles (50th/85th/95th). |
| `get_throughput` | "How much are we getting done?" — throughput and cycle time over a date range. |
| `forecast_completion_date` | "When will these N items be done?" — Monte Carlo forecast of weeks needed. |
| `forecast_items_in_timeframe` | "How much can we get done in N weeks?" — Monte Carlo forecast of item counts. |
| `get_aging_work` | "What's stuck?" — in-progress items aging beyond the typical delivery window. |
| `get_project_summary` | "How's the project going?" — combined throughput, delivery window, and aging headlines. |
| `list_configurations` | The Azure DevOps configurations azdoutil knows about (never returns tokens). |
| `list_team_projects` | Team projects in the org/collection. |
| `get_project_info` | Details for one project (id, URL, state, process). |
| `list_teams` | Teams in a project. |
| `list_process_templates` | Process templates available in the org. |
| `get_work_item_types` | Work item types in a project (PBI, Bug, Task, …). |
| `get_work_item_type_states` | Workflow states for a work item type (New → Done). |
| `list_work_item_queries` | Saved work item queries in a project. |
| `run_work_item_query` | Run a saved query by name and return the matching items. |
| `list_git_repositories` | Git repositories in a project. |
| `analyze_repository` | Build-readiness analysis of a repo without cloning it. |
| `discover_cli_commands` | Finds the right `azdoutil` command line for anything not exposed as a tool. |
