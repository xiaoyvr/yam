param(
    [string]$command = 'help', 
    [string[]] $files, 
    [string] $runtimeProfile = '',
    [string[]] $ends, 
    [switch] $reverse
)

.\src\AssemblyReferenceResolveTask\yam.ps1 $command $files $runtimeProfile $ends $reverse