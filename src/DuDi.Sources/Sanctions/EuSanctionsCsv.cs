using System.Text;

namespace DuDi.Sources.Sanctions;

/// <summary>Jeden alias (wariant nazwy) podmiotu lub osoby z listy sankcyjnej.</summary>
public sealed record SanctionsListEntry(
    string LogicalId,
    string? ReferenceNumber,
    string? SubjectType,
    string? Programme,
    string WholeName);

/// <summary>
/// Parser skonsolidowanej listy sankcji finansowych UE w formacie CSV (EU Financial Sanctions Files, separator „;”).
/// Kolumny wyszukujemy po nazwie z nagłówka, więc ten sam kod czyta pełny plik z UE i uproszczony plik symulowanej bazy.
/// </summary>
public static class EuSanctionsCsv
{
    public static IReadOnlyList<SanctionsListEntry> Parse(string csv)
    {
        using var rows = ReadRows(csv).GetEnumerator();

        if (!rows.MoveNext())
            return [];

        var header = rows.Current.Select(h => h.Trim().TrimStart('﻿')).ToList();

        int Column(string name)
        {
            var index = header.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index : throw new InvalidDataException($"Plik listy sankcyjnej nie ma kolumny {name}.");
        }

        var idColumn = Column("Entity_LogicalId");
        var referenceColumn = Column("Entity_EU_ReferenceNumber");
        var typeColumn = Column("Entity_SubjectType");
        var programmeColumn = Column("Entity_Regulation_Programme");
        var nameColumn = Column("NameAlias_WholeName");

        var entries = new List<SanctionsListEntry>();
        var seen = new HashSet<(string, string)>();

        while (rows.MoveNext())
        {
            var row = rows.Current;

            string? Cell(int index) => index < row.Count && !string.IsNullOrWhiteSpace(row[index]) ? row[index].Trim() : null;

            if (Cell(idColumn) is not { } id || Cell(nameColumn) is not { } name)
                continue;

            // W pełnym pliku UE ten sam alias powtarza się w wielu wierszach (adresy, daty urodzenia...).
            if (seen.Add((id, name)))
                entries.Add(new SanctionsListEntry(id, Cell(referenceColumn), Cell(typeColumn), Cell(programmeColumn), name));
        }

        return entries;
    }

    /// <summary>Prosty czytnik CSV: separator „;”, pola w cudzysłowach mogą zawierać separator, cudzysłów ("") i nowe linie.</summary>
    private static IEnumerable<List<string>> ReadRows(string csv)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;

                case ';':
                    row.Add(field.ToString());
                    field.Clear();
                    break;

                case '\r':
                    break;

                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row;
                    row = [];
                    break;

                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }
}
