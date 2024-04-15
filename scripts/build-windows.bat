@echo off

git pull
git submodule update --recursive --remote

set /p Choice=Build updater? (y/n): 

set /p Version=Enter version number: 

set /p Build=Single (s), multi (m), or both?: 

set /p Installer=Create installer? (y/n): 

if %Installer%==y (
    set Installer= -winpkg
) else (
    set Installer=
)

if %Choice%==y (
    if %Build%==s (
        py build.py -del -version %Version% -compile win-x64-single -buildupdater -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer%
    ) else (
        if %Build%==m (
            py build.py -del -version %Version% -compile win-x64-multi -buildupdater -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip
        ) else (
            py build.py -del -version %Version% -compile win-x64-single;win-x64-multi -buildupdater -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer%
        )
    )
) else (
    if %Build%==s (
        py build.py -del -version %Version% -compile win-x64-single -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer%
    ) else (
        if %Build%==m (
            py build.py -del -version %Version% -compile win-x64-multi -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip
        ) else (
            py build.py -del -version %Version% -compile win-x64-single;win-x64-multi -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip%Installer%
        )
    )
)

pause
