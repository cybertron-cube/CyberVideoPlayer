#!/bin/sh

read -p 'Enter version number: ' VERSION

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

UPDATER_DIR="$SCRIPT_DIR/cyber-lib/build"

py build.py -version $VERSION -compile "osx.13-arm64-single;osx.13-arm64-multi;" -resetversion -cpymds -cpyffmpeg -cpympv -cpyupdater $UPDATER_DIR -rmpdbs -delbinrel
