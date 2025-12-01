# Trumpf.Coparoo.Waiting.WinForms

Windows Forms-based visual waiting dialogs for automated tests.

## Overview

This package extends `Trumpf.Coparoo.Waiting` with interactive UI overlays that show the current state of test conditions in real-time. The dialogs turn red for false conditions, green when conditions are true, and grey to indicate when manual intervention is required.

**Note:** This is a Windows-only package that requires Windows Forms.

## Installation

```bash
dotnet add package Trumpf.Coparoo.Waiting.WinForms
```

## Usage

### Basic Usage with ConditionDialogWaiter

```csharp
using Trumpf.Coparoo.Waiting.WinForms;
using Trumpf.Coparoo.Waiting.Extensions;

var waiter = new ConditionDialogWaiter();
waiter.WaitFor(() => IsConditionMet(), "Waiting for condition to be true");
```

### Manual Interaction with Action Text

```csharp
using Trumpf.Coparoo.Waiting.WinForms;
using Trumpf.Coparoo.Waiting.WinForms.Extensions;

var waiter = new ConditionDialogWaiter();
waiter.WaitForUserAction(
    "Please click the 'Start' button in the application",
    () => IsStarted(),
    value => value,
    "Waiting for application to start"
);
```

## Features

- **Visual Feedback**: Color-coded dialogs (Red = false, Green = true, Grey = manual intervention needed)
- **Click-through Mode**: Transparent overlays that don't interfere with manual testing
- **Action Instructions**: Show instructions to test operators for semi-automated tests
- **Configurable Timeouts**: Separate timeouts for positive and negative conditions
- **Cross-Framework**: Supports .NET Framework 4.8 and .NET 8.0 (Windows)

## Relationship to Core Package

- **Trumpf.Coparoo.Waiting**: Cross-platform core library with `Wait`, `TryWait`, and `SilentWaiter`
- **Trumpf.Coparoo.Waiting.WinForms**: Windows-specific UI extensions with `ConditionDialogWaiter`

For cross-platform or headless scenarios, use only the core `Trumpf.Coparoo.Waiting` package with `SilentWaiter`.

## License

Licensed under the Apache License, Version 2.0. See LICENSE file for details.

Copyright 2016 - 2025 TRUMPF Werkzeugmaschinen GmbH + Co. KG.
