# UnderGrind — scena di test locomozione

## Requisiti e importazione del personaggio

- Unity **6000.6.9f1**.
- Pacchetti del progetto: Input System e Unity Test Framework.
- Il file Blender attualmente committato è `Assets/UnderGrind/Art/Source/humans.blend`.
- La sorgente riggata più recente è il progetto Blendstra `UnderGrind — Humans Source Separation` (projectId `2cdfaa98-3ea2-4a49-892a-b96488e12bc3`), versione `version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`. Aprire [la versione sorgente](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3&version=version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6) e scaricare il file Blender sorgente. Il file `Assets/UnderGrind/Art/Source/humans.blend` nel repository deve essere sostituito con questa versione: esportare il vecchio file senza prima trasferire la sorgente riggata genera FBX non aggiornati.
- Copiare la sorgente scaricata in `Assets/UnderGrind/Art/Source/humans.blend`, quindi eseguire da PowerShell nella root del repository `Tools/Blender/run-export.ps1 -Force`. Se Blender non è nel PATH, impostare `BLENDER_EXECUTABLE` o passare `-BlenderExecutable`.
- Il runner interrompe l'export se mancano armature, mesh, pesi, UV map o immagini texture referenziate ma non disponibili. L'export centra a zero la sola traslazione globale dell'armatura (senza cambiare gli offset relativi mesh/ossa), copia e incorpora le immagini referenziate e produce `Tools/Blender/last-export-report.json` con UV bounds, materiali, immagini e trasformazioni prima/dopo l'export.
- Dopo l'export, in Unity selezionare entrambi gli FBX e usare **Reimport**. Se le UV bounds riportate sono limitate a una porzione dell'atlante, non rimappare automaticamente: confrontare le coordinate con il layout della texture sorgente. Se l'FBX non contiene le immagini attese, controllare il report e i nodi Image Texture in Blender.
- Unity importa i file FBX tramite Blender installato sul computer. Se l'importazione non produce un modello nella finestra Project, verificare **Edit → Preferences → External Tools** e ripetere Reimport.

Il builder richiede i due prefab Humanoid maschile e femminile configurati in `PlayerRigSettings.asset`. Verifica Animator, Avatar Humanoid valido, SkinnedMeshRenderer, testa, corpo e gruppi renderer FP/TP. La proprietà `DefaultCharacter` dell'asset sceglie quale variante istanziare nella scena; entrambe vengono comunque validate. L'altezza del CharacterController è letta da `PlayerLocomotionSettings`, la stessa fonte usata per normalizzare il prefab. Se i prefab non superano la validazione, la scena non viene creata: non viene usato un manichino statico come sostituto.

## Animazioni

Gli otto movimenti selezionati da PINOC sono elencati in `Assets/UnderGrind/Animations/PlayerAnimationManifest.json`. I relativi FBX `mixamo-fbx` sono disponibili tramite gli asset ID nel manifest, ma non sono ancora nel repository. Scaricare i file con i nomi indicati nel manifest in `Assets/UnderGrind/Animations`; il builder li importa come Humanoid, assegna le clip per nome e genera l'Animator Controller con **UnderGrind → Player → Refresh Clips and Build Animator Controller**. La mesh X Bot inclusa negli FBX è solo un riferimento per l'animazione e non sostituisce il personaggio UnderGrind.

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

La modifica dei file nel repository non equivale a una verifica nell'Editor Unity. Importazione Blender, compilazione C#, rendering, EditMode e PlayMode devono essere eseguiti sul progetto aperto in Unity. La presenza di FBX, Avatar Humanoid e Animator validi deve essere verificata sui modelli definitivi.
