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

Other Features
===
Graph
---
To gerenate a project dependence graph, you need graphviz installed first. 
```
PS> .\yam.ps1 graph src\foo\foo.csproj -ends src\bar\bar.csproj
```
This command will give you the dependence graph between foo.csproj and bar.csproj

If you run
```
PS> .\yam.ps1 graph src\foo\foo.csproj
```
It will give you the all dependences of foo.csproj. 

If you want to find out who depends on bar.csproj, just run 
```
PS> .\yam.ps1 graph src\bar\bar.csproj -reverse
```
Resolve Copy Local Settings
---
The default copy local setting for references of any project in vistual studio is True. But for Class Library projects, it's not necessary. Most of time, we only needs to set it to true for projects like Application, Windows Service, WebSite or Test. By doing this, we can save lots of time for compiling. 

To check the copy local settings, run this command. 
```
PS> .\yam.ps1 cl
```
If yam find out there's a *.nuspec file in the project folder. It will ignore the copy local check. This command will only check projects without *.nuspec file. 
