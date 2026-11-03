# Plan prac – Checkpoint 2: „Agent i model”

> Projekt: **DuDi – asystent AI do weryfikacji kontrahentów**
> Przedmiot: Tworzenie agentów AI w .NET (PW MiNI, sem. zimowy 2026/2027)
> Wersja planu: 2026-11-02, zaktualizowana 2026-11-04

---

## Aktualizacja 2026-11-04 – zmiany względem pierwszej wersji

Plan zweryfikowano z repozytorium przedmiotu (`sulmar/pw-mini-ai-agents-dotnet`: `docs/grading.md`, `docs/project.md`, `docs/models.md`, laby 03–04). Zmiany:

1. **Tool calling wchodzi do tego checkpointu.** Na czwartek oddajemy połączone etapy 2 i 3: agent wywołuje narzędzia (`sprawdz_kontrahenta`, `sprawdz_rachunek`, `znajdz_powiazania_osoby`), ale **dane nadal pochodzą wyłącznie z symulowanej bazy**. Wstrzykiwanie kontekstu przez aplikację (sekcja 3.6) zostaje jako drugi tryb (`Agent:ContextMode=Injected`) do porównania.
2. **Model: `qwen3:4b`** zamiast `qwen3:8b`. Na labach najlepiej wypadł 8B, ale część zespołu nie ma na niego miejsca i pamięci. Uzasadnienie: `documents/model-selection.md`.
3. **Sekcja 1.2 była nieaktualna:** `vision.md` ma treść, a README ma wymagany nagłówek. Brakowało instrukcji uruchomienia (już jest). Pracujemy na forku, więc repozytorium dla prowadzącego ustalamy przy oddaniu.
4. **Spike API zrobiony** (wyniki w `documents/architecture.md`). Najważniejsze odkrycie: **publiczne API KRS maskuje imiona i nazwiska osób**, więc powiązań osobowych (scenariusz 5) i sankcji dla członków zarządu nie sprawdzimy na prawdziwym KRS bez innego źródła.
5. Solution w formacie `.slnx` (jak w przykładach prowadzącego), wersje pakietów takie jak w `sulmar/shelly` (`Microsoft.Extensions.AI` 10.11.0, `OllamaSharp` 5.5.0).

**Stan kroków z sekcji 4:**

| # | Krok | Stan |
|---|---|---|
| 0 | Porządki w repo | zrobione: `.gitignore`, `.editorconfig`, README z instrukcją, plan w `documents/` |
| 1 | Szkielet solution | zrobione: `DuDi.slnx`, 4 projekty + testy |
| 2 | Hello LLM, pętla czatu | zrobione: streaming, historia, sprawdzenie Ollamy i modelu na starcie, `/reset`, `/pomoc`, `/nip`, `/firmy` |
| 3 | Model domeny + walidator NIP | zrobione (+ walidator rachunku NRB) |
| 4 | Symulowana baza | zrobione: 8 firm + NIP spoza bazy, NIP-y, rachunki i numery KRS sprawdzone w prawdziwych API (`data/README.md`) |
| 5 | Spike API | zrobione: próbki w `data/samples`, wnioski w `documents/architecture.md` |
| 6 | RiskScorer v0 + testy | zrobione: testy dla każdego scenariusza |
| 7 | ContextBuilder | zrobione |
| 8 | Instrukcje + integracja | zrobione: prompt w `src/DuDi.Agent/Prompts/system-prompt.md`, tool calling + tryb `Injected` |
| 9 | Eksperyment z modelami | uzasadnienie gotowe, **wyniki z labu 03 i pomiary na naszych danych do wpisania** |
| 10 | Zalążek ewaluacji | zrobione: `eval/cases.md` (12 typowych + 9 problematycznych) |
| 11 | Iteracja promptu | v1 opisana w `eval/prompt-iterations.md`, **rundy do przeprowadzenia po uruchomieniu modelu** |
| 12 | Dokumentacja | zrobione |
| 13 | Próba generalna demo | **do zrobienia** na komputerze z Ollamą |

---

## 0. Podsumowanie

**Cel checkpointu:** działający w konsoli agent, z którym można porozmawiać po polsku o kontrahencie. Użytkownik podaje NIP, agent odpowiada prostym językiem: co wiadomo o firmie, co niepokoi i co warto zrobić. Na tym etapie dane pochodzą z **symulowanej bazy** (pliki JSON w formacie prawdziwych API), a nie z internetu.

**Kluczowe decyzje:**

