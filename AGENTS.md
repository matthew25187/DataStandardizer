# Agent instructions

## Branching: Gitflow

This repository follows Gitflow. The long-lived branches are:

- `master`: released code only. Every commit on `master` is either a release merge from `develop` or a commit already on `develop`.
- `develop`: the integration branch and the default branch. CI builds of `develop` publish preview packages.

`release/*` and `hotfix/*` branches are not used. Every package release is an immutable NuGet version, so a defect in a release is handled by deprecating that version on NuGet and shipping the fix through `develop` → `master` like any other change.

### Day-to-day work

- Branch from `develop` as `feature/<issue>-<short-name>` (or `docs/...`).
- Open the PR into `develop` and complete it with **Squash and merge**. Never target `master` with a feature PR.
- Pushes to a feature branch don't start a CI build. Only a PR does, and it builds the branch merged with its target. To get builds for work that isn't ready to merge, open the PR as a draft.
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

## Release notes

Each package's NuGet release notes are generated from the commits that changed its `src/<Package>` folder since its last release (`build/scripts/SetReleaseNotesArgument.ps1`). Each commit gives one line under one of these headings: Breaking changes, New, Fixed, Deprecated and removed, Changed. Write commits so those lines make sense to someone who uses the package.

- **The squash subject is the note.** A squash merge takes the PR title as its subject, or the commit's subject when the PR has one commit. Write both to describe what changed for a user of the package, not how it was implemented. Write "`IcalAlarm.TriggerDuration` no longer drops TRIGGER parameters", not "Replace the alarm TRIGGER in place".
- **Declare notes with `Release-note:` trailers in a commit message**, not in the PR description. The squash merge builds its body from the commit messages, so a trailer in the PR description never reaches git. Put the trailers on one commit only; a trailer repeated on several commits is published once per commit.
  - `Release-note: fixed` publishes the subject under that heading. Categories are `added` (or `new`), `fixed`, `changed`, `deprecated`, `removed`, `breaking` and `none`.
  - `Release-note: fixed: <text>` publishes the text instead of the subject. Add one trailer for each change a user would notice, such as a PR that fixes two bugs.
  - `Release-note: none` leaves out a commit that touches a package folder without affecting users, such as a build, test or documentation-comment change.
  - Declare a note on every PR that touches `src/`. Without one, the heading is guessed from the subject's first word (`Fix`/`Correct`, `Add`/`Support`/`Expose`, `Deprecate`/`Remove`/`Drop`), and anything else lands under Changed.
- **`BREAKING CHANGE: <text>`** is still published under Breaking changes, whatever else the commit declares.
- **Commits already on `develop`** can't be changed. Correct their notes in `build/release-notes-overrides.json`, keyed by the full commit id. An empty `notes` list leaves the commit out. Check the notes each package will get before a release, and add overrides where they would mislead.

## Repository rules

Branch rules for `develop` and `master` are kept as GitHub rulesets in `.github/rulesets/` and applied with `.github/rulesets/UpdateRepositoryRulesets.ps1`. Change the JSON there rather than in the GitHub UI, so the repo stays the source of truth. Both branches require a PR and a passing `DataStandardizer-CI` check.
