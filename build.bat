set /p Version=Enter version number: 

set UpdaterDir=A:\Cyber-lib\build

py build.py -del -version %Version% -compile all -cpymds -cpyffmpeg -cpyupdater %UpdaterDir% -rmpdbs -lib sc;portable-single;portable-multi -zip -delbinrel
pause