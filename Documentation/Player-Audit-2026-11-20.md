# Audit tecnico UnderGrind — 20 novembre 2026

## Riferimento

- Repository: `158999/UnderGrind`
- Branch: `main`
- Unity richiesto dal progetto: `6000.6.9f1`
- Pacchetti: Input System `1.20.1`, URP `17.6.0`, Test Framework `1.8.0`, uGUI `2.6.0`.
- Sorgente umanoide versionata: `Assets/UnderGrind/Art/Source/humans.blend`, SHA-256 `e74b80b77a8b7a9cfe607d2106de6a35bb65117548c91d587e6a77df0310e981`.
- FBX esportati e versionati: `Human_Male_Rigged.fbx` (209.468 byte) e `Human_Female_Rigged.fbx` (212.300 byte).
- HEAD esaminato nell'aggiornamento: `28425af47f6ecf13f7486048367a71ec76ef0962` (`Strengthen humanoid skeleton binding tests`).

## Stato dei test

L'ultimo report locale versionato è `TestResults/test/all-latest.json`, run `20261120-095537-299`, aggiornato il 20 novembre 2026 alle 09:56 (ora locale).

| Suite | Risultato XML/TRX | Esito aggregato |
|---|---:|---|
| Diagnostics standalone | 6/6 superati | Passed |
| Unity EditMode | 33/33 superati | Report valido, ma runner segnato Failed |
| Unity PlayMode | 13/13 superati | Report valido, ma runner segnato Failed |

La run `20261120-095537-299` riporta stato aggregato `Passed`, exit code standalone 0 e Unity 0. I 46 test Unity sono validi: 33 EditMode e 13 PlayMode, senza errori di compilazione, timeout, test falliti o inconclusive. I 6 test standalone Diagnostics sono anch'essi superati. Totale: 52 test superati su 52. Il precedente report `20261120-091314-434` era `Failed` per l'acquisizione errata del codice di uscita Unity; la correzione dei runner `run-unity-tests.ps1` e `run-all-tests.ps1` è ora verificata da questa nuova esecuzione locale.

La pipeline è stata estesa con `Tools/Testing/test-process-runner.ps1`, che avvia un processo figlio con exit code noto (23) e verifica acquisizione di stdout e stderr. La run CI `44707616452` sul commit `741d059fd69df3566ab88f4a9c44aad436ea894a` è conclusa con successo: parsing di tutti gli script PowerShell in `Tools/Testing` e `Tools/Blender`, smoke test dei processi sotto Windows PowerShell 5.1 e test standalone .NET superati. La CI **non avvia Unity**; i 46 test Unity sono stati eseguiti localmente con Unity `6000.6.9f1` e sono riportati nei file versionati. Il runner Blender è stato verificato sintatticamente, ma non eseguito con Blender in questo ambiente.

## Stato delle fondamenta del Player

| Area | Implementato nel repository | Limite verificato |
|---|---|---|
| Locomozione | Controller, ScriptableObject, input, camera, postura, salto e test | Nessuna verifica visiva sul personaggio definitivo |
| Input | Input System, rebinding, persistenza degli override | Manca una schermata UI dedicata al rebinding |
| Inventario | Slot, stack, peso, sovraccarico, UI, raccolta e rilascio | Mancano salvataggio persistente, contenitori, equipaggiamento e uso oggetti |
| Interazione | SphereCast dalla camera e pickup/rilascio | Non verificata nella scena di gioco definitiva |
| Rig | Due armature Blender con 22 ossa ciascuna; 12 mesh separate con parent, modificatori e pesi verificati; due FBX esportati e versionati | Avatar Humanoid Unity, mapping e prefab non ancora verificati |
| Animazione | Settings, driver e builder Animator | Nessuna clip FBX importata e nessun Animator Controller generato versionato |
| Scena | Scena LocomotionTest e builder con validazione prefab | La scena versionata contiene ancora geometria proxy; non è una prova del personaggio riggato |
| CI | Build e test standalone .NET, parsing degli script PowerShell | Nessun test Unity automatico nella CI corrente |

