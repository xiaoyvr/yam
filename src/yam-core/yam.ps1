param($command)

$bitness = ''
$ptrSize = [System.IntPtr]::Size
if ($ptrSize = 8) {
    $bitness = '64'
}
$version = 'v4.0.30319'
$msbuild = "$env:windir\Microsoft.NET\Framework$bitness\$version\MSBuild.exe"
$project = "C:\e x b\yam-c ore.csproj"
$result = &$msbuild yam.targets /t:GetProjectOutput /p:project=$project /nologo /v:m 
write-host $result -f yellow
