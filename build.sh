#!/usr/bin/env bash
set -e
dotnet run --project ./CakeBuild/CakeBuild.csproj -- "$@"