## Personaggi e pipeline 3D

Il progetto Blendstra `UnderGrind — Humans Source Separation`, versione `version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`, contiene:

- due armature distinte: `UG_Male_Armature` e `UG_Female_Armature`;
- sei mesh separate per personaggio: Body, Head, TP Arms, TP Hands, FP Arms e FP Hands;
- le mesh originali complete maschile e femminile conservate come sorgenti.

Il 20 novembre è stato eseguito un audit Python direttamente in Blender sulla versione corrente pulita `version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`. Entrambe le armature (`UG_Male_Armature` e `UG_Female_Armature`) contengono 22 ossa, 21 delle quali deformanti; tutte le 18 ossa richieste dal controllo Humanoid sono presenti. Per ciascun personaggio sono state verificate le sei mesh separate (Body, Head, TP Arms/Hands, FP Arms/Hands): parent diretto all'armatura corretta, modificatore Armature collegato, 21 gruppi deformanti, tutti i vertici con almeno un peso deformante valido, zero vertici non pesati e geometria non vuota. Totale: 12/12 mesh superate. Il confronto strutturale con la versione base non registra modifiche alle mesh; la geometria interna delle armature resta fuori dalla copertura dell'ispettore, ma i dati delle ossa e dei pesi sono stati letti direttamente dallo script Blender. L'audit ha prodotto una versione tecnica temporanea con marker, poi rimossa dalla testa del progetto: la versione corrente è stata ripristinata alla versione pulita `f438fed3`.

La verifica certifica struttura e pesi, non la qualità visiva delle deformazioni, l'export FBX o la validità degli Avatar Unity. Lo script `Tools/Blender/export-undergrind-humans.py` effettua i controlli fail-fast prima dell'export. È stato aggiunto `Tools/Blender/run-export.ps1` per avviare Blender in background, acquisire gli stream e verificare report e file FBX; non è stato ancora eseguito perché in questo ambiente non è disponibile un eseguibile Blender locale. I due FBX sono ora presenti in `Assets/UnderGrind/Art/Source/Rigged/`: `Human_Male_Rigged.fbx` (209.468 byte) e `Human_Female_Rigged.fbx` (212.300 byte). Il report Blender registra 22 ossa e sei mesh non vuote per ciascun personaggio. Il report è versionato in `Tools/Blender/last-export-report.json`. L'esportazione è quindi completata; non sono ancora stati importati e verificati in Unity.

## Animazioni selezionate

Sono stati acquisiti dalla libreria gratuita PINOC otto asset per la locomozione di base. Gli ID e la distinzione dei formati sono registrati in `Documentation/Player-Animation-Asset-Intake-2026-11-20.md`.

Per il retarget del personaggio UnderGrind è previsto il formato `mixamo-fbx` (mesh X Bot e scheletro con nomenclatura Mixamo), non il formato `splat-skeleton-fbx` (scheletro MetaHuman a 441 ossa, destinato alla pipeline Splat Engine). Gli asset sono acquisiti nella libreria PINOC, ma non sono ancora stati scaricati nel repository, importati in Unity o verificati sul rig UnderGrind. I link di download PINOC sono firmati e temporanei: non devono essere salvati nella documentazione.

Il builder Animator si aspetta le clip in `Assets/UnderGrind/Animations`, le configura secondo `PlayerAnimationSettings.asset` e genera `PlayerAnimator.controller` quando sono disponibili le sei clip fondamentali: Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk. Fall e Landing sono opzionali.

## Priorità tecniche

