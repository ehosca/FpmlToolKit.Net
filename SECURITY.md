# Security policy

## Supported versions

| Version | Supported |
| ------- | --------- |
| 2.x (latest release or prerelease) | Yes |
| 1.x (2012, .NET Framework 4.0) | No |

Fixes are made on `main` and ship in the next 2.x release. Only the latest 2.x release receives fixes.

## Reporting a vulnerability

Please report vulnerabilities privately through GitHub:
[**Report a vulnerability**](https://github.com/ehosca/FpmlToolKit.Net/security/advisories/new) (the Security tab of
this repository). Do not open a public issue, discussion or pull request for a vulnerability.

Include what you can of:

- the affected package(s) and version(s);
- a description of the problem and its impact;
- steps or a minimal document or program that reproduces it.

You can expect an acknowledgement within a week. Once the problem is confirmed, a fix is prepared in a private
advisory, released, and the advisory is published with credit to you unless you prefer otherwise.

## Scope

In scope: the code in this repository and the packages built from it, including the build-time code generation
(`build/`), the generated `FpmlSchema` validation helper, and the release workflows.

Out of scope, but please report to the upstream project:

- the FpML schemas themselves: [FpML / ISDA](https://www.fpml.org/);
- the LinqToXsdCore code generator and the XObjectsCore runtime:
  [mamift/LinqToXsdCore](https://github.com/mamift/LinqToXsdCore/security);
- .NET and `System.Xml`: [Microsoft](https://github.com/dotnet/runtime/security/policy).

If you are not sure where a problem belongs, report it here and it will be passed on.
