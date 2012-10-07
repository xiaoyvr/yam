param($command)

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
$codebaseConfig = @{
    'projectDirs' = @("$codebaseRoot\src", "$codebaseRoot\test") 
    'libDirs' = @("$codebaseRoot\libs", "$codebaseRoot\packages")
}

switch ($command){
    'config' {
        $prjs = $codebaseConfig.projectDirs | ? { Test-Path $_} | Get-ChildItem -Recurse -filter *.csproj | % { $_.FullName }
        $results = $prjs | % { 
            $output = &$msbuild $root\yam.targets /t:GetProjectOutput /p:project=$_ /nologo /v:m
            @{
                "Output" = $output.trim()
                "Project" = Resolve-Path $_ -Relative
            }
        } 
        if ($LastExitCode -ne 0) {
            throw "error: $outputs"
        }
        if (Test-Path $codebaseRoot\prj.config) {
            Remove-Item $codebaseRoot\prj.config    
        }
        $results | %{ 
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($_.Output)
            $prj = Resolve-Path $($_.Project) -Relative
            Add-Content $codebaseRoot\prj.config "Project, $fileName, $prj" 
        }

        $libs = $codebaseConfig.libDirs | ? { Test-Path $_} | Get-ChildItem -Recurse -filter *.dll | % { $_.FullName }
        $libs | %{ 
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($_)
            $prj = Resolve-Path $_ -Relative
            Add-Content $codebaseRoot\prj.config "Lib, $fileName, $prj" 
        }
    }
    'help'{

    }
}

