call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild ..\Simulation\trunk\SimulatorClient\SimulatorClient\SimulatorClient.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd ..\Simulation\trunk\SimulatorClient\SimulatorClient\SimulatorClient\bin\release\
  cls
  SimulatorClient
  cd ..\..
) ELSE (
  PAUSE
)