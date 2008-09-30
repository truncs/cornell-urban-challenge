call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild Operational\Operational.sln /property:Configuration=Release /p:Platform=Win32

IF %errorlevel% EQU 0 (
  cd Operational\bin\x86\release\
  cls
  Operational /test
  cd ..\..
) ELSE (
  PAUSE
)