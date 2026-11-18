# Revisione dell'implementazione del Player — 18 novembre 2026

## Ambito e criteri approvati

La revisione copre i quattro blocchi di sviluppo approvati:

1. **Character:** modelli maschile e femminile reali, proporzioni, materiali, rig e Avatar Humanoid validi. Nessun placeholder presentato come personaggio definitivo.
2. **Locomotion:** camminata, corsa, accelerazione, salto, gravità e collisioni, con parametri modificabili nell'Inspector. Profilo iniziale: altezza 1,78 m, camminata 1,40 m/s, corsa 1,8667 m/s, salto 0,40 m, gravità 9,81 m/s².
3. **Visual:** prima e terza persona, corpo visibile quando la visuale guarda verso il basso, mani e gruppi di mesh dedicati.
4. **Graybox:** ostacoli, scale, rampe, distanze e piattaforme per verificare il movimento.

Questa revisione distingue implementazione presente, risultati di test effettivamente eseguiti e attività ancora non verificate nell'Editor Unity.

## Stato dei quattro blocchi

### 1. Character — sorgente pronta, prefab non ancora verificati

La versione Blendstra corrente è [`version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07`](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3&version=version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07). L'ispezione registra 16 oggetti: due armature, sei mesh derivate per personaggio (corpo, testa, braccia e mani FP/TP) e le due mesh sorgenti originali.

Lo script di generazione costruisce una gerarchia di 22 ossa per personaggio e assegna pesi automatici, con fallback basato sulla prossimità. L'ispezione sorgente non espone la configurazione completa dei modificatori né i pesi effettivi: la struttura visibile non basta a certificare la deformazione.

È stato aggiunto `Tools/Blender/export-undergrind-humans.py`. Prima di esportare, verifica ossa Humanoid, mesh separate, parent, modificatori Armature, gruppi di pesi e peso deformante valido per ogni vertice. Valida entrambi i personaggi prima di produrre i due FBX configurati in `PlayerRigSettings.asset`.

**Non ancora verificato:** la sorgente Blendstra più recente non è stata sostituita automaticamente al file `Assets/UnderGrind/Art/Source/humans.blend` del repository; l'export FBX non è stato eseguito; i due Avatar Humanoid e i prefab Unity non sono ancora stati generati e verificati.

Il builder Unity ora richiede entrambi i prefab riggati. Verifica Animator, Avatar Humanoid, SkinnedMeshRenderer, testa, corpo e gruppi di arti FP/TP. Il fallback statico è stato rimosso: se i prefab non sono disponibili o non superano la validazione, la scena non viene creata.

### 2. Locomotion — implementata e configurabile

I valori iniziali nell'asset `Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset` sono:

| Parametro | Valore |
|---|---:|
| Altezza maschile | 1,78 m |
| Altezza femminile | 1,70 m |
| Camminata | 1,40 m/s |
| Moltiplicatore corsa | 1,3333334 |
| Corsa derivata | 1,8667 m/s |
| Altezza salto teorica | 0,40 m |
| Gravità | 9,81 m/s² |

L'asset contiene anche accelerazione, decelerazione, controllo aereo, coyote time, jump buffer, accovacciamento, dimensioni della capsula, step offset, pendenza, skin width, collisioni, sensibilità e parametri delle camere. Il controller legge questi valori dall'asset.

L'inventario usa una soglia di peso, non un limite di raccolta. Oltre soglia disabilita corsa e salto e riduce la velocità di camminata del 25%; la penalità si applica anche alla camminata accovacciata. Il salto già iniziato non viene interrotto.

### 3. Visual — logica implementata, modello reale da verificare

`PlayerVisualRig` gestisce gruppi separati per testa, corpo, arti FP e arti TP. Il corpo in prima persona usa le soglie configurabili `FirstPersonBodyRevealPitch` e `FirstPersonBodyHidePitch`, con isteresi per evitare lampeggi vicino alla soglia.

I token predefiniti corrispondono ai nomi delle mesh generate: `_Head`, `_Body`, `_FP_Arms`, `_FP_Hands`, `_TP_Arms` e `_TP_Hands`. La configurazione predefinita richiede testa, corpo e gruppi di arti FP/TP.

La camera supporta prima e terza persona, collisioni configurabili, distanza, offset, FOV e near clip. I test PlayMode verificano la logica su gerarchie sintetiche; il comportamento sui prefab definitivi non è ancora verificato.

### 4. Graybox — configurazione implementata

`Assets/UnderGrind/Settings/PlayerGrayboxSettings.asset` viene creato dal template JSON e controlla dimensioni, posizioni, colori, illuminazione, marcatori, blocchi e rampe.

Il percorso iniziale contiene:

- pavimento 50 × 50 m e marcatori ogni 2 m fino a 40 m;
- gradini per salti da 0,20, 0,40 e 0,60 m;
- scala a cinque gradini;
- pareti da 0,80, 1,00 e 1,20 m;
- rampa da 15° e rampa da 25°;
- tunnel basso per verificare l'accovacciamento;
- passaggio stretto e slalom;
- ostacoli bassi da 0,30 e 0,50 m;
- quattro piattaforme con altezze progressive e intervalli di salto;
- punto di spawn configurabile.

