#!/bin/bash
dotnet publish
mkdir -p $HOME/.local/bin
mkdir -p $HOME/.local/share/wowup
cp -r bin/Release/net9.0/publish/. $HOME/.local/share/wowup
ln -sf ~/.local/share/wowup/wowup ~/.local/bin/wowup
