# Security policy

## Supported versions

Security fixes go into the latest release of each *Data Standardizer* package, the version shown on [nuget.org](https://www.nuget.org/packages?q=DataStandardizer) and in the repository's [releases](https://github.com/matthew25187/DataStandardizer/releases).

Released versions are never changed or republished. When a release has a vulnerability, the fix ships in a new release of that package, and the affected versions are deprecated on nuget.org with a pointer to the fixed version. Earlier major versions don't get backported fixes; upgrade to the latest release to get a fix.

Preview packages built from the `develop` branch aren't supported.

## Verifying a package

Each package built by this repository has a signed [build provenance attestation](https://docs.github.com/en/actions/concepts/security/artifact-attestations) that links the `.nupkg` to the workflow run and commit that built it. The attestation is also attached to the package's GitHub Release, as `<package file>.sigstore.json`. Versions built before attestations were introduced don't have one.

To verify a package, download it from its [release](https://github.com/matthew25187/DataStandardizer/releases) and run this with the [GitHub CLI](https://cli.github.com/):

```shell
gh attestation verify DataStandardizer.Core.2.0.0.nupkg \
  --repo matthew25187/DataStandardizer \
  --signer-workflow matthew25187/DataStandardizer/.github/workflows/build-and-publish.yml \
  --source-ref refs/heads/master
```

Add `--bundle DataStandardizer.Core.2.0.0.nupkg.sigstore.json` to verify against the attached file instead of fetching the attestation from GitHub. For a preview version, use `--source-ref refs/heads/develop`.

A package downloaded from nuget.org doesn't verify, because nuget.org adds its repository signature to the package (`.signature.p7s`), which changes the file's hash. Its other contents are the same as the release's.

## Reporting a vulnerability

Report vulnerabilities privately through GitHub's [private vulnerability reporting](https://github.com/matthew25187/DataStandardizer/security/advisories/new) (the **Report a vulnerability** button on the repository's **Security** tab).

**Never report a vulnerability in a public issue, pull request, discussion or comment.**

Include as much of the following as you can:

- the package and version affected, and the target framework you're using
- what an attacker can do, and under what conditions
- steps or input that reproduce it, ideally a minimal code sample

## What to expect

*Data Standardizer* is maintained by one person in their personal time, so these are aims rather than guarantees:

- You'll get an acknowledgement on the advisory, normally within a week.
- The report is assessed and you'll be told whether it's accepted as a vulnerability. If it isn't, you'll be told why.
- For an accepted report, the fix is developed in the private advisory, and you'll be kept up to date there. You're welcome to review the fix.
- Once a fixed release is published, the advisory is published with a CVE where one applies, and the affected versions are deprecated on nuget.org. You'll be credited in the advisory unless you ask not to be.

Please keep the details private until the advisory is published.
