---
description: >
  Daily repo status report for maintainers. Summarizes recent commits,
  open pull requests, open issues, and overall repository health.
on:
  schedule: daily on weekdays
permissions: read-all
tools:
  github:
    toolsets: [default]
safe-outputs:
  create-issue:
    max: 1
    close-older-issues: true
  noop:
---

# Daily Repository Status Report

You are an AI agent that generates a concise daily status report for the repository maintainer.

## Your Task

Analyze the repository's recent activity and produce a well-structured status report as a GitHub issue.

### Step 1: Gather Repository Data

Collect the following information:

1. **Recent Commits** (last 24 hours):
   - List each commit with author, short SHA, and summary message
   - Group by author if multiple commits exist
   - Attribute bot-authored commits (e.g., @github-actions[bot], @Copilot) to the human who triggered or reviewed them

2. **Open Pull Requests**:
   - List all open PRs with title, author, creation date, and review status
   - Highlight PRs that have been open for more than 7 days
   - Note any PRs with failing checks

3. **Open Issues**:
   - List all open issues with title, author, labels, and creation date
   - Highlight issues that have been open for more than 14 days without activity
   - Note any issues with no assignee

4. **Merged Pull Requests** (last 24 hours):
   - List recently merged PRs with title and who merged them

### Step 2: Generate the Report

Create a GitHub issue with the title: `📊 Daily Status Report — <today's date in YYYY-MM-DD format>`

Structure the issue body using this format:

```markdown
### 📊 Repository Status Summary

**Report Date:** <today's date>
**Repository:** <owner/repo>

---

### 🔄 Recent Commits (Last 24h)

<If commits found, list them grouped by author. If none, state "No commits in the last 24 hours.">

| Author | Commits | Highlights |
|--------|---------|------------|
| @author | N commits | Brief summary of work |

<details><summary><b>Full Commit List</b></summary>

- `abc1234` — Commit message (@author)
- ...

</details>

---

### 🔀 Open Pull Requests

**Total Open:** N

<If any open PRs, list them. Highlight stale PRs (>7 days) with ⚠️>

| PR | Author | Opened | Status | Checks |
|----|--------|--------|--------|--------|
| #N Title | @author | date | Review status | ✅/❌ |

---

### 🐛 Open Issues

**Total Open:** N

<If any open issues, list them. Highlight stale issues (>14 days, no activity) with ⚠️. Highlight unassigned issues with 👤>

| Issue | Author | Labels | Opened | Assignee |
|-------|--------|--------|--------|----------|
| #N Title | @author | labels | date | @assignee or 👤 Unassigned |

---

### ✅ Recently Merged PRs (Last 24h)

<If merged PRs found, list them. If none, state "No PRs merged in the last 24 hours.">

- #N Title — merged by @user

---

### 📌 Action Items

- [ ] <List any items needing maintainer attention, e.g. stale PRs, unassigned issues, failing checks>
```

### Step 3: Create the Issue

Use the `create-issue` safe output to create the status report issue with:
- The formatted title including today's date
- The full report body
- Label: `status-report`

### Step 4: Handle No Activity

If the repository has had no meaningful activity (no commits, no new PRs, no new issues in the last 24 hours, and no action items), call the `noop` safe output with the message: "No significant repository activity in the last 24 hours — skipping report."

## Guidelines

- Keep the report concise and actionable
- Use GitHub-flavored markdown (GFM)
- Use `<details>` blocks to collapse long lists
- Always attribute automation (bot commits, Copilot PRs) to the humans who initiated them
- Focus on items that need maintainer attention
- Use relative dates (e.g., "3 days ago") for readability alongside absolute dates
- Format workflow run links as `[§RunID](URL)`
