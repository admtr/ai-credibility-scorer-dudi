# DuDi - asystent AI do weryfikacji kontrahentów

**Autorzy:**
- Barbara Gawlik
- Jakub Gronkiewicz
- Aleksandra Dmitruk

**Grupa:** 1

# O projekcie
Projekt obejmuje stworzenia agenta AI, który automatycznie przeprowadza wstępną weryfikację kontrahentów: po wpisaniu NIP-u zbiera dane z KRS, Białej listy VAT, CRBR i list sankcyjnych UE, a następnie przygotowuje raport ryzyka z uzasadnieniem opartym na przepisach.

Agent samodzielnie dobiera kolejne kroki analizy w zależności od tego, co znajdzie, a użytkownik może dopytać o podstawy oceny i zalecane działania. Rozwiązanie obejmuje polskie podmioty i wyłącznie bezpłatne, publiczne źródła. Raport wspiera decyzję użytkownika, ale jej nie podejmuje, i nie zastępuje oceny zdolności kredytowej, a także profesjonalnych usług wywiadowni gospodarczych.

## Struktura

```text
ai-credibility-scorer-dudi/
├── README.md
├── documents/
│   └── vision.md
└── src/
```
