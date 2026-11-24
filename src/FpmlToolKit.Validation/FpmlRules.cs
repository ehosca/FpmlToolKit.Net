using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FpmlToolKit.Validation
{
    /// <summary>
    /// The implemented FpML business validation rules. Rule identifiers, contexts and meaning follow the FpML 5.13
    /// Recommendation's validation rules ("Rules for Shared Elements" and "ID / IDREF Rules"). They run on FpML 5.x
    /// confirmation, reporting, recordkeeping and transparency documents of any version; a rule only runs where its
    /// context types exist.
    /// </summary>
    public static class FpmlRules
    {
        private static readonly string[] NoConvention = { "NONE", "NotApplicable" };

        /// <summary>Every implemented rule, in identifier order.</summary>
        public static IReadOnlyList<FpmlRule> All { get; } = Shared().Concat(References())
            .OrderBy(r => r.Id.Substring(0, r.Id.LastIndexOf('-')), StringComparer.Ordinal)
            .ThenBy(r => int.Parse(new string(r.Id.Substring(r.Id.LastIndexOf('-') + 1).TakeWhile(char.IsDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();

        /// <summary>The rule with the given identifier (for example <c>shared-1</c>), or null.</summary>
        public static FpmlRule? Find(string id) => All.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<FpmlRule> Shared()
        {
            yield return FpmlRule.Assert("shared-1",
                "businessCenters or businessCentersReference must exist if and only if businessDayConvention is not NONE or NotApplicable.",
                new[] { "BusinessDayAdjustments" },
                (e, _) => e.Has("businessCentersReference", "businessCenters") == !NoConvention.Contains(e.ValueAt("businessDayConvention")));

            yield return FpmlRule.Assert("shared-4",
                "If dayType is Business, businessDayConvention must be NONE.",
                new[] { "RelativeDateOffset" },
                (e, _) => e.ValueAt("dayType") != "Business" || e.Child("businessDayConvention") == null || e.ValueAt("businessDayConvention") == "NONE");

            yield return FpmlRule.Assert("shared-5",
                "payerPartyReference/@href must not equal receiverPartyReference/@href.",
                new[]
                {
                    "DirectionalLeg", "EquityPremium", "ExerciseFee", "ExerciseFeeSchedule", "FeaturePayment", "FixedPriceLeg",
                    "FloatingPriceLeg", "FxOptionPremium", "GrossCashflow", "IndependentAmount", "InitialPayment", "InterestRateStream",
                    "NonPeriodicFixedPriceLeg", "PassThroughItem", "Payment", "PaymentBase", "PrePayment", "PrincipalExchangeDescriptions",
                    "ReturnSwapAdditionalPayment", "SimplePayment", "TradeUnderlyer2",
                },
                (e, _) =>
                {
                    var payer = e.Child("payerPartyReference").Href();
                    var receiver = e.Child("receiverPartyReference").Href();
                    return payer == null || receiver == null || payer != receiver;
                });

            yield return FpmlRule.Assert("shared-6",
                "If latestExerciseTime exists, earliestExerciseTime/hourMinuteTime must be before latestExerciseTime/hourMinuteTime.",
                new[] { "AmericanExercise" }, EarliestBeforeLatest);

            yield return FpmlRule.Assert("shared-7",
                "If latestExerciseTime exists, earliestExerciseTime/hourMinuteTime must be before latestExerciseTime/hourMinuteTime.",
                new[] { "BermudaExercise" }, EarliestBeforeLatest);

            yield return FpmlRule.Assert("shared-8",
                "unadjustedFirstDate must be before unadjustedLastDate.",
                new[] { "DateRange" },
                (e, _) =>
                {
                    var first = e.Child("unadjustedFirstDate").Date();
                    var last = e.Child("unadjustedLastDate").Date();
                    return first == null || last == null || first < last;
                });

            yield return FpmlRule.Assert("shared-9",
                "businessDayConvention must be NONE or NotApplicable if and only if neither businessCenters nor businessCentersReference exists.",
                new[] { "BusinessDateRange" },
                (e, _) => !e.Has("businessCentersReference", "businessCenters") == NoConvention.Contains(e.ValueAt("businessDayConvention")));

            yield return FpmlRule.Each("shared-10",
                "calculationAgentPartyReference/@href values must be unique.",
                new[] { "CalculationAgent" },
                (e, _) => e.Children("calculationAgentPartyReference").Duplicates(r => r.Href()));

            yield return FpmlRule.Each("shared-11",
                "If the trade has a businessDateRange, every businessCentersReference/@href in the trade must match a businessCenters/@id in the same trade.",
                new[] { "Trade" },
                (e, c) =>
                {
                    if (!c.DescendantsInScope(e, "businessDateRange").Any())
                        return Enumerable.Empty<XElement>();
                    var ids = new HashSet<string?>(c.DescendantsInScope(e, "businessCenters").Select(b => (string?)b.Attribute("id")));
                    return c.DescendantsInScope(e, "businessCentersReference").Where(r => !ids.Contains(r.Href()));
                });

            yield return FpmlRule.Each("shared-14",
                "Every calculationAgentPartyReference/@href must match the @id of a party.",
                new[] { "Document" },
                (e, c) =>
                {
                    var parties = new HashSet<string?>(e.Children("party").Select(p => (string?)p.Attribute("id")));
                    return c.DescendantsInScope(e, "calculationAgentPartyReference").Where(r => !parties.Contains(r.Href()));
                });

            // As FpML's XPath defines it: only offsets that have a dayType are checked. The English also reads as requiring
            // dayType on non-zero day offsets (test case shared-15-03), but FpML's own examples routinely omit it.
            yield return FpmlRule.Assert("shared-15",
                "If dayType exists, period must be D exactly when periodMultiplier is not zero.",
                new[] { "Offset" },
                (e, _) => e.Child("dayType") == null || (e.ValueAt("period") == "D") == (e.Child("periodMultiplier").Decimal() != 0));

            yield return FpmlRule.Each("shared-18",
                "Each party/partyId (with its partyIdScheme) and each party/partyName must be unique.",
                new[] { "Document" },
                (e, _) =>
                {
                    var parties = e.Children("party").ToList();
                    var ids = parties.SelectMany(p => p.Children("partyId")).Duplicates(i => i.SchemeKey("partyIdScheme"));
                    var names = parties.SelectMany(p => p.Children("partyName")).Duplicates(n => n.Value.Trim());
                    return ids.Concat(names);
                });

            yield return FpmlRule.Each("shared-19",
                "Each account/accountId (with its accountIdScheme) must be unique.",
                new[] { "Document" },
                (e, _) => e.Children("account").SelectMany(a => a.Children("accountId")).Duplicates(i => i.SchemeKey("accountIdScheme")));

            yield return FpmlRule.Each("shared-20",
                "Each unadjustedDate must be distinct.",
                new[] { "AdjustableDates" },
                (e, _) => e.Children("unadjustedDate").Duplicates(d => d.Date()?.ToString("yyyy-MM-dd") ?? d.Value.Trim()));

            yield return FpmlRule.Each("shared-21",
                "Each businessCenter (with its businessCenterScheme) must be unique.",
                new[] { "BusinessCenters" },
                (e, _) => e.Children("businessCenter").Duplicates(b => b.SchemeKey("businessCenterScheme")));

            yield return FpmlRule.Each("shared-23",
                "Each referenceBank must be unique by referenceBankId (with its referenceBankIdScheme) and by referenceBankName.",
                new[] { "CashSettlementReferenceBanks" },
                (e, _) =>
                {
                    var banks = e.Children("referenceBank").ToList();
                    var ids = banks.Where(b => b.Child("referenceBankId") != null).Duplicates(b => b.Child("referenceBankId")!.SchemeKey("referenceBankIdScheme"));
                    var names = banks.Where(b => b.Child("referenceBankName") != null).Duplicates(b => b.ValueAt("referenceBankName"));
                    return ids.Concat(names).Distinct();
                });

            yield return FpmlRule.Each("shared-24",
                "Each routingId (with its routingIdCodeScheme) must be unique.",
                new[] { "RoutingIds" },
                (e, _) => e.Children("routingId").Duplicates(r => r.SchemeKey("routingIdCodeScheme")));

            yield return FpmlRule.Each("shared-25",
                "If there is more than one step, each step/stepDate must be unique.",
                new[] { "Schedule" },
                (e, _) => e.Children("step").Duplicates(s => s.Child("stepDate").Date()?.ToString("yyyy-MM-dd") ?? s.ValueAt("stepDate")));

            yield return FpmlRule.Assert("shared-26",
                "If parValue exists, currency must exist.",
                new[] { "Bond" },
                (e, _) => e.Child("parValue") == null || e.Child("currency") != null);

            yield return FpmlRule.Assert("shared-29",
                "buyerPartyReference/@href must not equal sellerPartyReference/@href, unless buyerAccountReference and sellerAccountReference both exist and differ.",
                new[]
                {
                    "CancelableProvision", "CommodityOption", "EquityDerivativeBase", "ExtendibleProvision", "Fra", "GeneralTerms",
                    "GenericProduct", "InstrumentTradeDetails", "Option", "ReturnSwapBase", "SinglePartyOption", "Swaption",
                },
                (e, _) =>
                {
                    var buyer = e.Child("buyerPartyReference").Href();
                    var seller = e.Child("sellerPartyReference").Href();
                    if (buyer == null || seller == null || buyer != seller)
                        return true;
                    var buyerAccount = e.Child("buyerAccountReference").Href();
                    var sellerAccount = e.Child("sellerAccountReference").Href();
                    return buyerAccount != null && sellerAccount != null && buyerAccount != sellerAccount;
                });
        }

        private static bool EarliestBeforeLatest(XElement e, RuleContext _)
        {
            if (e.Child("latestExerciseTime") == null)
                return true;
            var earliest = e.At("earliestExerciseTime/hourMinuteTime").Time();
            var latest = e.At("latestExerciseTime/hourMinuteTime").Time();
            return earliest == null || latest == null || earliest < latest;
        }

        private static IEnumerable<FpmlRule> References()
        {
            // ref-n: the @href of an XReference must be the @id of an element of type X (or a type derived from it).
            (string Id, string Reference, string Target)[] references =
            {
                ("ref-1", "AssetReference", "Asset"),
                ("ref-2", "FixedRateReference", "FixedRate"),
                ("ref-3", "ProtectionTermsReference", "ProtectionTerms"),
                ("ref-4", "SettlementTermsReference", "SettlementTerms"),
                ("ref-7", "InterestLegCalculationPeriodDatesReference", "InterestLegCalculationPeriodDates"),
                ("ref-8", "CalculationPeriodDatesReference", "CalculationPeriodDates"),
                ("ref-9", "InterestRateStreamReference", "InterestRateStream"),
                ("ref-11", "PaymentDatesReference", "PaymentDates"),
                ("ref-12", "ResetDatesReference", "ResetDates"),
                ("ref-13", "CreditEventsReference", "CreditEvents"),
                ("ref-25", "AccountReference", "Account"),
                ("ref-26", "BusinessCentersReference", "BusinessCenters"),
                ("ref-27", "BusinessDayAdjustmentsReference", "BusinessDayAdjustments"),
                ("ref-28", "LegalEntityReference", "LegalEntity"),
                ("ref-29", "PartyReference", "Party"),
                ("ref-32", "ProductReference", "Product"),
                ("ref-33", "RateReference", "Rate"),
                ("ref-34", "ScheduleReference", "Schedule"),
                ("ref-35", "SpreadScheduleReference", "SpreadSchedule"),
            };
            foreach (var (id, reference, target) in references)
            {
                yield return FpmlRule.Assert(id,
                    $"{reference}/@href must be the @id of an element of type {target}.",
                    new[] { reference },
                    (e, c) => c.IsInstanceOf(c.ById(e.Href()), target));
            }

            yield return FpmlRule.Assert("ref-10",
                "If @href exists, it must be the @id of an element of type PricingStructure.",
                new[] { "PaymentCalculationPeriod" },
                (e, c) => e.Href() == null || c.IsInstanceOf(c.ById(e.Href()), "PricingStructure"));

            // The rule names mandatoryEarlyTerminationDate's unadjustedDate and adjustedDate, but FpML's own valid test cases
            // describe the target as "an element mandatoryEarlyTerminationDate" and its examples reference the
            // mandatoryEarlyTerminationDate itself, so that is accepted too.
            yield return FpmlRule.Assert("ref-38",
                "cashSettlement/cashSettlementValuationDate/dateRelativeTo/@href must be the @id of mandatoryEarlyTerminationDate or of its unadjustedDate or adjustedDate.",
                new[] { "MandatoryEarlyTermination" },
                (e, _) =>
                {
                    var relativeTo = e.At("cashSettlement/cashSettlementValuationDate/dateRelativeTo");
                    if (relativeTo == null)
                        return true;
                    var href = relativeTo.Href();
                    var date = e.Child("mandatoryEarlyTerminationDate");
                    return href != null && date != null && new[] { date, date.Child("unadjustedDate"), date.Child("adjustedDate") }
                        .Any(d => d != null && href == (string?)d.Attribute("id"));
                });
        }
    }
}
