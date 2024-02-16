@echo off

set /p Version=Enter version number: 

set UpdaterDir=%~dp0..\cyber-lib\build

call py %~dp0build.py -version %Version% -compile win-x64-installer -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater %UpdaterDir% -rmpdbs -delbinrel

pause
