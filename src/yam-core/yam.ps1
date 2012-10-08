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
	if($output) {
		$output | % { $_.trim()}
	} else {
		@()
	}	
}

function Set-Config(){
	$tmpConfigFile = "$codebaseRoot\prj.config.tmp"
	Set-Content $tmpConfigFile $null
	
    $codebaseConfig.projectDirs | 
        ? { Test-Path $_} | 
        Get-ChildItem -Recurse -filter *.csproj | 
        % { $_.FullName }| % { 
            @{
                "Output" = Get-ProjectOutput $_
                "Project" = Resolve-Path $_ -Relative
            }
        } | %{ 
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

function Resolve-Projects([string[]] $files, [string] $profile = ''){
    $result = Get-PatchedResult $files $profile
	write-host '------------------------------  build  ------------------------------' -f cyan
	$result.CompileProjects | % { write-host "$($_.FullPath) -> $($_.Output)" -f DarkGray }
	
    $result.CopyItemSets | % { 
        $destOutput = Get-ProjectOutput $_.DestProject 
        $destDir = Split-Path $destOutput -Parent
		write-host "Copy to: $destDir" -f cyan
        $_.Projects | % { Get-ProjectOutput $_ } | ? { 
            $actDir = Split-Path $_ -Parent
            $actDir -ne $destDir
        } | % { write-host "$_ -> $destDir" -f DarkGray }

        $_.Libs | ? { 
            $libDir = Split-Path $_ -Parent
            $libDir -ne $destDir
        } | % { write-host "$_ -> $destDir" -f DarkGray }
		
        $_.ItemProjects | % { Get-ProjectOutputItems $_ } | % { write-host "$_ -> $destDir" -f DarkGray }
    }
	write-host '------------------------------  end  ------------------------------' -f cyan
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

function Get-PatchedResult ([string[]] $files, [string] $profile){
	$projects = $files | Get-Item | % {
		$ext = $_.extension
		$file = $_
		switch ($ext) {
			'.csproj'{
				$file
			}
			'.sln' {
				Get-SolutionProjects $file
			}
			'default' {
				throw 'Error: $file is not supported. '
			}
		}
	} | select -Unique

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
    $result = Get-PatchedResult $files $profile
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
function Show-Help {
@"
write some help here. 
"@
}

$msbuild = Get-MSBuild
$root = $MyInvocation.MyCommand.Path | Split-Path -parent
$codebaseRoot = "."
. .\codebaseConfig.ps1

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
