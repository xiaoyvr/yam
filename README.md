yam
===

Yet another make for .net. A patch for MSBuild. 

Install
===
Yam is based on powershell. 
```
code\base\root> .nuget\nuget.exe install yam -output <tools folder>
code\base\root> <tools folder>\yam.<version>\install.ps1 <codebase full path>
```

Config
===
Create `codebaseConfig.ps1` in codebase root folder. It tells yam the basic structure of your codebase. Things like where are .csproj files, where are all 3part dll files. 
```powershell
$codebaseConfig = @{
    'projectDirs' = @("$codebaseRoot\src", "$codebaseRoot\test") 
    'libDirs' = @("$codebaseRoot\libs", "$codebaseRoot\packages")
}
```

How to use
===
Generate the config file first. 
```
PS> .\yam.ps1 config
```
The you are ready to use. 
```
PS> .\yam.ps1 build mySolution.sln
```
Or 
```
PS> .\yam.ps1 build src\foo\foo.csproj
```
Or 
```
PS> .\yam.ps1 build src\foo\foo.csproj, src\bar\bar.csproj
```