| Obszar | Decyzja |
|---|---|
| Język / platforma | C#, .NET 10 (tak jak w materiałach kursu) |
| Dostęp do modelu | `Microsoft.Extensions.AI` (`IChatClient`) + `OllamaSharp`. Model można podmienić konfiguracją. |
| Model (start) | ~~`qwen3:8b`~~ **`qwen3:4b`** lokalnie w Ollamie (aktualizacja 2026-11-04: ograniczenia sprzętowe, uzasadnienie w `model-selection.md`). |
| Dane | Symulowana baza: pliki JSON z kilkoma fikcyjnymi firmami, każda ilustruje inny scenariusz ryzyka. Dostęp przez interfejsy, za które później podstawimy prawdziwe klienty HTTP. |
| Ocena ryzyka | **Liczona regułami w C#, nie przez LLM.** LLM tylko tłumaczy wynik i odpowiada na pytania. |
| Internet | Na ten checkpoint tylko rozpoznanie (spike) API Białej listy VAT i KRS. Pełna integracja to etapy 3–4. |

---

## 1. Gdzie jesteśmy

### 1.1 Etapy oceny projektu (z `docs/grading.md` repozytorium przedmiotu)

| # | Etap | Pkt | Status |
|---|---|---|---|
| 1 | Wizja projektu | 5 | gotowa (repo `ai-credibility-scorer-dudi`) |
| **2** | **Agent i model**: działający agent, dobór modelu, instrukcje i kontekst | **5** | **← ten plan** |
| 3 | Tool calling | 10 | **w tym checkpoincie** (narzędzia na symulowanej bazie) |
| 4 | MCP | 10 | – |
| 5 | RAG | 10 | – |
| 6 | Integracja rozwiązania | 10 | – |
| 7 | Ewaluacja | 10 | zaczynamy zbierać przypadki testowe już teraz |
| 8 | Prezentacja i obrona | 10 | – |

Na etapie 2 prowadzący ocenia: **uruchomienie agenta, świadomy dobór modelu, instrukcje agenta, odpowiedni kontekst i uzasadnienie decyzji**. Plan jest ułożony pod te punkty.

### 1.2 Stan repozytorium (zaktualizowano 2026-11-04)

- `documents/vision.md` ma treść wizji. Lista projektów prowadzącego linkuje do `admtr/ai-credibility-scorer-dudi`; pracujemy na forku, więc przy oddaniu trzeba upewnić się, że prowadzący ocenia repozytorium z aktualnym kodem.
- `README.md` ma wymagany nagłówek (nazwa projektu, **Autorzy:**, **Grupa:**) i instrukcję uruchomienia.
- Wymaganie przedmiotu: *„Historia repozytorium powinna odzwierciedlać rozwój projektu”*. Commitujemy małymi krokami, każdy członek zespołu ze swojego konta.

---

## 2. Cel i definicja „gotowe”

Checkpoint uznajemy za zaliczony, gdy:

1. `dotnet run --project src/DuDi.Cli` uruchamia czat w konsoli (przy działającej Ollamie).
2. Agent prowadzi rozmowę wieloturową (pamięta wcześniejsze wypowiedzi), odpowiada po polsku i strumieniuje tekst.
3. Po podaniu NIP-u z symulowanej bazy agent przedstawia **raport w prostych słowach**: werdykt, co sprawdzono, co niepokoi, co zrobić, skąd pochodzą dane.
4. Użytkownik może dopytać („dlaczego średnie ryzyko?”, „co mam zrobić, jeśli rachunku nie ma na Białej liście?”), a agent odpowiada na podstawie danych, **bez zmyślania**.
5. Dla nieznanego NIP-u lub pytania spoza zakresu agent odpowiada, że nie ma danych lub że to poza jego rolą, i nie wymyśla informacji.
5a. *(dodane 2026-11-04)* Agent sam wywołuje narzędzia na symulowanej bazie: po podaniu NIP-u `sprawdz_kontrahenta`, a przy powiązaniach sam sprawdza firmę powiązaną. Błędne argumenty i awarie źródeł obsługuje bez wyjątków.
6. W repozytorium jest dokument z uzasadnieniem wyboru modelu i wynikami krótkiego porównania (`documents/model-selection.md`).
7. README zawiera wymagany nagłówek i instrukcję uruchomienia.

---

## 3. Decyzje i ich uzasadnienie

### 3.1 Wybór modelu

**Wymagania naszego projektu wobec modelu:**

