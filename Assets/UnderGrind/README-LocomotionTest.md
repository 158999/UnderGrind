# UnderGrind — scena di test locomozione

## Requisiti e importazione del personaggio

- Unity **6000.6.7f1**.
- Pacchetti del progetto: Input System e Unity Test Framework.
- Il file Blender attualmente committato è `Assets/UnderGrind/Art/Source/humans.blend`.
- La versione di lavoro più recente con armature e mesh separate è nel progetto [Blendstra Rigged Human Source](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3), versione `version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07`. Non è ancora stata trasferita nel file locale del repository.
- Prima di esportare, trasferire la versione Blendstra nel file `humans.blend`, aprirlo in Blender ed eseguire `Tools/Blender/export-undergrind-humans.py`. Lo script interrompe l'export se non trova tutte le mesh, i gruppi di pesi o i modificatori Armature richiesti.
- Unity importa i file FBX tramite Blender installato sul computer. Se l'importazione non produce un modello nella finestra Project, verificare **Edit → Preferences → External Tools** e ripetere Reimport.

Il builder richiede i due prefab Humanoid maschile e femminile configurati in `PlayerRigSettings.asset`. Verifica Animator, Avatar Humanoid valido, SkinnedMeshRenderer, testa, corpo e gruppi renderer FP/TP. Se i prefab non superano la validazione, la scena non viene creata: non viene usato un manichino statico come sostituto.

## Animazioni

Gli otto movimenti selezionati da PINOC sono elencati in `Assets/UnderGrind/Animations/PlayerAnimationManifest.json`. I relativi FBX `mixamo-fbx` sono nella libreria PINOC dell'utente, non nel repository. Scaricare i file con i nomi indicati nel manifest in `Assets/UnderGrind/Animations`; il builder li importa come Humanoid, assegna le clip per nome e genera l'Animator Controller con **UnderGrind → Player → Refresh Clips and Build Animator Controller**. La mesh X Bot inclusa negli FBX è solo un riferimento per l'animazione e non sostituisce il personaggio UnderGrind.

## Creazione della scena

1. Verificare che Unity abbia completato l'importazione dei due FBX configurati.
2. Verificare **Edit → Project Settings → Player → Other Settings → Active Input Handling**: Input System Package (New) oppure Both.
3. Dal menu scegliere **UnderGrind → Build First Locomotion Test Scene**.
4. Aprire `Assets/UnderGrind/Scenes/LocomotionTest.unity` e premere Play.

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

La modifica dei file nel repository non equivale a una verifica nell'Editor Unity. Importazione Blender, compilazione C#, rendering, EditMode e PlayMode devono essere eseguiti sul progetto aperto in Unity. La presenza di FBX, Avatar Humanoid e Animator validi deve essere verificata sui modelli definitivi.
