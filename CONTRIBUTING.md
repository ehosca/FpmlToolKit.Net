# Contributing

Thanks for helping. Bug reports, fixes, documentation and new FpML versions are all welcome. For a security problem,
follow [SECURITY.md](SECURITY.md) instead of opening an issue.

## Issues

When reporting a bug, include the package and version, the target framework of your app, and a minimal FpML document
or code that shows the problem. Check first whether it is in the schema itself (FpML), the generated code
(LinqToXsdCore) or this repository's build; problems in the first two are usually best reported upstream, but a report
here is fine if you are not sure.

## Setting up

You need the .NET SDK version in [`global.json`](global.json) (any later feature band of the same major version works).

    dotnet tool restore
    dotnet build FpmlToolKit.slnx
    dotnet test FpmlToolKit.slnx

The first build generates C# from every schema set and takes several minutes (longer on small machines); later builds
regenerate only projects whose schemas or generation settings changed. On Windows the tests also run on .NET Framework
4.8. See [Building](README.md#building) in the README for how generation works.

## Making changes

- **Generated code is never committed or edited.** It lives in each project's `obj/` folder. To change it, change the
  generation (`build/LinqToXsd.targets`, `build/FpmlSchema.cs.template`, a project's `xsd/namespaceconfig.xml`) or fix
  the generator upstream in [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore).
- **Schemas are not edited.** Don't change files under `fpml-*/xsd/` other than `namespaceconfig.xml`; a schema that
  needs patching to compile is left out instead (as FpML 5.8 transparency is). New versions use FpML's published files
  unchanged. The 4.0-4.9 and 5.0-5.3 schemas are combined single-file versions from 2012, kept as they are so their
  generated API doesn't change; see [Building](README.md#building) for how they differ from FpML's current downloads.
- **New FpML versions** are added with `scripts/add-schema-version.cs`; see the README.
- **Generator and runtime versions move together.** The LinqToXsdCore tool version in `dotnet-tools.json` must equal
  `XObjectsCoreVersion` in `Directory.Build.props`.
- **Add or update tests** in `tests/FpmlToolKit.Tests` for behaviour you change, and keep the build free of warnings.
- **Keep the netstandard2.0 build working.** Library code (including `build/FpmlSchema.cs.template`) must compile for
  `netstandard2.0` and run on .NET Framework; test code must compile for `net48`.

## Pull requests

- Open pull requests against `main`. Direct pushes to `main` are blocked.
- CI must pass: the `build` job (Linux: build, test on `net10.0`, pack) and the `windows` job (test on .NET Framework
  4.8). A change that touches schemas, `dotnet-tools.json` or `build/LinqToXsd.targets` regenerates the affected
  projects in CI, which can take up to about 40 minutes.
- Keep a pull request to one change, and describe what it changes and why.
- Add a line under **Unreleased** in [CHANGELOG.md](CHANGELOG.md) for changes users will notice.
- Pull requests are merged with a merge commit.

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE) of this project.
