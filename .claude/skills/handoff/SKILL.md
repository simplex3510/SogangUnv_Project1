---
name: handoff
description: "Saves the session checkpoint to git so another PC can continue: copies active.md to production/handoff.md, commits and pushes. Use when the user says they are switching PCs, stopping for the day on this machine, or asks to hand off."
argument-hint: "[optional: one line on what to tell the next session]"
user-invocable: true
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Bash(git status *), Bash(git diff *), Bash(git branch *), Bash(git rev-parse *), Bash(git add *), Bash(git commit *), Bash(git push *), Bash(git switch *), AskUserQuestion
---

# /handoff — leave this PC

Typing `/handoff` is the user's instruction to commit and push for this purpose. Commit nothing beyond what the steps below name.

1. **Refresh the checkpoint.** Update the CHECKPOINT region of `production/session-state/active.md` so a cold reader can resume: Updated (today), Branch (`git rev-parse --abbrev-ref HEAD`), Current task, Next step, Blocked on, Files in progress, Open questions. Add the user's note ($ARGUMENTS) under Notes if given. If the file does not exist, create it from `.claude/docs/templates/session-state.md`.
2. **Copy it.** Write the whole file to `production/handoff.md`, with this line first: `<!-- Written by /handoff on <date> from branch <branch>. /pickup copies it back to production/session-state/active.md. -->`
3. **Uncommitted work.** Run `git status --porcelain`. If anything other than `production/handoff.md` is modified or untracked, show the list and ask:
   - Commit it on the current branch (ask for a one-line Conventional Commit message), or
   - Commit it on a new branch `wip/handoff-<YYYY-MM-DD>`, or
   - Leave it uncommitted (it stays on this PC only; say so in the handoff Notes).
4. **Commit and push.** `git add production/handoff.md` (plus the files chosen in step 3), commit as `chore(handoff): <Next step line>`, then `git push` (with `-u origin <branch>` if the branch has no upstream).
5. **Report** the branch, the commit hash, whether the push succeeded, and anything left uncommitted. If the push failed, say so first: the other PC will not see this handoff.
