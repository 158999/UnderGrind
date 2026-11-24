# Skill — Level design e ambienti urbani

**Attivazione:** quartieri, strade, interni, edifici, percorsi, POI, blockout, illuminazione e storytelling ambientale.

**Fonte omgskills:** [level-design](https://github.com/pluginagentmarketplace/custom-plugin-game-developer/tree/aa7edfe267b34eac63d888f60b13e08aca7850ed).

## Procedura

1. Leggere GDD e guida della scena, inclusa `Gameplay_Test_Scene_Guide.md`.
2. Definire la funzione del luogo nel loop criminal-business: frequentatori, attività, rischi e opportunità.
3. Definire scala, flussi, accessi, landmark, linee di vista, coperture, densità e leggibilità.
4. Produrre prima un blockout: percorsi primari/secondari, spazi di interazione e test di navigazione.
5. Controllare scala rispetto al player, collisioni, porte, finestre, marciapiedi e larghezza dei percorsi.
6. Coordinare geometria e NavMesh: collider, superfici, ostacoli, links, aree e agent settings.
7. Pianificare luce, palette e landmark coerenti con il sandbox urbano contemporaneo 3D low-poly.
8. Verificare la scena con percorso player e NPC; uno screenshot statico non basta.
9. Documentare coordinate, hierarchy, prefab, layer, collider e bake solo dopo aver verificato i transform dei parent.

## Output

Mappa funzionale, blockout e dimensioni, prefab/materiali/collider/layer, flussi player/NPC e checklist visiva/PlayMode.

**Accettazione:** luogo leggibile, navigabile, coerente col GDD e utile al gameplay; non basta che sia esteticamente gradevole.
