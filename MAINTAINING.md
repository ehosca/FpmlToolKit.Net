# Maintaining FpmlToolKit.Net

How to ship a release, bring in a new FpML version, and understand the build. Everything here is automated, so most
of it is a single command or a tag push.

## Releasing

A release is one tag on a commit that is already on `main`:

    git tag -a v2.0.0-beta.6 -m "FpmlToolKit.Net 2.0.0-beta.6"
    git push origin v2.0.0-beta.6

Before tagging, move the **Unreleased** entries in [CHANGELOG.md](CHANGELOG.md) under the new version, through a pull
request as usual.

The tag runs the Release workflow (`.github/workflows/release.yml`):

1. **`release` job:** builds and tests everything with that version, packs one `.nupkg` per FpML version and attaches
   them to a GitHub release for the tag. Versions with a suffix such as `-beta.6` are marked as prereleases. The
   release notes are generated from pull request titles; replacing them with the version's CHANGELOG section reads
   better.
2. **`nuget` job:** pushes the same packages to NuGet.org. It runs in the `nuget` environment, which requires approval
   from a reviewer, so you'll get a "Review deployments" prompt on the run. It uses `--skip-duplicate`, so re-running it
   after a partial push is safe. NuGet.org versions can't be deleted, only unlisted.

Running the workflow by hand from the Actions tab ("Run workflow") is a dry run: the same build, test and pack, with
the packages kept as a workflow artifact and no GitHub release or NuGet.org push.

### NuGet.org publishing setup

Publishing uses NuGet [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): each run
exchanges its GitHub OIDC token for a short-lived API key, so no key is stored anywhere. The setup, already in place:

- **nuget.org Trusted Publishing policy:** owner `ehosca`, repository `ehosca/FpmlToolKit.Net`, workflow file
  `release.yml`, environment `nuget`, scope "Push new packages and package versions", glob `FpmlToolKit.*`.
- **`nuget` environment** (Settings > Environments): the variable `NUGET_USER` (the nuget.org profile name that owns
  the policy) and a required reviewer.
- **Repository variable `NUGET_PUBLISH` = `true`** (Settings > Secrets and variables > Actions > Variables). It has to
  be a repository variable, not an environment one: the `nuget` job's `if:` is evaluated before the job enters the
  environment, when environment variables aren't loaded yet. Set it to anything else to turn publishing off.

If the "Log in to NuGet.org" step fails, check that the policy still matches the repository, workflow file name and
environment exactly.

## Adding or updating an FpML version

Every schema and example comes unchanged from FpML's "Schema and Examples" downloads, from the latest Recommendation
build of each version.

1. **Download.** On [fpml.org](https://www.fpml.org/the_standard/current/) (older versions are under Previous
   Versions), open the version's latest REC build and download the "Schema and Examples" zip of each view. The links
   only show when you're logged in, but the files themselves need no login. Keep the zips under their path on fpml.org,
   for example `zips/fpml-5-13-8-rec-2/xml/confirmation-5-13_xml.zip`, and unzip copies of them in a separate folder.
2. **Import.** Run `dotnet run scripts/add-schema-version.cs -- <unzipped-folder>`. It creates one project per
   `fpml-main-*.xsd` it finds (`fpml-<version>-<view>`, or `fpml-<version>` for 4.x), skipping schemas that don't
   compile, adds it to the solution, copies five schema-valid official examples per view into
   `tests/FpmlToolKit.Tests/Examples/`, and regenerates the per-version package projects in `packages/`. To move an
   existing version to a newer FpML build, add `--replace`.
3. **Record.** Run `dotnet run scripts/official-files.cs -- write <zips-folder>` to update `official-files.txt` with
   the source and SHA-256 of every new file.
4. **Check.** `dotnet run scripts/official-files.cs -- verify` downloads the listed zips from fpml.org and checks every
   file against them. Then build, test, and add a CHANGELOG entry noting any API changes.

## How code generation works

C# classes are generated from the XSDs at build time into each project's `obj/` folder (see `build/LinqToXsd.targets`,
imported through `Directory.Build.targets`) and are never committed, since each schema set produces about 10 MB of code.

- A project regenerates only when the content hash of its schemas, namespace config, `dotnet-tools.json` or
  `build/LinqToXsd.targets` changes.
- CI caches the generated code between runs, keyed on those same inputs, so a push regenerates only the projects whose
  schemas changed. The Windows job restores the Linux job's cache, which is why it keeps LF line endings.
- The LinqToXsdCore tool version (`dotnet-tools.json`) and the `XObjectsCore` runtime version (`XObjectsCoreVersion`
  in `Directory.Build.props`) must match.
- Each assembly embeds its schemas and gets a generated `FpmlSchema` class (`build/FpmlSchema.cs.template`) that
  validates against them with no file or network access.
