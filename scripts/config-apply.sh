#!/bin/sh

set -e

SCRIPT_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
REPO_DIR=$( dirname "$SCRIPT_DIR" )

if [ $# -eq 0 ]
then
    # ARGS=false
    read -p 'Enter config name: ' CONFIG
else
    # ARGS=true
    CONFIG_NAME="$1"
fi

CONFIG_FILE="$SCRIPT_DIR/config-$CONFIG_NAME.json"

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: Config file \"$CONFIG_NAME\" not found!"
    exit
fi

cat "$CONFIG_FILE" > "$REPO_DIR/.releaserc"
