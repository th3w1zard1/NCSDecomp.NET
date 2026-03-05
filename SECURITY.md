# Security Policy

## Reporting Vulnerabilities

Please report security vulnerabilities by opening a GitHub issue tagged `security` or by contacting the maintainers directly.

---

## Known Historical Disclosures

### HIST-001 — Private Repository Name in Git History

**Severity:** Medium  
**Status:** Partially mitigated (current files clean; git history retains the disclosure)  
**Introduced:** Commit `55b54b0` (PR #1, "Development environment setup")  
**Mitigated in current files:** Commit `bae779e` (same PR)

**Description:**  
The first version of `AGENTS.md` (commit `55b54b0`) contained a sentence that explicitly named a
private GitHub repository:

> "This library is only available in the **private** `OldRepublicDevs/HoloPatcher.NET` repository
> at `src/CSharpKOTOR/`."

The dependency was replaced with a vendored library in the next commit (`bae779e`), removing the
reference from the current working tree. However, the original text is permanently accessible in
the public repository's git history via `git show 55b54b0:AGENTS.md`.

**Impact:**  
Anyone with read access to this repository can enumerate the name and internal path structure
(`src/CSharpKOTOR/`) of the private `OldRepublicDevs/HoloPatcher.NET` repository.

**Recommended Remediation:**  
To fully remove this disclosure from history, perform a history rewrite using `git filter-repo`:

```bash
# Install git-filter-repo if not already present
pip install git-filter-repo

# Rewrite history to remove the private repo reference from AGENTS.md in commit 55b54b0
# (This rewrites all subsequent commit SHAs — coordinate with all contributors first)
git filter-repo --path AGENTS.md --blob-callback '
    data = data.decode("utf-8", errors="replace")
    data = data.replace(
        "OldRepublicDevs/HoloPatcher.NET",
        "[REDACTED-PRIVATE-REPO]"
    )
    return data.encode("utf-8")
'

# Force-push all affected branches and tags after the rewrite
git push --force --all
git push --force --tags
```

> **Warning:** Rewriting published history is destructive. All collaborators must re-clone or
> rebase their local copies after the force-push. GitHub's "View file at commit" links for the
> original commit SHA will no longer resolve.

**Prevention:**  
- Never reference private repository names, URLs, or internal paths in files committed to public
  repositories, even temporarily.
- Review `AGENTS.md`, `README.md`, and any CI/CD configuration files before committing to ensure
  they contain no references to private infrastructure.

---

### HIST-002 — Developer Personal Filesystem Paths in Config File

**Severity:** High  
**Status:** Mitigated — `KNCSDecomp.conf` removed from working tree and added to `.gitignore`  
**Introduced:** Commit `cd1a449` (initial commit)  
**Mitigated:** This PR (`fix/security-private-repo-disclosure` companion: `fix/security-remove-dev-config`)

**Description:**  
`KNCSDecomp.conf`, committed in the initial commit, contained personal Windows filesystem paths
from a developer's machine (e.g., `G:\GitHub\HoloPatcher.NET\src\KNCSDecomp`). See PR
`fix/security-remove-dev-config` for the working-tree fix.

The paths remain permanently in git history at commit `cd1a449`. The same `git filter-repo`
approach described above can be used to rewrite them out of history if needed.
