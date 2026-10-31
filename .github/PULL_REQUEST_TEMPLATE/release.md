<!--
Release PR: develop into master. See "Releasing" in AGENTS.md.
This description is public. Record only that each step was done; never describe a security finding here.
-->

## Release checklist

### Before opening this PR
- [ ] Release notes checked for each package being released, with overrides added to `build/release-notes-overrides.json` where a note would mislead
- [ ] [Security-audit skill](https://github.com/cloudflare/security-audit-skill) run locally on the packages being released; confirmed findings fixed, or recorded in a private security advisory
- [ ] Open security alerts triaged

### Merging
- [ ] Complete with **Create a merge commit** (never squash or rebase)
- [ ] No `[skip ci]` in the title or the merge commit

### After merging
- [ ] Release workflow succeeded
- [ ] Back-merge PR from `master` into `develop` opened and completed with **Create a merge commit**
