#!/bin/sh

BUILD_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
REPO_DIR=$( dirname "$BUILD_DIR" )
OUTPUT_DIR="$BUILD_DIR/output"
CFG_DIR="$BUILD_DIR/setup-config-osx"
PY_SCRIPT="$BUILD_DIR/build.py"

# $1 = Architecture
CreateAppPackageAndPKG() {
    local APP_NAME="$OUTPUT_DIR/CVP-$1/Applications/CyberVideoPlayer.app"
    local PUBLISH_OUTPUT_DIRECTORY="$OUTPUT_DIR/$1/."
    local INFO_PLIST="$CFG_DIR/Info.plist"
    local ICON_FILE="cyber-logo-sunset.icns"
    local ICON_PATH="$REPO_DIR/src/CyberPlayer.Player/Assets/Logo/$ICON_FILE"

    if [ -d "$APP_NAME" ]
    then
        rm -rf "$APP_NAME"
    fi

    mkdir -p "$APP_NAME"
    mkdir "$APP_NAME/Contents"
    mkdir "$APP_NAME/Contents/MacOS"
    mkdir "$APP_NAME/Contents/Resources"

    cp -f "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
    cp -f "$ICON_PATH" "$APP_NAME/Contents/Resources/$ICON_FILE"
    cp -f -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"

    echo "creating pkg for $1 ..."
    local dir="$(pwd)"
    cd "$CFG_DIR"
    chmod +x scripts/postinstall
    pkgbuild --root "$OUTPUT_DIR/CVP-$1" --scripts scripts --component-plist MainComponent.plist --ownership preserve MainComponent.pkg
    productbuild --resources resources --distribution distribution.xml --package-path MainComponent.pkg "$OUTPUT_DIR/CVP-$1-setup.pkg"
    rm -f MainComponent.pkg
    cd "$dir"
}

TarBuilds() {
    local dir="$(pwd)"
    cd "$OUTPUT_DIR"
    if [ "$ARCHITECTURE" = "osx-arm64;osx-x64" ]
    then
        echo "archiving osx-arm64 ..."
        tar -czf "osx-arm64.tar.gz" "osx-arm64"
        echo "archiving osx-x64 ..."
        tar -czf "osx-x64.tar.gz" "osx-x64"
    else
        echo "archiving $ARCHITECTURE ..."
        tar -czf "$ARCHITECTURE.tar.gz" "$ARCHITECTURE"
    fi
    cd "$dir"
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
    read -p 'Build app bundle? [y/n]: ' BUILD_BUNDLE
    # Can natively compile for both architectures on arm64 system
    # Can only natively compile arm64 on arm64 system
    read -p 'Build x64, arm64, or both [x, a, b]: ' ARCHITECTURE
else
    # ARGS=true
    PYTHON="python"
    VERSION="$1"
    BUILD_UPDATE="$2"
    BUILD_BUNDLE="$3"
    ARCHITECTURE="$4"
fi

if [ "$ARCHITECTURE" = "x" ]
then
    ARCHITECTURE="osx-x64"
elif [ "$ARCHITECTURE" = "a" ]
then
    ARCHITECTURE="osx-arm64"
else
    ARCHITECTURE="osx-arm64;osx-x64"
fi

if [ "$VERSION" != "n" ]
then
    rm -rf "$OUTPUT_DIR"
    if [ "$BUILD_UPDATE" = "y" ]
    then
        $PYTHON "$PY_SCRIPT" -version $VERSION -compile $ARCHITECTURE -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel
    else
        $PYTHON "$PY_SCRIPT" -version $VERSION -compile $ARCHITECTURE -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel
    fi
    TarBuilds
fi

if [ "$BUILD_BUNDLE" != "y" ]
then
    $PYTHON "$PY_SCRIPT" -resetversion
    exit
fi

if [ "$ARCHITECTURE" = "osx-arm64;osx-x64" ]
then
    CreateAppPackageAndPKG "osx-arm64"
    CreateAppPackageAndPKG "osx-x64"
else
    CreateAppPackageAndPKG "$ARCHITECTURE"
fi

$PYTHON "$PY_SCRIPT" -resetversion
