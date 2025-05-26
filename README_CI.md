# Gum CI Build Workflow Documentation
## Purpose
Automate building of all Gum Sample solutions in the repository.

## CI Build Status
[![Build All](https://github.com/vchelaru/Gum/actions/workflows/ci.yaml/badge.svg)](https://github.com/vchelaru/Gum/actions/workflows/ci.yaml)

## Artifact
The Gum CI build workflow file is located at .github/workflows/ci.yaml

## Core Concepts
- **Event-Driven**: Triggered by GitHub events (push, pull request) on `master` branch.
- **Modular Jobs**: Separates solution discovery (`list-solutions`) from building (`build`).
- **Matrix Builds**: Parallelizes builds across multiple .NET versions and solutions.
- **Error Tolerance**: Failures in one build don’t halt others (`fail-fast: false`).

## Workflow Structure
1. **Solution Discovery**:
   - Scans `Samples` directory for `.sln` files.
   - Excludes specified solutions (e.g., `GumTest`).
   - Outputs JSON list of solutions for dynamic build configuration.
2. **Build Execution**:
   - Depends on solution list.
   - Uses matrix to iterate over .NET versions (`6.0.x`, `8.0.x`) and solutions.
   - Each build:
     - Initializes submodules.
     - Sets up .NET environment.
     - Restores and builds solution in Release mode with minimal output.

## Design Principles
- **Scalability**: Matrix strategy handles varying numbers of solutions and .NET versions.
- **Reusability**: Dynamic solution discovery avoids hardcoding.
- **Efficiency**: Quiet verbosity and error-only logging reduce noise.
- **Robustness**: Strict error handling (`continue-on-error: false`) ensures clear failure points.
## Workflow: Build All

### Trigger Events
- **Push**: On `master` branch
- **Pull Request**: Targeting `master` branch

### Jobs

#### 1. `list-solutions`
- **Purpose**: Identify all `.sln` files in the `Samples` directory, excluding specified solutions.
- **Runner**: `windows-latest`
- **Outputs**: 
  - `solutions`: JSON array of solution names
- **Steps**:
  1. **Checkout Repository**:
     - Action: `actions/checkout@v4`
     - Error Handling: `continue-on-error: false`
  2. **List Solutions**:
     - ID: `set-solutions`
     - Script:
       - Excludes: `GumTest`
       - Finds `.sln` files recursively in `Samples`
       - Filters, sorts, and converts to JSON
       - Outputs: `solutions` to `$env:GITHUB_OUTPUT`

#### 2. `build`
- **Purpose**: Build each solution with specified .NET versions.
- **Runner**: `windows-latest`
- **Dependencies**: Requires `list-solutions`
- **Matrix Strategy**:
  - **Variables**:
    - `dotnet-version`: `['6.0.x', '8.0.x']`
    - `solution`: Dynamically populated from `list-solutions` output
  - **Fail-Fast**: `false` (continues on failure)
- **Steps**:
  1. **Checkout Repository**:
     - Action: `actions/checkout@v4`
     - Error Handling: `continue-on-error: false`
  2. **Initialize Submodules**:
     - Command: `git submodule update --init --recursive`
     - Error Handling: `continue-on-error: false`
  3. **Setup .NET**:
     - Action: `actions/setup-dotnet@v4`
     - Parameter: `dotnet-version` from matrix
     - Error Handling: `continue-on-error: false`
  4. **Build Solution**:
     - Script:
       - Sets environment variables
       - Locates solution file in `Samples`
       - Runs:
         - `dotnet restore` with quiet verbosity
         - `dotnet build` in Release configuration, no restore, minimal output, errors only
     - Error Handling: `continue-on-error: false`
