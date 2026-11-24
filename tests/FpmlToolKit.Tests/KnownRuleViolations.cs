namespace FpmlToolKit.Tests;

// Official FpML files whose rule results differ from what their file name says, each reviewed by hand.
// ValidationRuleTests expects exactly these differences, so an entry that stops being true fails the build.
internal static class KnownRuleViolations
{
    private const string C513 = "fpml-5-13-confirmation";

    public sealed class Entry(string project, string example, string[] alsoRaises, string[] doesNotRaise)
    {
        public string Project { get; } = project;
        public string Example { get; } = example;
        public string[] AlsoRaises { get; } = alsoRaises;
        public string[] DoesNotRaise { get; } = doesNotRaise;
    }

    private static Entry Raises(string project, string example, params string[] rules) => new(project, example, rules, []);

    public static IReadOnlyList<Entry> All { get; } =
    [
        // Invalid test cases for another rule that also break one of ours.
        // buyerPartyReference and sellerPartyReference are the same party (shared-29).
        Raises(C513, "validation/invalid-testcases/invalid-cd-5-01.xml", "shared-29"),
        Raises(C513, "validation/invalid-testcases/invalid-cd-6-01.xml", "shared-29"),
        Raises(C513, "validation/invalid-testcases/invalid-eqd-22-01.xml", "shared-29"),
        // cd-9 and cd-10 point primaryObligorReference / guarantorReference at a party instead of the reference entity.
        Raises(C513, "validation/invalid-testcases/invalid-cd-9-01.xml", "ref-28"),
        Raises(C513, "validation/invalid-testcases/invalid-cd-10-01.xml", "ref-28"),
        // The added dateAdjustmentsReference points at a party.
        Raises(C513, "validation/invalid-testcases/invalid-cd-21-08.xml", "ref-27"),
        Raises(C513, "validation/invalid-testcases/invalid-cd-21-10.xml", "ref-27"),
        Raises(C513, "validation/invalid-testcases/invalid-cd-21b-06.xml", "ref-27"),
        Raises(C513, "validation/invalid-testcases/invalid-cd-21b-08.xml", "ref-27"),
        Raises("fpml-5-7-confirmation", "validation/invalid-testcases/invalid-cd-21b-08.xml", "ref-27"),
        // ird-8 is the swap-stream form of shared-5: payer and receiver are the same party.
        Raises(C513, "validation/invalid-testcases/invalid-ird-8-01.xml", "shared-5"),
        Raises(C513, "validation/invalid-testcases/invalid-ird-8-02.xml", "shared-5"),
        // The loan test cases' base document gives several parties the same partyId.
        .. new[] { "ln-5-01", "ln-27-01", "ln-28-01", "ln-29-01", "ln-30-01", "ln-31-01", "ln-32-01", "ln-33-01", "ln-33-02", "ln-34-01", "ln-34-02" }
            .Select(t => Raises(C513, $"validation/invalid-testcases/invalid-{t}.xml", "shared-18")),
        // The dangling reference these test cases add is also a reference to the wrong type.
        Raises(C513, "validation/invalid-testcases/invalid-shared-11-01.xml", "ref-26"),
        Raises(C513, "validation/invalid-testcases/invalid-shared-12-01.xml", "ref-29"),
        Raises(C513, "validation/invalid-testcases/invalid-shared-13-01.xml", "ref-29"),
        Raises(C513, "validation/invalid-testcases/invalid-shared-14-01.xml", "ref-29"),
        // shared-22 (unique calculation agents) is now shared-10.
        Raises(C513, "validation/invalid-testcases/invalid-shared-22-01.xml", "shared-10"),
        // The base document references terminationDate through calculationPeriodDatesReference (as ird-ex33 does).
        Raises(C513, "validation/invalid-testcases/invalid-ref-12-01.xml", "ref-8"),

        // Test cases that don't test what their name says.
        // Its calculationPeriodDatesReference points at a swapStream: that breaks ref-8, and there is no interest leg.
        new(C513, "validation/invalid-testcases/invalid-ref-7-01.xml", ["ref-8"], ["ref-7"]),
        // Removes a dayType that the English rule requires; FpML's XPath, which this implements, doesn't check it.
        new(C513, "validation/invalid-testcases/invalid-shared-15-03.xml", [], ["shared-15"]),

        // Official examples that break published rules.
        // A business day convention other than NONE without business centers (shared-1), a business-day offset with
        // a convention other than NONE (shared-4), or a dayType on a zero-day offset (shared-15).
        Raises(C513, "products/commodity-derivatives/com-ex34-gas-put-option-european-floating-strike.xml", "shared-15", "shared-4"),
        Raises("fpml-5-5-confirmation", "products/commodity-derivatives/com-ex34-gas-put-option-european-floating-strike.xml", "shared-1", "shared-15", "shared-4"),
        Raises(C513, "products/commodity-derivatives/com-ex35-call-option-gas-power-heat-rate-daily.xml", "shared-1", "shared-15"),
        Raises(C513, "products/commodity-derivatives/com-ex36-gas-call-option-european-spread-negative-premium-floating-strike.xml", "shared-1"),
        Raises(C513, "products/commodity-derivatives/com-ex43-WTI-variance-swap.xml", "shared-1"),
        Raises(C513, "products/commodity-derivatives/com-ex46-simple-financial-put-option.xml", "shared-1"),
        Raises(C513, "products/fx-derivatives/fx-ex27-flexible-term-forward.xml", "shared-4"),
        Raises(C513, "products/fx-derivatives/fx-ex31-volatility-swap.xml", "shared-15"),
        Raises(C513, "products/inflation-swaps/inflation-asset-swap-ex02-ratio-zc-floored.xml", "shared-1", "shared-15", "shared-4"),
        Raises("fpml-5-14-recordkeeping", "digital-regulatory-reporting/products/equity/Equity-Swap-Price-Return-Basket-ex03-Amend.xml", "shared-1"),
        // Two parties with the same partyId and scheme (shared-18).
        Raises(C513, "products/inflation-swaps/inflation-asset-swap-ex03-par-par.xml", "shared-18"),
        Raises(C513, "products/inflation-swaps/inflation-asset-swap-ex04-proceeds.xml", "shared-18"),
        Raises("fpml-5-11-recordkeeping", "full-product/record-ex601-ird-fra.xml", "shared-18"),
        Raises("fpml-5-12-recordkeeping", "flat-product/record-ex113-novation-from_transferor.xml", "shared-18"),
        Raises("fpml-5-13-recordkeeping", "digital-regulatory-reporting/events/Clearing-cleared-alpha-Trade-03.xml", "shared-18"),
        Raises("fpml-5-13-recordkeeping", "digital-regulatory-reporting/products/fx/FX-NDF-ex05-Cleared.xml", "shared-18"),
        Raises("fpml-5-14-recordkeeping", "deprecated/flat-product/deprecated-record-ex110-amendment.xml", "shared-18"),
        // References to the wrong type of element: a partyReference to an account (ref-29), a paymentDatesReference to
        // principalExchanges (ref-11), a calculationPeriodDatesReference to a terminationDate (ref-8), and in trs-ex04 a
        // businessCentersReference to a single businessCenter (ref-26) and an interest leg's
        // calculationPeriodDatesReference to valuationDates (ref-7).
        Raises(C513, "business-processes/clearing/msg-ex102-clearingConfirmed-credit-event-single-name.xml", "ref-29"),
        Raises(C513, "business-processes/clearing/msg-ex103-clearingConfirmed-credit-event-index.xml", "ref-29"),
        Raises(C513, "products/interest-rate-derivatives/ird-ex29-non-deliverable-settlement-swap.xml", "ref-11"),
        Raises(C513, "products/interest-rate-derivatives/ird-ex31-non-deliverable-settlement-swap.xml", "ref-11"),
        Raises(C513, "products/interest-rate-derivatives/ird-ex33-BRL-CDI-swap.xml", "ref-8"),
        Raises(C513, "products/equity-swaps/trs-ex04-index-ios.xml", "ref-26", "ref-7"),
    ];

    public static Entry For(string project, string example) =>
        All.SingleOrDefault(e => e.Project == project && e.Example == example);
}
