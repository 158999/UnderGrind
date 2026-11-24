# Skill — UI/UX di gioco

**Attivazione:** HUD, inventario, dialoghi, contrattazione, menu, prompt interazione, input e accessibilità.

**Fonte omgskills:** [game-ui-design](https://github.com/omer-metin/skills-for-antigravity/tree/e8dcf4e8737921a10088bd5c9eb65e81f74c051f).

## Procedura

1. Leggere `Documentation/Inventory.md`, guida scena e componenti UI esistenti.
2. Identificare informazione necessaria e momento in cui deve essere visibile.
3. Collegare azioni e dati ai servizi/domain corretti; la UI non possiede denaro, inventario, stock o regole.
4. Riutilizzare Input Actions e binding già presenti.
5. Progettare gerarchia visiva, priorità, feedback, stati vuoti/errori, conferme, focus e navigazione.
6. Verificare leggibilità su risoluzioni/aspect ratio pertinenti e non coprire il playfield senza motivo.
7. Verificare tastiera/mouse e controller quando supportato; focus, Escape, cursor lock, pausa e ripristino input.
8. Testare flusso completo: apri, seleziona, modifica, conferma, annulla, chiudi, riapri.
9. Aggiungere test di stato/interazione e PlayMode per input; screenshot/Game View per la qualità visiva.
10. Documentare hierarchy, prefab, componenti, eventi, Input Actions e stati.

**Accettazione:** informazioni leggibili, flusso completo, input ripristinato, UI sincronizzata col Domain. Non dichiarare responsive/controller-ready/accessibile senza test specifici.
