#!/bin/sh

SCRIPT_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
REPO_DIR=$( dirname "$SCRIPT_DIR" )
BUILD_DIR="$REPO_DIR/build"

git pull
git submodule update --recursive --remote

read -p 'Build updater? [y/n]: ' BUILD_UPDATE

read -p 'Enter version number: ' VERSION

read -p 'Single (s), multi (m), or both?: ' TARGET

rm -rf "$BUILD_DIR"

if [ "$TARGET" = "s" ] && [ "$BUILD_UPDATE" = "y" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-single" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ "$TARGET" = "s" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

if [ "$TARGET" = "m" ] && [ "$BUILD_UPDATE" = "y" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-multi" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ "$TARGET" = "m" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-multi" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

if [ "$TARGET" != "s" ] && [ "$TARGET" != "m" ] && [ "$BUILD_UPDATE" = "y" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-multi;linux-x64-single" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
elif [ "$TARGET" != "s" ] && [ "$TARGET" != "m" ]
then
    python3 "$SCRIPT_DIR/build.py" -version $VERSION -compile "linux-x64-multi;linux-x64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

chmod -R +x "$BUILD_DIR"

cd "$BUILD_DIR"

if [ "$TARGET" = "s" ]
then
    echo "zipping single..."
    tar -czf linux-x64-single.tar.gz linux-x64-single
fi

if [ "$TARGET" = "m" ]
then
    echo "zipping multi..."
    tar -czf linux-x64-multi.tar.gz linux-x64-multi
else
    echo "zipping multi..."
    tar -czf linux-x64-multi.tar.gz linux-x64-multi
    echo "zipping single..."
    tar -czf linux-x64-single.tar.gz linux-x64-single
fi
