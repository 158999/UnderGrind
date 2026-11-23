# Pipeline di preparazione dei modelli umanoidi

## Modelli sorgente e conservazione

Il progetto Blendstra [UnderGrind — Humans Source Separation](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3) contiene le sorgenti maschile e femminile:

- `Human_Male`: altezza modellata 1,78 m; 260 vertici e 486 triangoli.
- `Human_Female`: altezza modellata 1,70 m; 284 vertici e 529 triangoli.

Le due mesh originali devono rimanere conservate. Le modifiche di rigging e le varianti sono derivate e non devono sostituire le sorgenti.

L'esportazione di verifica non copre la configurazione dei modificatori e le coordinate UV valutate. Questi aspetti non sono certificati dai conteggi geometrici.

## Stato del prototipo di rig

La sorgente corrente è la versione Blendstra [`version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3&version=version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6). L'ispezione registra 16 oggetti:

- Due armature: `UG_Male_Armature` e `UG_Female_Armature`.
- Sei mesh derivate per personaggio: corpo, testa, braccia e mani dedicate alla prima e alla terza persona.
- Le due mesh sorgenti originali `Human_Male` e `Human_Female`, conservate e nascoste dal rendering.

I nomi delle mesh derivate corrispondono ai token di `PlayerRigSettings.asset`: `_Body`, `_Head`, `_TP_Arms`, `_TP_Hands`, `_FP_Arms` e `_FP_Hands`. La separazione testa/corpo è stata eseguita con un piano al 78% dell'altezza del bounding box; braccia e mani sono state estratte usando i gruppi di pesi delle ossa. Non si tratta di una separazione manuale dei poligoni.

Lo script che ha generato questa versione crea una gerarchia di 22 ossa per personaggio e tenta il weight painting automatico di Blender, con distribuzione per prossimità come fallback. L'ispezione sorgente non espone la configurazione completa dei modificatori né i pesi effettivi: la presenza degli oggetti non dimostra da sola che le deformazioni siano corrette.

`Tools/Blender/export-undergrind-humans.py` esegue i controlli direttamente in Blender prima dell'esportazione: ossa Humanoid richieste, sei mesh, parent corretti, modificatore Armature, gruppi di pesi collegati alle ossa deformanti e almeno un peso deformante valido per ogni vertice. Valida entrambi i personaggi prima di esportare e produce due FBX separati. I percorsi provengono da `PlayerRigSettings.asset` o, se l'asset non esiste ancora, dal template JSON.

Il controllo delle ossa verifica anche la catena degli antenati Humanoid (ad esempio `RightHand` discendente da `RightLowerArm`) e registra nel report `boneHierarchy` nome, parent, flag deform e coordinate locali head/tail di ogni osso. Il conteggio totale delle ossa non è sufficiente a dimostrare che Unity possa risolvere il mapping.



La sorgente nativa della versione Blendstra è stata scaricata e versionata in `Assets/UnderGrind/Art/Source/humans.blend`. Il file corrisponde alla sorgente riggata verificata (SHA-256 `e74b80b77a8b7a9cfe607d2106de6a35bb65117548c91d587e6a77df0310e981`).

Le versioni `version_9333bec316691d50b66236e6ed6e172d9dbb5921f28563f3d3ae9b2b96ff0591`, `version_b8a60164bd0c3dbcb97cb79bf78d03069c370fc7c28abb093cd4025fbaf1a662` e il progetto separato `UnderGrind — Rigged Human Derivatives` sono iterazioni precedenti. La versione corrente con le 16 parti è la sorgente da esportare.

La sorgente corrente non contiene canali di animazione. Le clip locomotion vengono importate separatamente da PINOC. L'export FBX è stato eseguito e i due file sono versionati: `Human_Male_Rigged.fbx` (209.468 byte) e `Human_Female_Rigged.fbx` (212.300 byte). Il report `Tools/Blender/last-export-report.json` registra per entrambi 22 ossa e sei mesh non vuote (Body, Head, TP Arms/Hands, FP Arms/Hands). Questa verifica certifica l'export Blender, non l'importazione Unity o la qualità visiva delle deformazioni.

## Orientamento del modello

L'ispezione del modello sorgente nella vista frontale del viewer mostra il personaggio di profilo: il suo asse frontale è `-X`, mentre il controller Player usa `+Z` come forward. Questa differenza spiega la silhouette stretta osservata quando il prefab viene guardato lungo il forward del Player: Avatar, ossa e renderer possono essere presenti e validi, ma il modello è rivolto di 90° rispetto alla camera.

`PlayerRigSettings.asset` espone `ModelYawOffset` (intervallo da -180° a +180°). Il valore predefinito è `90°`: il builder applica questa rotazione al root del modello prima del calcolo dei bounds e del salvataggio del prefab, portando il forward sorgente `-X` verso il forward Player `+Z`. Il valore è configurabile nell'Inspector e può essere regolato se cambia l'orientamento delle sorgenti Blender.

## Importazione in Unity e generazione dei prefab

Il rig Blender contiene entrambi i personaggi, ma Unity importa e genera un prefab separato per ciascuno. I due FBX devono quindi essere esportati separatamente e collocati nei percorsi configurati in `PlayerRigSettings.asset`:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

I percorsi sono modificabili nell'Inspector tramite `MaleSourceAssetPath` e `FemaleSourceAssetPath`. Ogni FBX deve contenere la mesh del personaggio, la relativa armatura configurata in `MaleArmatureRootName` o `FemaleArmatureRootName`, pesi di skinning verificati e un renderer della testa separato.

Quando entrambi gli FBX sono disponibili:

1. Verificare i percorsi nell'asset `Assets/UnderGrind/Settings/PlayerRigSettings.asset`.
2. Eseguire **UnderGrind → Player → Build Male and Female Prefabs From Configured FBX**.
3. Il builder importa ciascun FBX come Humanoid, crea il relativo Avatar, controlla che i due Avatar siano validi e distinti e genera i prefab nella cartella `Assets/UnderGrind/Generated/Prefabs`. Le opzioni di importazione di animazioni, materiali, camere, luci, ottimizzazione e curve di visibilità sono configurate in `PlayerRigSettings.asset`; il tipo Humanoid e la creazione dell'Avatar dal modello sono requisiti del rig.
4. Controllare che la testa sia separata e che i gruppi di renderer corrispondano ai token configurati.
5. Aprire i prefab e verificare le deformazioni prima di usarli nella scena.

Il builder rifiuta una sorgente se manca l'armatura configurata, l'Avatar Humanoid non è valido oppure la testa non è separata quando `RequireSeparateHead` è attivo. Non effettua tagli geometrici automatici. L'esportazione FBX è completata; il builder deve ancora essere eseguito in Unity per creare e validare i prefab.

`PlayerVisualRig` usa gruppi distinti di renderer:

- Testa: `HeadRendererSuffix`, disattivata in prima persona.
- Braccia e mani dedicate alla prima persona: `FirstPersonArmsToken` e `FirstPersonHandsToken`.
- Braccia e mani dedicate alla terza persona: `ThirdPersonArmsToken` e `ThirdPersonHandsToken`.

I gruppi dedicati alla prima e alla terza persona sono opzionali e configurabili. Se un gruppo non esiste, il sistema non inventa una mesh sostitutiva; resta visibile il corpo disponibile. Il renderer del corpo è identificato da `BodyRendererToken`. In prima persona il corpo viene nascosto quando la visuale è frontale e mostrato quando il giocatore guarda verso il basso: `FirstPersonBodyRevealPitch` definisce la soglia di comparsa e `FirstPersonBodyHidePitch` quella di scomparsa, con isteresi per evitare lampeggi vicino al limite. Entrambi i valori sono configurabili nell'Inspector.

La scena di test richiede entrambi i prefab riggati e verifica Animator, Avatar Humanoid, SkinnedMeshRenderer, testa, corpo e gruppi di arti. Se uno dei prefab manca, avvia la generazione dai due FBX configurati; se la validazione fallisce, interrompe la creazione della scena. Non usa mesh statiche come fallback.

La versione Blendstra più recente contiene quattro mesh separate per testa e corpo e le due armature hanno i nomi esatti richiesti. Questo supera il limite geometrico della prima revisione, ma non certifica ancora pesi, deformazioni, configurazione dei modificatori, UV o Avatar Humanoid. Il builder può quindi verificare gerarchia e renderer solo dopo l'esportazione e l'importazione dei due FBX; l'accettazione del rig resta subordinata ai test di deformazione in Unity.

## Verifica automatica dell'importazione FBX

`Assets/UnderGrind/Tests/Editor/HumanoidRigImportTests.cs` contiene test EditMode che caricano i due FBX effettivi dal progetto Unity. I controlli verificano:

- importazione Humanoid con Avatar generato dal modello e ottimizzazione GameObject disattivata;
- Avatar distinti, validi e Humanoid, con mapping delle ossa principali;
- sei `SkinnedMeshRenderer` per personaggio, mesh e bind pose presenti, root bone assegnato e riferimenti alle ossa risolti;
- riconoscimento delle sei parti da parte di `PlayerVisualRig`, con una testa, un corpo, due renderer FP e due TP.

### Correzione della posa T-pose in Unity

Se il mapping delle ossa è presente ma Unity mostra `Character is not in T pose`, aprire **Configure** sul FBX e usare **Pose → Enforce T-Pose**. Controllare la posa risultante, quindi premere **Apply** e **Done**. Se l'avviso resta, correggere manualmente le ossa residue nella finestra Avatar Configuration e applicare nuovamente.

Unity salva queste correzioni nella `HumanDescription` del `ModelImporter`. `HumanoidAvatarImporter.EnsureValidAvatar` conserva una descrizione manuale valida. Se il mapping automatico di Unity produce un Avatar invalido, ricostruisce prima la tabella `HumanBone` associando ogni osso Humanoid al Transform omonimo, preservando lo skeleton e la posa generati dall'importer. Questo evita l'errore osservato nel quale Unity assegnava `RightHand` a `RightLowerArm` e lasciava `RightHand` senza mapping. Se anche il mapping esplicito fallisce, viene tentato il fallback automatico pulito.

Il test `EnsureValidAvatarPreservesValidManualPoseCorrections` verifica che una descrizione valida con mapping e posa non venga cancellata. Non sostituisce il controllo visivo della posa, delle deformazioni e del retargeting. La suite verifica anche che `Animator.GetBoneTransform` restituisca entrambe le mani, oltre agli arti già controllati.

### Diagnostica dell'ultima importazione Unity — 20 novembre 2026

La run `20261120-132802-174` ha eseguito 39 test EditMode: 34 superati e 5 falliti; PlayMode 13/13 e Diagnostics standalone 6/6 superati. I cinque fallimenti si verificano nel `SetUp` di `HumanoidRigImportTests`, quando Unity non genera l'Avatar maschile. Il log ripete `Required human bone 'RightHand' not found`. Non sono presenti diagnostiche di compilazione C#; il codice di uscita 2 indica test EditMode falliti, non un errore del compilatore.

`HumanoidAvatarImporter` registra ora, in caso di Avatar invalido, tutti i percorsi Transform importati, il mapping Humanoid e lo skeleton esposti dal ModelImporter. Questo permette di distinguere un osso assente nell'FBX da un problema di mapping o gerarchia, senza dedurre il nome del transform dal solo conteggio delle ossa.

Il report Blender precedente registra 22 ossa ma non ne elenca i nomi né i parent; inoltre il suo schema non include i nuovi campi UV/materiali prodotti dallo script attuale. Non è quindi prova sufficiente che il report provenga dall'ultima versione dell'esportatore. Eseguire nuovamente lo script Blender aggiornato e verificare il nuovo `boneHierarchy` prima di esportare i FBX.

Il log Unity segnala inoltre che un poligono di `Human_Male_Body` è auto-intersecante ed è stato scartato durante l'importazione. Il warning va risolto nella mesh sorgente e verificato dopo una nuova esportazione; la validità dell'Avatar e la validità della mesh sono controlli distinti.



I report versionati `TestResults/test/all-latest.json` registrano la run `20261123-104029-879`: Diagnostics standalone 6/6, Unity EditMode 40/40 e PlayMode 13/13, totale Unity 53/53, senza errori di compilazione o test falliti. Questa run precede i nuovi controlli di posa e di incrocio dei piedi descritti sotto: non dimostra che le clip si deformino correttamente durante il movimento. I test Unity vengono eseguiti localmente tramite Test Runner; la GitHub Action non avvia Unity.

### Correzione automatica della T-Pose — 23 novembre 2026

`PlayerRigSettings.EnforceHumanoidTPose` è un'opzione serializzata e modificabile dall'Inspector, attiva per impostazione predefinita. `HumanoidAvatarImporter` verifica la posa usando `UnityEditor.AvatarSetupTool`. Se la posa è già valida, conserva integralmente le correzioni manuali. Se non è valida, applica `MakePoseValid` e salva lo skeleton corretto nel `ModelImporter` prima della reimportazione. Il menu **UnderGrind → Player → Validate and Enforce Humanoid T-Pose** esegue la verifica per entrambi i personaggi; anche la generazione dell'Animator Controller la esegue automaticamente.

L'accesso a `AvatarSetupTool` avviene tramite reflection perché è un'API interna dell'Editor Unity. Se la versione Unity non espone i metodi attesi, l'operazione fallisce con un messaggio che indica il percorso manuale **Avatar Configuration → Pose → Enforce T-Pose → Apply → Done**; non dichiara la correzione completata senza verifica.

`HumanoidRigImportTests` verifica la T-Pose maschile e femminile e campiona Walking, Running e Crouch Walk a 30 Hz sullo scheletro Mixamo sorgente e sui due Avatar Player. La prima esecuzione Unity, run `20261123-120617-992`, ha riportato 42/44 EditMode, 13/13 PlayMode e 6/6 test standalone. Il controllo dei piedi ha rilevato:
- Walking: nessun incrocio sul Mixamo sorgente; incroci sui due Avatar Player (minimo -0,046 m maschile e -0,063 m femminile).
- Running: incroci già presenti sul sorgente (-0,157 m) e sui due Avatar Player.
- Crouch Walk: incroci già presenti sul sorgente (-0,377 m) e sui due Avatar Player.

Questi valori provengono dalla prima versione del diagnostico, che proiettava i piedi su un asse laterale fisso ricavato dalla posa di riposo. Il controllo è stato aggiornato per misurare la separazione lungo l'asse anatomico sinistro-destro ricalcolato a ogni campione, mantenendo anche la misura sull'asse fisso per confronto. Il nuovo risultato deve essere raccolto con un'altra esecuzione Unity prima di concludere quali incroci siano reali e quali dipendano dalla rotazione del bacino.

La stessa run ha individuato anche una regressione: Fall e Hard Landing risultavano obbligatorie nonostante la documentazione e il builder le trattassero come opzionali. Il controllo delle clip obbligatorie è stato corretto: Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono richieste; Fall e Land possono essere assenti, ma se assegnate devono essere clip valide.

Per ripetere la verifica, su Windows dalla root del repository eseguire `.\\TESTA.bat`. I campioni dettagliati sono registrati in `TestResults/test/Unity/EditMode.log`; il riepilogo è in `TestResults/test/all-latest.json`.

### Correzione della soglia Walk/Run — 23 novembre 2026

`HumanLocomotionController` invia all'Animator la velocità orizzontale misurata in metri al secondo. La velocità Walk configurata è `1,4 m/s`, ma la precedente soglia `walkThreshold = 0,1` faceva già dominare il movimento Run a velocità di camminata: con `runThreshold = 1,65`, a 1,4 m/s il Blend Tree assegnava circa l'84% del peso alla corsa e il 16% alla camminata.

La soglia Walk predefinita è ora `1,4`, allineata alla velocità Walk del controller; la soglia Run resta `1,65`, tra velocità Walk e Run. La migrazione `PlayerAnimationSettings` versione 6 aggiorna il vecchio valore predefinito `0,1` a `1,4`, preservando valori personalizzati diversi. `HumanLocomotionTuningTests` verifica che `WalkThreshold == WalkSpeed` e che `RunThreshold` resti compresa tra Walk e Run. La correzione viene applicata quando Unity migra l'asset e quando viene ricostruito l'Animator Controller. Dopo aver aggiornato il repository, eseguire in Unity **UnderGrind → Player → Refresh Clips and Build Animator Controller**: il builder applica la T-Pose, aggiorna i riferimenti delle clip e rigenera il Blend Tree con la soglia Walk corretta.

## Sorgenti delle animazioni

Sono stati individuati nella libreria PINOC otto movimenti:

- Standing Idle
- Walking
- Running
- Jump
- Crouching Idle
- Crouch Walk Forward

Le due famiglie di esportazione FBX hanno finalità diverse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese le ossa facciali, senza mesh. È destinato ai personaggi PINOC con Splat Engine e rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomenclatura Mixamo. È il formato candidato per il retargeting tramite Blender o altri strumenti DCC verso il rig UnderGrind.

X Bot è un modello di supporto per il retargeting e non deve sostituire il corpo del personaggio UnderGrind. Gli otto movimenti sono stati aggiunti alla libreria PINOC dell'utente. Gli asset selezionati sono:

| Movimento | Asset ID |
|---|---|
| Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |
| Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |

Per il rig Humanoid UnderGrind va usato l'export `mixamo-fbx` di ciascun asset: contiene X Bot e lo scheletro Mixamo ed è il formato previsto per il retargeting. Il formato predefinito restituito da `take_library_motions` è invece un GLB con solo lo scheletro MetaHuman a 441 ossa, senza mesh; è destinato al workflow Splat e non va assegnato direttamente al controller Humanoid.

Gli otto export `mixamo-fbx` sono ora versionati in `Assets/UnderGrind/Animations`. `PlayerAnimationControllerBuilder` li importa come clip Humanoid, genera il controller e lo assegna ai prefab maschile e femminile. Il retargeting attualmente usato è quello Humanoid di Unity, con un Avatar sorgente per ciascun FBX; non è un bake DCC delle curve sullo scheletro UnderGrind. La presenza delle clip nel controller non certifica la qualità della deformazione: il controllo di T-Pose, piedi e transizioni resta necessario. Prima di distribuire o includere gli export nel prodotto occorre verificare le condizioni d'uso applicabili.

### Rigenerazione delle mesh e degli export — 23 novembre 2026

Il workflow `Repair mirrored humanoid source` è terminato con successo e ha rigenerato il file Blender e gli FBX maschile/femminile con Blender 5.2.4. Il report ha classificato tutte le 12 mesh come `already_complete`: non sono state aggiunte metà mancanti e i conteggi di vertici/facce sono rimasti invariati. La run verifica quindi il passaggio di export, ma non certifica da sola la qualità dei pesi o delle animazioni.

Prima di includere gli export nel gioco distribuito, verificare il piano e i diritti applicabili a ciascun asset PINOC. I termini ufficiali limitano il piano Free all'uso non commerciale, personale, di valutazione e prototipazione; i piani Starter e Pro indicano l'uso commerciale. La disponibilità di un clip nella libreria non sostituisce la verifica dei diritti sul singolo contenuto. Riferimenti: [PINOC pricing](https://viggle.ai/pinoc/pricing/) e [Viggle Terms of Use](https://viggle.ai/terms-of-use/).

Dopo l'importazione delle clip nella cartella `Assets/UnderGrind/Animations`, **UnderGrind → Player → Refresh Clips and Build Animator Controller** aggiorna i riferimenti e genera il controller usando `PlayerAnimationSettings.asset`. Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono richieste; Fall e Land sono opzionali.

## Criteri di completamento

- [ ] Mesh sorgenti maschile e femminile preservate e confrontate con i derivati.
- [ ] Isole geometriche disconnesse separate e ispezionate.
- [ ] Testa separata in un renderer indipendente, verificata da più angolazioni.
- [ ] Posizione delle ossa verificata su pose di riferimento.
- [ ] Pesi verificati con deformazioni di spalle, gomiti, polsi, anche, ginocchia e caviglie.
- [x] Export FBX maschile e femminile generati e versionati; report Blender con 22 ossa e sei mesh ciascuno.
- [x] Avatar Humanoid Unity valido per entrambi i personaggi (EditMode verificato nella run 53/53 del 23 novembre 2026).
- [ ] Prefab maschile e femminile generati e controllati.
- [ ] Mesh dedicate a braccia e mani in prima persona create e posate, se richieste dal modello finale.
- [x] Soglie Walk/Run del Blend Tree allineate alle velocità misurate; verifica automatica aggiunta, esecuzione Unity ancora da confermare.
- [ ] Retargeting Humanoid verificato su entrambi gli Avatar, senza incroci o deformazioni dei piedi.
- [ ] Cicli di camminata e corsa controllati visivamente per scivolamento dei piedi, transizioni e movimento della radice.
- [ ] Test PlayMode superati sul rig importato.
- [ ] Visuale in prima e terza persona controllata nella scena Unity.
