# UnderGrind — istruzioni operative per ChatGPT

**Destinatario:** istanze ChatGPT standard che operano sul repository tramite plugin GitHub. Nessuna CLI AI.

## Fonti obbligatorie

Prima di progettare o implementare:
1. [GDD canonico](https://github.com/158999/unity-context/blob/main/Documentation/UnderGrind_Game_Design_Document.md): identità, visione, gameplay, regole e decisioni aperte.
2. [Piano e riferimento tecnico](https://github.com/158999/unity-context/blob/main/Documentation/UnderGrind_Tutorial_Piano_e_Riferimento.txt): roadmap, architettura, metodo e criteri di completamento.
3. [Architecture](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Architecture.md), [Systems Guide](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Systems-Guide.md), [Developer Guide](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Developer-Guide.md), [Source Inventory](https://github.com/158999/unity-context/blob/main/Documentation/Technical/Source-Inventory.md).
4. [XML Documentation Standard](https://github.com/158999/unity-context/blob/main/Documentation/Technical/XML-Documentation-Standard.md).
5. Guide pertinenti in `Documentation/` del repository di gioco, poi sorgenti, asmdef, asset, scene, prefab, test e workflow del branch corrente.

**Gerarchia:** GDD = design canonico; guide tecniche = architettura e metodo; repository UnderGrind = stato implementato. Una feature descritta nel GDD non è automaticamente implementata.

## Baseline da ricontrollare nel branch

- Unity `6000.6.9f1`, URP `17.6.0`, Input System `1.20.1`, Test Framework `1.8.0`, uGUI `2.6.0`.
- Assembly runtime: Core, Domain, Gameplay, Infrastructure, Presentation.
- Domain resta indipendente da Unity; Presentation collega input/oggetti Unity ai contratti e visualizza i risultati.
- Player: `HumanLocomotionController` e `PlayerLocomotionSettings.asset`.
- Inventario: `PlayerInventorySettings.asset`.
- Pipeline personaggi: sorgenti originali maschile/femminile preservate; due rig e sei mesh per personaggio (Body, Head, TP Arms, TP Hands, FP Arms, FP Hands).
- Runner test esistenti: standalone .NET, Unity EditMode, PlayMode. Export Blender: `Tools/Blender/run-export.ps1`.

## Regole non negoziabili

- Non inventare decisioni di design aperte.
- Distinguere COMPLETATO, PRESENTE DA VERIFICARE, IN CORSO, PIANIFICATO e DEPRECATO.
- Cercare prima il proprietario dello stato; niente manager generici o stato duplicato.
- Non spostare economia, inventario o simulazione nei MonoBehaviour.
- Non duplicare input o parametri già posseduti da ScriptableObject.
- Non introdurre package senza verificarne compatibilità con la versione Unity effettiva.
- Denaro di gioco con `decimal`.
- Evitare allocazioni per frame, lookup ripetuti e Instantiate/Destroy frequenti senza motivazione misurata.
- XML docs specifici e verificabili; aggiornare la documentazione italiana.
- Non sovrascrivere sorgenti, clip, scene, prefab o asset senza autorizzazione.
- Non dichiarare test o verifiche visive mai eseguiti.

## GitHub e verifica

Lavorare su branch dedicato, niente force-push o riscrittura di main. Leggere prima i file e test coinvolti, fare modifiche piccole, aggiornare test e docs, controllare il diff completo e aprire PR con limiti espliciti.

Comandi per lo sviluppatore:
- `Tools/Testing/run-tests.ps1`
- `Tools/Testing/run-unity-tests.ps1 -Platform EditMode|PlayMode|All`
- `Tools/Testing/run-all-tests.ps1`
- `Tools/Blender/run-export.ps1`

ChatGPT via GitHub non ha eseguito questi comandi solo perché li ha letti o suggeriti. Separare sempre analisi statica, test source, test realmente eseguiti, compilazione Unity, EditMode, PlayMode, export Blender e verifica visiva. Fasi non eseguite = NON ESEGUITE; dati assenti = NON VALUTABILE.

Scegliere solo la skill pertinente dall'[indice](SKILL-INDEX.md), non caricarle tutte indiscriminatamente.
