---
# Serena MCP Server - C# Code Analysis
# Language Server Protocol (LSP)-based tool for deep C# code analysis
#
# Documentation: https://github.com/oraios/serena
#
# Capabilities:
#   - Semantic code analysis using LSP (go to definition, find references, etc.)
#   - Symbol lookup and cross-file navigation
#   - Type inference and structural analysis
#   - Deeper insights than text-based grep approaches
#
# Usage:
#   imports:
#     - shared/mcp/serena-csharp.md

tools:
  serena: ["csharp"]
---

## Serena C# Code Analysis

The Serena MCP server is configured for C# code analysis in this workspace:
- **Workspace**: `${{ github.workspace }}`
- **Memory**: `/tmp/gh-aw/cache-memory/serena/`

### Project Activation

Before analyzing code, activate the Serena project:
```
Tool: activate_project
Args: { "path": "${{ github.workspace }}" }
```

### Analysis Constraints

1. **Only analyze `.cs` files** — Ignore all other file types
2. **Skip generated files** — Never analyze files in `obj/` or `bin/`
3. **Use Serena for semantic analysis** — Leverage LSP capabilities for deeper insights
