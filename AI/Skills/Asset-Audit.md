# Skill — Audit degli asset

**Attivazione:** naming, formati, dimensioni, texture, mesh, clip, materiali, prefab, riferimenti e asset orfani.

**Fonte omgskills:** [asset-audit](https://github.com/Donchitos/Claude-Code-Game-Studios/tree/05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418).

## Procedura

1. Definire categorie e cartelle incluse/escluse.
2. Leggere convenzioni e pipeline esistenti.
3. Elencare file realmente accessibili e controllare metadati/riferimenti.
4. Misurare dimensioni e formati solo se i dati binari sono accessibili; altrimenti NON VALUTATO.
5. Cercare riferimenti da scene, prefab, ScriptableObject, Animator e codice.
6. Distinguere orfano reale da asset referenziato indirettamente o caricato via Resources.
7. Non cancellare, rinominare o sovrascrivere durante un audit read-only.
8. Consegnare findings con path, evidenza, impatto e intervento.

Se mancano dati o strumenti, usare **NON VALUTABILE — DATI ASSENTI**. Non stimare dimensioni o formati. Nessun risultato non equivale a PASS se l'area non è stata scansionata.

**Accettazione:** inventario riproducibile, misure reali, nessuna cancellazione accidentale, limiti dichiarati.
