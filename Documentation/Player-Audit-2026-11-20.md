# Audit tecnico UnderGrind — 20 novembre 2026

## Riferimento

- Repository: `158999/UnderGrind`
- Branch: `main`
- Unity richiesto dal progetto: `6000.6.9f1`
- Pacchetti: Input System `1.20.1`, URP `17.6.0`, Test Framework `1.8.0`, uGUI `2.6.0`.
- Sorgente umanoide versionata: `Assets/UnderGrind/Art/Source/humans.blend` (645.776 byte).
- Ultimo commit esaminato: `923080faa71ac18455ca42d2fca80d403d009897` (`tests`), che aggiorna i report locali versionati.

## Stato dei test

L'ultimo report locale versionato è `TestResults/test/all-latest.json`, run `20261120-095537-299`, aggiornato il 20 novembre 2026 alle 09:56 (ora locale).

| Suite | Risultato XML/TRX | Esito aggregato |
|---|---:|---|
| Diagnostics standalone | 6/6 superati | Passed |
| Unity EditMode | 33/33 superati | Report valido, ma runner segnato Failed |
| Unity PlayMode | 13/13 superati | Report valido, ma runner segnato Failed |

La run `20261120-095537-299` riporta stato aggregato `Passed`, exit code standalone 0 e Unity 0. I 46 test Unity sono validi: 33 EditMode e 13 PlayMode, senza errori di compilazione, timeout, test falliti o inconclusive. I 6 test standalone Diagnostics sono anch'essi superati. Totale: 52 test superati su 52. Il precedente report `20261120-091314-434` era `Failed` per l'acquisizione errata del codice di uscita Unity; la correzione dei runner `run-unity-tests.ps1` e `run-all-tests.ps1` è ora verificata da questa nuova esecuzione locale.

La pipeline è stata estesa con `Tools/Testing/test-process-runner.ps1`, che avvia un processo figlio con exit code noto (23) e verifica acquisizione di stdout e stderr. La run CI `44706657031` sul commit `923080faa71ac18455ca42d2fca80d403d009897` è conclusa con successo: parsing PowerShell, smoke test eseguito sotto Windows PowerShell 5.1 e test standalone .NET superati. La CI **non avvia Unity**; i 46 test Unity sono stati eseguiti localmente con Unity `6000.6.9f1` e sono riportati nei file versionati.

## Stato delle fondamenta del Player

| Area | Implementato nel repository | Limite verificato |
|---|---|---|
| Locomozione | Controller, ScriptableObject, input, camera, postura, salto e test | Nessuna verifica visiva sul personaggio definitivo |
| Input | Input System, rebinding, persistenza degli override | Manca una schermata UI dedicata al rebinding |
| Inventario | Slot, stack, peso, sovraccarico, UI, raccolta e rilascio | Mancano salvataggio persistente, contenitori, equipaggiamento e uso oggetti |
| Interazione | SphereCast dalla camera e pickup/rilascio | Non verificata nella scena di gioco definitiva |
| Rig | Due armature Blender con 22 ossa ciascuna; 12 mesh separate con parent, modificatori e pesi verificati | FBX di output, Avatar Humanoid Unity e prefab non ancora generati/verificati |
| Animazione | Settings, driver e builder Animator | Nessuna clip FBX importata e nessun Animator Controller generato versionato |
| Scena | Scena LocomotionTest e builder con validazione prefab | La scena versionata contiene ancora geometria proxy; non è una prova del personaggio riggato |
| CI | Build e test standalone .NET, parsing degli script PowerShell | Nessun test Unity automatico nella CI corrente |

## Personaggi e pipeline 3D

Il progetto Blendstra `UnderGrind — Humans Source Separation`, versione `version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07`, contiene:

- due armature distinte: `UG_Male_Armature` e `UG_Female_Armature`;
- sei mesh separate per personaggio: Body, Head, TP Arms, TP Hands, FP Arms e FP Hands;
- le mesh originali complete maschile e femminile conservate come sorgenti.

