# azdoutil Code Review Findings

**Date:** 2026-07-11
**Reviewer:** Claude Code
**Scope:** Maintainability, testability, and readiness for adding an MCP server interface.
**Focus:** Flow Metrics commands (`agingwork`, `cycletimeconfidence`, `forecastdurationforitemcount`, `forecastitemsinweeks`, `forecastworkitem`, `suggest-sle`, `throughputcycletime`), the Azure DevOps API client, and overall solution structure.

## Severity legend

| Severity | Meaning |
|----------|---------|
| **blocker** | Must fix before building the MCP server. The MCP work is impractical or wrong without it. |
| **refactor** | Should fix before adding MCP. Not strictly blocking, but will cause pain or duplication if left. |
| **cleanup** | Nice to have. Improves quality but can be deferred. |

---

## 1. Solution structure and project organization

### 1.1 Business logic and console I/O are fused inside command classes — **blocker**
Every Flow Metrics calculation lives inside a class that inherits `AzureDevOpsCommandBase → AsynchronousCommand` (Benday.CommandsFramework). The classes are simultaneously responsible for:

- reading CLI arguments (`Arguments.GetInt32Value(...)`),
- building the authenticated `HttpClient` and calling Azure DevOps,
- performing the calculation (percentiles, Monte Carlo simulation, grouping),
- and writing human-readable text to `ITextOutputProvider` (`WriteLine`, `IsQuietMode`).

There is no way to obtain the *numbers* (85th-percentile cycle time, forecast confidence weeks, aging item ages) without instantiating a command, feeding it a parsed `CommandExecutionInfo`, running it, and either scraping stdout or reading a handful of public properties. Several results (e.g. the Monte Carlo confidence levels in `ForecastDurationForItemCountCommand` / `ForecastItemCountInWeeksCommand`) are only ever produced *inside* `WriteLine` calls and are never returned at all.

An MCP tool must return a structured object. As written, the calculation logic cannot be called programmatically and cannot return structured results.

**Fix:** Extract the calculation logic into plain service/calculator classes in the Api project that accept parameters and return DTOs, with no dependency on `ITextOutputProvider`, `ArgumentCollection`, or `CommandExecutionInfo`. Have the existing commands become thin adapters that call the services and format the output. (This is the Phase-1 prerequisite the Phase-2 brief calls out.)

### 1.2 Commands orchestrate other commands by cloning CLI arguments — **refactor**
Composition between flow-metrics operations is done by re-parsing arguments and constructing sibling commands:

```csharp
var args = ExecutionInfo.GetCloneOfArguments(Constants.CommandArgumentNameGetCycleTimeAndThroughput, true);
var getDataCommand = new GetCycleTimeAndThroughputCommand(args, _OutputProvider);
await getDataCommand.ExecuteAsync();
DataGroupedByWeek = getDataCommand.GroupedByWeek;   // read result off a public property
```

This appears in `CalculateSuggestedServiceLevelExpectationCommand`, `CycleTimeConfidenceRangesCommand`, `ForecastDurationForItemCountCommand`, `ForecastItemCountInWeeksCommand`, and `ForecastWorkItemDeliveryCommand`. The "API" of one calculation is therefore a string command name plus a bag of stringly-typed arguments plus a set of `public { get; private set; }` properties. This is brittle (a renamed argument breaks callers silently), hard to test, and forces the MCP layer to speak in CLI arguments rather than typed parameters.

**Fix:** Once calculation logic is in services (1.1), commands should compose *services*, not other commands. The `throughputcycletime` calculation, for example, becomes a `ThroughputService.GetThroughput(config, project, team, days)` call reused by SLE, forecast, and MCP.

### 1.3 Responsibilities between Api and ConsoleUi are otherwise clean — **cleanup / (positive)**
`ConsoleUi` is a genuinely minimal bootstrapper (`Program.cs`, ~30 lines) that hands the assembly to `DefaultProgram`. All commands live in Api. There is no business logic stranded in ConsoleUi and no circular dependency (ConsoleUi → Api only). This is good and means the MCP server can live in Api and be launched from a new command without moving code between projects. No change required beyond adding the new command.

---

## 2. Flow Metrics code specifically

### 2.1 Monte Carlo simulation logic is duplicated across two commands — **refactor**
`ForecastDurationForItemCountCommand` and `ForecastItemCountInWeeksCommand` each contain near-identical `CreateForecast()`, `GetDistribution()`, and a `GetIterationCount`/`GetThroughput` confidence-lookup method. They differ only in (a) whether the inner loop runs a fixed number of weeks or loops until an item target is met, and (b) whether the distribution is keyed by weeks or by throughput. The `1000` simulations, the 50/80/90/99% confidence buckets, and the crypto RNG sampling are copy-pasted.

