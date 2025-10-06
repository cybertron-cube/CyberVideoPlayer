#!/bin/sh

BUILD_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
REPO_DIR=$( dirname "$BUILD_DIR" )
OUTPUT_DIR="$BUILD_DIR/output"
CFG_DIR="$BUILD_DIR/setup-config-linux"
PY_SCRIPT="$BUILD_DIR/build.py"

CreateSetup() {
    local STAGE="$OUTPUT_DIR/CVP-$1"
    local ASSETS="$STAGE/assets"
    local PUBLISH_OUTPUT_DIRECTORY="$OUTPUT_DIR/$1/."
    local ICONS="$REPO_DIR/src/CyberPlayer.Player/Assets/Logo/cyber-logo-sunset.iconset/."

    echo "creating setup for $1 ..."

    mkdir -p "$ASSETS"
    mkdir -p "$STAGE/CyberVideoPlayer"

    # Main program files
    cp -f -a "$PUBLISH_OUTPUT_DIRECTORY" "$STAGE/CyberVideoPlayer"

    # Desktop shortcut
    cp -f "$CFG_DIR/cybervideoplayer.desktop" "$ASSETS/cybervideoplayer.desktop"

    # Hicolor icons
    cp -f -a "$ICONS" "$ASSETS"

    # Installation script
    cp -f "$CFG_DIR/install.sh" "$STAGE/install.sh"
    chmod +x "$STAGE/install.sh"

    echo "archiving CVP-$1-setup ..."
    cd "$OUTPUT_DIR"
    tar -czf CVP-$1-setup.tar.gz CVP-$1
}

set -e

git pull
git submodule update --recursive --remote

if [ $# -eq 0 ]
then
    # ARGS=false
    PYTHON="python3"
    read -p 'Enter version number: ' VERSION
    read -p 'Build updater? [y/n]: ' BUILD_UPDATE
else
    # ARGS=true
    PYTHON="python"
    VERSION="$1"
    BUILD_UPDATE="$2"
fi

rm -rf "$OUTPUT_DIR"

if [ "$BUILD_UPDATE" = "y" ]
then
    $PYTHON "$PY_SCRIPT" -version $VERSION -compile "linux-x64" -buildupdater -cpymds -cpyffmpeg -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
else
    $PYTHON "$PY_SCRIPT" -version $VERSION -compile "linux-x64" -cpymds -cpyffmpeg -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

chmod -R 777 "$OUTPUT_DIR"

cd "$OUTPUT_DIR"

echo "archiving linux-x64 ..."
tar -czf linux-x64.tar.gz linux-x64

CreateSetup "linux-x64"
