# Architektura DuDi

## Przepływ

```text
┌────────────┐  pytanie / NIP   ┌──────────────────────────────────────────────┐
│ Użytkownik │ ───────────────▶ │ DuDi.Cli – pętla czatu, komendy, streaming   │
└────────────┘ ◀─────────────── └──────────────────────┬───────────────────────┘
               odpowiedź                               │
                                ┌──────────────────────▼───────────────────────┐
                                │ DuDi.Agent                                   │
                                │  DuDiAgent: prompt systemowy + historia      │
                                │  IChatClient + UseFunctionInvocation         │
                                │  RegistryTools ── narzędzia dla modelu       │
                                │  ContextBuilder ── RiskReport → tekst        │
                                └──────────────────────┬───────────────────────┘
                                                       │ (wywołanie narzędzia)
                                ┌──────────────────────▼───────────────────────┐
                                │ DuDi.Core                                    │
                                │  CounterpartyVerifier: NIP → WL → KRS → ...  │
                                │  RiskScorer: reguły → werdykt + ustalenia    │
                                │  Nip, BankAccountNumber, IRegistrySource     │
                                └──────────────────────┬───────────────────────┘
                                                       │
                                ┌──────────────────────▼───────────────────────┐
                                │ DuDi.Sources                                 │
                                │  MockRegistrySource ← teraz (data/fixtures)  │
                                │  mappery WL / KRS / CRBR / sankcje UE        │
                                │  klienci HTTP ← etap 3–4                     │
                                └──────────────────────────────────────────────┘
```

Przykładowa tura w trybie `Tools`:

1. Użytkownik: „Sprawdź 925-575-73-34, rachunek z faktury 20 1240 1037 …”.
2. Model zgłasza wywołanie `sprawdz_kontrahenta(nip, numerRachunku)`.
3. Aplikacja waliduje NIP i rachunek (sumy kontrolne), a `CounterpartyVerifier` odpytuje źródła: Biała lista → numer KRS → odpis KRS → CRBR → lista sankcyjna (firma, zarząd, beneficjenci) → powiązania osobowe (jeden poziom w głąb).
4. `RiskScorer` liczy werdykt i ustalenia. `ContextBuilder` zamienia je na blok `<dane_kontrahenta>`.
5. Wynik narzędzia wraca do modelu, który tłumaczy go na prosty język według formatu z promptu systemowego.

## Najważniejsze decyzje

| Decyzja | Dlaczego |
|---|---|
| **Werdykt liczą reguły w C#, a LLM tylko go tłumaczy** | Wynik jest powtarzalny i testowalny (testy jednostkowe dla każdej reguły i scenariusza). Model nie „wymyśla” poziomu ryzyka, a na obronie łatwo pokazać, skąd wziął się werdykt. Gotowe zalecenia (`Finding.Advice`) ograniczają halucynacje małego modelu. |
| **Tool calling: 3 wąskie narzędzia** (`sprawdz_kontrahenta`, `sprawdz_rachunek`, `znajdz_powiazania_osoby`) | Model decyduje, *co* sprawdzić (np. sam sprawdza firmę powiązaną), ale nie *jak*: parametry to NIP, rachunek albo imię i nazwisko, nigdy URL. Mała liczba narzędzi jest łatwiejsza dla modelu 4B. Szczegółowe zapytania (WL, KRS, CRBR…) siedzą za jednym narzędziem, bo i tak zawsze idą razem. |
| **Dwa tryby kontekstu** (`Agent:ContextMode`) | `Tools` (domyślny): model sam wywołuje narzędzia. `Injected`: aplikacja rozpoznaje NIP regexem i dokleja dane. Drugi tryb służy do porównania „aplikacja decyduje” z „model decyduje” oraz jako fallback dla modeli bez tool callingu. |
| **Mock w formacie prawdziwych API** | Przejście na internet to nowa implementacja `IRegistrySource`, bez zmian w agencie i regułach. Testy `SampleParsingTests` dowodzą, że te same mappery czytają prawdziwe odpowiedzi. |
| **Błąd źródła ≠ „OK”** | Każdy wyjątek źródła daje status `Unavailable`, czyli punkt „NIE SPRAWDZONO” w raporcie i ostrzeżenie o niepełnych danych. Awarię można zasymulować przez `Data:Mock:UnavailableSources`. |
| **Pochodzenie każdego faktu** | Każde ustalenie i każdy punkt „Co sprawdziliśmy” ma źródło i datę. Prompt każe je cytować. |
| **`Microsoft.Extensions.AI` (`IChatClient`)** | Jedna abstrakcja dla Ollamy i API zgodnych z OpenAI (PCSS). Model i dostawcę zmienia się w `appsettings.json`. `UseFunctionInvocation` obsługuje pętlę tool callingu. Ten sam stos co w przykładach z zajęć (`sulmar/shelly`). |
| **Historia rozmowy w pamięci agenta** | Dopytania („dlaczego średnie?”) korzystają z wyników narzędzi zapisanych w historii. Przerwana tura nie trafia do historii. |
| **Prompt w osobnym pliku** (`src/DuDi.Agent/Prompts/system-prompt.md`) | Łatwo go czytać, porównywać wersje i iterować (`eval/prompt-iterations.md`). |

