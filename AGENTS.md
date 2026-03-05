# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview

KNCSDecomp.NET is a C#/.NET 8.0 Avalonia UI desktop application that decompiles NCS (NWScript Compiled Script) bytecode files from KOTOR into readable NSS source. It has both GUI (default) and CLI modes.

### Dependencies

The BioWare.NET library (extracted from [Andastra](https://github.com/OldRepublicDevs/Andastra)) is vendored at `vendor/BioWare.NET/`. This provides the core decompilation engine under namespace `BioWare.Resource.Formats.NCS.Decomp`.

### .NET SDK

Requires .NET 8.0 SDK. In Cloud VMs it is installed at `$HOME/.dotnet` via the dotnet-install script. Ensure `DOTNET_ROOT=$HOME/.dotnet` and `$HOME/.dotnet` is on PATH (the update script handles this).

### Build & Run

- **Restore**: `dotnet restore`
- **Build**: `dotnet build`
- **Run CLI**: `dotnet run -- --help nonexistent.ncs` (needs a dummy file arg to enter CLI mode)
- **Run CLI decompile**: `dotnet run -- file.ncs --output-dir ./output`
- **Run GUI**: `dotnet run` (requires display server / X11)
- **Lint**: Build warnings serve as lint. No separate linter configured.

### No automated tests

This project has no test projects or test frameworks configured. Validation is done manually by decompiling `.ncs` files.

### Runtime data

The decompiler requires `nwscript.nss` (KOTOR's standard script library) at runtime for full decompilation. Path is configurable in Settings. Without it, decompilation will fail with a `DecompilerException`.

### Gotchas

- The CLI `--help` flag only triggers when at least one file path is also provided as an argument (otherwise the app defaults to GUI mode).
- GUI mode requires X11/Wayland display. In headless environments it exits gracefully with a warning.