Il 20 novembre è stato eseguito un audit Python direttamente in Blender sulla versione corrente pulita `version_f438fed3b4ebd67a0ac2e4edb0a391c017544b8d5c01ded12d03275cbeb397a6`. Entrambe le armature (`UG_Male_Armature` e `UG_Female_Armature`) contengono 22 ossa, 21 delle quali deformanti; tutte le 18 ossa richieste dal controllo Humanoid sono presenti. Per ciascun personaggio sono state verificate le sei mesh separate (Body, Head, TP Arms/Hands, FP Arms/Hands): parent diretto all'armatura corretta, modificatore Armature collegato, 21 gruppi deformanti, tutti i vertici con almeno un peso deformante valido, zero vertici non pesati e geometria non vuota. Totale: 12/12 mesh superate. Il confronto strutturale con la versione base non registra modifiche alle mesh; la geometria interna delle armature resta fuori dalla copertura dell'ispettore, ma i dati delle ossa e dei pesi sono stati letti direttamente dallo script Blender. L'audit ha prodotto una versione tecnica temporanea con marker, poi rimossa dalla testa del progetto: la versione corrente è stata ripristinata alla versione pulita `f438fed3`.

La verifica certifica struttura e pesi, non la qualità visiva delle deformazioni, l'export FBX o la validità degli Avatar Unity. Lo script `Tools/Blender/export-undergrind-humans.py` effettua i controlli fail-fast prima dell'export. È stato aggiunto `Tools/Blender/run-export.ps1` per avviare Blender in background, acquisire gli stream e verificare report e file FBX; non è stato ancora eseguito perché in questo ambiente non è disponibile un eseguibile Blender locale. Nel repository non risultano ancora FBX riggati né prefab maschile/femminile generati.

## Animazioni selezionate

Sono stati acquisiti dalla libreria gratuita PINOC otto asset per la locomozione di base. Gli ID e la distinzione dei formati sono registrati in `Documentation/Player-Animation-Asset-Intake-2026-11-20.md`.

Per il retarget del personaggio UnderGrind è previsto il formato `mixamo-fbx` (mesh X Bot e scheletro con nomenclatura Mixamo), non il formato `splat-skeleton-fbx` (scheletro MetaHuman a 441 ossa, destinato alla pipeline Splat Engine). Gli asset sono acquisiti nella libreria PINOC, ma non sono ancora stati scaricati nel repository, importati in Unity o verificati sul rig UnderGrind. I link di download PINOC sono firmati e temporanei: non devono essere salvati nella documentazione.

Il builder Animator già presente si aspetta le clip in `Assets/UnderGrind/Animations`, le configura secondo `PlayerAnimationSettings.asset` e genera `PlayerAnimator.controller` solo quando tutte le clip richieste sono disponibili.

## Priorità tecniche

1. **Chiudere la pipeline dei personaggi:** eseguire `./Tools/Blender/run-export.ps1` su Windows con Blender installato; l'audit dei pesi è superato, quindi passare all'export FBX e verificare i due file.
2. **Validare i due Avatar:** ottenere due Avatar Humanoid validi e distinti, verificare il mapping delle ossa e le deformazioni di entrambe le mesh.
3. **Generare i prefab:** eseguire `Build Male and Female Prefabs From Configured FBX` e controllare renderer separati per testa, corpo, arti FP e arti TP.
4. **Integrare le animazioni:** scaricare i clip Mixamo-FBX selezionati, importarli come Humanoid, verificare il retarget e generare il controller.
5. **Integrare il Player nella scena:** rigenerare la scena soltanto dopo la validazione dei prefab; provare prima/terza persona, visuale verso il basso, movimento, salto e accovacciamento.
6. **Aumentare la copertura d'integrazione:** aggiungere test che carichino i prefab reali e verifichino Avatar, renderer e controller; i test con GameObject costruiti artificialmente non sostituiscono questa verifica.
7. **Completare l'input UI:** realizzare una schermata di rimappatura che usi il controller esistente e i relativi parametri configurabili.

## Limiti della verifica

In questa sessione sono stati ispezionati repository, report, log e sorgenti; è stato eseguito un audit Blender CPU sul rig. Non è stato possibile eseguire Unity Editor o una build standalone del gioco nell'ambiente di lavoro corrente. Nessuna verifica visiva delle deformazioni, degli Avatar Unity o del gameplay reale viene quindi dichiarata superata.
