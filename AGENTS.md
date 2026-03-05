# AGENTS.md

## Cursor Cloud specific instructions

### Project Overview

KNCSDecomp.NET is a C#/.NET 8.0 Avalonia UI desktop application that decompiles NCS (NWScript Compiled Script) bytecode files from KOTOR into readable NSS source. It has both GUI (default) and CLI modes.

### Critical Dependency: CSharpKOTOR

This project references a sibling project at `../CSharpKOTOR/CSharpKOTOR.csproj` which provides the core decompilation engine under namespace `CSharpKOTOR.Formats.NCS.KNCSDecomp`. This library is only available in the **private** `OldRepublicDevs/HoloPatcher.NET` repository at `src/CSharpKOTOR/`. Without cloning HoloPatcher.NET as a sibling, the project will not build.

To set up a full build environment, clone HoloPatcher.NET so that `CSharpKOTOR` is at `../CSharpKOTOR` relative to this repo's root:

```
parent_dir/
  CSharpKOTOR/          # from HoloPatcher.NET/src/CSharpKOTOR
  NCSDecomp.NET/        # this repo (workspace)
```

### .NET SDK

Requires .NET 8.0 SDK. Installed at `$HOME/.dotnet` via the dotnet-install script. Ensure `DOTNET_ROOT=$HOME/.dotnet` and `$HOME/.dotnet` is on PATH.

### Build & Run

- **Restore**: `dotnet restore` (succeeds for NuGet packages; warns about missing CSharpKOTOR project)
- **Build**: `dotnet build` (requires CSharpKOTOR sibling project)
- **Run GUI**: `dotnet run` (requires display server / X11; falls back gracefully)
- **Run CLI**: `dotnet run -- --help` or `dotnet run -- file1.ncs file2.ncs`
- **Lint**: No separate linter configured; build warnings serve as lint.

### No automated tests

This project has no test projects or test frameworks. Validation is done manually by decompiling `.ncs` files.

### Runtime data

The decompiler requires `nwscript.nss` (KOTOR's standard script library) at runtime. Path is configurable in Settings. Without it, decompilation will fail with a `DecompilerException`.
