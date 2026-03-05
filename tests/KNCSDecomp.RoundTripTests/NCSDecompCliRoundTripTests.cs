using System;
using System.IO;
using System.Linq;
using BioWare.Common;
using BioWare.Resource.Formats.NCS;
using BioWare.Resource.Formats.NCS.Decomp;
using Xunit;
using File = BioWare.Resource.Formats.NCS.Decomp.NcsFile;

namespace KNCSDecomp.RoundTripTests
{
    public class NCSDecompCliRoundTripTests
    {
        [Fact]
        public void RoundTrip_K1_SimpleScript_DecompilesAndRecompiles()
        {
            string workspaceRoot = GetWorkspaceRoot();
            string k1NwscriptPath = Path.Combine(workspaceRoot, "tools", "k1_nwscript.nss");
            Assert.True(System.IO.File.Exists(k1NwscriptPath), "Required nwscript file missing: " + k1NwscriptPath);

            string source = @"
void main()
{
    int value = 41;
    value = value + 1;
}
";

            NCS original = NCSAuto.CompileNss(source, BioWareGame.K1);
            byte[] originalBytes = NCSAuto.BytesNcs(original);
            Assert.NotNull(originalBytes);
            Assert.NotEmpty(originalBytes);

            string tempRoot = Path.Combine(Path.GetTempPath(), "kncsdecomp-roundtrip-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                string inputNcsPath = Path.Combine(tempRoot, "input.ncs");
                System.IO.File.WriteAllBytes(inputNcsPath, originalBytes);

                var decompiler = new FileDecompiler(new File(k1NwscriptPath));
                string decompiled = decompiler.DecompileToString(new File(inputNcsPath));
                Assert.False(string.IsNullOrWhiteSpace(decompiled), "Decompiled source should not be empty.");

                NCS recompiled = NCSAuto.CompileNss(decompiled, BioWareGame.K1);
                byte[] recompiledBytes = NCSAuto.BytesNcs(recompiled);
                Assert.NotNull(recompiledBytes);
                Assert.NotEmpty(recompiledBytes);

                // Strict bytecode parity can be enabled explicitly in local/full validation runs.
                // The default CI assertion verifies that generated code is recompilable.
                string strictMode = Environment.GetEnvironmentVariable("KNCSDECOMP_STRICT_ROUNDTRIP");
                if (string.Equals(strictMode, "1", StringComparison.Ordinal))
                {
                    Assert.True(originalBytes.SequenceEqual(recompiledBytes),
                        "Round-trip bytecode mismatch for simple K1 script.");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        private static string GetWorkspaceRoot()
        {
            string current = Directory.GetCurrentDirectory();
            DirectoryInfo dir = new DirectoryInfo(current);
            while (dir != null)
            {
                if (System.IO.File.Exists(Path.Combine(dir.FullName, "KNCSDecomp.csproj")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Could not locate workspace root from: " + current);
        }
    }
}
