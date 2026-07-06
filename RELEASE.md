## Release process

`SingleStoreConnector` releases are automated through GitHub Actions. A new NuGet package is built and published automatically when a new version tag is pushed to the GitHub repository.

A draft GitHub Release is also created automatically. The release remains a draft because the release notes must be reviewed and completed manually before publishing.

### Prerequisites

Before creating a release tag:

* Make sure the release changes are merged into the default branch.
* Make sure CI is passing.
* Update `CONNECTOR_VERSION` in `.github/workflows/config.yml` with the new package version.
* Update the connector version in the `README.md` title.
* Add a section for the new version to `docs/VersionHistory.md` describing the changes in this release.
* Make sure the version tag matches the value of `CONNECTOR_VERSION`.
* The version tag must use the `vX.Y.Z` format.

For example, if `CONNECTOR_VERSION` is `1.4.0`, the release tag should be `v1.4.0`.

The version section in `docs/VersionHistory.md` must use a `### X.Y.Z` header that matches `CONNECTOR_VERSION` (for example `### 1.4.0`), because the release workflow extracts that section and uses it to seed the draft GitHub Release notes.

### Creating a release

From the default branch, run:

```bash
git checkout master
git pull origin master
git tag vX.Y.Z
git push origin vX.Y.Z
```

Replace `X.Y.Z` with the version being released.

After the tag is pushed, GitHub Actions will automatically:

1. Run the test workflows.
2. Build the connector.
3. Pack the NuGet package.
4. Publish the `SingleStoreConnector` `.nupkg` package to NuGet.
5. Create a draft GitHub Release for the pushed tag, seeding the release notes from the matching section of `docs/VersionHistory.md` and attaching all build artifacts.

### Verifying the NuGet release

After the release workflow finishes successfully:

1. Check that the GitHub Actions workflow completed without errors.
2. Verify that the new package version is available on [NuGet](https://www.nuget.org/packages/SingleStoreConnector).
3. Optionally install the released package locally:

```bash
dotnet add package SingleStoreConnector --version X.Y.Z
```

### Publishing the GitHub Release

After the workflow creates the draft GitHub Release:

1. Open the repository's Releases page.
2. Open the draft release for the pushed tag, for example `vX.Y.Z`.
3. Review and complete the release notes. They are pre-filled from the matching `docs/VersionHistory.md` section, so verify they are accurate and complete.
4. Publish the GitHub Release.

The GitHub Release is intentionally kept as a draft because it requires complete release notes before publishing.

### Failed releases

If the release workflow fails before publishing to NuGet, fix the issue and rerun the workflow or recreate the tag as needed.

If the package was already published to NuGet, do not reuse the same version number. NuGet package versions are immutable, so a fix must be released with a new version.
