#!/usr/bin/env bash

dotnet tool restore
dotnet paket install
dotnet build

cd test/Automation
pwsh bin/Debug/net8.0/playwright.ps1 install
pwsh bin/Debug/net8.0/playwright.ps1 install-deps

cd ../..
