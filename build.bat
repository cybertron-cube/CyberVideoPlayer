set /p Version=Enter version number: 

set UpdaterDir=A:\Cyber-lib\build

py build.py -del -version %Version% -compile win-x64-multi;linux-x64-multi -resetversion -cpymds -cpyffmpeg -cpympv -cpyupdater %UpdaterDir% -rmpdbs -lib sc;portable-single;portable-multi -zip -delbinrel -delbuilddirs

pause
