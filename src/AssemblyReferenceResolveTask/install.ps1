param([Parameter(Mandatory=$true)] $codebaseDir)

$root = Split-Path -parent $MyInvocation.MyCommand.Definition
$yamMainScript = Join-Path $root "tools\yam.ps1"

Set-Location $codebaseDir
$codebaseRootToMainScript = Resolve-Path $yamMainScript -Relative
$scriptText = @"
param(
    [string]`$command = 'help', 
    [string[]] `$files, 
    [string] `$runtimeProfile = '',
    [string[]] `$ends, 
    [switch] `$reverse
)

& "$codebaseRootToMainScript" `$command `$files `$runtimeProfile `$ends `$reverse
"@
Set-Content $codebaseDir\yam.ps1 $scriptText -Force