#!/bin/bash

reinstall="0";

while getopts r: flag
do
    case "${flag}" in
        r) reinstall=${OPTARG};;
    esac
done

if [ "$reinstall" != "1" ]; then
    echo "Installing..."
else
    echo "Reinstalling..."
    ./uninstall.sh
fi

dotnet build

dotnet tool install --global --add-source ./Benday.AzureDevOpsUtil.ConsoleUi/bin/Debug azdoutil
