#!/bin/sh

PARENT_DIR=$( dirname -- "$( readlink -f -- "$0"; )"; )
ASSETS="$PARENT_DIR/assets"
INSTALL_LOC="$HOME/opt/CyberVideoPlayer"
DESKTOP="/usr/share/applications"
PIXMAPS="/usr/share/pixmaps"
HICOLOR="/usr/share/icons/hicolor"

set -e

# Main application files
echo "Installing application to \"$INSTALL_LOC\" ..."
mkdir -p "$INSTALL_LOC"
cp -f -a "$PARENT_DIR/CyberVideoPlayer/." "$INSTALL_LOC"
chmod -R u+rwx "$INSTALL_LOC"
chown -R $USER "$INSTALL_LOC"

# Soft link for path
echo "Adding to path ..."
sudo ln -s "$INSTALL_LOC/CyberVideoPlayer" "/usr/local/bin/cvp"

# Desktop entry
echo "Creating desktop entry ..."
sudo cp -f "$ASSETS/cybervideoplayer.desktop" "$DESKTOP/cybervideoplayer.desktop"

# Icons
echo "Creating icons ..."
sudo cp -f "$ASSETS/icon_512x512@2x.png" "/usr/share/pixmaps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_16x16.png" "$HICOLOR/16x16/apps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_32x32.png" "$HICOLOR/32x32/apps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_32x32@2x.png" "$HICOLOR/64x64/apps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_128x128.png" "$HICOLOR/128x128/apps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_256x256.png" "$HICOLOR/256x256/apps/cybervideoplayer.png"
sudo cp -f "$ASSETS/icon_512x512.png" "$HICOLOR/512x512/apps/cybervideoplayer.png"

# Set as default video player?
# (won't work for all linux desktop environments)
read -p 'Would you like to set CVP as the default video player? [y/N]: ' CHOICE
if [ "$CHOICE" = "y" ]
then
    xdg-mime default cybervideoplayer.desktop video/x-matroska
    xdg-mime default cybervideoplayer.desktop video/mp4
    xdg-mime default cybervideoplayer.desktop application/mxf
    xdg-mime default cybervideoplayer.desktop video/quicktime
    xdg-mime default cybervideoplayer.desktop video/x-msvideo
    xdg-mime default cybervideoplayer.desktop video/x-ogm+ogg
    xdg-mime default cybervideoplayer.desktop video/ogg
    xdg-mime default cybervideoplayer.desktop video/x-ogm
    xdg-mime default cybervideoplayer.desktop video/x-theora+ogg
    xdg-mime default cybervideoplayer.desktop video/x-theora
    xdg-mime default cybervideoplayer.desktop video/x-ms-asf
    xdg-mime default cybervideoplayer.desktop video/x-ms-asf-plugin
    xdg-mime default cybervideoplayer.desktop video/x-ms-asx
    xdg-mime default cybervideoplayer.desktop video/x-ms-wm
    xdg-mime default cybervideoplayer.desktop video/x-ms-wmv
    xdg-mime default cybervideoplayer.desktop video/x-ms-wmx
    xdg-mime default cybervideoplayer.desktop video/x-ms-wvx
    xdg-mime default cybervideoplayer.desktop video/divx
    xdg-mime default cybervideoplayer.desktop video/msvideo
    xdg-mime default cybervideoplayer.desktop video/vnd.divx
    xdg-mime default cybervideoplayer.desktop video/avi
    xdg-mime default cybervideoplayer.desktop video/x-avi
    xdg-mime default cybervideoplayer.desktop video/vnd.rn-realvideo
    xdg-mime default cybervideoplayer.desktop video/mp2t
    xdg-mime default cybervideoplayer.desktop video/mpeg
    xdg-mime default cybervideoplayer.desktop video/mpeg-system
    xdg-mime default cybervideoplayer.desktop video/x-mpeg
    xdg-mime default cybervideoplayer.desktop video/x-mpeg2
    xdg-mime default cybervideoplayer.desktop video/x-mpeg-system
    xdg-mime default cybervideoplayer.desktop video/mp4v-es
    xdg-mime default cybervideoplayer.desktop video/x-m4v
    xdg-mime default cybervideoplayer.desktop video/webm
    xdg-mime default cybervideoplayer.desktop video/3gp
    xdg-mime default cybervideoplayer.desktop video/3gpp
    xdg-mime default cybervideoplayer.desktop video/3gpp2
    xdg-mime default cybervideoplayer.desktop video/vnd.mpegurl
    xdg-mime default cybervideoplayer.desktop video/dv
    xdg-mime default cybervideoplayer.desktop video/x-anim
    xdg-mime default cybervideoplayer.desktop video/x-nsv
    xdg-mime default cybervideoplayer.desktop video/fli
    xdg-mime default cybervideoplayer.desktop video/flv
    xdg-mime default cybervideoplayer.desktop video/x-flc
    xdg-mime default cybervideoplayer.desktop video/x-fli
    xdg-mime default cybervideoplayer.desktop video/x-flv
fi
