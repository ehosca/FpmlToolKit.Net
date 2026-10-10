using System.Xml.Linq;
using fpml_5_13_confirmation;
using Confirmation = fpml_5_13_confirmation;
using Reporting = fpml_5_13_reporting;

namespace FpmlToolKit.Tests;

// Every code example in README.md and in the package READMEs (scripts/add-schema-version.cs), run against the official
// FpML 5.13 FRA example: 25,000,000 CHF at 4%, traded 1991-05-14 between party1 and party2. Keep the two in sync.
public class ReadmeExampleTests
{
    private static readonly string TradePath = Path.Combine(AppContext.BaseDirectory, "Examples", "fpml-5-13-confirmation",
        "products", "interest-rate-derivatives", "ird-ex08-fra.xml");

    // Quick start
    [Fact]
    public void Quick_start()
    {
        var fpml = dataDocument.Load(TradePath);
        var trade = fpml.trade[0];

        var deal = Assert.IsType<fra>(trade.product);
        var line = FormattableString.Invariant(
            $"{trade.tradeHeader.tradeDate.TypedValue:yyyy-MM-dd}: FRA on {deal.notional.amount:N0} {deal.notional.currency.TypedValue}, fixed {deal.fixedRate.TypedValue} vs {deal.floatingRateIndex.TypedValue}");

        Assert.Equal("1991-05-14: FRA on 25,000,000 CHF, fixed 0.04 vs CHF-LIBOR-BBA", line);
    }

    // Recipes: Read any document
    [Fact]
    public void Reading_any_document()
    {
        var doc = XRoot.Load(TradePath);

        var data = Assert.IsType<dataDocument>(doc.Root);
        Assert.Equal(new DateTime(1991, 5, 14), Assert.Single(data.trade).tradeHeader.tradeDate.TypedValue);
        Assert.Equal(["party1: TR24TWEY5RVRQV65HD49", "party2: BFXS5XCH7N0Y05NIXW11"],
            data.party.Select(party => $"{party.id}: {party.partyId[0].TypedValue}"));
    }

    [Fact]
    public void Load_and_Parse()
    {
        var fromFile = dataDocument.Load(TradePath);
        var fromString = dataDocument.Parse(File.ReadAllText(TradePath));

        Assert.Equal(fromFile.ToString(), fromString.ToString());
    }

    // Recipes: Query with LINQ
    [Fact]
    public void Query_with_LINQ()
    {
        var fpml = dataDocument.Load(TradePath);
        var deal = (fra)fpml.trade[0].product;

        var buyer = fpml.party.Single(p => p.id == deal.buyerPartyReference.href);

        Assert.Equal("TR24TWEY5RVRQV65HD49", buyer.partyId[0].TypedValue);
    }

    [Fact]
    public void Query_a_folder_of_trades()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
        try
        {
            File.Copy(TradePath, Path.Combine(folder, "fra-1.xml"));
            File.Copy(TradePath, Path.Combine(folder, "fra-2.xml"));

            var chfNotional = Directory.EnumerateFiles(folder, "*.xml")
                .Select(dataDocument.Load)
                .SelectMany(doc => doc.trade)
                .Select(trade => trade.product)
                .OfType<fra>()
                .Where(deal => deal.notional.currency.TypedValue == "CHF")
                .Sum(deal => deal.notional.amount);

            Assert.Equal(50_000_000m, chfNotional);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // Recipes: Build a document
    [Fact]
    public void Building_a_document()
    {
        var document = new dataDocument
        {
            fpmlVersion = "5-13",
            party =
            [
                new Party
                {
                    id = "party1",
                    partyId = [new PartyId { partyIdScheme = new Uri("http://www.fpml.org/coding-scheme/external/iso17442"), TypedValue = "549300EXAMPLEBANK001" }],
                },
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

        Assert.True(FpmlSchema.IsValid(document));
    }

    // Recipes: Edit and save
    [Fact]
    public void Edit_and_save()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xml");
        try
        {
            var fpml = dataDocument.Load(TradePath);
            var deal = (fra)fpml.trade[0].product;
            deal.notional.amount = 30_000_000m;
            fpml.Save(path);

            Assert.True(FpmlSchema.IsValid(fpml));
            Assert.Equal(30_000_000m, ((fra)dataDocument.Load(path).trade[0].product).notional.amount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Recipes: Validate
    [Fact]
    public void Validation_catches_mistakes()
    {
        var draft = new dataDocument
        {
            fpmlVersion = "5-13",
            party = [new Party { id = "p1", partyId = [new PartyId { TypedValue = "ACME" }] }],
        };

        var problems = FpmlSchema.Validate(draft);

        Assert.Contains("partyIdScheme", Assert.Single(problems).Message);
        Assert.False(FpmlSchema.IsValid(draft));
    }

    [Fact]
    public void Validating_any_xml()
    {
        var incoming = XDocument.Load(TradePath);

        Assert.True(FpmlSchema.IsValid(incoming));
        Assert.Empty(FpmlSchema.Validate(incoming));
        Assert.True(FpmlSchema.CreateSchemaSet().IsCompiled);
    }

    // Recipes: Several views or versions
    [Fact]
    public void Using_several_views()
    {
        var confirmation = Confirmation.XRoot.Load(TradePath);
        var report = new Reporting.dataDocument { fpmlVersion = "5-13" };

        Assert.IsType<Confirmation.dataDocument>(confirmation.Root);
        Assert.Equal("http://www.fpml.org/FpML-5/reporting", report.Untyped.Name.NamespaceName);
    }

    // Recipes: Drop down to LINQ to XML
    [Fact]
    public void Untyped_shares_the_element()
    {
        var fpml = dataDocument.Load(TradePath);

        Assert.Equal(["fra"], fpml.trade[0].Untyped.Elements().Select(e => e.Name.LocalName).Where(n => n == "fra"));

        fpml.party[0].Untyped.SetAttributeValue("id", "renamed");
        Assert.Equal("renamed", fpml.party[0].id);
    }

    // The package README (scripts/add-schema-version.cs)
    [Fact]
    public void Package_readme_example()
    {
        var doc = XRoot.Load(TradePath);

        Assert.Equal("dataDocument", doc.Root.GetType().Name);
        Assert.True(FpmlSchema.IsValid(doc.Root));
    }
}
