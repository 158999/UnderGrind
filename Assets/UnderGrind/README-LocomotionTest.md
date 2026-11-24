# UnderGrind — scena di test locomozione

## Rig e importazione del personaggio

- Unity **6000.6.9f1**.
- Pacchetti del progetto: Input System e Unity Test Framework.
- Sorgente originale: `Assets/UnderGrind/Art/Source/humans.blend`. Non sostituirla con un file esterno durante la procedura di riparazione.
- Blend derivato con pesi anatomici riparati: `Tools/Blender/Generated/humans_reweighted.blend`.
- FBX maschile/femminile: `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx` e `Human_Female_Rigged.fbx`.
- Prefab: `Assets/UnderGrind/Generated/Prefabs/Human_Male_Rigged.prefab` e `Human_Female_Rigged.prefab`.

Il workflow GitHub Actions **UnderGrind anatomical skin-weight repair** genera il blend derivato e i due FBX senza sovrascrivere `humans.blend`. Il report `Tools/Blender/weight-repair-report.json` registra 12 mesh, zero vertici non pesati, zero influenze oltre il limite di quattro, zero pesi non normalizzati e zero assegnazioni anatomiche vietate. Sono stati usati 14 fallback su vertici femminili (10 testa, 4 mani). `Tools/Blender/player-skinning-deformation-audit.json` valuta 54 pose sintetiche per personaggio. Questi report verificano i dati e la valutazione dello skinning in Blender, non la resa finale in Unity.

Per rigenerare i prefab con le impostazioni correnti, aprire il progetto in Unity e usare **UnderGrind → Player → Build Male and Female Prefabs From Configured FBX**. Il builder valida entrambi gli Avatar Humanoid, i sei gruppi renderer e salva i renderer in stato neutro; la modalità camera viene applicata a runtime da `PlayerVisualRig`.

La scena completa richiede entrambi i prefab validi. Se i prefab non superano la validazione, la scena non viene creata e non viene usato un manichino statico di ripiego.


## Animazioni

Gli otto FBX `mixamo-fbx` sono già versionati in `Assets/UnderGrind/Animations` e assegnati in `Assets/UnderGrind/Settings/PlayerAnimationSettings.asset`. Ogni FBX ha un Avatar Humanoid sorgente; Unity esegue il retargeting runtime sull'Avatar del Player. La mesh X Bot inclusa negli FBX è solo un riferimento per l'animazione e non sostituisce il personaggio UnderGrind.

La clip `Crouch_Walk_Forward` resta non approvata: l'ultima run Unity del 24 novembre ha rilevato 6/232 incroci dei piedi sulla sorgente, 10/232 sul Player maschile e 12/232 sul femminile. Walking e Running avevano zero incroci nel test del 23 novembre, prima della riparazione dei pesi.

La cartella `Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk` contiene otto candidati non assegnati al controller. Il report Blender `Tools/Blender/pinoc-crouch-walk-candidate-audit.json` analizza lo scheletro FBX grezzo e non certifica il retargeting Unity. Il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` confronta la sorgente e gli otto candidati sugli Avatar sorgente, maschile e femminile; genera `TestResults/test/Unity/CrouchWalkCandidateAudit.txt`. Il test è stato aggiunto nel branch `fix/crouch-walk-animation-audit` ma non è ancora stato eseguito in Unity.

Per aggiornare i riferimenti e rigenerare il controller usare **UnderGrind → Player → Refresh Clips and Build Animator Controller**. Non sostituire la clip di produzione finché una variante non supera il confronto Unity su entrambi gli Avatar e la verifica visiva del ciclo.


## Creazione della scena

1. Verificare che Unity abbia completato l'importazione dei due FBX configurati.
2. Verificare **Edit → Project Settings → Player → Other Settings → Active Input Handling**: Input System Package (New) oppure Both.
3. Per testare subito il movimento senza i modelli definitivi, scegliere **UnderGrind → Build Graybox Locomotion Test (Temporary Proxy)**. La scena `LocomotionGraybox.unity` usa una capsula temporanea, dichiarata come proxy e non come personaggio finale.
4. Dopo l'importazione e la validazione dei due prefab Humanoid, scegliere **UnderGrind → Build First Locomotion Test Scene** per creare `LocomotionTest.unity` con la variante selezionata in `PlayerRigSettings.asset`.
5. Aprire la scena desiderata e premere Play.

Il graybox è generato da `PlayerGrayboxSettings.asset` e comprende marcatori di distanza, gradini, scala, pareti, rampe, tunnel, passaggio stretto, slalom, ostacoli e piattaforme.

## Configurazione del Player

Selezionare il Player nella Hierarchy. `HumanLocomotionController` usa l'asset `PlayerLocomotionSettings.asset`: i parametri di gameplay non sono duplicati sul componente. L'asset contiene gli otto gruppi di velocità, salto, gravità, accelerazione, postura/collisioni, camera/sensibilità e timing del salto. I valori iniziali effettivi sono quelli serializzati nell'asset.

Input, interazione, UI inventario, animazioni, rig e graybox sono configurati nei rispettivi ScriptableObject. I valori iniziali sono forniti dai JSON in `Assets/UnderGrind/Settings/Defaults`; le migrazioni versionate compilano i campi aggiunti senza sovrascrivere le modifiche già fatte dall'Inspector.

## Test e report

I runner sono:

- `Tools/Testing/run-tests.ps1`: build diagnostica e test C# standalone.
- `Tools/Testing/run-unity-tests.ps1`: test Unity EditMode e PlayMode, in sequenza.
- `Tools/Testing/run-all-tests.ps1`: esegue entrambe le suite come un'unica esecuzione e produce anche `all-latest.json` e `all-latest.txt`.

La conservazione è limitata a due cartelle nella root `TestResults`:

- `TestResults/test`: esecuzione corrente.
- `TestResults/test old`: esecuzione precedente.

A ogni nuova esecuzione viene eliminata la vecchia `test old`, la cartella `test` viene spostata in `test old` e viene creata una nuova `test`. Le esecuzioni autonome e Unity avviate separatamente applicano ciascuna una rotazione; `run-all-tests.ps1` esegue invece entrambe le suite nella stessa cartella con una sola rotazione. I risultati del Test Runner avviato dall'Editor vengono salvati in `TestResults/test/UnderGrind` con nomi fissi, senza creare una directory per ogni esecuzione.

I report JSON distinguono esecuzioni superate, fallite e non configurate. XML assente, non valido o con zero test non può essere considerato un successo. Gli errori di compilazione C# sono riportati nei dettagli della suite Unity.

## Limiti della verifica

L'ultima esecuzione Unity versionata è la run `20261124-110717-910` su Unity 6000.6.9f1: 65/68 test superati (EditMode 53/55, PlayMode 12/13). I tre fallimenti erano:
- Crouch Walk Forward: incroci anatomici dei piedi sulla sorgente e sui due Avatar Player.
- Visibilità dei renderer FP durante il cambio camera.
- Conservazione dello stato iniziale dei renderer disabilitati.

I due problemi dei renderer sono corretti nel branch `fix/crouch-walk-animation-audit`: il rig conserva gli stati enabled originali, il builder ripristina i default prima di salvare e i prefab sono stati aggiornati allo stato neutro. La correzione non è ancora stata verificata con una nuova run Unity. Il problema Crouch Walk resta aperto e nessun candidato è stato promosso.

La presenza di FBX, Avatar Humanoid e Animator validi non equivale a una verifica visiva. Importazione, compilazione, EditMode, PlayMode, deformazione delle mesh, posizione degli arti FP e resa in prima/terza persona devono essere controllati nell'Editor Unity.
