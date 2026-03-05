# BioWare.NET

A .NET library for reading, writing, and manipulating BioWare game file formats from Knights of the Old Republic (KotOR), The Sith Lords (TSL), and related Aurora/Odyssey engine games.

## Features

- **File Format Support**: GFF, TLK, 2DA, NCS, ERF, RIM, TPC, MDL, LYT, VIS, SSF, LIP, BWM, KEY, BIF, WAV, and more
- **NCS Decompiler**: Decompile compiled NWScript (`.ncs`) bytecode back to readable `.nss` source
- **NCS Compiler**: Compile NWScript source (`.nss`) to bytecode (`.ncs`)
- **TSLPatcher Engine**: Apply and create mod patches using the TSLPatcher format
- **Resource Management**: Chitin/KEY+BIF archive extraction and capsule (ERF/RIM/MOD) management
- **Cross-Platform**: Targets .NET 8.0

## Installation

### NuGet Package

```bash
dotnet add package BioWare.NET
```

### Project Reference (submodule)

```bash
git submodule add https://github.com/th3w1zard1/BioWare.NET.git vendor/BioWare.NET
```

Then reference in your `.csproj`:

```xml
<ProjectReference Include="vendor/BioWare.NET/BioWare.csproj" />
```

## Namespace Structure

| Namespace | Description |
|---|---|
| `BioWare.Common` | Shared types: `BioWareGame`, `ResourceType`, `Language`, `LocalizedString` |
| `BioWare.Resource.Formats.*` | File format readers/writers (GFF, TLK, 2DA, NCS, TPC, etc.) |
| `BioWare.Resource.Formats.NCS.Decomp` | NCS bytecode decompiler |
| `BioWare.Resource.Formats.NCS.Compiler` | NCS/NSS script compiler |
| `BioWare.Extract` | Chitin/KEY+BIF archive, Capsule (ERF/RIM), Installation management |
| `BioWare.Merge` | Module merging utilities |
| `BioWare.TSLPatcher` | TSLPatcher mod installation engine |
| `BioWare.Tools` | High-level tool functions for modding workflows |
| `BioWare.Utility` | System utilities, LZMA compression, geometry helpers |

## Quick Start

### Decompile an NCS file

```csharp
using BioWare.Resource.Formats.NCS.Decomp;

var decompiler = new FileDecompiler();
var ncsFile = new File("script.ncs");
int result = decompiler.Decompile(ncsFile);
string source = decompiler.GetGeneratedCode(ncsFile);
```

### Read a GFF file

```csharp
using BioWare.Resource.Formats.GFF;

byte[] data = System.IO.File.ReadAllBytes("template.utc");
var gff = GFFBinaryReader.Read(data);
string tag = gff.Root["Tag"].StringValue;
```

### Read a 2DA table

```csharp
using BioWare.Resource.Formats.TwoDA;

byte[] data = System.IO.File.ReadAllBytes("appearance.2da");
var table = TwoDABinaryReader.Read(data);
string modelName = table.GetCell(0, "modelb");
```

## Building

```bash
dotnet build
```

## License

Business Source License 1.1 (BSL 1.1). See [LICENSE](LICENSE) for details.

## Origin

Extracted from the [Andastra](https://github.com/OldRepublicDevs/Andastra) project by OldRepublicDevs.
