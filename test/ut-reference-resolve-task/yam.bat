@ECHO OFF
SETLOCAL

IF "%msbuild_exe%" == "" GOTO SET_MSBUILD
GOTO SET_MSBUILD_FINISHED

:SET_MSBUILD
SET msbuild_exe=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe /m:4
:SET_MSBUILD_FINISHED

IF /I [%1]==[] (
	SET target=
) ELSE (
	SET target=%1
)
IF /I [%1]==[build] (
	SET property=/p:buildTarget=%2
)
IF /I [%1]==[resolve] (
	SET property=/p:buildTarget=%2
)
IF /I [%1]==[buildp] (
	SET target=build
	SET property=/p:buildTarget=%2
)
IF /I [%1]==[buildfp] (
	SET target=build
	SET property=/p:buildTarget=%2
)
IF /I [%1]==[buildffp] (
	SET property=/p:fprjf=%2
	SET target=build
)
IF /I [%1]==[graph] (
	SET target=graphviz
	SET property=/p:buildTarget=%2
)
IF /I [%1]==[rgraph] (
	SET target=graphviz
	SET property=/p:buildTarget=%2;reverse=true
)
IF /I [%3]==[] (
	rem SET property=%property%
) ELSE (
	SET property=%property%;runtimeProfile=%3
)
IF /I [%1]==[locate] (
	SET target=graphviz
	SET property=/p:buildTarget=%2;dest=%3;runtimeProfile=%4
)

IF /I [%target%] == [] (
	rem set target = 
) ELSE (
	set target=/t:%target%
)
set currentdir=%~dp0
echo Command: %msbuild_exe% %currentdir%yam.build %target%  %property%
%msbuild_exe% %~dp0\yam.build %target% %property%