**Fix:** Extract a single `MonteCarloForecaster` that takes the weekly throughput history and exposes `ForecastWeeksForItemCount(...)` and `ForecastItemsInWeeks(...)`, each returning a distribution + confidence map. Both commands and both MCP tools call it.

### 2.2 Cycle-time / throughput data fetch + team resolution is duplicated three times — **refactor**
`ValidateTeamName()` and the OData `GetData()` URL construction are copy-pasted (with small variations) in `GetCycleTimeAndThroughputCommand`, `GetAgingWorkItemsCommand`, and `ForecastWorkItemDeliveryCommand`. Each rebuilds the `AnalyticsUrl/{project}/_odata/v1.0/...` query string and the `AreaLevel2 eq '{team}'` lookup by hand.

**Fix:** Extract an analytics data-access helper (e.g. `FlowMetricsAnalyticsClient`) with `ResolveTeamAreaAsync(project, team)`, `GetCompletedItemsAsync(project, area, sinceDays)`, and `GetInProgressItemsAsync(project, area)`. Removes ~3 copies of the OData string building and centralizes the URL-encoding.

### 2.3 The confidence levels are computed but never returned — **blocker (for MCP)**
In both forecast commands the 50/80/90/99% numbers exist only as local variables inside `DisplayForecast()` and are emitted via `WriteLine`. The public surface exposes `DataGroupedByWeek` but not the forecast result. An MCP tool has nothing structured to return.

**Fix:** Have the extracted `MonteCarloForecaster` return a result object (`{ Confidence50, Confidence70/80, Confidence85/90, Confidence95/99, Distribution }`) that both the CLI formatter and the MCP tool consume.

### 2.4 `agingwork` writes diagnostic output even in quiet mode — **cleanup (minor correctness)**
`GetAgingWorkItemsCommand.GetData()` calls `WriteLine($"Using analytics URL: {analyticsUrl}")` and `WriteLine($"Getting data for team project ...")` unconditionally, i.e. even when `--quiet` is set. Every other flow-metrics command gates output on `IsQuietMode == false`. Under quiet mode (which the MCP host will effectively want) this pollutes output. Once the fetch moves into a service (2.2) this text should be dropped or moved to the CLI formatter behind the quiet-mode check.

### 2.5 Percentile math is shared but wrapped in an I/O method — **refactor**
The percentile index math is nicely centralized in `Utilities.GetIndexForPercentForecast(...)` and unit-tested. However the higher-level "cycle time at percent P" logic lives in `CalculateSuggestedServiceLevelExpectationCommand.GetCycleTimeAtPercent(int)`, which also emits a `WriteLine` warning and depends on the command's private `_Data`. `CycleTimeConfidenceRangesCommand` reaches into a constructed SLE command to call `GetCycleTimeAtPercent(50)`.

**Fix:** Move percentile-over-a-dataset into a `CycleTimeCalculator.GetCycleTimeAtPercentile(items, percent)` pure function (already trivially derivable from `Utilities`). The low-item-count warning becomes a value on the returned result, not a `WriteLine`.

### 2.6 Shared data models exist and are reused — **cleanup / (positive)**
Commands do share DTOs (`CycleTimeDataResponse`, `WorkItemCycleTimeData`, `AgingWorkItemDataResponse`, `AreaData`, `ThroughputIteration`, `ForecastGroup`, `IterationForecast`) rather than each redefining its own. Good. These become the natural inputs to the extracted services. `ThroughputIteration.AverageCycleTime` even encapsulates a calculation reusably. Minor: these live at the Api project root rather than in a `FlowMetrics/` folder — consider grouping when extracting.

---

## 3. AzDO API client code

### 3.1 Authentication is reusable but bound to the command base class — **refactor**
`GetHttpClientInstanceForAzureDevOps(...)` (PAT Basic auth vs. Windows default-credentials) and all the typed call helpers (`CallEndpointViaGetAndGetResult<T>`, `SendPatchForBody...`, `SendPostForBody...`, `SendPutForBody...`) live as `protected` methods on `AzureDevOpsCommandBase`. Any non-command caller (an MCP tool, a test) must subclass a command to make an authenticated call. The auth logic itself is clean and correct; it is just trapped behind `protected` on an abstract command.

