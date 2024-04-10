@echo off

git pull
git submodule update --recursive --remote

set /p Choice=Build updater? (y/n): 

set /p Version=Enter version number: 

if %Choice%==y (
    py build.py -del -version %Version% -compile win-x64-single -buildupdater -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip
)
else (
    py build.py -del -version %Version% -compile win-x64-single -resetversion -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -zip
)

pause
