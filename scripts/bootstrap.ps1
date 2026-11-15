# Sets up this clone on this PC. Safe to re-run. See docs/framework-design.md (section 4).
# Usage: pwsh scripts/bootstrap.ps1
$ErrorActionPreference = 'Stop'
$root = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
Set-Location $root
$problems = @()

function Have($cmd) { [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }

Write-Host "== Tools"
foreach ($t in 'git', 'git-lfs', 'gh', 'node', 'unity', 'graphify') {
    if (Have $t) { Write-Host "  ok       $t" } else { Write-Host "  MISSING  $t"; $problems += "$t not on PATH" }
}
# CCGS hooks need Git Bash; without it they silently switch off. Look beside git.exe:
# `bash` on PATH is often WSL's (System32\bash.exe), which is not what Claude Code runs hooks with.
$gitBash = if (Have 'git') { Join-Path (Split-Path (Split-Path (Get-Command git).Source)) 'bin\bash.exe' }
if ($gitBash -and (Test-Path $gitBash)) { Write-Host "  ok       git bash ($gitBash)" } else { $problems += 'Git Bash not found beside git.exe: CCGS hooks will not run (install Git for Windows)' }
if (Have 'gh') { gh auth status *> $null; if ($LASTEXITCODE) { $problems += 'gh is not logged in (gh auth login)' } }

Write-Host "== Git hooks (LFS first, then Graphify: Graphify appends, LFS refuses to)"
# On a re-run Graphify has already appended to the hooks, so `git lfs install` would refuse; skip it when LFS is in place.
$postCommit = Join-Path (git rev-parse --git-path hooks) 'post-commit'
if ((Test-Path $postCommit) -and (Select-String -Path $postCommit -Pattern 'git lfs' -Quiet)) { Write-Host '  ok       git lfs hooks (already installed)' }
elseif (Have 'git-lfs') { git lfs install --local | Out-Null; Write-Host '  ok       git lfs hooks' }
if (Have 'graphify') { graphify hook install | Out-Null; Write-Host '  ok       graphify hooks' } else { Write-Host '  SKIPPED  graphify hooks (graphify missing)' }

Write-Host "== Merge drivers (per-PC git config)"
git config merge.ours.driver true
$yamlMerge = Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\*\Editor\Data\Tools\UnityYAMLMerge.exe' -ErrorAction SilentlyContinue |
    Sort-Object FullName | Select-Object -Last 1
if ($yamlMerge) {
    git config merge.unityyamlmerge.name 'UnityYAMLMerge'
    git config merge.unityyamlmerge.driver "'$($yamlMerge.FullName -replace '\\','/')' merge -p %O %B %A %A"
    Write-Host "  ok       unityyamlmerge -> $($yamlMerge.FullName)"
} else {
    Write-Host '  SKIPPED  unityyamlmerge (no Unity Editor under Program Files\Unity\Hub\Editor; git falls back to text merge)'
    $problems += 'UnityYAMLMerge not configured'
}

Write-Host "== Claude local settings (shared memory lives in this repo)"
$localPath = Join-Path $root '.claude/settings.local.json'
$local = if (Test-Path $localPath) { Get-Content $localPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
$local['autoMemoryDirectory'] = ($root -replace '\\', '/') + '/.claude/memory'
$local | ConvertTo-Json -Depth 20 | Set-Content $localPath -Encoding utf8NoBOM
Write-Host "  ok       autoMemoryDirectory -> $($local['autoMemoryDirectory'])"

Write-Host "== GitHub"
$origin = git remote get-url origin 2>$null
if ($origin -match 'github\.com[:/](.+?)(\.git)?$') {
    if (Have 'gh') { gh repo set-default $Matches[1] | Out-Null; Write-Host "  ok       gh default repo -> $($Matches[1])" }
} else { $problems += 'origin is not a GitHub remote: gh default repo not set' }
if ((git remote) -contains 'upstream') { $problems += "a remote is named 'upstream': gh prefers it over origin; rename it (git remote rename upstream ccgs)" }
if ((git config --global credential.helper) -eq 'store') { $problems += 'git stores your token in plain text (credential.helper=store); run: gh auth setup-git' }

Write-Host ''
if ($problems) { Write-Host "== $($problems.Count) problem(s):"; $problems | ForEach-Object { Write-Host "  - $_" }; exit 1 }
Write-Host '== All checks passed.'
