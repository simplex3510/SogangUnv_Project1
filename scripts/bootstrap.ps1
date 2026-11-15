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
# git.exe sits in Git\cmd from PowerShell but in Git\mingw64\bin from Git Bash, so walk up to Git\bin\bash.exe.
$gitBash = $null
if (Have 'git') { $d = Split-Path (Get-Command git).Source; for ($i = 0; $i -lt 3 -and -not $gitBash; $i++) { $d = Split-Path $d; $c = Join-Path $d 'bin\bash.exe'; if (Test-Path $c) { $gitBash = $c } } }
if ($gitBash) { Write-Host "  ok       git bash ($gitBash)" } else { $problems += 'Git Bash not found beside git.exe: CCGS hooks will not run (install Git for Windows)' }
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
# A fresh clone of a game has only origin; restore the template remote so template updates still merge.
$templateUrl = (Get-Content (Join-Path $PSScriptRoot 'template.url') -ErrorAction SilentlyContinue | Select-Object -First 1)
if ($templateUrl -and $origin -ne $templateUrl -and -not ((git remote) -contains 'template')) {
    git remote add template $templateUrl; Write-Host "  ok       template remote -> $templateUrl (added)"
} elseif ((git remote) -contains 'template') { Write-Host "  ok       template remote" }
if ((git remote) -contains 'upstream') { $problems += "a remote is named 'upstream': gh prefers it over origin; rename it (git remote rename upstream ccgs)" }
if ((git config --global credential.helper) -eq 'store') { $problems += 'git stores your token in plain text (credential.helper=store); run: gh auth setup-git' }

Write-Host "== Versions (PC tools follow latest, see docs/framework-design.md section 5)"
# Template tags are template-v*; plain v* tags are CCGS releases that came in with its history.
$tag = git describe --tags --abbrev=0 --match 'template-v*' 2>$null
Write-Host "  template $(if ($tag) { $tag } else { '(no tag)' })"
$installed = Join-Path $HOME '.claude/plugins/installed_plugins.json'
$plugins = if (Test-Path $installed) { (Get-Content $installed -Raw | ConvertFrom-Json -AsHashtable)['plugins'] } else { @{} }
foreach ($id in 'ponytail@ponytail', 'unity@claude-plugins-official', 'aseprite@aseprite-ai-artist') {
    $p = $plugins[$id] | Select-Object -First 1
    if ($p) { Write-Host "  ok       $id $($p['version'])" } else { $problems += "plugin $id not installed: open Claude Code here and accept the plugin prompt" }
}
if (Have 'unity') {
    $u = unity self-update --check --format json 2>$null | ConvertFrom-Json
    if ($u.success) {
        Write-Host "  ok       unity cli $($u.data.currentVersion)"
        if ($u.data.latestVersion -ne $u.data.currentVersion) { Write-Host "  NOTICE   unity cli $($u.data.latestVersion) is out: unity self-update" }
    }
}
if (Have 'graphify') { Write-Host "  ok       $(graphify --version 2>$null)" }

# The Unity plugin is beta and auto-updates, so skills get renamed. Every skill named in
# integration.md's Unity routing table must exist in the installed plugin.
$unityPlugin = $plugins['unity@claude-plugins-official'] | Select-Object -First 1
if ($unityPlugin) {
    $inUnity = $false
    $skills = foreach ($line in Get-Content (Join-Path $root '.claude/docs/framework/integration.md')) {
        if ($line -match '^## ') { $inUnity = $line -eq '## Unity'; continue }
        if ($inUnity -and $line -cmatch '^\| [a-z]') { (($line -split '\|')[2] -split '[,→/]').Trim() | Where-Object { $_ } }
    }
    $missing = @($skills | Where-Object { -not (Test-Path (Join-Path $unityPlugin['installPath'] "skills/$_/SKILL.md")) })
    if ($missing) { $problems += "integration.md routes to Unity skills the installed plugin ($($unityPlugin['version'])) does not have: $($missing -join ', '). Update the table." }
    else { Write-Host "  ok       $(@($skills).Count) Unity skills in the routing table exist" }
}

Write-Host ''
if ($problems) { Write-Host "== $($problems.Count) problem(s):"; $problems | ForEach-Object { Write-Host "  - $_" }; exit 1 }
Write-Host '== All checks passed.'
