param(
    [string]$command = 'help', 
    [string[]] $files, 
    [string] $runtimeProfile = '',
    [string[]] $ends, 
    [switch] $reverse
)

function Get-MSBuild(){
    $bitness = ''
    $ptrSize = [System.IntPtr]::Size
    if ($ptrSize = 8) {
        $bitness = '64'
    }
    $version = 'v4.0.30319'
    "$env:windir\Microsoft.NET\Framework$bitness\$version\MSBuild.exe"
}

function Get-ProjectOutputFromFile ($file) {
    $output = &$msbuild $root\yam.targets /t:GetProjectOutputFromFile /p:"file=$file;rootDir=$fullCodebaseRoot" /nologo /v:m
    if ($LastExitCode -ne 0) {
        throw "error: $output"
    }
    $output | % { $_.trim() }
}

function Call-MSBuild ($target, $properties, $verbosity = 'n') {
    &$msbuild $root\yam.targets /t:$target /p:$properties /m:4 /v:$verbosity
    if ($LastExitCode -ne 0) {
        throw "Error: MSBuild failed. "
    }
}

function Set-Config(){
    $tmpConfigFile = "$codebaseRoot\prj.config.tmp"
    Set-Content $tmpConfigFile $null
    
    Set-Location $codebaseRoot
    $tmpFileName = [io.path]::GetTempFileName()
    $codebaseConfig.projectDirs | 
        ? { Test-Path $_} | 
        Get-ChildItem -Recurse -filter *.csproj | 
        Resolve-Path -Relative | %{ $_.SubString(2) } | Set-Content $tmpFileName
    Pop-Location

    Get-ProjectOutputFromFile $tmpFileName | Add-Content $tmpConfigFile
    Remove-Item $tmpFileName

    $codebaseConfig.libDirs | 
        ? { Test-Path $_} | 
        Get-ChildItem -Recurse -filter *.dll | 
        % { $_.FullName } | 
        %{ 
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($_)
            $fullName = (Resolve-Path $_ -Relative).SubString(2)
            Add-Content $tmpConfigFile "Lib, $fileName, $fullName" 
        }
    Move-Item $tmpConfigFile $configFile -Force
}

function Get-SolutionProjects($sln){
    Get-Content $sln | 
        ? { $_ -match 'Project\("\{(?<Type>.{36})\}"\)\s*=\s*"(?<Name>.*)",\s*"(?<Path>.*)",\s*"\{(?<Id>.{36})\}"' } | 
        ? { $matches.Path.EndsWith('.csproj')} | 
        % { 
            $prjPath = Join-Path (Split-Path $sln.FullName -Parent) $matches.Path 
            Resolve-Path $prjPath
        }
}

Function Get-InputProjects([string[]] $files){
    if ($files) {
        $files | Get-Item | % {
            $ext = $_.extension
            $file = $_
            switch ($ext) {
                '.csproj'{
                    $file
                }
                '.sln' {
                    Get-SolutionProjects $file
                }
                '.txt'{
                    Get-Content $files
                }
                'default' {
                    throw 'Error: $file is not supported. '
                }
            }
        } | select -Unique        
    }
}

function Get-PatchedResult ([string[]] $files, [string] $runtimeProfile){
    $projects = Get-InputProjects $files
    $fullCodebaseDir = Resolve-Path $codebaseRoot
    $fullRootDir = Resolve-Path $root
    $fullProjectDirs = $projects | Resolve-Path
    Start-Job {
        param($fullCodebaseDir, $fullRootDir, $fullProjectDirs, $runtimeProfile)
        $configDir = Get-Item "$fullCodebaseDir\prj.config"
        Get-ChildItem $fullRootDir "Yam.Core.dll" -Recurse | % { Add-Type -Path $_.FullName }
        Update-TypeData -prependpath "$fullRootDir\yam.types.ps1xml"
        $cfg = new-object Yam.Core.ResolveConfig $configDir.FullName, $fullCodebaseDir
        $patcher = new-object Yam.Core.MSBuildPatcher $cfg
        $result = $patcher.Resolve([string[]]$fullProjectDirs, $runtimeProfile)
        $result
    } -ArgumentList $fullCodebaseDir, $fullRootDir, $fullProjectDirs, $runtimeProfile |
        Wait-Job | Receive-Job
}

