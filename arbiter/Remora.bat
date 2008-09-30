call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild .\trunk\RemoraAdvanced\RemoraAdvanced.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd .\trunk\RemoraAdvanced\RemoraAdvanced\bin\release
  cls
  start RemoraAdvanced
  cd ..\..
) ELSE (
  PAUSE
)