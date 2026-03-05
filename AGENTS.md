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

### Tests & Format Check

- **Tests**: `dotnet test tests/KNCSDecomp.RoundTripTests/KNCSDecomp.RoundTripTests.csproj`
- **Format check**: `dotnet format KNCSDecomp.csproj --verify-no-changes` (matches CI)

### Round-trip test flow

The single xUnit test `RoundTrip_K1_SimpleScript_DecompilesAndRecompiles` proves the decompiler produces recompilable output:

```
NSS source string
  → NCSAuto.CompileNss(source, K1)        // NssLexer → NssParser → NCS object
  → NCSAuto.BytesNcs(ncs)                 // NCSBinaryWriter → byte[]
  → write to temp .ncs file
  → FileDecompiler.DecompileToString(file) // NCSBinaryReader → AST → GenerateCode → NSS string
  → NCSAuto.CompileNss(decompiled, K1)    // recompile decompiled output
  → NCSAuto.BytesNcs(recompiled)          // serialize again
  → assert recompiled bytes are non-empty (default)
     or assert byte-for-byte parity        (KNCSDECOMP_STRICT_ROUNDTRIP=1)
```

Key classes (all in `vendor/BioWare.NET/`):
- `NCSAuto` (`Resource/Formats/NCS/NCSAuto.cs`) — compile/decompile/serialize entry points
- `FileDecompiler` (`Resource/Formats/NCS/Decomp/FileDecompiler.cs`) — NCS→NSS decompilation engine
- `NcsFile` (`Resource/Formats/NCS/Decomp/JavaStubs.cs`) — Java `File` shim wrapping `FileInfo`
- `BioWareGame` (`Common/BiowareGame.cs`) — enum selecting K1/TSL game variant
- `NCSBinaryReader/Writer` (`Resource/Formats/NCS/`) — NCS bytecode serialization

### Runtime data

The decompiler needs `nwscript.nss` / `k1_nwscript.nss` at runtime for function signature resolution. These must be in the working directory or the build output directory. Copies from Andastra's `tools/` directory work. Without them, `DecompilerException` is thrown at decompile time.

### Gotchas

- The CLI `--help` flag only works when at least one file path arg is also provided (otherwise the app enters GUI mode).
- GUI mode requires X11/Wayland. Cloud VMs have Xvfb running on `DISPLAY=:1` by default, so the GUI works out of the box.
- The `vendor/BioWare.NET/` directory is excluded from the main project's `<Compile>` scope to avoid assembly info conflicts. The `<Compile Remove="vendor\**" />` in `KNCSDecomp.csproj` handles this.
- `nwnnsscomp.exe` is optional; without it the decompiler skips bytecode round-trip verification but still produces decompiled NSS output.
- CI builds with `-p:TreatWarningsAsErrors=true` but all 126 warnings come from the vendored `BioWare.NET` library, not the main project. A plain `dotnet build` succeeds and the main project compiles warning-free.
