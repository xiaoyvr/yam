param(
    [string]$command = 'help'    
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

function Call-MSBuild ($target, $properties) {
    &$msbuild $root\yam.targets /t:$target /p:$properties /m:4
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

function Resolve-Projects([string[]] $files, [string] $profile = ''){
    $tmpFileName = [System.IO.Path]::GetTempFileName()
    Get-InputProjects $files | Set-Content $tmpFileName
    $props = "rootDir=$fullCodebaseRoot;configFile=$configFile;file=$tmpFileName;runtimeProfile=$profile"
    Call-MSBuild "Resolve" $props
    Remove-Item $tmpFileName
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

function Get-PatchedResult ([string[]] $files, [string] $profile){
    $projects = Get-InputProjects $files
    $fullCodebaseDir = Resolve-Path $codebaseRoot
    $fullRootDir = Resolve-Path $root
    $fullProjectDirs = $projects | Resolve-Path
    Start-Job {
        param($fullCodebaseDir, $fullRootDir, $fullProjectDirs, $profile)
        $configDir = Get-Item "$fullCodebaseDir\prj.config"
        Get-ChildItem $fullRootDir "Yam.Core.dll" -Recurse | % { Add-Type -Path $_.FullName }
        Update-TypeData -prependpath "$fullRootDir\yam.types.ps1xml"
        $cfg = new-object Yam.Core.ResolveConfig $configDir.FullName, $fullCodebaseDir
        $patcher = new-object Yam.Core.MSBuildPatcher $cfg
        $result = $patcher.Resolve([string[]]$fullProjectDirs, $profile)
        $result
    } -ArgumentList $fullCodebaseDir, $fullRootDir, $fullProjectDirs, $profile |
        Wait-Job | Receive-Job
}

function Build-Projects ([string[]] $files, [string] $profile = ''){
    $tmpFileName = [System.IO.Path]::GetTempFileName()
    Get-InputProjects $files | Set-Content $tmpFileName
    $props = "rootDir=$fullCodebaseRoot;configFile=$configFile;file=$tmpFileName;runtimeProfile=$profile"
    Call-MSBuild "Build" $props
    Remove-Item $tmpFileName
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
        Build-Projects @args
    }
    'resolve'{
        Resolve-Projects @args
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