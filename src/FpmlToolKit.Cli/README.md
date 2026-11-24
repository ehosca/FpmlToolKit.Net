# FpmlToolKit.Cli

The `fpml` command validates FpML documents of any version from 4.0 to 5.14. It finds the version and view in the
document itself and checks it against that schema and against FpML's business validation rules.

    dotnet tool install -g FpmlToolKit.Cli

    fpml validate trade.xml
    fpml validate inbox/ --format json
    fpml detect message.xml
    fpml rules

`validate` prints one line per problem, in the `file(line): error id: message` form that editors and CI logs link to:

    trade.xml(57): error ref-8: CalculationPeriodDatesReference/@href must be the @id of an element of type CalculationPeriodDates. [/dataDocument/trade/swap/swapStream[1]/paymentDates/calculationPeriodDatesReference]
    trade.xml: FpML 5.13 confirmation: 1 error

Exit codes: `0` when every document is valid, `1` when some have errors, `2` when a file couldn't be read or isn't
FpML. Folders are searched recursively for `*.xml`.

The business rules are FpML's shared (`shared-*`) and ID/IDREF (`ref-*`) rules, from the
[FpmlToolKit.Validation](https://www.nuget.org/packages/FpmlToolKit.Validation) package. They run on FpML 5.x
documents in the confirmation, reporting, recordkeeping and transparency views. Other documents get schema validation
only. The tool ships FpML's schemas, which ISDA publishes under the FpML Public License. See THIRD-PARTY-NOTICES.md.
