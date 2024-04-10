#!/bin/sh

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
REPO_DIR=$( dirname "$SCRIPT_DIR" )

git pull
git submodule update --recursive --remote

read -p 'Build updater? [y/n]: ' BUILD_UPDATE

read -p 'Enter version number: ' VERSION

read -p 'Single (s), multi (m), or both?: ' TARGET

rm -rf build

if [ $TARGET = "s" ] && [ $BUILD_UPDATE = "y" ]
then
    python3 build.py -version $VERSION -compile "linux-x64-single" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ $TARGET = "s" ]
    python3 build.py -version $VERSION -compile "linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

if [ $TARGET = "m" ] && [ $BUILD_UPDATE = "y" ]
then
    python3 build.py -version $VERSION -compile "linux-x64-multi" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ $TARGET = "m" ]
    python3 build.py -version $VERSION -compile "linux-x64-multi" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

if [ $TARGET != "s" ] && [ $TARGET != "m" ] && [ $BUILD_UPDATE = "y" ]
then
    python3 build.py -version $VERSION -compile "linux-x64-multi;linux-x64-single" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ $TARGET != "s" ] && [ $TARGET != "m" ]
    python3 build.py -version $VERSION -compile "linux-x64-multi;linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
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
