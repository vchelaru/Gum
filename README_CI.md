# Gum CI Build Workflow Documentation
## Purpose
Automate building of all Gum Sample solutions in the repository.

## CI Build Status
[![Build All](https://github.com/vchelaru/Gum/actions/workflows/ci.yaml/badge.svg)](https://github.com/vchelaru/Gum/actions/workflows/ci.yaml)

## Artifact
The Gum CI build workflow file is located at .github/workflows/ci.yaml

## Core Concepts
- **Event-Driven**: Triggered by GitHub events (push, pull request) on `main` branch.
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
- **Push**: On `main` branch
- **Pull Request**: Targeting `main` branch

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

## Workflow: Test Runtimes (`build-and-test.yaml`) fast mode

Required checks are `Build-and-Test (windows-latest)` and `Build-and-Test (macos-15)`. Both always report.

### Scopes
- **full** (default): no label, `workflow_dispatch`, push to main. Every job runs on real runners, including the Mesa test streams, Avalonia on three OSes (dispatch), Native-AOT and Build-Generated-Code.
- **fast**: the PR has the `fast-ci` label. Windows still compiles every test project but runs only the headless test projects (3 concurrent streams). The macOS leg is a no-op on `ubuntu-latest`. Avalonia, Native-AOT and Build-Generated-Code run only if the PR touches their paths (the lists are in the `Changes` job). Adding or removing the label re-runs the PR's run (`fast-ci-label.yaml`); the scope is read from the label when the `Changes` job runs.
- Docs-only PRs and the Windows leg of a push to main are no-ops on `ubuntu-latest` (about 10 s).

### What fast mode does not run
Mesa projects (IntegrationTests, Shapes, Cli, RaylibGum, ProjectServices.Raylib/MonoGame), the macOS-only steps (iOS/Android builds, `.app` bundle check, Cli tests on macOS), and Avalonia/AOT/GenCode when their paths are untouched. A break only one of those would catch is found by the full run, so every fast session ends with one.

### Full run on demand
`gh workflow run build-and-test.yaml --ref main` runs everything (about 8 minutes wall, all jobs, Windows and macOS on real runners). A push to main does not: it only seeds caches.

### Session-end checklist (after a fast session)
1. Make sure every PR of the session is merged, then dispatch the full run on main: `gh workflow run build-and-test.yaml --ref main`, and `gh workflow run project-sweep.yaml --ref main`. Note the head sha of main when you dispatch; both runs must be on that sha.
2. Require every job green in both runs. Do not rerun a red job: read the log and fix the cause.
3. Run the FRB1 canary (`frb-build-verification`); no Gum CI job covers it.
4. If a run is red, find the PR: push temporary branches from the session's merge commits (bisect) and dispatch the full run on the suspect, then fix forward or revert.
5. Never cut a release unless a full run was green on the exact sha being released.
