# Intake animazioni Player — 20 novembre 2026

## Scopo

Selezione di clip di locomozione per il Player umanoide UnderGrind. Gli asset sono stati acquisiti dalla libreria di movimento PINOC senza generare nuovi contenuti e senza consumare crediti.

## Asset selezionati

| Uso | Clip PINOC | Asset ID |
|---|---|---|
| Idle | Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Camminata | Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Corsa | Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Salto | Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Caduta | Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Atterraggio | Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |
| Idle accovacciato | Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Camminata accovacciata | Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |

## Formato corretto

La libreria espone due famiglie di esportazione:

- **`mixamo-fbx`**: include mesh X Bot e scheletro con nomenclatura Mixamo. È il formato da usare per il retargeting DCC/Unity verso i rig umanoidi UnderGrind.
- **`splat-skeleton-fbx`**: scheletro MetaHuman a 441 ossa, senza mesh, pensato per personaggi PINOC con Splat Engine e rig Generic. Non è il formato da usare nel normale Animator Humanoid del Player.

Non confondere il formato di default riportato da PINOC (GLB scheletrico MetaHuman) con il formato `mixamo-fbx` disponibile per ciascuno degli otto asset. Per l'importazione nel progetto va richiesto esplicitamente `mixamo-fbx`.

## Destinazione e procedura

1. Recuperare un nuovo link firmato per ciascun asset tramite PINOC usando il relativo Asset ID e il formato `mixamo-fbx`. I link firmati scadono e non devono essere versionati.
2. Salvare i file in `Assets/UnderGrind/Animations`, usando nomi coerenti con le clip: `Standing_Idle.fbx`, `Walking.fbx`, `Running.fbx`, `Jump.fbx`, `Fall_down.fbx`, `Hard_Landing.fbx`, `Crouching_Idle.fbx`, `Crouch_Walk_Forward.fbx`.
3. In Unity, importare le clip con rig Humanoid e verificare che ogni Avatar di importazione sia valido. Il controller del Player deve usare il proprio Avatar per il retargeting.
4. Eseguire **UnderGrind → Player → Refresh Clips and Build Animator Controller**.
5. Verificare transizioni, loop, root motion, velocità percepita e allineamento dei piedi sul prefab maschile e femminile.

## Provenienza e diritti

Fonte: libreria curata PINOC, asset identificati dagli ID sopra. Il servizio descrive la libreria come gratuita da usare; la risposta dello strumento non espone un identificatore di licenza né chiarisce separatamente i diritti per distribuzione commerciale. Conservare provenienza e ID; verificare le condizioni del provider prima di una release commerciale.

## Stato iniziale — 20 novembre 2026

Al momento dell'acquisizione gli otto asset erano disponibili nella libreria PINOC ma non ancora presenti nel repository.

## Stato aggiornato — 24 novembre 2026

Gli otto FBX `mixamo-fbx` sono ora versionati in `Assets/UnderGrind/Animations`. I riferimenti alle otto clip e al controller sono assegnati in `Assets/UnderGrind/Settings/PlayerAnimationSettings.asset`. Ogni FBX usa un Avatar Humanoid sorgente; il Player usa il proprio Avatar maschile o femminile per il retargeting runtime. Non è stato eseguito un bake DCC delle curve sullo scheletro UnderGrind.

### Risultati Unity disponibili — run `20261124-114001-048`

La run Unity su 6000.6.9f1 ha eseguito 57 test EditMode e 13 PlayMode. Esito: 56/57 EditMode, 13/13 PlayMode e 69/70 test Unity complessivi. I test standalone C# sono 6/6. L'unico fallimento è `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars`, relativo alla clip originale `Crouch_Walk_Forward.fbx`: 6/232 incroci sulla sorgente, 10/232 sul Player maschile e 12/232 sul femminile. Non sono stati rilevati errori di compilazione.

I precedenti problemi Visual Rig sono risolti e verificati: il test ora distingue gli FBX sorgente dai prefab runtime e i quattro casi passano. Anche il test PlayMode sulla conservazione dei renderer inizialmente disabilitati passa.

### Ricerca di un sostituto per Crouch Walk

Otto candidati PINOC sono presenti in `Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk`, con importazione Humanoid e Avatar sorgente. Il report Blender grezzo non riproduce il retargeting Humanoid di Unity e non determina da solo il candidato definitivo.

Il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` è stato eseguito nella run `20261124-114001-048` e ha generato `TestResults/test/Unity/CrouchWalkCandidateAudit.txt`. Il candidato 08 è primo nella classifica diagnostica: zero incroci di piedi e ginocchia su sorgente, Player maschile e femminile; separazione minima dei piedi rispettivamente 0,254 m, 0,183 m e 0,155 m; errore di chiusura del loop 0 m. Il candidato 07 è anch'esso privo di incroci, ma ha una separazione minima dei piedi di 0,103 m sul Player femminile e una separazione minima delle ginocchia maggiore.

Il 24 novembre 2026 il candidato 08 è stato assegnato temporaneamente sulla base della classifica numerica, ma questa decisione è stata revocata: il superamento dei soli controlli di incrocio non dimostra che l'animazione sia utilizzabile. Nessuno degli otto candidati è approvato. Il test di locomozione verifica la clip effettivamente configurata; l'audit comparativo continua a includere la clip originale e tutti i candidati. La clip originale resta configurata e il suo difetto viene mantenuto visibile nei test fino a quando non sarà disponibile una sostituzione realmente valida.

### Limiti e diritti

La presenza di un asset nella libreria o nel progetto non certifica i diritti di distribuzione commerciale. Prima di una release commerciale verificare le condizioni d'uso del piano PINOC applicabile e conservare gli identificativi di provenienza riportati in questo documento.


## Nuova generazione Crouch Walk — 24 novembre 2026

È stata avviata e completata una generazione PINOC dedicata a un ciclo di camminata accovacciata in-place, con quattro campioni dello stesso prompt. Durata richiesta: 6 secondi. Costo registrato dal servizio: 6 crediti gratuiti.

Il prompt richiede:
- postura accovacciata mantenuta per tutto il ciclo, busto leggermente inclinato e testa stabile;
- alternanza sinistra/destra con trasferimento del peso, fase d'appoggio e fase di oscillazione;
- piedi e ginocchia sempre separati, senza incroci o compenetrazioni;
- piede d'appoggio quasi fermo, passi moderati, contro-rotazione naturale di bacino e spalle;
- ciclo continuo con posa iniziale/finale coerente;
- root centrato, senza traslazione o rotazione, per il Blend Tree in-place del Player.

| Campione | Nome PINOC | Asset ID | Formato richiesto per Unity |
|---|---|---|---|
| 01 | Crouch Walk (1) | `d441b0b6-a674-4849-9e9e-eeeb3b1b57c3` | `mixamo-fbx` |
| 02 | Crouch Walk (2) | `e81092b1-33f8-410e-8628-d67ec26fe36d` | `mixamo-fbx` |
| 03 | Crouch Walk (3) | `0ff23662-9938-41d3-8e99-def4e4d1e7de` | `mixamo-fbx` |
| 04 | Crouch Walk (4) | `17dc1540-bfe8-4f08-b293-5053305aaef8` | `mixamo-fbx` |

**Stato:** generati e disponibili per l'esportazione FBX; non ancora importati nel repository, retargettizzati in Unity, ispezionati visivamente o sottoposti alla matrice di test. Nessuno dei quattro è assegnato a `PlayerAnimationSettings.asset`. I link firmati PINOC sono temporanei e non vengono versionati; gli Asset ID sono il riferimento permanente per recuperarli.