function Use-TempFile ([scriptblock] $act){
    $_ = [System.IO.Path]::GetTempFileName()
    & $act
    Remove-Item $_
}

function Use-TempFiles ([int]$count = 1, [scriptblock] $act){
    $files = @()
    @(1..$count) | % {
        $files += [System.IO.Path]::GetTempFileName()
    }
    $_ = $files
    & $act
    $files | Remove-Item
}

function Build-Projects ([string[]] $files, [string] $runtimeProfile){
    Use-TempFile {
        Get-InputProjects $files | Set-Content $_
        $props = "rootDir=$fullCodebaseRoot;configFile=$configFile;file=$_;runtimeProfile=$runtimeProfile"
        Call-MSBuild "Build" $props        
    }
}
function Resolve-Projects([string[]] $files, [string] $runtimeProfile){
    Use-TempFile {
        Get-InputProjects $files | Set-Content $_
        $props = "rootDir=$fullCodebaseRoot;configFile=$configFile;file=$_;runtimeProfile=$runtimeProfile"
        Call-MSBuild "Resolve" $props
    }
}
function Create-Graph([string[]] $starts, [string[]] $ends, $reverse, [string] $runtimeProfile){    
    Use-TempFiles 2 {
        Get-InputProjects $starts | Set-Content $_[0]
        Get-InputProjects $ends | Set-Content $_[1]        
        $props = "rootDir=$fullCodebaseRoot;configFile=$configFile;starts=$($_[0]);ends=$($_[1]);reverse=$reverse;runtimeProfile=$runtimeProfile"
        $output = &$msbuild $root\yam.targets /t:Graphviz /p:"$props" /nologo /v:m
        if ($LastExitCode -ne 0) {
            throw "Error: MSBuild failed. $output"
        }
        $result = $output | % {
            $lineItems = $_.trim().Split(">")
            $incomming = $lineItems[0].trim()
            if ($lineItems[1]) {
                $lineItems[1].trim().split(";")
            }
        } | % {
            $start = [System.IO.Path]::GetFileNameWithoutExtension($incomming)
            $end = [System.IO.Path]::GetFileNameWithoutExtension($_)
            "`"$start`" -> `"$end`""
        }

        $result | write-host -f darkgray
        $startStr = [System.String]::Join(".", ($starts | % { [System.IO.Path]::GetFileName($_) }))
        $endStr = [System.String]::Join(".", ($ends | % { [System.IO.Path]::GetFileName($_) }))
        if ($endStr) {
            $endStr = ".$endStr"
        }
        if ($reverse) {
            $prefix = ".r"
        } else {
            $prefix = ".o"
        }

        $fileName = "$startStr$prefix$endStr"
        $outputFile = "$fullCodebaseRoot\dependencies\$fileName.txt"
        if (-not (Test-Path "$fullCodebaseRoot\dependencies")) {
            New-Item "$fullCodebaseRoot\dependencies" -Type Directory
        }
        Set-Content $outputFile "digraph `"$fileName`" {"
        $result | %{ Add-Content $outputFile "  $_" } 
        Add-Content $outputFile "}"

        $svgOutput = "$outputFile.svg"
        if (Test-Path $svgOutput) {
            Remove-Item $svgOutput
        }
        & dot -O -Tsvg $outputFile
        & start $svgOutput
    }
}

function Show-Help {
@"
write some help here. 
"@
}

$start = Get-Date

$msbuild = Get-MSBuild
$root = $MyInvocation.MyCommand.Path | Split-Path -parent
$codebaseRoot = "."
. .\codebaseConfig.ps1
$fullCodebaseRoot = Resolve-Path $codebaseRoot
$configFile = "$fullCodebaseRoot\prj.config"

switch ($command){
    'config' {
        Set-Config
    }
    'build'{
        Build-Projects $files $runtimeProfile
    }
    'resolve'{
        Resolve-Projects $files $runtimeProfile
    }
    'graph'{
        Create-Graph $files $ends $reverse $runtimeProfile
    }
    'help'{
        Show-Help
    }
    default {
        Write-Host "Error: '$command' is not a valid command." -f red
        Show-Help
        Exit 1
    }
}
$timeSpent = New-TimeSpan $start $(Get-Date) 
write-host "Time Elapsed $timeSpent"