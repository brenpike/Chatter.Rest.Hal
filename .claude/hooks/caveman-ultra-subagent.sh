#!/usr/bin/env bash

cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "SubagentStart",
    "additionalContext": "Caveman mode requirement for this project: operate in caveman ultra mode for this entire subagent conversation. Do not silently fall back to full, lite, or normal verbosity unless the user explicitly requests it."
  }
}
EOF
