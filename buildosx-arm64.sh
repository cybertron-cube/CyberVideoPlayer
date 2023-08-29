#!/bin/sh

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

UPDATER_DIR="$SCRIPT_DIR/cyber-lib/build"

read -p 'Build updater? [y/n]: ' BUILD_UPDATE

if [ $BUILD_UPDATE = "y" ]
then
    dotnet publish "$SCRIPT_DIR/cyber-lib/UpdaterAvalonia/UpdaterAvalonia.csproj" -o "$UPDATER_DIR/osx-arm64" -r osx.13-arm64 -p:PublishSingleFile=true -p:PublishTrimmed=true -c release --sc true
fi

read -p 'Enter version number: ' VERSION

py build.py -version $VERSION -compile "osx.13-arm64-single;osx.13-arm64-multi;" -cpymds -cpyffmpeg -cpympv -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel




read -p 'Build app bundle? [y/n]: ' BUILD_BUNDLE

if [ $BUILD_BUNDLE != "y" ]
then
    py build.py -resetversion
    exit
fi




APP_NAME="$SCRIPT_DIR/build/CyberVideoPlayer.app"
PUBLISH_OUTPUT_DIRECTORY="$SCRIPT_DIR/build/osx.13-arm64-single/."
INFO_PLIST="$SCRIPT_DIR/Info.plist"
ICON_FILE="cyber-logo-sunset.icns"
ICON_PATH="$SCRIPT_DIR/src/CyberPlayer.Player/Assets/Logo/$ICON_FILE"

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME"
mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
cp "$ICON_PATH" "$APP_NAME/Contents/Resources/$ICON_FILE"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"

py build.py -resetversion