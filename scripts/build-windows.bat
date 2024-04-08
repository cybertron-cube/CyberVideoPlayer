@echo off

git pull
git submodule update --recursive --remote

set /p Choice=Build updater? (y/n): 

if %Choice%==y (
    py build.py -buildupdater
)

set /p Version=Enter version number: 

set UpdaterDir=%~dp0..\cyber-lib\build

py build.py -del -version %Version% -compile win-x64-single -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater %UpdaterDir% -rmpdbs -delbinrel -zip

pause
