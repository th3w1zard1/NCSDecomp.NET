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
- **Run CLI**: `dotnet run -- --help dummy.ncs` (needs a file arg to enter CLI mode)
- **Run CLI decompile**: `dotnet run -- file.ncs --output-dir ./output`
- **Run GUI**: `dotnet run` (requires X11 display server)
- **Lint**: Build warnings serve as lint; no separate linter configured.

### No automated tests

No test projects or frameworks. Validation is manual via NCS file decompilation.

### Runtime data

The decompiler needs `nwscript.nss` / `k1_nwscript.nss` at runtime for function signature resolution. These must be in the working directory or the build output directory. Copies from Andastra's `tools/` directory work. Without them, `DecompilerException` is thrown at decompile time.

### Gotchas

- The CLI `--help` flag only works when at least one file path arg is also provided (otherwise the app enters GUI mode).
- GUI mode requires X11/Wayland. In headless environments it exits gracefully.
- The `vendor/BioWare.NET/` directory is excluded from the main project's `<Compile>` scope to avoid assembly info conflicts. The `<Compile Remove="vendor\**" />` in `KNCSDecomp.csproj` handles this.
- `nwnnsscomp.exe` is optional; without it the decompiler skips bytecode round-trip verification but still produces decompiled NSS output.
