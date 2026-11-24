# Skill — Documentazione tecnica e XML C#

**Attivazione:** commenti XML, Doxygen, guide sistema, README, architettura, changelog e onboarding.

**Fonte omgskills:** [code-documentation](https://github.com/bytedance/deer-flow/tree/4889f61f1d7e6b1ba131edaccde83ddbbf576b31).

## Procedura

1. Leggere il codice reale prima di descriverlo.
2. Distinguere comportamento corrente, design futuro e stato da verificare.
3. Documentare assembly, ownership, dipendenze, flussi, input/output, invarianti e lifecycle.
4. Per API pubbliche documentare summary, parametri, ritorni, eccezioni reali e mutazioni.
5. Commentare callback Unity e metodi privati quando lifecycle o side effect non sono ovvi.
6. Usare Mermaid per flussi/dipendenze se migliora la comprensione.
7. Aggiornare guide esistenti invece di duplicarle; mantenere l'italiano.
8. Verificare link, percorsi, nomi e comandi sul repository.
9. Non modificare manualmente output Doxygen generati: intervenire sulle sorgenti/configurazioni.

**Accettazione:** ogni affermazione è verificabile o marcata come design/roadmap; esempi compatibili con la versione reale.