**Fix:** Extract an `IAzureDevOpsClient` / `AzureDevOpsHttpClientFactory` seeded from an `AzureDevOpsConfiguration`, holding the HTTP verbs and retry logic. `AzureDevOpsCommandBase` keeps its `protected` methods as thin delegations for source compatibility. Services and MCP tools then depend on the client, not the command base.

### 3.2 `HttpClient` is created and disposed per call — **cleanup**
Every helper does `using var client = GetHttpClientInstanceForAzureDevOps()`. Creating and disposing `HttpClient` per request risks socket exhaustion under load and is a known .NET anti-pattern. For the CLI (one call, then exit) it is harmless; for a long-lived MCP server process making many calls it matters.

**Fix:** When extracting the client (3.1), use a single reusable `HttpClient` (or `IHttpClientFactory` via the generic host that the MCP server will already be running). Low risk, meaningful for the server scenario.

### 3.3 Retry logic silently changes error-handling semantics — **cleanup (latent bug)**
`CallEndpointViaGetAndGetResult<T>` catches **all** exceptions and retries once. On the retry it does **not** forward `throwExceptionOnError`:

```csharp
catch {
    await Task.Delay(...);
    // note: throwExceptionOnError defaults back to true here
    return await CallEndpointViaGetAndGetResultSingleAttempt<T>(requestUrl, writeStringContentToInfo, azureDevOpsUrlTargetType: azureDevOpsUrlTargetType);
}
```

A caller that passed `throwExceptionOnError: false` to get a soft `null` on 404 will instead get a thrown exception if the first attempt fails and the second returns non-success. The bare `catch` also retries on non-transient errors (auth failures, 400s), doubling the wait for genuinely bad requests.

