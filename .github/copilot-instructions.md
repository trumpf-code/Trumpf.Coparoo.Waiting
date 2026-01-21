# Copilot Instructions for Trumpf.Coparoo.Waiting

## Project Overview

Trumpf.Coparoo.Waiting is a .NET library for automated testing that provides intelligent waiting mechanisms with visual feedback. It enables tests to poll conditions with configurable timeouts and provides visual dialogs for semi-automated test scenarios.

## Architecture

### Projects
1. **Trumpf.Coparoo.Waiting** (Core Library)
   - Platform-independent waiting logic (netstandard2.0, net8.0, net451)
   - Contains: `Wait`, `TryWait`, `SilentWaiter`, interfaces
   - No UI dependencies

2. **Trumpf.Coparoo.Waiting.WinForms**
   - Windows Forms-specific implementation
   - Contains: `ConditionDialogWaiter` with visual feedback dialogs
   - Color-coded status: Red (false), Green (true), Gray (manual intervention)

3. **Trumpf.Coparoo.Waiting.Tests**
   - Unit tests and manual integration tests
   - Test base classes in `Base/` folder

### Key Components
- **Wait**: Synchronous/async waiting with exceptions on timeout
- **TryWait**: Boolean-returning variant without exceptions
- **SilentWaiter**: No visual feedback (for CI/CD environments)
- **ConditionDialogWaiter**: Visual feedback dialogs for manual test scenarios
- **Exceptions**: `WaitForTimeoutException`, `WaitForAbortedException`

## Coding Guidelines

### Language & Framework
- C# with multi-targeting support (net451, netstandard2.0, net8.0)
- Use async/await patterns where appropriate
- Support both synchronous and asynchronous waiting APIs

### Naming Conventions
- Use descriptive names: `WaitUntil`, `TryWaitFor`, etc.
- Exception classes end with `Exception`
- Test classes end with `Tests`
- Manual test classes end with `ManualTests`

### Testing
- Unit tests go in `Trumpf.Coparoo.Waiting.Tests/Wait/`
- Manual tests go in `Trumpf.Coparoo.Waiting.Tests/ManualTests/`
- Inherit from `WaiterTestBase` for common test functionality
- Test both sync and async variants

### Exception Handling
- Throw `WaitForTimeoutException` when conditions timeout
- Throw `WaitForAbortedException` when user aborts waiting
- `TryWait` methods should NOT throw exceptions; return bool instead

### Code Quality
- Sign assemblies with `Key.snk`
- Maintain XML documentation for public APIs
- Support cancellation tokens for async operations
- Use proper disposal patterns (IDisposable where needed)

### Dependencies
- Keep core library dependency-free
- WinForms library should only reference System.Windows.Forms
- Minimize external package dependencies

## Common Tasks

### Adding New Wait Methods
1. Add to both `Wait` and `TryWait` classes if applicable
2. Provide both sync and async overloads
3. Include XML documentation with examples
4. Add corresponding unit tests
5. Consider manual test scenarios for visual waiters

### Modifying Visual Feedback
- Changes to dialogs go in `Trumpf.Coparoo.Waiting.WinForms`
- Maintain color scheme: Red = false, Green = true, Gray = manual
- Ensure thread-safe UI updates

### Version Updates
- Update `AssemblyInfo.cs` in Properties folders
- Update `.csproj` files with new version
- Rebuild to generate new `.snupkg` in artifacts/

## Build & Release
- Solution file: `Trumpf.Coparoo.Waiting.sln`
- Clean build artifacts: `clean_bin_obj.bat`
- NuGet packages generated in `artifacts/` and `bin/Debug|Release/`
- GitHub workflow exists at `.github/workflows/dotnet-desktop.yml`
