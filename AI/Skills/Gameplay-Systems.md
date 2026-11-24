# Skill — Game design e sistemi di gameplay

**Attivazione:** economia, commercio, inventario, bisogni, progressione, criminalità, polizia, gang, proprietà, interazioni e salvataggi.

**Fonti omgskills:** [game-developer](https://github.com/Jeffallan/claude-skills/tree/078a25cc5bfd8011459be7b91d004e900c1d02d3), [gameplay-implementation-review](https://github.com/fagemx/gstack-game/tree/7259ab9782fa9c17e45c16f1fb8347823ddb4379).

## Flusso operativo

1. Leggere GDD e guide correlate. Classificare ogni requisito come CONFERMATO, DIREZIONE, DECISIONE APERTA, PRESENTE, DA VERIFICARE o PIANIFICATO.
2. Descrivere il loop: obiettivo, input, costo, rischio, risultato, feedback e conseguenze.
3. Definire attori, stato autorevole, invarianti, casi limite, tempo e persistenza.
4. Cercare modelli/servizi esistenti: non creare secondo inventario, economia, clock o registry.
5. Mantenere le regole nel Domain, casi d'uso nel Gameplay e presentazione/input in Presentation.
6. Specificare casi limite pertinenti: fondi/stock insufficienti, ID sconosciuti, attore inattivo, save/load, cambio distretto e arresto.
7. Scrivere test deterministici per regole e transazioni; test di integrazione per flussi completi.
8. Aggiornare il GDD solo con approvazione dell'utente per nuove decisioni canoniche; aggiornare le guide per lo stato implementato.

## Focus UnderGrind

Sandbox urbano contemporaneo 3D low-poly sullo sviluppo di un'attività criminale di distribuzione e business. NPC, mercato, denaro, stock, relazioni, polizia, proprietà e gang devono sostenere il loop centrale e produrre conseguenze.

## Output

Regole verificabili, mappa dello stato, dipendenze riutilizzabili, decisioni aperte da sottoporre all'utente, criteri di accettazione e test.

**Accettazione:** nessun design drift, stato non duplicato, regole testabili, design futuro distinto dal codice corrente.
