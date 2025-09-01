#!/bin/bash
dotnet publish -c Release --self-contained
echo "Creating output archive..."
tar -czf bin/wowup.tar.gz -C bin/Release/net9.0/linux-x64/publish .
echo "Archive created at bin/wowup.tar.gz"
