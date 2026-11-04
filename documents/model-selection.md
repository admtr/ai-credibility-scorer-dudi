# Wybór modelu

## Decyzja

**`DeepSeek-V3.1` udostępniany przez PCSS** (`https://llm.hpc.psnc.pl/v1`, API zgodne z OpenAI), z temperaturą 0.2.
Wariant lokalny do pracy bez dostępu do API to **`qwen3:4b` w Ollamie**. Zmiana modelu lub dostawcy to kilka ustawień w konfiguracji (`Agent:Provider`, `Agent:Endpoint`, `Agent:Model`), bez zmian w kodzie.

## Wymagania projektu wobec modelu

| Wymaganie | Dlaczego |
|---|---|
| Dobry język polski | Odpowiedzi czyta księgowa lub zakupowiec. |
| Niezawodne tool calling | Od tego checkpointu agent sam wywołuje narzędzia rejestrów. |
| Trzymanie się danych z kontekstu | Zero halucynacji o konkretnych firmach. To kluczowe ryzyko projektu. |
| Okno kontekstu ≥ 16k | Dane z rejestrów + historia rozmowy + (od etapu 5) fragmenty przepisów. |
| Bez kosztów dla zespołu | Wymóg przedmiotu (płatne modele nie są wymagane). Modele PCSS są dostępne dla uczelni bezpłatnie. |

## Jak wybieraliśmy

Testy modeli w tym projekcie **nie były rozległe** i robiliśmy to celowo. Mieliśmy już doświadczenie z wyboru modelu z laboratorium 03, gdzie porównywaliśmy modele lokalne i dostępne przez PCSS. Wiedzieliśmy z niego, że jeśli mały lokalny Qwen nie poradzi sobie z naszym zadaniem, najlepszym wyborem spośród dostępnych modeli będzie DeepSeek.

Dlatego sprawdziliśmy tylko, czy ta zależność potwierdza się na naszych danych:

1. Najpierw uruchomiliśmy agenta na `qwen3:4b` (lokalnie, najtańszy wariant). W scenariuszu T01 model przechodził na angielski, krążył wokół niepodanego rachunku bankowego i dodawał zmyślone treści. To dyskwalifikuje go jako model domyślny, bo halucynacje o firmach to największe ryzyko projektu.
2. Następnie uruchomiliśmy te same scenariusze na `DeepSeek-V3.1`. Odpowiadał po polsku, zwięźle, wywoływał właściwe narzędzie i trzymał się formatu raportu.

Większego porównania (więcej modeli i wszystkie scenariusze) nie robiliśmy, bo wynik był zgodny z oczekiwaniami z labu 03. Pełniejszą ewaluację planujemy w etapie 7.

## Wyniki na naszych danych

Szczegóły w `eval/cases.md` (sekcja „Wyniki”), prompt v1.

| Model | Scenariusze | Poprawne wywołanie narzędzi | Brak halucynacji (0 = brak) | Język (0–2) | Format (0–2) |
|---|---|---|---|---|---|
| `qwen3:4b` (Ollama) | T01 | tak | 2 (zmyślone treści) | 0 (angielski) | 1 |
| `DeepSeek-V3.1` (PCSS) | T01, T02, T03, P01, P08 | tak | 0 we wszystkich | 2 we wszystkich | 2 we wszystkich |

Pozostałe scenariusze z `eval/cases.md` pokażemy na żywo podczas prezentacji.

## Dlaczego DeepSeek, a nie model lokalny

| Kryterium | `qwen3:4b` (Ollama) | `DeepSeek-V3.1` (PCSS) |
|---|---|---|
| Jakość polszczyzny | słaba, przechodzi na angielski | dobra |
| Halucynacje w naszych testach | tak | nie |
| Tool calling | działa | działa |
| Wymagania sprzętowe | ok. 2,5 GB, działa na laptopach zespołu | brak, model działa po stronie PCSS |
| Zależność od sieci | nie | tak, potrzebny internet i klucz API |

Główny minus DeepSeeka to zależność od dostępności usługi PCSS. Dlatego wariant lokalny zostaje w konfiguracji jako zapasowy. Słabszą jakość małego modelu częściowo kompensuje architektura:

1. **Werdykt i zalecenia liczy kod**, a nie model. Model tylko tłumaczy gotowy, zwięzły raport (`ContextBuilder`) na prosty język.
2. **Mało narzędzi z wąskimi parametrami** (3 narzędzia, parametry: NIP / rachunek / imię i nazwisko).
3. **Walidacja po stronie aplikacji.** Błędny NIP od modelu wraca jako komunikat, nie jako zapytanie do rejestru.
4. **Tryb `ContextMode=Injected`**: jeśli model zawodzi przy wywoływaniu narzędzi, aplikacja sama dokleja dane.

## Ustawienia (jawne)

| Ustawienie | Wartość | Dlaczego |
|---|---|---|
| `Temperature` | 0.2 | Powtarzalne, rzeczowe odpowiedzi. |
| `ContextWindow` (`num_ctx`) | 16384 | Tylko Ollama. Domyślne okno Ollamy bywa małe i wtedy model po cichu „zapomina” dane z początku rozmowy. |
| `Thinking` | `false` | Tylko Ollama (Qwen3). W czacie liczy się czas odpowiedzi, a rozumowanie o ryzyku robi kod. |

Klucz API czytamy ze zmiennej środowiskowej `LLM_API_KEY`, żeby nie trafił do repozytorium. Dla Ollamy kod sam sprawdza na starcie (`/api/show`), czy model ma capability `tools` i `thinking`, i ustawia `think: false` tylko tam, gdzie ma to sens.

## Alternatywy

| Model | Gdzie | Uwagi |
|---|---|---|
| `qwen3:4b` | Ollama | Wariant lokalny, zapasowy. Słaby polski i halucynacje w naszych testach. |
| `qwen3:8b` | Ollama | Lepszy od 4B, ale części zespołu brakuje na niego pamięci. |
| `gemma3:4b` | Ollama | Dobry polski jak na rozmiar, ale słabsze tool calling. |
| Bielik (SpeakLeash) | Ollama / PCSS | Najlepszy polski. Trzeba by sprawdzić tool calling w aktualnej wersji. |
