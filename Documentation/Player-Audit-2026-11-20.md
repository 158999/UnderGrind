# Audit tecnico UnderGrind — 20 novembre 2026

## Riferimento

- Repository: `158999/UnderGrind`
- Branch: `main`
- Unity richiesto dal progetto: `6000.6.9f1`
- Pacchetti: Input System `1.20.1`, URP `17.6.0`, Test Framework `1.8.0`, uGUI `2.6.0`.
- Sorgente umanoide versionata: `Assets/UnderGrind/Art/Source/humans.blend`, SHA-256 `e74b80b77a8b7a9cfe607d2106de6a35bb65117548c91d587e6a77df0310e981`.
- FBX esportati e versionati: `Human_Male_Rigged.fbx` (209.468 byte) e `Human_Female_Rigged.fbx` (212.300 byte).
- Ultimo commit verificato prima della revisione corrente: `52fb7fafc864e597e21d564287914003b94bb0a9` (`created fbxs`); la correzione del builder Animator è registrata nei commit successivi di `main`.

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

Il progetto Blendstra `UnderGrind — Humans Source Separation`, versione `version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07`, contiene:

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
