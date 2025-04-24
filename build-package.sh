#!/bin/bash

# Exit on error
set -e

# Default configuration
CONFIG="Release"
SOLUTION="Serilog.Sinks.Datadog.Logs.sln"
OUTPUT_DIR="./artifacts"

# Process arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --config|-c)
      CONFIG="$2"
      shift 2
      ;;
    --solution|-s)
      SOLUTION="$2"
      shift 2
      ;;
    --output|-o)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --help|-h)
      echo "Usage: $(basename "$0") [options]"
      echo "Options:"
      echo "  --config, -c CONFIG     Build configuration (Debug/Release, default: Release)"
      echo "  --solution, -s SOLUTION Solution file to build (default: Serilog.Sinks.Datadog.Logs.sln)"
      echo "  --output, -o DIR        Output directory for packages (default: ./artifacts)"
      echo "  --help, -h              Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

echo "Building package with configuration: $CONFIG"
echo "Solution: $SOLUTION"
echo "Output directory: $OUTPUT_DIR"

# Step 1: Restore packages
echo "Restoring packages..."
dotnet msbuild "$SOLUTION" /t:restore /p:Configuration="$CONFIG"

# Step 2: Build solution
echo "Building solution..."
dotnet msbuild "$SOLUTION" /p:Configuration="$CONFIG"

# Step 3: Create NuGet package
echo "Creating NuGet package..."
dotnet msbuild "$SOLUTION" /t:pack /p:Configuration="$CONFIG" /p:PackageOutputPath="$OUTPUT_DIR"

echo "Build completed successfully!"
echo "Packages are available in: $OUTPUT_DIR"