using DuDi.Core.Registry;
using DuDi.Core.Verification;

namespace DuDi.Core.Risk.Rules;

/// <summary>Ocena statusu podmiotu w wykazie podatników VAT (Biała lista MF).</summary>
public sealed class VatStatusRiskRule : IRiskRule
{
    public void Evaluate(CompanyProfile profile, ICollection<Finding> findings, ICollection<CheckSummary> checks)
    {
        var vat = profile.Vat;

        switch (vat.Status)
        {
            case CheckStatus.Unavailable:
                checks.Add(vat.NotChecked("status VAT i rachunki bankowe"));
                return;

            case CheckStatus.NotFound:
                findings.Add(new Finding(
                    FindingCodes.VatNotFound,
                    RiskLevel.High,
                    "Podmiotu nie ma w wykazie podatników VAT (Biała lista).",
                    "Upewnij się, czy kontrahent może wystawić fakturę z VAT. W razie wątpliwości skonsultuj się z księgowością przed zapłatą.",
                    vat.Source, vat.CheckedOn));
                checks.Add(vat.Summary(CheckOutcome.Problem, "brak podmiotu w wykazie podatników VAT"));
                return;
        }

        var subject = vat.Value!;

        switch (subject.Status)
        {
            case VatStatus.Active:
                checks.Add(vat.Summary(CheckOutcome.Passed, "czynny podatnik VAT"));
                break;

            case VatStatus.Exempt:
                findings.Add(new Finding(
                    FindingCodes.VatExempt,
                    RiskLevel.Low,
                    "Firma jest podatnikiem zwolnionym z VAT.",
                    "Faktury od tego kontrahenta nie będą zawierały VAT – to normalne dla podatnika zwolnionego, nie jest to sygnał ostrzegawczy.",
                    vat.Source, vat.CheckedOn));
                checks.Add(vat.Summary(CheckOutcome.Info, "podatnik zwolniony z VAT"));
                break;

            case VatStatus.NotRegistered:
                var removal = subject.RemovalDate is { } date
                    ? $" Wykreślono go z rejestru VAT {RiskRuleHelpers.Format(date)}{(subject.RemovalBasis is not null ? $" (podstawa: {subject.RemovalBasis})" : "")}."
                    : "";
                findings.Add(new Finding(
                    FindingCodes.VatNotActive,
                    RiskLevel.High,
                    $"Podmiot nie jest czynnym podatnikiem VAT.{removal}",
                    "Nie odliczaj VAT z faktury od tego kontrahenta, dopóki nie wyjaśnisz jego statusu. Poproś o wyjaśnienie i skonsultuj się z księgowością.",
                    vat.Source, vat.CheckedOn));
                checks.Add(vat.Summary(CheckOutcome.Problem, "podmiot niezarejestrowany jako podatnik VAT"));
                break;
        }
    }
}
