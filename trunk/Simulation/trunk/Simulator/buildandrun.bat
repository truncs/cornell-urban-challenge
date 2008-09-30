call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild Simulator.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd simulator\bin\release\
  cls
  start Simulator
) ELSE (
  PAUSE
)