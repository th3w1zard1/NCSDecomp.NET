# KNCSDecomp.NET — Copilot Instructions

## Project

C#/.NET 8.0 Avalonia UI app that decompiles NCS (NWScript Compiled Script) bytecode from KOTOR into readable NSS source. GUI (default) and CLI modes.

## Build & Run

- `dotnet restore` / `dotnet build` / `dotnet run`
- CLI mode: `dotnet run -- file.ncs --output-dir ./output`
- GUI mode: `dotnet run` (needs X11 display)
- Format check: `dotnet format KNCSDecomp.csproj --verify-no-changes`

## Round-trip test flow

Run: `dotnet test tests/KNCSDecomp.RoundTripTests/KNCSDecomp.RoundTripTests.csproj`

```
NSS source string
  → NCSAuto.CompileNss(source, K1)         // NssLexer → NssParser → NCS object
  → NCSAuto.BytesNcs(ncs)                  // NCSBinaryWriter → byte[]
  → write to temp .ncs file
  → FileDecompiler.DecompileToString(file)  // NCSBinaryReader → AST → GenerateCode → NSS string
  → NCSAuto.CompileNss(decompiled, K1)     // recompile the decompiled output
  → NCSAuto.BytesNcs(recompiled)           // serialize again
  → assert non-empty (default) or byte parity (KNCSDECOMP_STRICT_ROUNDTRIP=1)
```

Key classes (all in `vendor/BioWare.NET/`):
- `NCSAuto` — compile/decompile/serialize entry points (`Resource/Formats/NCS/NCSAuto.cs`)
- `FileDecompiler` — NCS→NSS decompilation engine (`Resource/Formats/NCS/Decomp/FileDecompiler.cs`)
- `NcsFile` — Java `File` shim wrapping `FileInfo` (`Resource/Formats/NCS/Decomp/JavaStubs.cs`)
- `BioWareGame` — K1/TSL game enum (`Common/BiowareGame.cs`)
- `NCSBinaryReader/Writer` — NCS bytecode serialization (`Resource/Formats/NCS/`)

## Conventions

- Vendored `BioWare.NET` library at `vendor/BioWare.NET/` is excluded from main project compile scope.
- Runtime data (`tools/*.nss`) is auto-copied to build output by the csproj.
- CI runs `dotnet build -p:TreatWarningsAsErrors=true`; all warnings are from the vendor lib, not the main project.
