@echo off

set /p Choice=Update submodules and build updater? (y/n): 

if %Choice%==y (
    call git submodule update --recursive --remote
    call py build.py -buildupdater
)

set /p Version=Enter version number: 

set UpdaterDir=%~dp0cyber-lib\build

call py build.py -del -version %Version% -compile win-x64-multi;linux-x64-multi -resetversion -cpymds -cpyffmpeg -cpympv -cpyupdater %UpdaterDir% -rmpdbs -lib sc;portable-single;portable-multi -zip -delbinrel -delbuilddirs

pause
