call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild SimulatorClient.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd SimulatorClient\bin\release\
  cls
  SimulatorClient
  cd ..\..
) ELSE (
  PAUSE
)