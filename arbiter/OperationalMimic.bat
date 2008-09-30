call "C:\Program Files\Microsoft Visual Studio 8\VC\vcvarsall.bat" x86
msbuild ..\operational\FakeOperational\FakeOperational.sln /property:Configuration=Release

IF %errorlevel% EQU 0 (
  cd ..\operational\FakeOperational\FakeOperational\bin\release\
  cls
  FakeOperational
  cd ..\..
) ELSE (
  PAUSE
)