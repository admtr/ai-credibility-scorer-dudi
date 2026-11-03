# Handoff – checkpoint na czwartek 2026-11-05

> Stan na 2026-11-04. Checkpoint łączy etap 2 (agent i model) z tool callingiem. Agent działa na **symulowanej bazie danych**, bez prawdziwych rejestrów.

**Ważne:** kodu jeszcze nikt nie skompilował ani nie uruchomił. Na laptopie, na którym powstał, nie ma .NET SDK ani Ollamy. Kod jest napisany według źródeł OllamaSharp 5.5.0 i przykładów prowadzącego (`sulmar/shelly`), z tymi samymi wersjami pakietów. Pierwszy `dotnet build` może jednak pokazać drobne błędy. Zmiany nie są jeszcze zacommitowane.

## Co powstało

- **Działający szkielet agenta:** czat w konsoli z historią rozmowy i strumieniowaniem odpowiedzi, model `qwen3:4b` w Ollamie. Na starcie aplikacja sprawdza, czy Ollama działa i czy model jest pobrany. Komendy: `/nip`, `/firmy`, `/reset`, `/pomoc`, `/wyjdz`.
- **Tool calling:** 3 narzędzia (`sprawdz_kontrahenta`, `sprawdz_rachunek`, `znajdz_powiazania_osoby`) w `src/DuDi.Agent/Tools/RegistryTools.cs`. Błędne dane od modelu (np. NIP z literówką) aplikacja odrzuca i odsyła mu czytelny komunikat zamiast wyjątku.
- **Drugi tryb pracy** (`Agent:ContextMode=Injected`): aplikacja sama znajduje NIP w wiadomości i dokleja dane firmy, bez tool callingu. Służy do pokazania na obronie różnicy „aplikacja decyduje” vs „model decyduje”.
- **Ocena ryzyka liczona regułami w C#** (`src/DuDi.Core/Risk/RiskScorer.cs`), a nie przez model. Model tylko tłumaczy wynik na prosty język. Każde ustalenie ma źródło, datę i gotowe zalecenie. Jeśli rejestr nie odpowie, raport pokazuje „NIE SPRAWDZONO”, a nie „wszystko OK”. Awarię można zasymulować w konfiguracji (`Data:Mock:UnavailableSources`).
- **Mock-baza** (`data/fixtures`): 8 fikcyjnych firm, każda ilustruje inny scenariusz, plus jeden NIP spoza bazy. Pliki mają format prawdziwych API Białej listy i KRS. NIP-y, rachunki i numery KRS sprawdziliśmy w prawdziwych API: żaden nie należy do istniejącej firmy. Tabela scenariuszy z NIP-ami i oczekiwanymi werdyktami: `data/README.md`.
- **Testy xUnit** (`tests/DuDi.Tests`, nie potrzebują Ollamy): walidacja NIP i rachunków, reguły, każdy scenariusz z bazy, parsowanie prawdziwych odpowiedzi API (`data/samples`) i wywoływanie narzędzi tak, jak robi to model.
- **Dokumentacja:**
  - `README.md`: instrukcja uruchomienia i konfiguracji,
  - `documents/architecture.md`: architektura, decyzje, wyniki rozpoznania API,
  - `documents/model-selection.md`: dlaczego `qwen3:4b`,
  - `documents/plan-checkpoint-2.md`: plan z sekcją aktualizacji i stanem każdego kroku,
  - `eval/cases.md`: 12 typowych i 9 problematycznych scenariuszy,
  - `eval/prompt-iterations.md`: historia zmian promptu.
- **Prompt systemowy:** `src/DuDi.Agent/Prompts/system-prompt.md`.

## Rozpoznanie API: jedna ważna rzecz

**Publiczne API KRS ukrywa imiona i nazwiska członków zarządu** (zwraca np. `"F*****"`). Na prawdziwych danych nie da się więc wykryć powiązań osobowych (scenariusz Epsilon ↔ Delta) ani sprawdzić członków zarządu na liście sankcyjnej. W mocku to działa, bo dane są fikcyjne. Do etapu 3 trzeba będzie znaleźć inne źródło albo ograniczyć tę funkcję. Warto to powiedzieć na checkpoincie jako świadome ograniczenie.

Pozostałe ustalenia: Biała lista i KRS działają bez klucza, a lista sankcyjna UE jest do pobrania jako CSV. Publicznego API CRBR nie znaleźliśmy. Szczegóły: `documents/architecture.md`.

## Do zrobienia przed czwartkiem

1. **Zbudować i uruchomić** na komputerze z [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) i [Ollamą](https://ollama.com/download):

   ```bash
   dotnet build
   dotnet test
   ollama pull qwen3:4b
   dotnet run --project src/DuDi.Cli
   ```

   Jeśli kompilacja zgłosi błędy, należy je poprawić (spodziewamy się najwyżej drobnych).
2. **Uzupełnić `documents/model-selection.md`** o wyniki porównania modeli z labu 03 (miejsce oznaczone TODO). Liczby celowo nie zostały wpisane z pamięci.
3. **Przejść scenariusze z `eval/cases.md`** na `qwen3:4b` i wpisać wyniki w tabeli na końcu pliku. Najtrudniejszy dla małego modelu jest T08: agent ma sam sprawdzić firmę powiązaną. Jeśli coś nie działa, poprawić prompt i zapisać zmianę w `eval/prompt-iterations.md`.
4. **Zrobić próbę demo** według sekcji 5 planu (`documents/plan-checkpoint-2.md`), najlepiej na komputerze innej osoby niż ta, która budowała.
5. **Ustalić, które repo pokazujemy prowadzącemu.** Jego lista projektów wskazuje na `admtr/ai-credibility-scorer-dudi`, a kod powstał na forku `allesgrau/ai-credibility-scorer`.
6. **Commity:** zmiany czekają niezacommitowane. Wymóg przedmiotu mówi, że historia ma odzwierciedlać rozwój projektu, więc warto je podzielić na kilka commitów z kont członków zespołu.