- dobry **język polski**: odpowiedzi są dla księgowej lub zakupowca, nie dla programisty;
- niezawodne **tool calling** (potrzebne od etapu 3, więc wybieramy model, którego nie trzeba będzie zmieniać);
- **trzymanie się danych z kontekstu**: zero halucynacji o konkretnych firmach;
- **duży kontekst**: dane z rejestrów + historia rozmowy + (później) fragmenty przepisów;
- działanie **lokalnie i za darmo** na laptopie studenta (wymóg: płatne modele nie są wymagane).

**Kandydaci:**

| Model | Gdzie | Za | Przeciw |
|---|---|---|---|
| `qwen3:8b` | Ollama, lokalnie | Bardzo dobre tool calling, przyzwoity polski, mieści się w ~6 GB VRAM/RAM, **już zainstalowany** | Tryb „thinking” spowalnia odpowiedzi, trzeba go wyłączyć (`think: false`) |
| `qwen3:4b` | Ollama | Szybki, dobry do iteracji nad promptem | Słabszy polski i rozumowanie |
| `gemma3:4b` | Ollama | Dobry polski jak na rozmiar | Słabsze, mniej przewidywalne tool calling |
| Bielik (SpeakLeash, np. v2.x/v3 Instruct) | Ollama / PCSS | Najlepszy polski, model polskiej społeczności | Trzeba sprawdzić jakość tool calling w aktualnej wersji |
| Większy model przez PCSS (`llm.hpc.psnc.pl`) | API (OpenAI-compatible) | Lepsza jakość bez kosztów | Zależność od dostępności usługi, trzeba sprawdzić zasady dostępu |

**Rekomendacja:** startujemy na `qwen3:8b`. Równolegle robimy **mały eksperyment porównawczy** (wzorowany na EXP-01 z labu 03):

- 3 modele: `qwen3:8b`, `gemma3:4b`, Bielik;
- 8–10 pytań na danych z symulowanej bazy (te same pytania i dane dla każdego modelu, temperatura 0.2);
- prosta skala 0–2 dla: poprawności faktów, poprawności polszczyzny, zrozumiałości dla laika, braku halucynacji; plus czas odpowiedzi;
- wynik i decyzja → `documents/model-selection.md`.

Dzięki abstrakcji `IChatClient` zmiana modelu lub dostawcy to zmiana w `appsettings.json`, nie w kodzie. To też argument na obronie.

**Ustawienia, które ustalamy jawnie:**

- `temperature` ≈ 0.2: chcemy powtarzalnych, rzeczowych odpowiedzi;
- `num_ctx`: **ustawić jawnie** (np. 16k). Domyślne okno kontekstu w Ollamie bywa małe i wtedy model po cichu „zapomina” dane z początku rozmowy;
- wyłączony tryb „thinking” w Qwen3 (szybsze odpowiedzi w czacie).

### 3.2 Stos technologiczny

| Element | Wybór | Dlaczego |
|---|---|---|
| Runtime | .NET 10 | Zgodnie z kursem |
| Abstrakcja LLM | `Microsoft.Extensions.AI` | Standard Microsoftu. Jeden interfejs dla Ollamy, PCSS (OpenAI-compatible) i innych dostawców. Wbudowane wsparcie dla tool calling (`UseFunctionInvocation`), przyda się w etapie 3. |
| Klient Ollamy | `OllamaSharp` | Implementuje `IChatClient` |
| Agent | Na etapie 2 własna, prosta pętla czatu na `IChatClient`. **Microsoft Agent Framework** (literatura kursu) rozważamy od etapu 3, przed wdrożeniem sprawdzamy aktualną wersję pakietów. | Nie dokładamy frameworka, zanim jest potrzebny, ale budujemy na tych samych abstrakcjach |
| Konfiguracja / DI | `Microsoft.Extensions.Hosting` | `appsettings.json`, zmienne środowiskowe, wstrzykiwanie zależności |
| UI konsoli | `Spectre.Console` (opcjonalnie) | Czytelne tabele i kolory w raporcie |
| Testy | xUnit | Testy reguł oceny ryzyka i parsowania danych |
| HTTP (od etapu 3) | `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` | Timeouty, ponowienia, typowane klienty |

### 3.3 Architektura

```text
┌──────────────┐   pytanie / NIP   ┌───────────────────────────────────────────┐
│ Użytkownik   │ ────────────────▶ │ DuDi.Cli (pętla czatu, historia, komendy) │
└──────────────┘ ◀──────────────── └──────────────────┬────────────────────────┘
                 odpowiedź (stream)                   │
                                   ┌──────────────────▼────────────────────────┐
                                   │ DuDi.Agent                                │
                                   │  • instrukcje systemowe (prompt)          │
                                   │  • ContextBuilder: fakty → zwięzły tekst  │
                                   │  • IChatClient (Ollama / PCSS)            │
                                   └───────┬───────────────────────┬───────────┘
                                           │                       │
                         ┌─────────────────▼─────┐      ┌──────────▼───────────┐
                         │ DuDi.Core             │      │ DuDi.Sources         │
                         │  • model domeny       │◀─────│  • Mock (JSON) ← teraz│
                         │  • RiskScorer (reguły)│      │  • Http (MF, KRS…)   │
                         └───────────────────────┘      │    ← etap 3          │
                                                        └──────────────────────┘
```

