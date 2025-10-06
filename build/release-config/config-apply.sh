#!/bin/sh

set -e

RELEASE_CONFIG_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
BUILD_DIR=$( dirname "$RELEASE_CONFIG_DIR" )
REPO_DIR=$( dirname "$BUILD_DIR" )

if [ $# -eq 0 ]
then
    # ARGS=false
    read -p 'Enter config name: ' CONFIG
else
    # ARGS=true
    CONFIG_NAME="$1"
fi

CONFIG_FILE="$RELEASE_CONFIG_DIR/config-$CONFIG_NAME.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: Config file \"$CONFIG_NAME\" not found!"
    exit
fi

cat "$CONFIG_FILE" > "$REPO_DIR/.releaserc.json"

cat "$REPO_DIR/.releaserc.json"
