# Agent Workflows

GitHub Agent Workflows (`gh aw`) let you define and run automated workflows powered by GitHub Actions. This document covers the key commands for setting up and running the `daily-repo-status` workflow.

## Add a Workflow

Use the add-wizard to interactively configure a new agent workflow from a template:

```bash
gh aw add-wizard githubnext/agentics/daily-repo-status
```

This walks you through setting up the `daily-repo-status` workflow, which generates a summary report of your repository and creates an issue with the results.

## Regenerate the Workflow YAML

If you have changed the frontmatter of your workflow file, regenerate the workflow YAML by running:

```bash
gh aw compile
```

This reads the frontmatter in your workflow file and produces the corresponding GitHub Actions YAML. Run this every time you modify the frontmatter to keep the workflow definition in sync.

## Commit and Push

After compiling, commit and push the changes to your repository so the updated workflow is available on GitHub.

## Trigger a Run

Optionally trigger another run manually:

```bash
gh aw run daily-repo-status
```

After waiting for the workflow to complete, check the new issue created with your updated report!

## Costs & Usage Tracking

Costs vary depending on workflow complexity, AI model, and execution time. GitHub Copilot CLI uses 1–2 premium requests per workflow execution with agentic processing. Track usage with:

- `gh aw logs` — view runs and metrics
- `gh aw audit <run-id>` — detailed token usage and costs
- Your AI provider's usage portal

Consider creating separate PAT/API keys per repository for granular cost tracking.