#!/bin/bash
mkdir -p $HOME/.local/bin
mkdir -p $HOME/.local/share/wowup
mkdir -p $HOME/.cache/wowup
mkdir -p $HOME/.config/wowup
cp -r . $HOME/.local/share/wowup
ln -sf ~/.local/share/wowup/wowup ~/.local/bin/wowup

if [[ ":$PATH:" == *":$HOME/.local/bin:"* ]]; then
  echo "wowup installed."
else
  echo "wowup installed but $HOME/.local/bin is not in PATH."
fi
