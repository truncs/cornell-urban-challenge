call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild .\trunk\ArbiterAdvanced\ArbiterAdvanced.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd .\trunk\ArbiterAdvanced\ArbiterAdvanced\bin\release
  cls
  ArbiterAdvanced
  cd ..\..
) ELSE (
  PAUSE
)