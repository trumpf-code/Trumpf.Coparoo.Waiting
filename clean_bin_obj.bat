@echo on
@set pathToBatch=%~dp0
@pushd %pathToBatch%\..\src
FOR /d /r . %%d IN (bin,obj) DO @if exist "%%d" RMDIR /S /Q "%%d
RMDIR /S /Q TestResults
@popd
@exit /b