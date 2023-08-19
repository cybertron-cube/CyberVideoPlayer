#!/bin/sh

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

UPDATER_DIR="$SCRIPT_DIR/cyber-lib/build"

read -p 'Build updater? [y/n]: ' CHOICE

if [ $CHOICE = "y" ]
then
    dotnet publish "$SCRIPT_DIR/cyber-lib/UpdaterAvalonia/UpdaterAvalonia.csproj" -o "$UPDATER_DIR/osx-arm64" -r osx.13-arm64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true
fi

read -p 'Enter version number: ' VERSION

py build.py -version $VERSION -compile "osx.13-arm64-single;osx.13-arm64-multi;" -resetversion -cpymds -cpyffmpeg -cpympv -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel
