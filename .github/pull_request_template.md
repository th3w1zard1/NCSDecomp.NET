## Description

<!-- Briefly describe what this PR does and why. -->

## Type of change

- [ ] Bug fix (non-breaking change that resolves an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing behavior to change)
- [ ] Refactor / code quality improvement
- [ ] Documentation update
- [ ] CI/CD / build system update

## Related issues

<!-- Link any related issues: Closes #123 -->

## Testing

<!-- Describe how you tested this change. -->

- [ ] Built locally with `dotnet build` — no errors or new warnings
- [ ] Ran the CLI smoke test: `dotnet run -- --help dummy.ncs`
- [ ] Tested decompilation with a real `.ncs` file (if applicable)
- [ ] Tested GUI mode (if applicable — requires X11/Wayland display)

## Checklist

- [ ] Code compiles cleanly (0 warnings, 0 errors)
- [ ] `dotnet format KNCSDecomp.csproj --verify-no-changes` passes
- [ ] No secrets, credentials, or sensitive data committed
- [ ] Vendor directory (`vendor/BioWare.NET/`) not modified (changes belong upstream)
