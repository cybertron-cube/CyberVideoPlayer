#!/bin/sh

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
REPO_DIR=$( dirname "$SCRIPT_DIR" )

UPDATER_DIR="$REPO_DIR/cyber-lib/build"

read -p 'Build updater? [y/n]: ' BUILD_UPDATE

if [ $BUILD_UPDATE = "y" ]
then
    dotnet publish "$REPO_DIR/cyber-lib/UpdaterAvalonia/UpdaterAvalonia.csproj" -o "$UPDATER_DIR/linux-x64" -r linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true
fi

read -p 'Enter version number: ' VERSION

read -p 'Single (s), multi (m), or both?: ' TARGET

rm -rf build

if [ $TARGET = "s" ]
then
    python3 build.py -version $VERSION -compile "linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel -resetversion
fi

if [ $TARGET = "m" ]
then
    python3 build.py -version $VERSION -compile "linux-x64-multi" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel -resetversion
else
    python3 build.py -version $VERSION -compile "linux-x64-multi" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel
    python3 build.py -compile "linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel -resetversion
fi

chmod -R +x build

cd build

if [ $TARGET = "s" ]
then
    echo "zipping single..."
    tar -czf linux-x64-single.tar.gz linux-x64-single
fi

if [ $TARGET = "m" ]
then
    echo "zipping multi..."
    tar -czf linux-x64-multi.tar.gz linux-x64-multi
else
    echo "zipping multi..."
    tar -czf linux-x64-multi.tar.gz linux-x64-multi
    echo "zipping single..."
    tar -czf linux-x64-single.tar.gz linux-x64-single
fi
