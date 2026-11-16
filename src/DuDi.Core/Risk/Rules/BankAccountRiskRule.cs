using DuDi.Core.Registry;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena obecności rachunku bankowego z faktury na Białej liście VAT.</summary>
public sealed class BankAccountRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        if (profile.Account is not { } account)
        {
            checks.Add(new CheckSummary(SourceNames.WhiteList, profile.CheckedOn, CheckOutcome.Info,
                "nie podano rachunku z faktury – nie sprawdzono, czy jest na Białej liście"));
            return;
        }

        if (account.Status == CheckStatus.Unavailable)
        {
            checks.Add(account.NotChecked("czy rachunek z faktury jest na Białej liście"));
            return;
        }

        var result = account.Value!;
        var number = result.Account.ToDisplayString();

        if (result.Assigned)
        {
            checks.Add(account.Summary(CheckOutcome.Passed, $"rachunek {number} jest na Białej liście tej firmy"));
            return;
        }

        findings.Add(new Finding(
            FindingCodes.AccountNotOnWhiteList,
            RiskLevel.High,
            $"Rachunek z faktury {number} nie jest przypisany do tej firmy na Białej liście VAT.",
            "Poproś kontrahenta o fakturę z rachunkiem widocznym na Białej liście. Przy płatności powyżej 15 000 zł na rachunek spoza listy " +
            "możesz stracić prawo do zaliczenia wydatku w koszty i odpowiadać solidarnie za VAT kontrahenta; jeśli zapłata już nastąpiła, " +
            "w ciągu 7 dni można złożyć zawiadomienie ZAW-NR do urzędu skarbowego.",
            account.Source, account.CheckedOn));
        checks.Add(account.Summary(CheckOutcome.Problem, $"rachunek {number} NIE jest na Białej liście tej firmy"));
    }
}