1. **Validare i due Avatar:** importare i due FBX come Humanoid, ottenere Avatar validi e distinti e verificare il mapping delle ossa.
2. **Generare e controllare i prefab:** eseguire `Build Male and Female Prefabs From Configured FBX`; controllare skinning, bind pose, testa, corpo, arti FP e arti TP e deformazioni delle articolazioni.
3. **Integrare le animazioni:** scaricare i clip Mixamo-FBX selezionati, importarli come Humanoid, verificare il retarget e generare il controller.
4. **Integrare il Player nella scena:** rigenerare la scena soltanto dopo la validazione dei prefab; provare prima/terza persona, visuale verso il basso, movimento, salto e accovacciamento.
5. **Versionare i metadati Unity:** dopo l'importazione, controllare e versionare i `.meta` generati per i due FBX e per gli asset prodotti.
6. **Aumentare la copertura d'integrazione:** aggiungere test che carichino i prefab reali e verifichino Avatar, renderer e controller; i test con GameObject costruiti artificialmente non sostituiscono questa verifica.
7. **Completare l'input UI:** realizzare una schermata di rimappatura che usi il controller esistente e i relativi parametri configurabili.

## Limiti della verifica

In questa sessione sono stati ispezionati repository, report, log e sorgenti; è stato eseguito un audit Blender CPU sul rig. Non è stato possibile eseguire Unity Editor o una build standalone del gioco nell'ambiente di lavoro corrente. Nessuna verifica visiva delle deformazioni, degli Avatar Unity o del gameplay reale viene quindi dichiarata superata.

## Aggiornamento dopo i push del 20 novembre

HEAD corrente al momento dell'aggiornamento: `28425af47f6ecf13f7486048367a71ec76ef0962`.

Sono stati aggiunti `Assets/UnderGrind/Tests/Editor/HumanoidRigImportTests.cs` e il relativo file `.meta`. La nuova suite EditMode carica i due FBX versionati e verifica importazione Humanoid, Avatar distinti e validi, mapping delle ossa principali, presenza dell'armatura configurata, sei `SkinnedMeshRenderer`, bind pose, root bone e appartenenza delle ossa all'armatura. Verifica inoltre che `PlayerVisualRig` riconosca i sei gruppi di renderer configurati.

La suite è stata aggiunta dopo l'ultima esecuzione locale registrata (47 test Unity superati: 34 EditMode e 13 PlayMode). **I nuovi test di importazione non sono compresi in quel risultato e non sono ancora stati eseguiti in Unity.** La workflow GitHub Actions continua a eseguire soltanto Standalone Diagnostics; non certifica la compilazione Unity, gli Avatar FBX o i nuovi test EditMode.

La documentazione di `Assets/UnderGrind/Animations/README.md` è stata allineata a `PlayerAnimationSettings.GetMissingRequiredClipNames()`: Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono obbligatorie; Fall e Land sono opzionali.


## Diagnosi dei risultati caricati il 20 novembre alle 11:22

Il commit dell'esecuzione ha caricato un report Unity completo. La run `20261120-112221-901` riporta:

| Suite | Superati | Falliti | Totale |
|---|---:|---:|---:|
| Standalone Diagnostics | 6 | 0 | 6 |
| Unity EditMode | 35 | 3 | 38 |
| Unity PlayMode | 13 | 0 | 13 |

I tre fallimenti EditMode sono:

1. `ImportedFbxHasValidHumanoidAvatarAndSkinnedParts` per il modello maschile: il test si aspettava `LegacyImport` mentre Unity ha restituito `ImportViaMaterialDescription`.
2. Lo stesso test per il modello femminile, con lo stesso disallineamento.
3. `MaleAndFemaleFbxUseDistinctHumanoidAvatars`: l'Avatar maschile non è valido.

I log Unity precisano i problemi di mapping Humanoid: nel modello maschile manca il transform richiesto `RightHand`; nel modello femminile manca `LeftLowerLeg`. Non sono errori di compilazione C# né errori del processo di test.

La causa dei primi due fallimenti è un'aspettativa errata nel test e nel builder: `PlayerRigSettings.ImportMaterials` è un booleano, mentre l'importer FBX corrente usa `ImportViaMaterialDescription`. Il valore `materialImportMode: 2` nei due `.meta` corrisponde a questa modalità. Il test e il builder sono stati allineati a tale modalità.

