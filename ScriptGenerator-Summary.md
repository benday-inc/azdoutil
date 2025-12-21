# ScriptGenerator: Work Item Simulation Logic

This document describes the logic used in the `ScriptGenerator` module to generate work items and simulate realistic team activity over multiple sprints. The original implementation targets Azure DevOps but the concepts are platform-agnostic.

## Purpose

Generate realistic project management data that simulates:
- A product backlog with items in various states
- Sprint-based work with planning, daily activities, and completion
- Team velocity and burndown patterns
- Refinement/grooming workflows

This is useful for demos, training, testing, and populating sample data.

---

## Core Entities

### Work Item (PBI / User Story)
Represents a backlog item (Product Backlog Item, User Story, Issue, etc.)

| Property | Description |
|----------|-------------|
| ID | Unique identifier |
| Title | Randomly generated title |
| State | Current workflow state |
| Effort | Fibonacci estimate (1, 2, 3, 5, 8, 13, 21) |
| Iteration | Which sprint it belongs to |
| Children | List of child tasks |

### Task
A child work item representing a unit of work

| Property | Description |
|----------|-------------|
| ID | Unique identifier |
| Title | Description of the task |
| State | Current state (New, In Progress, Done) |
| RemainingWork | Hours remaining (1, 2, 3, 4, 5, 6, 8, 12) |
| Parent | Reference to parent work item |

### Sprint Configuration

| Parameter | Typical Value | Description |
|-----------|---------------|-------------|
| SprintDuration | 14 days | Two-week sprints |
| NewPbiCount | 15 | PBIs created per sprint for future work |
| RefinedPerMeeting | 5 | PBIs refined in each refinement meeting |
| SprintPbiCount | 4 | PBIs committed to the sprint |
| TasksPerPbi | 3 | Average tasks created per PBI |
| TeamMemberCount | 7 | Number of team members |
| DailyHoursPerMember | 6 | Available work hours per person per day |

### Action
A timestamped operation representing a change to the system

| Property | Description |
|----------|-------------|
| ActionType | Create or Update |
| WorkItemType | PBI or Task |
| Timestamp | When the action occurs (day + hour + minute offset) |
| Fields | Key-value pairs of fields to set |

---

## Workflow States

### Simple Workflow (Scrum-style)
```
New → Approved → Committed → Done
```

### Extended Workflow (with Refinement)
```
New → Needs Refinement → Ready for Sprint → Committed → Done
```

---

## Simulation Flow

### Overall Process

```
For each sprint:
  1. Create new PBIs for the backlog (future work)
  2. Run Refinement Meeting 1 (day 3)
  3. Run Refinement Meeting 2 (day 10)
  4. Sprint Planning (day 0 of sprint)
  5. Daily work simulation (days 2-5, 8-12)
  6. Sprint end - complete or return items to backlog
```

### Phase 1: Backlog Population

At the start of each sprint, create new PBIs to maintain a healthy backlog:
- Generate 15 new PBIs with random titles
- Initial state: "New"
- These feed into refinement meetings

**Title Generation:**
- Combine random action words + random words + ending phrases
- Example: "Implement user authentication module"

### Phase 2: Refinement Meeting 1 (Day 3)

Simulates the first backlog refinement/grooming session:
- Select 5 random PBIs from "needs first refinement" pool
- Update state to "Needs Refinement" or equivalent
- Move to "ready for second refinement" pool

### Phase 3: Refinement Meeting 2 (Day 10)

Simulates the second refinement session:
- Select 5 random PBIs from "needs second refinement" pool
- Assign Fibonacci effort estimates (random: 1, 2, 3, 5, 8, 13, 21)
- Update state to "Ready for Sprint"
- Move to "ready for sprint planning" pool

### Phase 4: Sprint Planning (Day 0)

Commit work for the sprint:
1. Select 4 PBIs from "Ready for Sprint" pool
2. For each PBI:
   - Set state to "Committed"
   - Assign to current sprint/iteration
   - Assign effort estimate if not already set
   - Create child tasks (average 3 per PBI)
3. For each Task:
   - Generate descriptive title
   - Set initial state to "New" or "To Do"
   - Assign remaining work hours (random: 1, 2, 3, 4, 5, 6, 8, 12)
   - Link to parent PBI

### Phase 5: Daily Work Simulation

Simulate team work during the sprint (weekdays only):

