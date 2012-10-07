param(
    [string]$command = 'help'    
)

$bitness = ''
$ptrSize = [System.IntPtr]::Size
if ($ptrSize = 8) {
    $bitness = '64'
}
$version = 'v4.0.30319'
$msbuild = "$env:windir\Microsoft.NET\Framework$bitness\$version\MSBuild.exe"



# GetProjectOutputItems
# project regex in solution Project\("\{(?<Type>.{36})\}"\)\s*=\s*"(?<Name>.*)",\s*"(?<Path>.*)",\s*"\{(?<Id>.{36})\}"

$root = $MyInvocation.MyCommand.Path | Split-Path -parent


$codebaseRoot = "."
. .\codebaseConfig.ps1

function Get-ProjectOutput ($project) {
    $output = &$msbuild $root\yam.targets /t:GetProjectOutput /p:project=$project /nologo /v:m
    if ($LastExitCode -ne 0) {
        throw "error: $output"
    }
    $output.trim()
}

function Get-ProjectOutputItems ($project) {
    $output = &$msbuild $root\yam.targets /t:GetProjectOutputItems /p:project=$project /nologo /v:m
    if ($LastExitCode -ne 0) {
        throw "error: $output"
    }
    $output.trim()
}

function Set-Config(){
    $projects = $codebaseConfig.projectDirs | 
        ? { Test-Path $_} | 
        Get-ChildItem -Recurse -filter *.csproj | 
        % { $_.FullName }| 
        % { 
            @{
                "Output" = Get-ProjectOutput $_
                "Project" = Resolve-Path $_ -Relative
            }
        }

    $tmpConfigFile = "$codebaseRoot\prj.config.tmp"
    if(Test-Path $tmpConfigFile){
        Remove-Item $tmpConfigFile
    }
    $projects | %{ 
        $fileName = [System.IO.Path]::GetFileNameWithoutExtension($_.Output)
        $prj = Resolve-Path $($_.Project) -Relative
        Add-Content $tmpConfigFile "Project, $fileName, $prj" 
    }

    $codebaseConfig.libDirs | 
        ? { Test-Path $_} | 
        Get-ChildItem -Recurse -filter *.dll | 
        % { $_.FullName } | 
        %{ 
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($_)
            $fullName = Resolve-Path $_ -Relative
            Add-Content $tmpConfigFile "Lib, $fileName, $fullName" 
        }
    Move-Item $tmpConfigFile $codebaseRoot\prj.config -Force
}

function Build-Projects ([string[]] $projects, [string] $profile = ''){
    $fullCodebaseDir = Resolve-Path $codebaseRoot
    $fullRootDir = Resolve-Path $root
    $fullProjectDirs = $projects | Resolve-Path
    
    $result = Start-Job {
        param($fullCodebaseDir, $fullRootDir, $fullProjectDirs, $profile)
        $configDir = Get-Item "$fullCodebaseDir\prj.config"
        Add-Type -Path "$fullRootDir\bin\debug\Yam.Core.dll"        
        Update-TypeData -prependpath "$fullRootDir\yam.types.ps1xml"
        $cfg = new-object Yam.Core.ResolveConfig $configDir.FullName, $fullCodebaseDir
        $patcher = new-object Yam.Core.MSBuildPatcher $cfg
        $result = $patcher.Resolve([string[]]$fullProjectDirs, $profile)
        $result
    } -ArgumentList $fullCodebaseDir, $fullRootDir, $fullProjectDirs, $profile |
        Wait-Job | Receive-Job
    $env:EnableNuGetPackageRestore = "true"
    $result.CompileProjects | % { &$msbuild $_.FullPath }
    $result.CopyItemSets | % { 
        $destOutput = Get-ProjectOutput $_.DestProject 
        $destDir = Split-Path $destOutput -Parent

        $_.Projects | % { Get-ProjectOutput $_ } | ? { 
            $actDir = Split-Path $_ -Parent
            $actDir -ne $destDir
        } | % { Copy-Item $_ -destination $destDir }

        $_.Libs | ? { 
            $libDir = Split-Path $_ -Parent
            $libDir -ne $destDir
        } | % { Copy-Item $_ -destination $destDir }

        $_.ItemProjects | % { Get-ProjectOutputItems $_ } | % { Copy-Item $_ -destination $destDir }        
    }
}

switch ($command){
    'config' {
        Set-Config
    }
    'build'{
        Build-Projects @args
    }
    'help'{
        "write some help here"
    }
}