**Struktura solution:**

```text
src/
  DuDi.Core/        model domeny (CompanyProfile, VatStatus, Finding, RiskReport),
                    interfejsy źródeł, RiskScorer, walidator NIP
  DuDi.Sources/     MockRegistrySource (JSON), później WhiteListClient, KrsClient…
  DuDi.Agent/       prompty, ContextBuilder, konfiguracja IChatClient
  DuDi.Cli/         aplikacja konsolowa (czat)
tests/
  DuDi.Tests/       testy reguł, walidatora NIP, mapowania danych
data/
  fixtures/         symulowana baza (JSON)
eval/
  cases.md          scenariusze rozmów do ręcznej (a później automatycznej) oceny
documents/
  vision.md, plan-checkpoint-2.md, model-selection.md, architecture.md
```

Etap 4 (MCP) doda `DuDi.McpServer`, a etap 5 (RAG) doda `DuDi.Knowledge`. Struktura jest na to gotowa.

### 3.4 Symulowana baza danych

**Założenie:** fikcyjne dane zapisujemy **w formacie odpowiedzi prawdziwych API** (Biała lista VAT, KRS). Wtedy przejście na internet to podmiana implementacji interfejsu, bez zmian w agencie i regułach.

```csharp
public interface IRegistrySource
{
    Task<WhiteListResult?> GetVatSubjectAsync(Nip nip, DateOnly date, CancellationToken ct);
    Task<bool?> IsAccountOnWhiteListAsync(Nip nip, string iban, DateOnly date, CancellationToken ct);
    Task<KrsExtract?> GetKrsExtractAsync(string krsNumber, CancellationToken ct);
    Task<BeneficialOwners?> GetBeneficialOwnersAsync(Nip nip, CancellationToken ct); // CRBR
    Task<IReadOnlyList<SanctionHit>> CheckSanctionsAsync(string name, CancellationToken ct);
}
```

Wybór implementacji przez konfigurację: `"DataSource": "Mock" | "Live"`.

**Scenariusze w bazie** (6–8 firm, każda pokazuje inną sytuację):

| # | Firma (fikcyjna) | Co ilustruje | Oczekiwany werdykt |
|---|---|---|---|
| 1 | „Alfa Budownictwo sp. z o.o.” | Wszystko w porządku | Niskie ryzyko |
| 2 | „Beta Trans sp. z o.o.” | Rachunek z faktury **nie** jest na Białej liście | Wysokie: konsekwencje podatkowe |
| 3 | „Gamma Handel sp. z o.o.” | Brak sprawozdań finansowych za 2 ostatnie lata | Średnie |
| 4 | „Delta Logistics sp. z o.o.” | Spółka w likwidacji / ogłoszona upadłość (dział 6 KRS) | Wysokie |
| 5 | „Epsilon Consulting sp. z o.o.” | Członek zarządu zasiada w zarządzie firmy nr 4 (powiązania) | Średnie: materiał na etap 3 (agent sam sprawdza powiązaną firmę) |
| 6 | „Zeta Import sp. z o.o.” | Beneficjent rzeczywisty na liście sankcyjnej | Krytyczne: nie płacić, skonsultować |
| 7 | „Eta Usługi sp. z o.o.” | Status VAT „zwolniony”, firma zarejestrowana 3 miesiące temu | Informacyjne / niskie |
| 8 | — | NIP spoza bazy | Agent mówi, że nie ma danych |

**Zasady bezpieczeństwa danych testowych:**

- Nazwy i osoby są fikcyjne. W trybie `Mock` każda odpowiedź ma oznaczenie **[DANE TESTOWE]**.
- NIP-y generujemy z poprawną sumą kontrolną, a potem sprawdzamy jednym zapytaniem do Białej listy, że **nie należą do prawdziwej firmy**. Nie chcemy, żeby demo „oskarżało” istniejący podmiot.
- Próbki prawdziwych odpowiedzi API (np. dużej spółki giełdowej) zapisujemy osobno w `data/samples/`, tylko do testów parsowania.

### 3.5 Połączenie z internetem (rozpoznanie teraz, wdrożenie w etapach 3–4)

