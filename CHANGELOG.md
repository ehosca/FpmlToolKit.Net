# Changelog

Notable changes to FpmlToolKit.Net, newest first. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and versions follow [Semantic Versioning](https://semver.org/). Packages for each release are attached to its
[GitHub release](https://github.com/ehosca/FpmlToolKit.Net/releases).

## [Unreleased]

### Added

- Every view assembly embeds the FpML schemas it was generated from and has a `FpmlSchema` class in its namespace:
  `Validate` and `IsValid` for documents and typed elements, `CreateSchemaSet` for a compiled `XmlSchemaSet`, and
  `SchemaFileNames` and `OpenSchema` for the raw files. Includes and imports resolve from the embedded files only.
  ([#6](https://github.com/ehosca/FpmlToolKit.Net/pull/6))
- README usage documentation: reading, building, validating, and using several views together.
- The test suite also runs on .NET Framework 4.8 against the `netstandard2.0` assemblies, in a Windows CI job.
  ([#7](https://github.com/ehosca/FpmlToolKit.Net/pull/7))
- `SECURITY.md`, `CONTRIBUTING.md`, this changelog, and Dependabot updates for NuGet packages, the .NET SDK and
  GitHub Actions.

- FpML 4.10 (`FpmlToolKit.Fpml410`) and FpML 5.14 (`FpmlToolKit.Fpml514`, all six views).
- `official-files.txt` records the FpML download, entry and SHA-256 of every schema and example, and the tests fail
  if any of them changes. `scripts/official-files.cs` regenerates it and checks it against the downloads on fpml.org.

### Changed

- **Breaking:** every FpML version now uses FpML's own published files, unchanged, from its latest Recommendation
  build, along with the official examples from that build. 4.0-4.5 and 5.0-5.2 previously used single-file schemas
  made in 2012 with Eclipse EMF tooling. 4.6-4.9 used FpML's merged single-file schema and now use the multi-file
  download, which generates the same types. 5.3 moves from build 6 to build 9, and 5.11 from build 7 to build 9.
  Changes to the generated API:
  - 4.2 and 4.3 gain the types FpML added for the 2021 ISDA definitions and benchmark fallbacks (`BenchmarkRate`,
    `FallbackRate`, `CalculationParameters`, `MidMarketValuation`, ...). `Stub.floatingRate` is now
    `IList<StubFloatingRate>`, and in 4.3 `StubValue` is gone: `StubCalculationPeriodAmount.initialStub` and
    `finalStub` are `Stub`, and `ValuationScenarioReference.href` is removed.
  - 5.3 confirmation no longer has `EmbeddedOptionType` or the `embeddedOptionType` property on products. 5.3
    transparency gains `buyerPartyReference` and `sellerPartyReference` on options, and `FxCashSettlement.fixing` is
    removed.
  - 5.11: `EarlyTerminationProvision`'s `mandatoryEarlyTermination`, `optionalEarlyTermination` and
    `optionalEarlyTerminationParameters`, and `Knock.knockOut`, are now lists. `regulation` on `InapplicableRegulation`
    and `RegulatorApplicability` is `IList<RegulationName>`, and `RegulationName.reportingRegimeNameScheme` is
    `regulationNameScheme`. `AccrualOptionChangeEvent` is `AccrualOptionChange`. `RateObservation.observationWeight`
    is optional. New: `TermPointReference` and `WithdrawalPartyTradeInformation.category`.
  - 4.0, 4.1, 4.4, 4.5, 5.0-5.2, 5.3 recordkeeping and 5.3 reporting: same types and properties.
- Packages no longer contain a top-level `xsd/` folder, which NuGet never copied into consuming projects; the schemas
  are embedded in the assemblies instead. ([#6](https://github.com/ehosca/FpmlToolKit.Net/pull/6))
- Packages carry `LICENSE.txt` (MIT for the toolkit, with the FpML and W3C schemas under their own terms) instead of an
  MIT license expression, plus a generated `README.md` per FpML version.
  ([#5](https://github.com/ehosca/FpmlToolKit.Net/pull/5))

## [2.0.0-beta.3] - 2026-10-07

### Changed

- LinqToXsdCore and XObjectsCore upgraded to 3.4.24. The generator now produces FpML 4.1–4.9 `creditCurve` correctly
  itself ([mamift/LinqToXsdCore#106](https://github.com/mamift/LinqToXsdCore/issues/106)), so the post-generation
  workaround is removed. ([#4](https://github.com/ehosca/FpmlToolKit.Net/pull/4))

## [2.0.0-beta.2] - 2026-10-02

### Changed

- **Breaking:** the W3C XML Signature types moved from the shared `org.w3.xmldsig` namespace to one under each view's
  namespace, such as `fpml_5_13_confirmation.xmldsig` or `fpml_4_9.xmldsig`. Referencing more than one view assembly
  (which any package does) no longer makes their names ambiguous (CS0433). XML is unchanged; code that names
  `org.w3.xmldsig.*` types needs the new namespace. ([#3](https://github.com/ehosca/FpmlToolKit.Net/pull/3))

### Added

- Release workflow: a version tag on `main` builds, tests and packs, and creates a GitHub release with the packages.
  ([#2](https://github.com/ehosca/FpmlToolKit.Net/pull/2))

## [2.0.0-beta.1] - 2026-10-02

The 2012 toolkit modernised. ([#1](https://github.com/ehosca/FpmlToolKit.Net/pull/1))

### Added

- FpML 5.4 to 5.13, with every view in FpML's downloads (legal from 5.7, pretrade from 5.5) except 5.8 transparency,
  whose published schema does not compile.
- NuGet packages, one per FpML version (`FpmlToolKit.Fpml<major><minor>`), each with every view assembly of that
  version.
- Tests: every schema compiles and every assembly loads; 275 official FpML examples validate, load through the typed
  API and round-trip unchanged.
- GitHub Actions CI and `THIRD-PARTY-NOTICES.md` for the FpML and W3C schemas.

### Changed

- **Breaking:** targets `netstandard2.0` and `net10.0`. .NET Framework 4.0 is no longer supported; use .NET Framework
  4.6.2 or later, or .NET (Core).
- Code is generated by [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore) at build time instead of being produced
  by the CodePlex LinqToXsd binaries. Namespaces and the API shape are unchanged, but expect small differences.
- SDK-style projects, an `.slnx` solution and a pinned .NET SDK replace the 2012 solution.

## 1.x - 2012

FpML 4.0 to 4.9 and 5.0 to 5.3 for .NET Framework 4.0, generated with CodePlex LinqToXsd. Not published as packages.

[Unreleased]: https://github.com/ehosca/FpmlToolKit.Net/compare/v2.0.0-beta.3...HEAD
[2.0.0-beta.3]: https://github.com/ehosca/FpmlToolKit.Net/compare/v2.0.0-beta.2...v2.0.0-beta.3
[2.0.0-beta.2]: https://github.com/ehosca/FpmlToolKit.Net/compare/v2.0.0-beta.1...v2.0.0-beta.2
[2.0.0-beta.1]: https://github.com/ehosca/FpmlToolKit.Net/releases/tag/v2.0.0-beta.1
