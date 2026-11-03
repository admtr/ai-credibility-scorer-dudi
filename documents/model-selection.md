# Wybór modelu

## Decyzja

**`qwen3:4b` w lokalnej Ollamie**, z wyłączonym trybem „thinking”, temperaturą 0.2 i oknem kontekstu 16k tokenów.
Model docelowy po zwolnieniu zasobów to **`qwen3:8b`**. Zmiana to jedno ustawienie w konfiguracji (`Agent:Model`), bez zmian w kodzie.

## Wymagania projektu wobec modelu

| Wymaganie | Dlaczego |
|---|---|
| Dobry język polski | Odpowiedzi czyta księgowa lub zakupowiec. |
| Niezawodne tool calling | Od tego checkpointu agent sam wywołuje narzędzia rejestrów. |
| Trzymanie się danych z kontekstu | Zero halucynacji o konkretnych firmach. To kluczowe ryzyko projektu. |
| Okno kontekstu ≥ 16k | Dane z rejestrów + historia rozmowy + (od etapu 5) fragmenty przepisów. |
| Lokalnie i za darmo, na laptopie studenta | Wymóg przedmiotu (płatne modele nie są wymagane) i ograniczenia sprzętowe zespołu. |

## Porównanie z laboratorium 03

Na laboratorium 03 (eksperyment porównawczy modeli) najlepiej wypadł **`qwen3:8b`**.

> TODO (zespół): wkleić tabelę wyników z labu 03 albo link do raportu – liczby nie są tu przepisane, żeby nie przekłamać wyników.

## Dlaczego `qwen3:4b`, a nie zwycięski `qwen3:8b`

| Kryterium | `qwen3:4b` | `qwen3:8b` |
|---|---|---|
| Rozmiar w Ollamie (kwantyzacja Q4) | ok. 2,5 GB | ok. 5,2 GB |
| Pamięć do uruchomienia | mieści się na słabszych laptopach zespołu | wymaga więcej RAM/VRAM |
| Tool calling | tak (capability `tools`) | tak |
| Tryb „thinking” | tak, wyłączamy (`think: false`) | tak, wyłączamy |
| Okno kontekstu modelu | ≥ 32k (dokładnie: `ollama show qwen3:4b`) | ≥ 32k (`ollama show qwen3:8b`) |
| Jakość (lab 03) | słabsza od 8B | najlepsza w porównaniu |

Uzasadnienie: część zespołu nie ma miejsca na dysku ani pamięci na model 8B, a **demo musi działać na każdym komputerze w zespole**. Słabszą jakość mniejszego modelu kompensujemy architekturą:

1. **Werdykt i zalecenia liczy kod**, a nie model. Model nie musi „rozumować” o ryzyku, tylko przetłumaczyć gotowy, zwięzły raport (`ContextBuilder`) na prosty język.
2. **Mało narzędzi z wąskimi parametrami** (3 narzędzia, parametry: NIP / rachunek / imię i nazwisko). Mały model rzadziej się w nich gubi.
3. **Walidacja po stronie aplikacji.** Błędny NIP od modelu wraca jako komunikat, nie jako zapytanie do rejestru.
4. **Fallback `ContextMode=Injected`**: jeśli 4B zawodzi przy wywoływaniu narzędzi, aplikacja sama dokleja dane.

## Ustawienia (jawne)

| Ustawienie | Wartość | Dlaczego |
|---|---|---|
| `Temperature` | 0.2 | Powtarzalne, rzeczowe odpowiedzi. |
| `ContextWindow` (`num_ctx`) | 16384 | Domyślne okno Ollamy bywa małe. Wtedy model po cichu „zapomina” dane z początku rozmowy. |
| `Thinking` | `false` | W czacie liczy się czas odpowiedzi. Rozumowanie o ryzyku robi kod. |

Kod sam sprawdza na starcie (`/api/show`), czy model ma capability `tools` i `thinking`, i ustawia `think: false` tylko tam, gdzie ma to sens.

## Alternatywy

| Model | Gdzie | Uwagi |
|---|---|---|
| `qwen3:8b` | Ollama | Docelowy, gdy pozwoli sprzęt. Zwycięzca labu 03. |
| `gemma3:4b` | Ollama | Dobry polski jak na rozmiar, ale słabsze tool calling. |
| Bielik (SpeakLeash) | Ollama / PCSS | Najlepszy polski. Trzeba sprawdzić tool calling w aktualnej wersji. |
| Większy model przez PCSS (`llm.hpc.psnc.pl`) | API zgodne z OpenAI | Obsłużone w kodzie: `Agent:Provider=OpenAICompatible`, `Agent:Endpoint`, klucz w `LLM_API_KEY`. |

## Weryfikacja wyboru na naszych danych

Kryteria i scenariusze są w `eval/cases.md`. Po uruchomieniu agenta zespół wpisuje tu wyniki `qwen3:4b` (i dla porównania `qwen3:8b` na komputerze, który go uniesie):

| Model | Poprawne wywołanie narzędzi | Werdykt przekazany bez zmian | Brak halucynacji | Polszczyzna / zrozumiałość | Średni czas odpowiedzi |
|---|---|---|---|---|---|
| `qwen3:4b` | _do uzupełnienia_ | | | | |
| `qwen3:8b` | _do uzupełnienia_ | | | | |