Sprawdziliśmy (2026-11-02), że API Białej listy i KRS odpowiadają bez klucza, a **Biała lista zwraca numer KRS**. Mamy więc naturalny łańcuch: **NIP → Biała lista → numer KRS → odpis KRS**.

| Źródło | Dostęp | Co daje | Uwagi |
|---|---|---|---|
| **Biała lista VAT** (MF) | REST `https://wl-api.mf.gov.pl`, bez klucza. Środowisko testowe: `wl-test.mf.gov.pl` | Nazwa, status VAT, adres, **numer KRS**, rachunki bankowe, sprawdzenie „NIP + rachunek” | **Dzienne limity zapytań** (szczególnie `search`), więc konieczny cache. Do sprawdzenia rachunku używamy endpointu `check`. |
| **KRS** (MS) | REST `https://api-krs.ms.gov.pl/api/krs/OdpisAktualny/{KRS}?rejestr=P&format=json` (też `OdpisPelny`) | Zarząd i prokurenci (dział 2), **wzmianki o złożonych sprawozdaniach finansowych** (dział 3), likwidacja / upadłość (dział 6) | Wymaga numeru KRS (bierzemy z Białej listy). Odpis jest duży, więc do modelu przekazujemy wyciąg, nie surowy JSON. |
| **CRBR** (MF) | Publiczna wyszukiwarka `crbr.podatki.gov.pl` | Beneficjenci rzeczywiści | **Do rozpoznania (spike):** nie potwierdziliśmy stabilnego publicznego API. Plan B: dane mock + link do ręcznej weryfikacji w raporcie. |
| **Listy sankcyjne UE** | Plik skonsolidowanej listy (EU Financial Sanctions Files, XML/CSV) + polska lista MSWiA | Trafienia po nazwie firmy / osoby | Pobieramy plik okresowo, indeksujemy lokalnie, dopasowanie „rozmyte” (odmiana nazwisk, transliteracja). |
| *(później)* KRZ | Krajowy Rejestr Zadłużonych | Postępowania upadłościowe / restrukturyzacyjne | Poza zakresem v1, kandydat na rozszerzenie |
| *(poza zakresem v1)* CEIDG | API z darmowym tokenem | Jednoosobowe działalności | Wizja skupia się na spółkach z KRS |

**Zasady techniczne dla warstwy internetowej:**

