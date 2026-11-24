# Skill — NPC, simulazione e navigazione

**Attivazione:** comportamento NPC, schedule, routine, percorsi, NavMesh, path planner, popolazione, LOD e simulazione economica.

**Fonte omgskills:** [Unity AI Navigation — initialize-ai-navigation](https://github.com/unity-technologies/skills/tree/b7cc0c0e8648bef8ac64bce550aa39b21dcc9565).

## Fonti locali

Leggere [NPC Architecture and Next Steps](https://github.com/158999/unity-context/blob/main/Documentation/NPC_Architecture_and_Next_Steps.md), Systems Guide, Source Inventory, scena `testScene.unity`, fixture, prefab, schedule asset, planner e test.

## Procedura

1. Individuare `NPCState` e il proprietario del comportamento.
2. Mantenere stato/regole indipendenti da Unity; GameObject e Transform sono rappresentazioni.
3. Riutilizzare registry, regole, driver e contratti esistenti.
4. Mantenere `INPCPathPlanner` nel confine applicativo/Domain e NavMesh in Presentation.
5. Distinguere schedule, scelta attività, path planning, movimento e presentazione.
6. Definire condizioni di stop: stato NPC, arresto, orario, destinazione, path invalid, agent disabilitato, cambio scena.
7. Verificare bake, area mask, agent radius/height, links, obstacles, path status, corner e destinazione.
8. Test deterministici per regole; PlayMode per raggiungibilità, avoidance e movimento.
9. Verificare attivazione/disattivazione e teardown.
10. Non dichiarare simulazione urbana completa sulla base di pochi NPC fixture o del solo LOD.

**Accettazione:** stato unico, planner disaccoppiato, percorsi raggiungibili, ostacoli gestiti e comportamento osservato in PlayMode. Indicare scena e numero NPC testati.
