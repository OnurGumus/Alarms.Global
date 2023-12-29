#!/usr/bin/env bash
npm install -g npm@10.2.5
npm ci
dotnet tool restore
dotnet paket install
dotnet build
npm run build --workspace=src/Client

cd test/Automation
pwsh bin/Debug/net8.0/playwright.ps1 install
pwsh bin/Debug/net8.0/playwright.ps1 install-deps

cd ../..
