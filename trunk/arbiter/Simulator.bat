call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild ..\Simulation\trunk\Simulator\Simulator.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd ..\Simulation\trunk\Simulator\Simulator\bin\release\
  cls
  Simulator
  cd ..\..
) ELSE (
  PAUSE
)