Il terzo problema resta un difetto reale della configurazione Humanoid esportata: la presenza dei nomi delle ossa nel report Blender non dimostra che Unity riesca a mapparli come transform Humanoid validi. Non è stato corretto né deve essere dichiarato risolto finché una nuova esecuzione Unity non conferma Avatar validi per entrambi i personaggi.

## Correzione del launcher di test

`TESTA.bat` avviava prima `run-tests.ps1` e poi `run-unity-tests.ps1` separatamente. Entrambi gli script, se avviati senza `-ReuseCurrentRun`, ruotano `TestResults/test` in `TestResults/test old`; il secondo avvio spostava quindi i report standalone appena prodotti. Inoltre il batch non propagava l'exit code dei test.

Il launcher ora richiama esclusivamente `run-all-tests.ps1`, che crea una sola cartella di esecuzione e passa `-ReuseCurrentRun` a entrambe le fasi. Il batch conserva e restituisce il codice di uscita aggregato, così una suite fallita non viene mascherata da un completamento apparentemente riuscito.

La correzione del launcher è stata applicata al branch `main`. La GitHub Action continua a verificare standalone e tooling, ma non esegue Unity: il report Unity resta generato dalla macchina locale tramite il launcher batch.


## Aggiornamento Humanoid e risultati più recenti — 20 novembre 2026

Il report locale più recente disponibile è la run `20261120-131605-907`:

| Suite | Risultato |
|---|---:|
| Diagnostics standalone | 6/6 superati |
| Unity EditMode | Non eseguito con report valido |
| Unity PlayMode | Non eseguito con report valido |

Unity è terminato con codice 1 in entrambe le piattaforme e non ha prodotto XML nuovo e valido; i totali Unity sono zero, senza diagnostica del compilatore e senza test conteggiati. Questa run non dimostra né una compilazione Unity fallita né test Unity superati. Nello screenshot delle 13:15 l'Editor Unity è aperto sul progetto mentre viene tentato il runner batchmode: la coincidenza rende plausibile un conflitto con l'istanza già aperta, ma il report non ne identifica direttamente la causa.

La precedente run valida per Unity, `20261120-123305-368`, aveva 34/38 EditMode superati, 4 falliti e 13/13 PlayMode superati. Tutti e quattro i fallimenti EditMode si fermavano nel `SetUp` perché Unity non trovava il transform Humanoid richiesto `RightHand` nel modello maschile. Le schermate successive mostrano `RightHand` nella gerarchia e nel mapping dell'Avatar, ma riportano ancora l'avviso `Character is not in T pose`. Le schermate sono un riscontro visivo dello stato dell'Editor, non una nuova esecuzione dei test né la prova che gli FBX versionati siano stati aggiornati.

### Correzione del mantenimento della posa

`HumanoidAvatarImporter.EnsureValidAvatar` è stato modificato per non cancellare automaticamente una `HumanDescription` manuale che produce già un Avatar Humanoid valido. Questo consente di mantenere le correzioni effettuate nella finestra Avatar Configuration, inclusa **Pose → Enforce T-Pose**. Il fallback al mapping automatico gerarchico avviene soltanto quando la descrizione manuale produce un Avatar invalido.

È stato aggiunto il test EditMode `EnsureValidAvatarPreservesValidManualPoseCorrections`, che applica al ModelImporter la descrizione di un Avatar valido, richiama l'importer UnderGrind e verifica che gli array di mapping e skeleton restino presenti. Il test non è ancora stato eseguito in Unity; il comportamento non è quindi dichiarato verificato.

### Prossimo passaggio di verifica

1. Nella configurazione Avatar di ciascun FBX, applicare **Pose → Enforce T-Pose** e controllare che l'avviso scompaia; premere **Apply** e **Done**.
2. Chiudere l'istanza grafica di Unity che usa il progetto.
3. Eseguire `TESTA.bat` e conservare il nuovo report completo.
4. Verificare che entrambi gli Avatar siano validi, distinti e che i test EditMode/PlayMode siano tutti superati.

