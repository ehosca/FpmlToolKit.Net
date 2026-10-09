using fpml_5_13_confirmation;
using Confirmation = fpml_5_13_confirmation;
using Reporting = fpml_5_13_reporting;

namespace FpmlToolKit.Tests;

// The code in the README's Usage section, run against an official example. Keep the two in sync.
public class ReadmeExampleTests
{
    private static readonly string TradePath = Path.Combine(AppContext.BaseDirectory, "Examples", "fpml-5-13-confirmation",
        "products", "interest-rate-derivatives", "ird-ex08-fra.xml");

    [Fact]
    public void Reading_a_document()
    {
        var doc = XRoot.Load(TradePath);
        var data = Assert.IsType<dataDocument>(doc.Root);

        var trade = Assert.Single(data.trade);
        Assert.Equal(new DateTime(1991, 5, 14), trade.tradeHeader.tradeDate.TypedValue);
        Assert.Equal("1991-05-14", $"{trade.tradeHeader.tradeDate.TypedValue:yyyy-MM-dd}");

        Assert.Equal(["party1", "party2"], data.party.Select(p => p.id));
        Assert.Equal("TR24TWEY5RVRQV65HD49", data.party[0].partyId[0].TypedValue);
    }

    [Fact]
    public void Load_and_Parse()
    {
        var data = dataDocument.Load(TradePath);
        var fromString = dataDocument.Parse(File.ReadAllText(TradePath));

        Assert.Equal(data.ToString(), fromString.ToString());
    }

    [Fact]
    public void Untyped_shares_the_element()
    {
        var data = dataDocument.Load(TradePath);

        Assert.Contains(data.Untyped.Descendants(), e => e.Name.LocalName == "fra");

        data.party[0].Untyped.SetAttributeValue("id", "renamed");
        Assert.Equal("renamed", data.party[0].id);
    }

    [Fact]
    public void Building_and_validating_a_document()
    {
        var document = new dataDocument
        {
            fpmlVersion = "5-13",
            party =
            [
                new Party { id = "party1", partyId = [new PartyId { TypedValue = "549300EXAMPLEBANK001" }] },
            ],
        };

        string xml = document.ToString();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
        try
        {
            document.Save(path);
            Assert.Equal(dataDocument.Parse(xml).ToString(), dataDocument.Load(path).ToString());
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Empty(FpmlSchema.Validate(document));
        Assert.True(FpmlSchema.IsValid(document));
        Assert.True(FpmlSchema.CreateSchemaSet().IsCompiled);
    }

    [Fact]
    public void Using_several_views()
    {
        var confirmation = Confirmation.XRoot.Load(TradePath);
        var report = new Reporting.dataDocument { fpmlVersion = "5-13" };

        Assert.IsType<Confirmation.dataDocument>(confirmation.Root);
        Assert.Equal("http://www.fpml.org/FpML-5/reporting", report.Untyped.Name.NamespaceName);
    }
}
