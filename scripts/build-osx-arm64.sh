#!/bin/sh

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
REPO_DIR=$( dirname "$SCRIPT_DIR" )

git pull
git submodule update --recursive --remote

read -p 'Enter version number: ' VERSION

if [ $VERSION != "n" ]
then
    python3 build.py -version $VERSION -compile "osx-arm64-single" -cpymds -cpyffmpeg -cpympv -cpymediainfo -rmpdbs -delbinrel
fi




read -p 'Build app bundle? [y/n]: ' BUILD_BUNDLE

if [ $BUILD_BUNDLE != "y" ]
then
    python3 build.py -resetversion
    exit
fi




APP_NAME="$REPO_DIR/build/CyberVideoPlayer.app"
PUBLISH_OUTPUT_DIRECTORY="$REPO_DIR/build/osx-arm64-single/."
INFO_PLIST="$REPO_DIR/Info.plist"
ICON_FILE="cyber-logo-sunset.icns"
ICON_PATH="$REPO_DIR/src/CyberPlayer.Player/Assets/Logo/$ICON_FILE"

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

python3 build.py -resetversion
