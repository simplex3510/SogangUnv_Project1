---
name: pickup
description: "Continues work handed off from another PC: pulls, restores production/handoff.md into the local session checkpoint and summarizes where to resume. Use when the user just switched PCs or asks to pick up or continue from the other machine."
user-invocable: true
disable-model-invocation: true
allowed-tools: Read, Write, Bash(git status *), Bash(git fetch *), Bash(git pull *), Bash(git branch *), Bash(git switch *), Bash(git log *), Bash(git rev-parse *), AskUserQuestion
---

# /pickup — arrive on this PC

1. **Pull.** If `git status --porcelain` shows local changes, list them and stop: pulling over them is the user's call. Otherwise `git fetch --all --prune`, then `git pull --ff-only`. If the pull is not a fast-forward, stop and report.
2. **Find the handoff.** Read `production/handoff.md`. If it is missing, report `NO HANDOFF` and stop.
   - Its header names the branch. If you are on another branch, show both and ask whether to `git switch <branch>`.
   - List `wip/handoff-*` branches on origin (`git branch -r --list 'origin/wip/handoff-*'`) and mention any newer than the handoff.
   - **Stale check:** if the latest commit on the handoff branch is newer than the handoff commit and is not itself a handoff (`git log -1 --format=%s -- production/handoff.md` vs `git log -1 --format=%s`), warn that work was committed after the last handoff, so the checkpoint may be out of date.
3. **Restore.** If `production/session-state/active.md` exists and differs, ask before replacing it. Write the handoff content (minus its first comment line) to `production/session-state/active.md`.
4. **Summarize** in five lines or fewer: branch, current task, next step, blocked on, open questions. Then stop and wait for the user.