**Fix:** Forward `throwExceptionOnError` into the retry call and narrow the catch (or at least don't retry 4xx). Add this to the extracted client so it is fixed once.

---

## 4. Code quality concerns

### 4.1 Dead / unused code and stray usings — **cleanup**
- `using static System.Runtime.InteropServices.JavaScript.JSType;` appears in `GetCycleTimeAndThroughputCommand.cs` and `GetAgingWorkItemsCommand.cs` — almost certainly an accidental IDE auto-import; unused.
- `GetCycleTimeAndThroughputCommand.OnExecute` contains an empty `if (Data == null || Data.Items == null) { }` block (lines ~62-65) that does nothing.
- `_NumberOfWeeksOfForecast` is read from arguments in `CycleTimeConfidenceRangesCommand` and `CalculateSuggestedServiceLevelExpectationCommand` but never used; those commands don't even declare a `forecastweeks` argument, so `GetInt32Value` relies on defaulting behavior.
- Unused locals: `longestString` (in a couple `WriteThroughputForWeek` variants), `maxOccurrences`, and `sortedKeys` computed then ignored in the forecast display methods.
- Commented-out argument block in `ForecastWorkItemDeliveryCommand` (the `//arguments.AddString(...TeamProjectName...)` lines).

**Fix:** Delete on sight during the extraction. Consider enabling `TreatWarningsAsErrors` for a subset or at least fixing the CS-warning backlog (the Api build currently emits 16 warnings).

### 4.2 Long methods with mixed concerns — **refactor**
`ForecastWorkItemDeliveryCommand.OnExecute` (plus `GetWorkItemBacklogPosition`, `GetForecast`) is the worst offender: it fetches a work item, resolves its team project, validates a team name, computes a backlog position, then constructs and runs a sibling forecast command and formats a `StringBuilder`. `GetCycleTimeAndThroughputCommand` similarly mixes fetch + group + format. These are hard to test and reason about.

**Fix:** Falls out naturally from 1.1/2.1/2.2 — each concern becomes a service method.

### 4.3 Typos in user-facing messages — **cleanup**
E.g. `ForecastWorkItemDeliveryCommand`: "Could not determine team information **fro** the supplied team name." and "Could not **team project** '{name}' for work item." (missing verb). Minor but user-visible.

### 4.4 `ForecastWorkItemDeliveryCommand.GetWorkItem` ignores its parameter — **cleanup (latent)**
`GetWorkItem(int workItemId)` builds a `GetWorkItemByIdCommand` from cloned arguments and never uses `workItemId` to populate them — it relies on the `id` argument already being present in `ExecutionInfo`. It works today only because the same `id` argument was parsed at top level. It is fragile and confusing.

**Fix:** Pass the id explicitly when constructing the sub-operation (or, post-refactor, call a `WorkItemService.GetByIdAsync(id)`).

### 4.5 Hardcoded values that belong in configuration/constants — **cleanup**
- Work-item type filter strings `"Product Backlog Item"`, `"User Story"`, `"Bug"`, state `"Done"`, and `StateCategory eq 'InProgress'` are hardcoded inside OData query builders. Teams using custom process templates (Agile "User Story", custom "Done" states) will get empty results. The simulation constants (1000 runs, 500/800/900/999 buckets) are at least centralized in `Constants.cs`, which is good.
- OData API version `_odata/v1.0` is hardcoded in each query; a commented-out `v4.0-preview` hint sits next to it.

**Fix:** Not blocking for MCP, but when extracting the analytics client, take the work-item type(s) and "done"/"in-progress" definitions as parameters (defaulted to the Scrum values) so the calculations aren't silently Scrum-only.

---

## 5. Test coverage

### 5.1 Flow Metrics calculations are essentially untested — **blocker (for MCP confidence)**
The only flow-metrics-adjacent tests are `MiscTestFixture.GetIndexForPercentForecast_*`, which cover the percentile *index* helper in `Utilities`. There are **no** tests for:

- cycle-time percentile over a real dataset (`GetCycleTimeAtPercent`),
- weekly grouping (`GetCycleTimeAndThroughputCommand.GroupData` / `GetMondayOfWeek`),
- the Monte Carlo forecasters (weeks-for-items and items-in-weeks),
- aging-work age calculation,
- team-name resolution.

The suite is otherwise healthy (120 passing, 22 skipped) but concentrated on config management, work-item-type XML, Excel readers, and the solution/project file parsers.

**Why it's a blocker:** The MCP tools will expose these calculations to an LLM that presents them as delivery forecasts to a human. Extracting the logic (Section 1) is precisely what makes it testable; the extraction should be accompanied by unit tests that pin the calculation behavior (deterministic inputs → expected percentiles/confidence), so the MCP layer is trustworthy.

### 5.2 The calculations cannot currently be tested independently of the CLI — **refactor**
Because the logic is inside commands (1.1), any test today would have to parse arguments, stub the network, run the command, and scrape output. That is why coverage is absent. Post-extraction, the forecasters and percentile calculators are pure functions over in-memory lists and become trivially testable (seed the RNG or assert on distribution shape).

### 5.3 No coverage of the retry / error-handling paths — **cleanup**
The retry semantics issue in 3.3 has no test that would catch the `throwExceptionOnError` regression. When the client is extracted, add a test with a fake handler that fails once then succeeds, and one that returns 404 with `throwExceptionOnError: false`.

---

## Prioritized summary

| # | Finding | Severity |
|---|---------|----------|
| 1.1 | Calculation logic fused with console I/O in command classes | **blocker** |
| 2.3 | Forecast confidence numbers are never returned, only printed | **blocker** |
| 5.1 | Flow-metrics calculations essentially untested | **blocker** |
| 1.2 | Commands compose other commands via cloned CLI args | refactor |
| 2.1 | Monte Carlo logic duplicated across two commands | refactor |
| 2.2 | Analytics fetch + team resolution duplicated 3× | refactor |
| 2.5 | Percentile logic wrapped in an I/O-emitting command method | refactor |
| 3.1 | Auth + HTTP helpers trapped as `protected` on command base | refactor |
| 4.2 | Long, multi-concern methods (esp. `forecastworkitem`) | refactor |
| 5.2 | Calculations not independently testable | refactor |
| 2.4 | `agingwork` ignores quiet mode | cleanup |
| 3.2 | `HttpClient` created/disposed per call | cleanup |
| 3.3 | Retry drops `throwExceptionOnError`, retries 4xx | cleanup |
| 4.1 | Dead code, stray usings, unused locals | cleanup |
| 4.3 | User-facing typos | cleanup |
| 4.4 | `GetWorkItem(id)` ignores its parameter | cleanup |
| 4.5 | Hardcoded work-item types / states / OData version | cleanup |
| 5.3 | Retry/error paths untested | cleanup |
| 1.3 / 2.6 | Clean Api/ConsoleUi split; shared DTOs | positive |

## Recommended path to MCP readiness

The three blockers all resolve with one focused refactor: **extract the flow-metrics calculations and the Azure DevOps analytics access out of the command classes into reusable, testable services in the Api project**, returning structured result objects. The existing commands become thin formatters over those services (preserving current CLI output verbatim), the new MCP tools become an equally thin adapter over the same services, and the extracted calculators get unit tests. The refactor-level duplication findings (2.1, 2.2, 2.5) are subsumed by this same extraction, so doing it once pays down most of the debt while unblocking Phase 2.
