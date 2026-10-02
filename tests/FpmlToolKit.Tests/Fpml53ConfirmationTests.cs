using System.Xml.Linq;
using fpml_5_3_confirmation;

namespace FpmlToolKit.Tests;

public class Fpml53ConfirmationTests
{
    private static readonly string SamplePath =
        Path.Combine(AppContext.BaseDirectory, "Samples", "fpml-5-3-confirmation", "data-document-parties.xml");

    private static readonly Lazy<System.Xml.Schema.XmlSchemaSet> Schemas = new(() =>
        SchemaValidator.Load(Path.Combine(AppContext.BaseDirectory, "Schemas", "fpml-5-3-confirmation", "xsd", "fpml-main-5-3.xsd")));

    [Fact]
    public void Sample_is_valid_against_the_schema()
    {
        var errors = SchemaValidator.Validate(XDocument.Load(SamplePath), Schemas.Value);

        Assert.Empty(errors);
    }

    [Fact]
    public void Load_exposes_typed_values()
    {
        var doc = dataDocument.Load(SamplePath);

        Assert.Equal("5-3", doc.fpmlVersion);
        Assert.Equal(2, doc.party.Count);

        var bank = doc.party[0];
        Assert.Equal("party1", bank.id);
        Assert.Equal("549300EXAMPLEBANK001", bank.partyId[0].TypedValue);
        Assert.Equal(new Uri("http://www.fpml.org/coding-scheme/external/iso17442"), bank.partyId[0].partyIdScheme);
        Assert.Equal("Example Bank", bank.partyName.TypedValue);
    }

    [Fact]
    public void Document_built_in_code_round_trips_and_validates()
    {
        var built = new dataDocument
        {
            fpmlVersion = "5-3",
            party =
            [
                new Party { id = "p1", partyId = [new PartyId { TypedValue = "PARTY-ONE" }] },
            ],
        };

        var xml = built.ToString();
        var reloaded = dataDocument.Parse(xml);

        Assert.Equal("PARTY-ONE", reloaded.party.Single().partyId.Single().TypedValue);
        Assert.Empty(SchemaValidator.Validate(XDocument.Parse(xml), Schemas.Value));
    }
}
