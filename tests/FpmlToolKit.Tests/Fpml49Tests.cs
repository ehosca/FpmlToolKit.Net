using System.Xml.Linq;
using fpml_4_9;

namespace FpmlToolKit.Tests;

public class Fpml49Tests
{
    private static readonly string SamplePath =
        Path.Combine(AppContext.BaseDirectory, "Samples", "fpml-4-9", "credit-curve.xml");

    [Fact]
    public void Sample_is_valid_against_the_schema()
    {
        var schemas = SchemaValidator.Load(Path.Combine(AppContext.BaseDirectory, "Schemas", "fpml-4-9", "xsd", "fpml-main-4-9.xsd"));

        Assert.Empty(SchemaValidator.Validate(XDocument.Load(SamplePath), schemas));
    }

    // creditCurve.name and creditCurve.currency were generated twice before LinqToXsdCore 3.4.24 (mamift/LinqToXsdCore#106).
    [Fact]
    public void CreditCurve_exposes_both_currency_occurrences()
    {
        var curve = creditCurve.Load(SamplePath);

        Assert.Equal("ACME Corp senior CDS curve", curve.name);
        Assert.Equal(["USD", "EUR"], curve.currency.Select(c => c.TypedValue));
        Assert.Equal("ACME Corp", curve.referenceEntity.entityName.TypedValue);
    }

    [Fact]
    public void CreditCurve_round_trips()
    {
        var reloaded = creditCurve.Parse(creditCurve.Load(SamplePath).ToString());

        Assert.Equal("ACME Corp senior CDS curve", reloaded.name);
        Assert.Equal(["USD", "EUR"], reloaded.currency.Select(c => c.TypedValue));
    }
}
