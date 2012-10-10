param([Parameter(Mandatory=$true)] $codebaseDir)

$root = Split-Path -parent $MyInvocation.MyCommand.Definition
$yamMainScript = Join-Path $root "tools\yam.ps1"

Set-Location $codebaseDir
$codebaseRootToMainScript = Resolve-Path $yamMainScript -Relative
$scriptText = @"
& "$codebaseRootToMainScript" @args
"@
Set-Content $codebaseDir\yam.ps1 $scriptText -Force