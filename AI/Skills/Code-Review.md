# Skill — Code review e revisione PR

**Attivazione:** revisione commit/branch/PR, audit codice, prima del merge.

**Fonti omgskills:** [code-review](https://github.com/mattpocock/skills/tree/59db609eb1e18aaf59b63284d0e52255aff0c63c), [gameplay-implementation-review](https://github.com/fagemx/gstack-game/tree/7259ab9782fa9c17e45c16f1fb8347823ddb4379).

## Due assi

**Conformità tecnica:** dipendenze assembly, ownership, lifecycle, nullability, eventi, serializzazione, API, performance, test, XML docs.

**Intento di design:** rispetto del GDD e del loop principale, decisioni aperte, conseguenze per player/NPC/economia, separazione UI/domain, criteri verificabili.

## Procedura

1. Confrontare base/head effettivi e leggere il diff completo.
2. Leggere brief/issue e documentazione correlata.
3. Seguire i dati attraverso tutte le assembly coinvolte.
4. Controllare test e se dimostrano davvero il comportamento.
5. Segnalare problemi concreti con file/metodo/linea, condizione, impatto e correzione.
6. Separare blocker, rischio e suggerimento.
7. Non approvare sulla base di test non eseguiti o screenshot assenti.

Priorità: BLOCKER, HIGH, MEDIUM, LOW. Se non ci sono finding, dichiarare il perimetro revisionato e le verifiche mancanti.

**Accettazione:** nessun finding inventato, diff completo, design e codice valutati separatamente.