## Rozpoznanie API (spike, 2026-11-03)

| Źródło | Wynik rozpoznania | Wniosek dla etapów 3–4 |
|---|---|---|
| **Biała lista VAT** `wl-api.mf.gov.pl` | Działa bez klucza. `search/nip` zwraca nazwę, status VAT, **numer KRS** i rachunki (dla Orlenu 236 rachunków, wszystkie z poprawną sumą kontrolną). `check/nip/{nip}/bank-account/{nrb}` zwraca `TAK`/`NIE`. Niepoprawny NIP daje HTTP 400 `{"code":"WL-115"}`. Zapytania zbiorcze `search/nips`, `search/bank-accounts` (do 30 pozycji). | Klient HTTP + cache. Według dokumentacji MF obowiązują dzienne limity zapytań (nie testowaliśmy ich), więc tryb Mock zostaje domyślny na demo. |
| **KRS** `api-krs.ms.gov.pl/api/krs/OdpisAktualny/{krs}?rejestr=P&format=json` | Działa bez klucza. Nieistniejący numer daje 404. Dział 3 zawiera wzmianki o sprawozdaniach (`zaOkresOdDo`), dział 6 `postepowanieUpadlosciowe` (sprawdzone na spółce w upadłości). **Imiona, nazwiska i PESEL osób są zamaskowane** (`"F*****"`). | Łańcuch NIP → WL → KRS działa. **Powiązań osobowych i sankcji dla członków zarządu nie da się sprawdzić na publicznym API KRS** – potrzebne inne źródło (np. pełny odpis PDF lub komercyjne API) albo zawężenie funkcji. Struktury sekcji likwidacji jeszcze nie potwierdziliśmy (mapper sprawdza też dopisek „W LIKWIDACJI” w nazwie). |
| **Lista sankcyjna UE** | Pełny plik CSV (ok. 4,7 MB, ok. 16 tys. wierszy) jest dostępny pod publicznym adresem z tokenem z dokumentacji EU FSF. Separator `;`, wiele wierszy na podmiot. | Pobieranie okresowe + lokalny indeks. Parser `EuSanctionsCsv` czyta już prawdziwy format. Do zrobienia: dopasowanie rozmyte (odmiana, transliteracja). |
| **Lista MSWiA** | Strona gov.pl (HTML). | Do rozpoznania format (HTML/PDF). |
| **CRBR** | Wyszukiwarka `crbr.podatki.gov.pl` to aplikacja webowa. Nie znaleźliśmy publicznego API. | Plan B: format własny w mocku, a w raporcie link do ręcznej weryfikacji. |

## Ograniczenia (stan na etap 2)

- Dane pochodzą wyłącznie z symulowanej bazy (8 fikcyjnych firm).
- Dopasowanie nazwisk jest dokładne (bez diakrytyków, wielkości liter i kolejności słów), bez odmiany i transliteracji.
- Rok obrotowy jest przyjmowany jako kalendarzowy. Termin złożenia sprawozdania to 15 lipca (zatwierdzenie do 30 czerwca + 15 dni).
- Powiązania osobowe sprawdzamy jeden poziom w głąb i tylko dla zarządu (nie dla beneficjentów).
- W trybie `Injected` każdy 10-cyfrowy ciąg z poprawną sumą kontrolną jest traktowany jak NIP (także np. numer KRS).

## Co dalej

- **Etap 3:** klienci HTTP (`WhiteListClient`, `KrsClient`) za `IRegistrySource`, z `IHttpClientFactory`, resilience i cache. Obsługa błędów API (400/404/429).
- **Etap 4:** te same narzędzia jako serwer MCP „rejestry-pl” (C# SDK MCP). Agent jako klient MCP.
- **Etap 5:** RAG z przepisów (Biała lista – art. 96b VAT, art. 117ba Ordynacji, art. 15d CIT, CRBR, sankcje). Agent cytuje przepis przy zaleceniu.
- **Etap 7:** automatyczne uruchamianie `eval/cases.md`: werdykt (deterministycznie), wywołania narzędzi, brak halucynacji.