**Week 1:** Days 2, 3, 4, 5
**Week 2:** Days 8, 9, 10, 11, 12

**Daily Burndown Logic:**
```
available_hours = team_members × hours_per_member  # e.g., 7 × 6 = 42
actual_burndown = random(0, available_hours)       # simulate variable velocity

for each PBI in sprint:
    for each Task in PBI:
        if task.remaining_work > 0:
            hours_to_burn = min(task.remaining_work, available_hours)
            task.remaining_work -= hours_to_burn
            available_hours -= hours_to_burn

            if task.remaining_work == 0:
                task.state = "Done"

    if sum(task.remaining_work for task in PBI.tasks) == 0:
        PBI.state = "Done"
```

**Key Behaviors:**
- Work is burned down across tasks in order
- Tasks are marked "Done" when remaining work reaches 0
- PBIs are marked "Done" when all child tasks are complete
- Random variation in daily productivity simulates real team velocity

### Phase 6: Sprint End

Handle incomplete work:
- 25% chance: All remaining PBIs marked "Done" (successful sprint)
- 75% chance: Incomplete PBIs return to backlog with new estimates

---

## Timeline: 14-Day Sprint

| Day | Activities |
|-----|------------|
| 0 (Mon) | Sprint Planning - commit PBIs, create tasks |
| 1 (Tue) | - |
| 2 (Wed) | Daily work + burndown |
| 3 (Thu) | Daily work + Refinement Meeting 1 (for future sprints) |
| 4 (Fri) | Daily work + burndown |
| 5-6 | Weekend (no activity) |
| 7 (Mon) | - |
| 8 (Tue) | Daily work + burndown |
| 9 (Wed) | Daily work + burndown |
| 10 (Thu) | Daily work + Refinement Meeting 2 (for future sprints) |
| 11 (Fri) | Daily work + burndown |
| 12-13 | Weekend (no activity) |
| 14 | Sprint ends |

---

## Random Data Generation

### Effort Estimates (Fibonacci)
```
[1, 2, 3, 5, 8, 13, 21]
```

### Task Remaining Work (Hours)
```
[1, 2, 3, 4, 5, 6, 8, 12]
```

### Title Generation
Combine elements from word lists:
- Action words: "Implement", "Create", "Update", "Fix", "Refactor", etc.
- Subject words: Random nouns/concepts
- Ending phrases: "module", "feature", "component", etc.

---

## Multi-Team Support

The generator supports creating data for multiple teams:
- Each team gets its own backlog
- Work items are assigned to team-specific areas/labels
- Independent burndown per team
- Useful for simulating organization-level views

---

## Output Formats

The original implementation supports:
1. **Direct API calls** - Create items immediately in the target system
2. **Script export** - Save actions to a file (Excel) for later execution
3. **Script-only mode** - Generate the action list without executing

---

## Adaptation Notes for GitHub

When porting to GitHub Issues/Projects:

| Azure DevOps Concept | GitHub Equivalent |
|---------------------|-------------------|
| Work Item | Issue |
| Product Backlog Item | Issue with label (e.g., "user-story") |
| Task | Issue with label (e.g., "task") or Checklist item |
| State | Issue state (open/closed) + Labels or Project columns |
| Iteration/Sprint | Milestone or Project iteration field |
| Area Path | Labels or Repository |
| Parent-Child Link | Task list in issue body, or sub-issues |
| Effort | Custom field in Projects, or label |
| Remaining Work | Custom field in Projects |

**Key Differences:**
- GitHub Issues don't have native parent-child relationships (use task lists or sub-issues)
- States are simpler (open/closed) - use labels or Project board columns for workflow states
- GitHub Projects (new) support custom fields for effort, iteration, etc.
- Consider using GitHub Projects board columns to represent workflow states

---

## Configuration Parameters Summary

```
Sprint:
  - duration_days: 14
  - new_pbis_per_sprint: 15
  - refined_per_meeting: 5
  - committed_per_sprint: 4
  - tasks_per_pbi: 3

Team:
  - member_count: 7
  - hours_per_member_per_day: 6
  - total_daily_capacity: 42 hours

Meetings:
  - refinement_1_day: 3
  - refinement_2_day: 10
  - sprint_planning_day: 0

Work Days:
  - week_1: [2, 3, 4, 5]
  - week_2: [8, 9, 10, 11, 12]
```