1. **Model nigdy sam nie buduje URL-i.** Ma do dyspozycji narzędzia z wąskimi, walidowanymi parametrami (`nip`, `iban`, `krs`). NIP sprawdzamy sumą kontrolną przed wywołaniem.
2. **Normalizacja:** surowe odpowiedzi API → model domeny (`CompanyProfile`), dopiero to idzie do reguł i do LLM.
3. **Pochodzenie każdego faktu:** każdy fakt ma źródło i datę pobrania, a agent je cytuje („wg Białej listy VAT z 2026-11-02…”).
4. **Odporność:** timeouty, ponowienia, cache (pamięć + plik) z czasem życia ok. 24 h. Brak odpowiedzi źródła to osobny wynik („nie udało się sprawdzić”), **nigdy „wszystko OK”**.
5. **Jak to się mapuje na etapy kursu:**
   - **Etap 3 (tool calling):** `sprawdz_biala_liste`, `sprawdz_rachunek`, `pobierz_odpis_krs`, `sprawdz_sankcje` jako funkcje dla agenta. Agent sam decyduje, co sprawdzić dalej (np. powiązana firma ze scenariusza 5).
   - **Etap 4 (MCP):** te same narzędzia wydzielone do własnego serwera MCP „rejestry-pl” (C# SDK MCP). Uzasadnienie: dostęp do rejestrów jest wielokrotnego użytku (np. z VS Code / Claude Desktop) i oddzielony od logiki agenta.
   - **Etap 5 (RAG):** baza wiedzy z przepisów i procedur (lista niżej). Agent uzasadnia ocenę, cytując konkretny przepis.
   - **Etap 7 (ewaluacja):** symulowana baza = powtarzalny zestaw testowy.

**Kandydaci do bazy wiedzy RAG (etap 5),** wybieramy to, co faktycznie przydaje się w odpowiedziach:

- ustawa o VAT, art. 96b (wykaz podatników VAT, czyli Biała lista);
- Ordynacja podatkowa, art. 117ba (solidarna odpowiedzialność przy płatności na rachunek spoza wykazu);
- ustawa o CIT, art. 15d / ustawa o PIT, art. 22p (wyłączenie z kosztów) oraz Prawo przedsiębiorców, art. 19 (próg 15 000 zł);
- ustawa o przeciwdziałaniu praniu pieniędzy i finansowaniu terroryzmu (CRBR);
- ustawa z 13 kwietnia 2022 r. (sankcje, lista MSWiA), rozporządzenia Rady (UE) 269/2014 i 833/2014;
- ustawa o rachunkowości (terminy złożenia sprawozdań do KRS);
- Metodyka MF w zakresie oceny dochowania należytej staranności (VAT).

### 3.6 Jak agent tłumaczy wyniki prostymi słowami

**Podział odpowiedzialności, najważniejsza decyzja architektoniczna:**

- **Kod (deterministycznie)** zbiera fakty i liczy ryzyko według jawnych reguł → `RiskReport` z listą ustaleń (`Finding`: kod, waga, opis, źródło, data).
- **LLM** zamienia `RiskReport` na zrozumiały tekst, odpowiada na pytania i (od etapu 5) dokłada uzasadnienie z przepisów.

Dlaczego tak: wynik oceny jest **powtarzalny i testowalny** (testy jednostkowe reguł). Model nie „wymyśla” poziomu ryzyka, a na obronie łatwo pokazać, skąd wziął się werdykt.

**Reguły v0** (do dopracowania):

| Ustalenie | Waga |
|---|---|
| Trafienie na liście sankcyjnej (firma, zarząd lub beneficjent) | KRYTYCZNA |
| Rachunek z faktury spoza Białej listy | WYSOKA |
| Podmiot nie figuruje na Białej liście / wykreślony z rejestru VAT | WYSOKA |
| Likwidacja lub upadłość (dział 6 KRS) | WYSOKA |
| Brak sprawozdania finansowego za ostatni rok po terminie | ŚREDNIA |
| Brak wpisu w CRBR | ŚREDNIA |
| Powiązanie osobowe z firmą o wysokim ryzyku | ŚREDNIA |
| Firma zarejestrowana < 12 mies. | NISKA (informacja) |
| Źródło niedostępne | brak oceny tego punktu, wyraźna informacja |

Werdykt ogólny = najwyższa waga wśród ustaleń (Niskie / Średnie / Wysokie / Krytyczne).

**Instrukcje systemowe (wersja robocza):**

```text
Jesteś DuDi, asystentem do wstępnej weryfikacji polskich kontrahentów.
Rozmawiasz z księgową lub pracownikiem działu zakupów – nie z prawnikiem ani programistą.

ZASADY
1. Opieraj się WYŁĄCZNIE na danych z sekcji <dane_kontrahenta>. Jeśli czegoś tam nie ma,
   powiedz wprost, że tego nie sprawdzono. Nigdy nie zgaduj danych firm.
2. Poziom ryzyka jest już wyliczony w danych – nie zmieniaj go, tylko wyjaśnij, z czego wynika.
3. Pisz po polsku, krótko, bez żargonu. Jeśli musisz użyć terminu (np. „Biała lista VAT”),
   wyjaśnij go jednym zdaniem.
4. Przy każdym fakcie podawaj źródło i datę sprawdzenia.
5. Nie podejmujesz decyzji za użytkownika i nie oceniasz zdolności kredytowej.
   Możesz zalecić konkretne kroki (np. poprosić o inny rachunek, sprawdzić CRBR ręcznie).
6. Pytania niezwiązane z weryfikacją kontrahentów grzecznie odrzucaj.

FORMAT PIERWSZEJ ODPOWIEDZI O FIRMIE
**Werdykt:** <poziom ryzyka> – jedno zdanie dlaczego.
**Co sprawdziliśmy:** lista punktów z wynikiem i źródłem.
**Co budzi wątpliwości:** tylko istotne ustalenia, od najważniejszego.
**Co zrobić:** 1–3 konkretne kroki.
Na końcu: „To wstępna weryfikacja na podstawie publicznych rejestrów, nie decyzja.”
```

**Kontekst na etapie 2 (bez tool callingu):** aplikacja rozpoznaje NIP w wiadomości użytkownika (regex + suma kontrolna) albo komendę `/nip 1234567890`, pobiera dane z symulowanej bazy, liczy `RiskReport` i dokleja go do kontekstu jako zwięzły blok `<dane_kontrahenta>`. W etapie 3 tę samą czynność **przejmie model przez narzędzia**. Na obronie dobrze pokażemy różnicę: „kontekst wstrzykiwany przez aplikację” kontra „model sam decyduje, co pobrać”.

**Przykład oczekiwanej odpowiedzi (scenariusz 2):**

> **Werdykt: WYSOKIE ryzyko.** Rachunek z faktury nie widnieje na Białej liście VAT tej firmy.
>
> **Co sprawdziliśmy:**
> - Firma istnieje i jest czynnym podatnikiem VAT (Biała lista VAT, 2026-11-02) ✅
> - Zarząd: Jan Kowalski, prezes (KRS, 2026-11-02) ✅
> - Sprawozdania finansowe złożone za 2024 i 2025 (KRS) ✅
> - Rachunek PL61 1090… **nie jest** na liście rachunków firmy ❌
>
> **Co zrobić:** poproś kontrahenta o fakturę z rachunkiem widniejącym na Białej liście. Przy płatności powyżej 15 000 zł na rachunek spoza listy możesz stracić prawo do zaliczenia wydatku w koszty i odpowiadać solidarnie za VAT kontrahenta.
>
> *To wstępna weryfikacja na podstawie publicznych rejestrów, nie decyzja. [DANE TESTOWE]*

---

## 4. Kroki do wykonania

Szacunek: **ok. 2 tygodnie dla 3 osób**. Role A/B/C zespół przydziela sam. Kroki z tej samej grupy można robić równolegle.

### Tydzień 1: fundamenty

| # | Zadanie | Kto | Wynik | Czas |
|---|---|---|---|---|
| 0 | **Porządki w repo:** ustalić oficjalne repo, przenieść treść wizji do `documents/vision.md`, nagłówek README (autorzy, grupa), `.gitignore` dla .NET, `.editorconfig` | A | repo zgodne z wymaganiami | 1 h |
| 1 | **Szkielet solution:** `DuDi.sln`, projekty z sekcji 3.3, referencje, `appsettings.json` (model, URL Ollamy, `DataSource`) | A | `dotnet build` przechodzi | 2 h |
| 2 | **Hello LLM:** `IChatClient` (OllamaSharp) + prosta pętla czatu ze strumieniowaniem i historią | A | można „porozmawiać” z modelem | 2–3 h |
| 3 | **Model domeny + walidator NIP** (`Nip` jako typ z sumą kontrolną), testy walidatora | B | `DuDi.Core` + testy | 3 h |
| 4 | **Symulowana baza:** 7–8 plików JSON w formacie odpowiedzi Białej listy i KRS (wg sekcji 3.4) + `MockRegistrySource` + wygenerowane i sprawdzone NIP-y | C | `data/fixtures/*.json` | 4–5 h |
| 5 | **Spike API:** mały program/test, który pobiera prawdziwe odpowiedzi Białej listy i KRS dla 2–3 firm i zapisuje je do `data/samples/`. Rozpoznanie CRBR i pliku sankcji UE (czy jest API, format, limity) | B | notatka w `documents/architecture.md` + próbki | 3 h |

### Tydzień 2: agent, model, dokumentacja

| # | Zadanie | Kto | Wynik | Czas |
|---|---|---|---|---|
| 6 | **RiskScorer v0** (reguły z sekcji 3.6) + testy jednostkowe dla każdego scenariusza z bazy | B | werdykty zgodne z tabelą scenariuszy | 4 h |
| 7 | **ContextBuilder:** `CompanyProfile` + `RiskReport` → zwięzły blok tekstu dla modelu (nie surowy JSON) | C | stała, czytelna struktura kontekstu | 2–3 h |
| 8 | **Instrukcje systemowe + integracja:** rozpoznawanie NIP / komendy `/nip`, `/reset`, `/pomoc`; wstrzykiwanie kontekstu; oznaczenie [DANE TESTOWE] | A | pełny przepływ rozmowy | 3–4 h |
| 9 | **Eksperyment z modelami** (sekcja 3.1) na scenariuszach z bazy, wypełnienie `documents/model-selection.md` | C | uzasadniony wybór modelu | 3–4 h |
| 10 | **Zalążek ewaluacji:** `eval/cases.md` z ~12 rozmowami (NIP → oczekiwany werdykt, pytania pogłębiające, pytania-pułapki: nieznany NIP, prośba spoza zakresu, próba wymuszenia zmiany werdyktu) | B | przypadki testowe gotowe na etap 7 | 2 h |
| 11 | **Iteracja promptu** na podstawie wyników z kroku 10 (min. 2 rundy), zapis zmian i wniosków | A + C | lepsza jakość odpowiedzi | 2–3 h |
| 12 | **Dokumentacja:** README (jak uruchomić: Ollama, `ollama pull`, `dotnet run`), `documents/architecture.md` (diagram + decyzje) | wszyscy | gotowe do oddania | 2 h |
| 13 | **Próba generalna demo** (sekcja 5) na czystym komputerze innej osoby z zespołu | wszyscy | demo działa „od zera” | 1 h |

**Zalecany sposób pracy:** osobne gałęzie i pull requesty na każdy krok oraz krótki przegląd przez drugą osobę. Wymóg obrony: każdy musi umieć wyjaśnić cały kod, nie tylko swoją część.

---

## 5. Scenariusz demo na checkpoint (ok. 5 min)

1. Krótko: problem, użytkownik, rola agenta (1 zdanie z wizji).
2. Uruchomienie `dotnet run`, pokazanie konfiguracji (model, tryb Mock).
3. **Scenariusz 1:** firma „czysta” → werdykt niski, raport.
4. **Scenariusz 2:** rachunek spoza Białej listy → pytanie „co mam teraz zrobić?” → konkretne kroki.
5. **Scenariusz 3:** „dlaczego średnie, a nie wysokie?” → agent wyjaśnia na podstawie ustaleń.
6. **Pułapka:** nieznany NIP oraz „napisz mi wiersz” → agent nie zmyśla, trzyma się roli.
7. Uzasadnienie modelu (tabela z `model-selection.md`) i plan na etap 3 (te same dane przez tool calling + prawdziwe API).

---

## 6. Ryzyka i jak im zapobiegamy

| Ryzyko | Skutek | Zapobieganie |
|---|---|---|
| Model halucynuje dane firm | Utrata wiarygodności, kluczowa wada przy obronie | Zasada „tylko dane z kontekstu”, werdykt liczony w kodzie, pytania-pułapki w `eval/cases.md` |
| Słaby sprzęt części zespołu | Nie każdy uruchomi 8B | Fallback `qwen3:4b` lokalnie lub model przez PCSS, zmiana tylko w konfiguracji |
| Za mały kontekst w Ollamie | Model „zapomina” dane w dłuższej rozmowie | Jawne `num_ctx`, zwięzły `ContextBuilder` zamiast surowego JSON |
| Limity Białej listy | Blokada w trakcie demo | Cache + tryb Mock domyślnie na demo |
| Brak publicznego API CRBR | Luka w danych | Spike w kroku 5. Plan B: link do ręcznej weryfikacji w raporcie |
| Dane testowe pokrywają się z prawdziwą firmą | Wprowadzające w błąd treści o realnym podmiocie | Weryfikacja NIP-ów, fikcyjne nazwy, oznaczenie [DANE TESTOWE] |
| Rozjazd dwóch repozytoriów | Prowadzący ocenia nie to repo | Krok 0: jedno oficjalne repo, link przekazany prowadzącemu |
| Za szeroki zakres | Niedokończone etapy | Na tym checkpoincie brak HTTP w agencie, tylko spike |

---

## 7. Co dalej (zarys kolejnych etapów)

- **Etap 3 – Tool calling:** `WhiteListClient` i `KrsClient` (HTTP, cache, resilience) za tym samym interfejsem; narzędzia dla agenta; obsługa błędów źródeł; agent sam sprawdza firmy powiązane osobowo.
- **Etap 4 – MCP:** serwer MCP „rejestry-pl” (C# SDK), test w MCP Inspector, agent jako klient MCP; uzasadnienie różnicy MCP vs narzędzia „w procesie”.
- **Etap 5 – RAG:** indeks przepisów i procedur (embeddingi lokalnie przez Ollamę), cytowanie przepisów w uzasadnieniu oceny.
- **Etap 6 – Integracja:** spójny przepływ od NIP-u do raportu, ewentualnie prosty interfejs webowy (Blazor) zamiast konsoli.
- **Etap 7 – Ewaluacja:** automatyczne uruchamianie `eval/cases`: zgodność werdyktu (deterministycznie), poprawność wywołań narzędzi, brak halucynacji, jakość wyjaśnień (LLM-as-judge + ocena ręczna na próbce).
- **Etap 8 – Obrona:** demo, architektura, decyzje, wyniki ewaluacji, ograniczenia.

---

## 8. Pytania do prowadzącego

1. Czy na etapie 2 wystarczy aplikacja konsolowa, czy oczekiwany jest interfejs webowy?
2. Czy ocena ryzyka liczona regułami (a LLM jako „tłumacz”) jest akceptowalnym podziałem ról agenta?
3. Czy dostęp do modeli PCSS (`llm.hpc.psnc.pl`) wymaga osobnej rejestracji zespołu i jakie modele są obecnie dostępne?
4. Które repozytorium ma być oficjalne (`ai-credibility-scorer` czy `ai-credibility-scorer-dudi`)?
