using DuDi.Core.Risk.Rules;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk;

/// <summary>
/// Silnik oceny ryzyka kontrahenta. Uruchamia zestaw reguł <see cref="IRiskRule"/> i wyznacza werdykt ogólny
/// jako najwyższą wagę wśród ustaleń (brak ustaleń = ryzyko niskie).
/// </summary>
/// <remarks>
/// Ocena jest liczona w kodzie, a nie przez LLM: wynik jest powtarzalny, testowalny i łatwo pokazać, skąd się wziął.
/// Źródło, które nie odpowiedziało, nie generuje ustalenia – trafia na listę „nie sprawdzono”.
/// </remarks>
public sealed class RiskScorer(IEnumerable<IRiskRule> rules) : IRiskScorer
{
    private readonly IReadOnlyList<IRiskRule> rules = rules.ToList();

    public static IReadOnlyList<IRiskRule> DefaultRules { get; } =
    [
        new VatStatusRiskRule(),
        new BankAccountRiskRule(),
        new KrsStatusRiskRule(),
        new CompanyAgeRiskRule(),
        new BeneficialOwnersRiskRule(),
        new SanctionsRiskRule(),
        new RelationsRiskRule()
    ];

    public static RiskScorer Default { get; } = new(DefaultRules);

    public RiskScorer()
        : this(DefaultRules)
    {
    }

    RiskReport IRiskScorer.Score(CompanyProfile profile) => Evaluate(profile);

    public RiskReport Evaluate(CompanyProfile profile)
    {
        var findings = new List<Finding>();
        var checks = new List<CheckSummary>();

        if (!profile.IsKnown)
        {
            checks.Add(profile.Vat.Status == CheckStatus.Unavailable
                ? profile.Vat.NotChecked("nie udało się ustalić, czy podmiot figuruje na Białej liście")
                : new CheckSummary(profile.Vat.Source, profile.Vat.CheckedOn, CheckOutcome.Info,
                    "brak podmiotu o tym NIP w wykazie podatników VAT; bez numeru KRS nie da się sprawdzić pozostałych rejestrów"));

            return new RiskReport(profile, null, [], checks);
        }

        foreach (var rule in rules)
            rule.Evaluate(profile, findings, checks);

        var ordered = findings.OrderByDescending(f => f.Severity).ToList();
        var verdict = ordered.Count == 0 ? RiskLevel.Low : ordered[0].Severity;

        return new RiskReport(profile, verdict, ordered, checks);
    }

    public static RiskReport Score(CompanyProfile profile) => Default.Evaluate(profile);

    public static int LatestRequiredFiscalYear(DateOnly today) =>
        KrsStatusRiskRule.LatestRequiredFiscalYear(today);
}