Le definizioni sono serializzate nell'asset e modificabili nell'Inspector. La scena deve ancora essere rigenerata nell'Editor Unity.

## Input, inventario e animazioni

### Input

- `PlayerInputActions.inputactions` contiene la action map `Player` e le binding predefinite, modificabili nell'Input Actions editor.
- `PlayerInputSettings.asset` collega gli identificatori logici ai nomi delle azioni e configura dead zone, cursore, timeout e persistenza delle rimappature.
- `PlayerInputRebindingController` carica gli override prima di abilitare la action map, supporta rimappatura interattiva, annullamento, visualizzazione, salvataggio e reset.
- Non è ancora presente una schermata UI dedicata alla rimappatura.

### Inventario e interazione

- `PlayerInventory`: slot, stack, selezione, peso derivato e operazioni di trasferimento.
- `PlayerInventoryUI`: Canvas overlay, icone, quantità, selezione e peso; layout e stile configurabili.
- `PlayerInteractionController`: raccolta tramite SphereCast e rilascio dello stack selezionato.
- `WorldItemPickup`: conserva la quantità non trasferita.
- `ItemDefinition`: dati dell'oggetto e prefab da usare nel mondo.

I valori iniziali dell'inventario sono 24 slot, 50 kg di soglia e riduzione della camminata del 25%. Non sono ancora presenti salvataggio/caricamento dell'inventario, equipaggiamento o uso degli oggetti.

### Animazioni

`PlayerAnimationSettings.asset` configura clip, parametri Animator, soglie, transizioni, loop e opzioni di importazione FBX. `PlayerAnimationControllerBuilder` prepara i soli FBX in `Assets/UnderGrind/Animations` e genera il controller dalle otto clip richieste: Idle, Walk, Run, Jump, Fall, Land, Crouch Idle e Crouch Walk.

Gli otto movimenti sono disponibili nella libreria PINOC in formato `mixamo-fbx`, ma non sono ancora importati nel repository né retargettati sul rig UnderGrind. Di conseguenza il controller definitivo non è ancora stato generato e verificato.

## Configurazione iniziale e migrazioni

I valori iniziali di input, interazione, UI, animazione, rig e graybox sono file JSON in `Assets/UnderGrind/Settings/Defaults`. I builder li applicano quando creano gli asset ScriptableObject mancanti; non sovrascrivono le modifiche successive effettuate nell'Inspector. Le migrazioni versionate riempiono soltanto i campi introdotti nelle versioni successive, preservando i valori già configurati.

## Verifiche

### Risultati effettivamente eseguiti

- Test autonomi C#: **6/6 superati** nel report `TestResults/latest.json`.
- Unity EditMode: **12/12 superati** nel report `TestResults/Unity-20261118-103717-325/EditMode.xml`.
- Unity PlayMode: **7/7 superati** nel report `TestResults/UnderGrind/PlayMode_20261118_092745.xml`.

Questi report precedono una parte dei test aggiunti successivamente. La successiva esecuzione batch ha lanciato EditMode e PlayMode in parallelo: EditMode ha completato 12 test; PlayMode è terminato con codice 1 prima di produrre un report valido perché il progetto era ancora occupato dal processo EditMode. I conteggi a zero nel JSON aggregato non rappresentano l'esito reale di EditMode.

### Suite presenti nel codice, non ancora eseguite dopo le ultime modifiche

- EditMode: **29 test**.
- PlayMode: **13 test**.

Il runner PowerShell è stato modificato per attendere il completamento di ogni processo Unity, rifiutare XML assenti o non validi e non accettare report obsoleti. Il runner aggiornato e le suite complete non sono ancora stati eseguiti con Windows PowerShell 5.1 e Unity 6000.6.7f1.

Non risultano verifiche GitHub Actions associate agli ultimi commit. Non sono documentati test visivi nell'Editor Unity, validazione dei due Avatar, deformazione dei modelli o riproduzione delle otto animazioni sul rig definitivo.

## Attività necessarie per chiudere i quattro blocchi

1. Salvare la versione Blendstra corrente in `Assets/UnderGrind/Art/Source/humans.blend`.
2. Eseguire l'exporter Blender e verificare i due FBX prodotti.
3. Importare i FBX in Unity e generare i prefab Humanoid maschile e femminile.
4. Importare gli otto FBX Mixamo, generare l'Animator Controller e verificare il retargeting.
5. Rigenerare `LocomotionTest.unity` con il graybox configurato.
6. Eseguire i due script PowerShell di test e verificare i report aggiornati.
7. In Play Mode controllare camminata, corsa, salto, collisioni, tunnel, scale, rampe, piattaforme, visibilità del corpo e deformazione delle mesh.

Fino al completamento di queste verifiche, i quattro blocchi sono implementati in larga parte nel codice e negli asset di configurazione, ma non possono essere dichiarati verificati end-to-end.
