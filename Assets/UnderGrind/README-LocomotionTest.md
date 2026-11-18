# UnderGrind — scena di test locomozione

## Requisiti e importazione del personaggio

- Unity **6000.6.7f1**.
- Pacchetti del progetto: Input System e Unity Test Framework.
- Il modello sorgente è `Assets/UnderGrind/Art/Source/humans.blend`.
- Unity importa i file Blender tramite Blender installato sul computer. Se l'importazione non produce un modello nella finestra Project, verificare **Edit → Preferences → External Tools** e ripetere Reimport.

Il builder richiede i due prefab Humanoid maschile e femminile configurati in `PlayerRigSettings.asset`. Verifica Animator, Avatar Humanoid valido, SkinnedMeshRenderer, testa, corpo e gruppi renderer FP/TP. Se i prefab non superano la validazione, la scena non viene creata: non viene usato un manichino statico come sostituto.

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
