# DuDi - asystent AI do weryfikacji kontrahentów

**Autorzy:**
- Barbara Gawlik
- Jakub Gronkiewicz
- Aleksandra Dmitruk

**Grupa:** 1

# O projekcie
Projekt obejmuje stworzenie agenta AI, który automatycznie przeprowadza wstępną weryfikację kontrahentów: po wpisaniu NIP-u zbiera dane z KRS, Białej listy VAT, CRBR i list sankcyjnych UE, a następnie przygotowuje raport ryzyka z uzasadnieniem opartym na przepisach.

Agent samodzielnie dobiera kolejne kroki analizy w zależności od tego, co znajdzie, a użytkownik może dopytać o podstawy oceny i zalecane działania. Rozwiązanie obejmuje polskie podmioty i wyłącznie bezpłatne, publiczne źródła. Raport wspiera decyzję użytkownika, ale jej nie podejmuje, i nie zastępuje oceny zdolności kredytowej, a także profesjonalnych usług wywiadowni gospodarczych.

## Stan projektu

Etap 2 (agent i model) wraz z tool callingiem na **symulowanej bazie danych**: działający czat w konsoli, trzy narzędzia rejestrów, ocena ryzyka liczona regułami. Dane to fikcyjne firmy zapisane w formacie prawdziwych API (Biała lista VAT, KRS). Połączenie z prawdziwymi rejestrami pojawi się w etapach 3–4.

### Wybór modelu

Domyślnym modelem jest **`DeepSeek-V3.1`** udostępniany przez PCSS (`https://llm.hpc.psnc.pl/v1`, API zgodne z OpenAI). W scenariuszach z [eval/cases.md](eval/cases.md) (T01–T03, P01, P08) odpowiadał zwięźle i poprawną polszczyzną, wywoływał właściwe narzędzie, nie halucynował i trzymał się formatu raportu. Testowany wcześniej `qwen3:4b` w tym samym scenariuszu T01 przechodził na angielski i dodawał zmyślone treści. Uzasadnienie: [documents/model-selection.md](documents/model-selection.md).

`qwen3:4b` w Ollamie zostaje jako opcja do pracy lokalnej bez dostępu do API.

## Jak uruchomić

### Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- klucz API do modeli PCSS (`llm.hpc.psnc.pl`)
- opcjonalnie [Ollama](https://ollama.com/download), jeśli chcesz uruchomić model lokalnie

### Krok po kroku (domyślnie: DeepSeek-V3.1 przez PCSS)

```bash
# 1. Ustaw klucz API w zmiennej środowiskowej (klucz nie trafia do repozytorium)
export LLM_API_KEY=<twój klucz>          # bash
$env:LLM_API_KEY = "<twój klucz>"        # PowerShell

# 2. Uruchom agenta (z katalogu głównego repozytorium)
dotnet run --project src/DuDi.Cli
```

Bez ustawionego `LLM_API_KEY` aplikacja wyświetli komunikat i zakończy działanie.

### Wariant lokalny (qwen3:4b w Ollamie)

```bash
# 1. Pobierz model (ok. 2,5 GB)
ollama pull qwen3:4b

# 2. Upewnij się, że Ollama działa (zwykle startuje sama; jeśli nie:)
ollama serve

# 3. Uruchom agenta z lokalnym modelem
dotnet run --project src/DuDi.Cli -- --Agent:Provider=Ollama --Agent:Endpoint=http://localhost:11434 --Agent:Model=qwen3:4b
```

Jeśli modelu nie ma lokalnie, aplikacja zaproponuje jego pobranie.

### Przykładowa rozmowa

```text
Ty> /firmy                                   ← lista firm w symulowanej bazie
Ty> Sprawdź firmę 948-812-73-53              ← firma bez zastrzeżeń
Ty> /nip 9255757334 20 1240 1037 9198 6621 8187 1482   ← rachunek spoza Białej listy
Ty> Co mam teraz zrobić?
Ty> Sprawdź 540-915-36-39, a potem powiązaną firmę
Ty> /reset
```

Komendy: `/nip <NIP> [rachunek]`, `/firmy`, `/reset`, `/pomoc`, `/wyjdz`. `Ctrl+C` przerywa bieżącą odpowiedź.

### Testy

```bash
dotnet test
```

Testy nie wymagają Ollamy. Sprawdzają walidację NIP i rachunków, reguły ryzyka, wszystkie scenariusze z symulowanej bazy, parsowanie prawdziwych odpowiedzi API (`data/samples`) i narzędzia agenta.

### Konfiguracja

Ustawienia są w `src/DuDi.Cli/appsettings.json`. Każde można nadpisać argumentem po `--` albo zmienną środowiskową:

```bash
# inny model u tego samego dostawcy
dotnet run --project src/DuDi.Cli -- --Agent:Model=<nazwa modelu w PCSS>

# tryb bez tool callingu: aplikacja sama dokleja dane kontrahenta do kontekstu
dotnet run --project src/DuDi.Cli -- --Agent:ContextMode=Injected

# symulowana awaria rejestru (WhiteList, Krs, Crbr, Sanctions, Relations)
dotnet run --project src/DuDi.Cli -- --Data:Mock:UnavailableSources:0=Crbr

# model lokalny w Ollamie
dotnet run --project src/DuDi.Cli -- --Agent:Provider=Ollama --Agent:Endpoint=http://localhost:11434 --Agent:Model=qwen3:4b
```

| Ustawienie | Domyślnie | Opis |
|---|---|---|
| `Agent:Provider` | `OpenAICompatible` | `OpenAICompatible` (PCSS i inne API zgodne z OpenAI) albo `Ollama`. |
| `Agent:Endpoint` | `https://llm.hpc.psnc.pl/v1` | Adres API. Dla Ollamy: `http://localhost:11434`. |
| `Agent:Model` | `DeepSeek-V3.1` | Model u dostawcy API lub w Ollamie. |
| `Agent:ApiKeyEnvironmentVariable` | `LLM_API_KEY` | Nazwa zmiennej środowiskowej z kluczem API. |
| `Agent:Temperature` | `0.2` | Niska, żeby odpowiedzi były powtarzalne. |
| `Agent:ContextWindow` | `16384` | `num_ctx`, używane tylko w Ollamie. |
| `Agent:Thinking` | `false` | Tryb „thinking” Qwen3, używany tylko w Ollamie. |
| `Agent:ContextMode` | `Tools` | `Tools`: model wywołuje narzędzia. `Injected`: aplikacja dokleja dane. |
| `Data:Source` | `Mock` | Na tym etapie tylko symulowana baza. |

## Struktura

```text
ai-credibility-scorer/
├── README.md
├── DuDi.slnx
├── src/
│   ├── DuDi.Core/       model domeny, walidacja NIP/NRB, IRegistrySource, reguły ryzyka, weryfikator
│   ├── DuDi.Sources/    symulowana baza + mappery formatów API (Biała lista, KRS, CRBR, sankcje UE)
│   ├── DuDi.Agent/      prompt systemowy, narzędzia, ContextBuilder, IChatClient
│   └── DuDi.Cli/        aplikacja konsolowa (czat)
├── tests/DuDi.Tests/    testy xUnit
├── data/
│   ├── fixtures/        symulowana baza (fikcyjne firmy) – opis scenariuszy w data/README.md
│   └── samples/         prawdziwe odpowiedzi API do testów parsowania
├── eval/                scenariusze ewaluacji i iteracje promptu
└── documents/
    ├── vision.md
    ├── plan-checkpoint-2.md
    ├── architecture.md      architektura, decyzje, wyniki rozpoznania API
    └── model-selection.md   wybór modelu i jego uzasadnienie
```
