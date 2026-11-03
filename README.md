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

Etap 2 (agent i model) wraz z tool callingiem na **symulowanej bazie danych**: działający czat w konsoli, model `qwen3:4b` w Ollamie, trzy narzędzia rejestrów, ocena ryzyka liczona regułami. Dane to fikcyjne firmy zapisane w formacie prawdziwych API (Biała lista VAT, KRS). Połączenie z prawdziwymi rejestrami pojawi się w etapach 3–4.

## Jak uruchomić

### Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Ollama](https://ollama.com/download)

### Krok po kroku

```bash
# 1. Pobierz model (ok. 2,5 GB)
ollama pull qwen3:4b

# 2. Upewnij się, że Ollama działa (zwykle startuje sama; jeśli nie:)
ollama serve

# 3. Uruchom agenta (z katalogu głównego repozytorium)
dotnet run --project src/DuDi.Cli
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
# inny model
dotnet run --project src/DuDi.Cli -- --Agent:Model=qwen3:8b

# tryb bez tool callingu: aplikacja sama dokleja dane kontrahenta do kontekstu
dotnet run --project src/DuDi.Cli -- --Agent:ContextMode=Injected

# symulowana awaria rejestru (WhiteList, Krs, Crbr, Sanctions, Relations)
dotnet run --project src/DuDi.Cli -- --Data:Mock:UnavailableSources:0=Crbr

# model przez API zgodne z OpenAI (np. PCSS); klucz w zmiennej LLM_API_KEY
dotnet run --project src/DuDi.Cli -- --Agent:Provider=OpenAICompatible --Agent:Endpoint=https://llm.hpc.psnc.pl/v1 --Agent:Model=<nazwa>
```

| Ustawienie | Domyślnie | Opis |
|---|---|---|
| `Agent:Model` | `qwen3:4b` | Model w Ollamie lub u dostawcy API. |
| `Agent:Temperature` | `0.2` | Niska, żeby odpowiedzi były powtarzalne. |
| `Agent:ContextWindow` | `16384` | `num_ctx` w Ollamie. |
| `Agent:Thinking` | `false` | Tryb „thinking” Qwen3. |
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
