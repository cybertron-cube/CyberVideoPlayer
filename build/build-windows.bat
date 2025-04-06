@echo off

git pull
git submodule update --recursive --remote

set /p Choice=Build updater? (y/n): 

set /p Version=Enter version number: 

set /p Installer=Create installer? (y/n): 

if %Installer%==y (
    set Installer= -winpkg
) else (
    set Installer=
)

if %Choice%==y (
    py build.py -del -version %Version% -compile win-x64 -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer% -resetversion
) else (
    py build.py -del -version %Version% -compile win-x64 -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer% -resetversion
)

pause
