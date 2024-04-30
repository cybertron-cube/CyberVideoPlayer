#!/bin/sh

SCRIPT_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
REPO_DIR=$( dirname "$SCRIPT_DIR" )
BUILD_DIR="$REPO_DIR/build"
PY_SCRIPT="$SCRIPT_DIR/build.py"

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

rm -rf "$BUILD_DIR"

if [ "$BUILD_UPDATE" = "y" ]
then
    $PYTHON "$PY_SCRIPT" -version $VERSION -compile "linux-x64" -buildupdater -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
else
    $PYTHON "$PY_SCRIPT" -version $VERSION -compile "linux-x64" -cpymds -cpyffmpeg -cpympv -cpymediainfo -cpyupdater -rmpdbs -delbinrel -resetversion
fi

chmod -R 777 "$BUILD_DIR"

cd "$BUILD_DIR"

echo "archiving linux-x64 ..."
tar -czf linux-x64.tar.gz linux-x64
