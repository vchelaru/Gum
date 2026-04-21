# Shared Skills Directory

This directory is used by Gemini CLI and other AI agents as a standardized location for workspace skills.

## Claude Code Integration
Currently, most skills for this project are maintained in the `.claude/skills` directory. 

## Usage
To make these skills available to Gemini CLI on your local machine, you can create a directory symlink from this folder to the Claude skills folder:

### Windows (PowerShell/CMD as Admin)
```powershell
# From project root
cmd /c "mklink /D .agents\skills %CD%\.claude\skills"
```

### macOS / Linux
```bash
# From project root
ln -s ../.claude/skills .agents/skills
```

By using a symlink, both Claude Code and Gemini CLI will share the same source of truth for agent capabilities.
