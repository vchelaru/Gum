# MonoGame and Gum Example

## What
Sample project integrating MonoGame and Gum for a game with UI, using:
- `Program`: Bootstraps with dependency injection.
- `Game1`: Manages game loop, Gum UI, rendering.
- `MainMenuRuntime`: Sample Gum UI component.

## Why
This approach avoids anti-patterns:
- **Hardcoded Dependencies**: Uses DI to inject `Game1` and `GumService`, preventing tight coupling.
- **Global State**: Encapsulates Gum UI in `Game1`, avoiding scattered state.
- **Resource Leaks**: Implements `Dispose` for `SpriteBatch`, ensuring cleanup.
- **Monolithic Code**: Separates concerns (bootstrap, game logic, configuration).

**Benefits**:
- **Testability**: DI enables mocking for unit tests.
- **Maintainability**: Clear structure simplifies updates.
- **Scalability**: Modular design supports adding features.
- **Reliability**: Proper resource management prevents crashes.

## How
### Prerequisites
- .NET SDK.
- MonoGame (NuGet).
- MonoGame.Gum.
- Gum project file (`GumProject.gumx`).

### Setup
1. Place `GumProject.gumx` in `GumProject/`.
2. Add assets to `Content/`.
3. Restore dependencies:
   ```bash
   dotnet restore
   ```

### Running
```bash
dotnet run
```