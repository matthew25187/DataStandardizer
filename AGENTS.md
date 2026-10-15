# Agent instructions

## Branching: Gitflow

This repository follows Gitflow. The long-lived branches are:

- `master`: released code only. Every commit on `master` is either a release merge from `develop` or a commit already on `develop`.
- `develop`: the integration branch and the default branch. CI builds of `develop` publish preview packages.

`release/*` and `hotfix/*` branches are not used. Every package release is an immutable NuGet version, so a defect in a release is handled by deprecating that version on NuGet and shipping the fix through `develop` → `master` like any other change.

### Day-to-day work

- Branch from `develop` as `feature/<issue>-<short-name>` (or `docs/...`).
- Open the PR into `develop` and complete it with **Squash and merge**. Never target `master` with a feature PR.
- Never commit or push directly to `master` or `develop`, and never open a PR into `master` from any branch other than `develop`.
- Changes to either pipeline (`build/datastandardizer-ci-build-pipeline.yml` or `build/datastandardizer-release-build-pipeline.yml`) or to the shared templates and scripts in `build/` go through `develop` like any other change. They reach `master` with the next release.

### Releasing

1. Open a PR from `develop` into `master`.
2. Complete it with **Create a merge commit**. Never squash or rebase a release PR. The release pipeline tags the merge commit (`v<major>.<minor>.<patch>-<package>`), and change detection depends on that commit's ancestry.
3. Don't put `[skip ci]` in the release PR title or merge commit. The merge to `master` is what triggers the release build.
4. Wait for the release pipeline to succeed. If it fails, fix the cause on `develop` and release again. Don't patch `master`.
5. Back-merge: open a PR from `master` into `develop` and complete it with **Create a merge commit**, never squash. Do this after every successful release, before any other PR merges into `develop`. Don't resolve a conflict by deleting a file that exists on `master`.

Publishing a released package to nuget.org is a separate, manual step that the maintainer does per package. A release build may produce packages that are never published, and patch versions are set up by hand before a release build. Agents don't do either.

### Why the merge method matters

Packages are versioned from Azure DevOps variable groups, not from files in the repo. "Has this package changed?" is decided by diffing against the latest reachable package tag (`build/scripts/FindChangedPackages.ps1`). A squash or rebase moves the release onto a commit that has no tag, and a skipped back-merge means `develop` doesn't contain the release. Either one makes the next build publish the wrong set of packages.

## Repository rules

Branch rules for `develop` and `master` are kept as GitHub rulesets in `.github/rulesets/` and applied with `.github/rulesets/UpdateRepositoryRulesets.ps1`. Change the JSON there rather than in the GitHub UI, so the repo stays the source of truth. Both branches require a PR and a passing `DataStandardizer-CI` check.
