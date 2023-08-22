@echo off

set /p Version=Enter version number: 

set UpdaterDir=%~dp0cyber-lib\build

call py build.py -version %Version% -compile win-x64-installer -resetversion -cpymds -cpyffmpeg -cpympv -cpyupdater %UpdaterDir% -rmpdbs -delbinrel

pause
