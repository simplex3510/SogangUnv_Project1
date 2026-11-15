# Creates a private game repo from this template, keeping history so template updates merge later.
# Usage: pwsh scripts/new-game.ps1 <game-slug> [-WhatIf]
[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][ValidatePattern('^[a-z0-9][a-z0-9-]*$')][string]$Slug)
$ErrorActionPreference = 'Stop'

$template = (git -C $PSScriptRoot remote get-url origin).Trim()
$owner = (gh api user --jq .login).Trim()
$dest = Join-Path (Split-Path (git -C $PSScriptRoot rev-parse --show-toplevel)) $Slug
if (Test-Path $dest) { throw "$dest already exists" }

Write-Host "template: $template"
Write-Host "new repo: github.com/$owner/$Slug (private)"
Write-Host "folder:   $dest"

if ($PSCmdlet.ShouldProcess("github.com/$owner/$Slug", 'create private repo and push the template')) {
    gh repo create "$owner/$Slug" --private | Out-Null
    git clone $template $dest
    git -C $dest remote rename origin template
    git -C $dest remote add origin "https://github.com/$owner/$Slug.git"
    git -C $dest push -u origin main
    & (Join-Path $dest 'scripts/bootstrap.ps1')
    Write-Host "Done. Open Claude Code in $dest and run /start."
}