La GitHub Action `44734606590` sul commit `875862b75c75048cb936306febd1daa8e195e18b` è conclusa con successo, ma esegue soltanto i test standalone e la validazione sintattica degli strumenti. Non verifica la compilazione Unity, la configurazione T-pose o i test EditMode/PlayMode.


## Follow-up dopo il push `fail` — 20 novembre 2026

Run Unity locale `20261120-132751-816` / Unity `20261120-132802-174`:

| Suite | Totale | Superati | Falliti |
|---|---:|---:|---:|
| Diagnostics standalone | 6 | 6 | 0 |
| Unity EditMode | 39 | 34 | 5 |
| Unity PlayMode | 13 | 13 | 0 |

I cinque fallimenti EditMode si verificano tutti nel `SetUp` di `HumanoidRigImportTests`: `HumanoidAvatarImporter.EnsureValidAvatar` restituisce false per il modello maschile. Il log Unity segnala `Rig Error: Invalid Avatar Rig Configuration` e `Required human bone 'RightHand' not found`. Il codice di uscita EditMode è 2; non sono riportati errori del compilatore. Il report complessivo è Failed perché EditMode è fallito, non perché Diagnostics o PlayMode siano falliti.

La meta FBX versionata mostra `animationType: 3`, `avatarSetup: 1`, `autoGenerateAvatarMappingIfUnspecified: 1` e array `humanDescription.human/skeleton` vuoti. L'Avatar non viene quindi risolto dal mapping automatico attuale. Il precedente importer fallback non ha risolto il problema: eliminare una descrizione manuale invalida e chiedere a Unity di mappare automaticamente non corregge un transform assente o una gerarchia incompatibile nell'FBX.

Il report `Tools/Blender/last-export-report.json` indica 22 ossa, ma non contiene la lista delle ossa né i parent; inoltre il formato è più vecchio rispetto ai campi che lo script Blender attuale produce per UV e materiali. Non va usato come prova della struttura del FBX importato da Unity.

### Modifiche diagnostiche successive

- `HumanoidAvatarImporter.cs`: quando Unity non genera un Avatar valido, registra mapping, skeleton e tutti i percorsi Transform effettivamente importati. Questo è un diagnostico, non una correzione dichiarata del rig.
- `Tools/Blender/export-undergrind-humans.py`: controlla che le ossa Humanoid richieste rispettino anche la catena degli antenati e include `boneHierarchy` nel report con nome, parent, `useDeform`, coordinate locali head/tail.
- La documentazione Humanoid registra i risultati effettivi e distingue gli errori di Avatar dai problemi geometrici.
- Il log Unity segnala un poligono auto-intersecante scartato da `Human_Male_Body`. La geometria sorgente deve essere corretta e riesportata separatamente dal mapping Humanoid.

Le nuove diagnostiche non sono ancora state eseguite con Unity o Blender. La GitHub Action associata al commit `cc19ac4c` è verde, ma esegue solo i test standalone: non compila gli assembly Unity e non esegue i test EditMode/PlayMode. Le Actions avviate dai commit diagnostici successivi risultano ancora in corso/in attesa al momento della registrazione.

### Passi necessari per una correzione verificabile

1. Eseguire `Tools/Blender/export-undergrind-humans.py` con Blender sul file sorgente corrente e controllare il nuovo `boneHierarchy` di entrambi i personaggi.
2. Se il controllo della catena fallisce, correggere la parentela nell'armatura sorgente; se passa, usare i percorsi Transform registrati da Unity per trovare l'eventuale differenza tra nomi Blender e nomi FBX importati.
3. Correggere il poligono auto-intersecante nella mesh sorgente, quindi esportare nuovamente entrambi gli FBX.
4. Reimportare in Unity, controllare Avatar Configuration e T-pose, quindi eseguire nuovamente `TESTA.bat`.
5. Accettare il rig soltanto dopo Avatar validi per entrambi i modelli, EditMode e PlayMode superati e controllo visivo delle deformazioni.

