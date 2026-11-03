# Dane

## `fixtures/` – symulowana baza (wszystko fikcyjne)

Pliki są w **formacie odpowiedzi prawdziwych API** i przechodzą przez te same mappery, których użyje klient HTTP w etapach 3–4.

| Katalog | Format | Źródło formatu |
|---|---|---|
| `whitelist/{nip}.json` | odpowiedź `GET /api/search/nip/{nip}` | Biała lista VAT (`wl-api.mf.gov.pl`) |
| `krs/{krs}.json` | odpis aktualny `GET /api/krs/OdpisAktualny/{krs}` | API KRS (`api-krs.ms.gov.pl`) |
| `crbr/{nip}.json` | **format własny** | CRBR nie ma potwierdzonego publicznego API |
| `sanctions/eu-sanctions.csv` | CSV z separatorem `;`, nazwy kolumn jak w pliku UE | EU Financial Sanctions Files |

### Scenariusze

Stan na dzień oceny 2026-11-02 (tę datę ustawiają testy).

| # | Firma | NIP | Co ilustruje | Werdykt |
|---|---|---|---|---|
| 1 | Alfa Budownictwo | 948-812-73-53 | wszystko w porządku | NISKIE |
| 2 | Beta Trans | 925-575-73-34 | rachunek z faktury `20 1240 1037 9198 6621 8187 1482` **nie** jest na Białej liście (rachunek z listy: `70 1050 1445 3872 5907 3619 6366`) | WYSOKIE (z tym rachunkiem), NISKIE (bez rachunku) |
| 3 | Gamma Handel | 910-143-87-31 | brak sprawozdań za 2024 i 2025, brak zgłoszenia w CRBR | ŚREDNIE |
| 4 | Delta Logistics | 265-017-23-91 | ogłoszona upadłość (dział 6), brak sprawozdania za 2025 | WYSOKIE |
| 5 | Epsilon Consulting | 540-915-36-39 | prezes (Tomasz Wójcik) zasiada w zarządzie Delty | ŚREDNIE |
| 6 | Zeta Import | 809-720-28-19 | beneficjent rzeczywisty (Arkadij Zmyślonow) na liście sankcyjnej | KRYTYCZNE |
| 7 | Eta Usługi | 718-195-22-50 | zwolniona z VAT, zarejestrowana 3 miesiące temu | NISKIE (informacje) |
| 8 | Iota Serwis | 459-779-56-31 | wykreślona z rejestru VAT 2026-02-10 | WYSOKIE |
| – | (spoza bazy) | 103-286-05-45 | poprawny NIP, którego nie ma w bazie | brak oceny |

### Bezpieczeństwo danych testowych

- Nazwy firm i osób są fikcyjne.
- Wszystkie NIP-y mają poprawną sumę kontrolną. **2026-11-03 sprawdziliśmy jednym zapytaniem do Białej listy** (`/api/search/nips/...`), że żaden z nich nie należy do prawdziwego podmiotu. To samo sprawdziliśmy dla rachunków (`/api/search/bank-accounts/...`).
- Numery KRS `0009990001`–`0009990008` zwracają 404 w API KRS, czyli nie istnieją.
- Nazwisk z fikcyjnej listy sankcyjnej nie ma w prawdziwym pliku sankcji UE.
- W trybie Mock agent oznacza odpowiedzi jako **[DANE TESTOWE]**.

Fixtures wygenerował skrypt na podstawie struktury prawdziwych odpowiedzi. Przy edycji ręcznej trzeba pilnować sum kontrolnych NIP i NRB. Test `ScenarioTests` od razu pokaże, jeśli werdykt się zmieni.

## `samples/` – prawdziwe odpowiedzi API (tylko do testów parsowania)

Pobrane 2026-11-03 podczas rozpoznania API (spike). Dotyczą dużych, publicznych spółek, a dane osób są w nich zamaskowane przez samo API.

| Plik | Zapytanie |
|---|---|
| `whitelist-search-7740001454.json` | Biała lista, `search/nip/7740001454` (Orlen) |
| `whitelist-search-notfound.json` | Biała lista, NIP spoza wykazu (`subject: null`) |
| `whitelist-check-tak.json` / `whitelist-check-nie.json` | Biała lista, `check/nip/{nip}/bank-account/{nrb}` |
| `whitelist-error-invalid-nip.json` | Biała lista, niepoprawny NIP (`WL-115`) |
| `krs-odpis-0000028860.json` | KRS, odpis aktualny (Orlen) |
| `krs-odpis-0000304735-upadlosc.json` | KRS, spółka w upadłości (struktura działu 6) |
| `eu-sanctions-sample.csv` | pierwsze 15 wierszy skonsolidowanej listy sankcji UE |
