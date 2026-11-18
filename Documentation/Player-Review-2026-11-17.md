# Revisione dell'implementazione del Player — 17 novembre 2026

## Ambito della revisione

La revisione ha confrontato il codice del Player, i test, la gerarchia dei modelli sorgente e i formati delle animazioni disponibili.

Sono stati esaminati:

- Responsabilità, correttezza, casi limite e copertura dei test dei componenti C#.
- Gerarchia e limiti dei modelli sorgente ispezionati in Blendstra.
- Formati effettivamente disponibili per i sei movimenti selezionati nella libreria PINOC.
- Stato del repository e risultati delle esecuzioni GitHub Actions.

Questa revisione riguarda codice e asset. Non costituisce una verifica visiva nell'Editor Unity.

## Risultati

### I modelli sorgente non consentono di nascondere soltanto la testa

La versione ispezionata contiene due oggetti mesh: una mesh statica completa per `Human_Male` e una per `Human_Female`. Nessuna delle due ha uno scheletro, pesi di deformazione o un renderer separato per la testa.

I test precedenti di `PlayerVisualRig` utilizzano una gerarchia artificiale con renderer distinti per testa, corpo e braccia. Dimostrano la logica di visibilità del componente, ma non verificano il comportamento sul modello sorgente reale.

È stato predisposto un progetto Blendstra separato per creare derivati riggati senza alterare le mesh originali. Il tentativo di generazione disponibile è però terminato con l'errore `DUPLICATE_OR_INVALID_OBJECT_ID`; non è stata salvata una versione utilizzabile. La separazione della testa e la pesatura delle mesh restano quindi da realizzare e verificare.

### I formati PINOC hanno finalità differenti

I sei movimenti selezionati espongono due famiglie di file FBX:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese quelle facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con Splat Engine e rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomi Mixamo. È destinato al retargeting tramite Blender o altri strumenti DCC.

Per il normale rig umanoide di UnderGrind, il formato Mixamo è il candidato pertinente. X Bot è soltanto un modello di supporto al retargeting e non deve sostituire il corpo del giocatore. I sei movimenti risultano disponibili nella libreria, ma non sono ancora importati nel repository né trasferiti sul rig UnderGrind.

### Associazione dei renderer

- Le associazioni esplicite di `PlayerVisualRig` hanno priorità sulla ricerca euristica per nome.
- `ConfigureFromHierarchy` riconosce la convenzione `*_Head`, `*_FP_Arms` e `*_FP_Hands`.
- Un test PlayMode verifica la convenzione dei nomi, la visibilità della testa e delle braccia in prima persona e il mantenimento della visibilità di renderer non pertinenti, come i capelli.
- Il test utilizza una gerarchia sintetica: non certifica il modello generato.

### Sicurezza dei parametri Animator

`PlayerAnimationDriver` rileva la sostituzione del Runtime Animator Controller durante l'esecuzione e controlla l'esistenza e il tipo dei parametri prima di chiamare `SetFloat` o `SetBool`.

### Selezione delle clip

Il generatore della scena di test esclude le clip di accovacciamento dalla selezione degli stati normali di riposo, camminata, corsa e salto. Per gli stati di accovacciamento richiede invece clip con i relativi termini identificativi.

## Criticità ancora aperte

- La linea di separazione della testa e i pesi automatici devono essere verificati sul modello derivato.
- Non esiste ancora una mesh dedicata per braccia e mani in prima persona.
- La verifica di collisione della telecamera usa una maschera di layer configurabile: il prefab definitivo deve escludere il layer del giocatore oppure il filtro deve impedire in modo affidabile le collisioni con il corpo stesso.
- Il generatore della scena cerca attualmente clip incorporate in `humans.blend`; non importa né individua automaticamente i file FBX esterni provenienti da PINOC.
- Le soglie tra riposo, camminata e corsa sono tarate sui valori di 1,40 m/s e 1,8667 m/s, ma richiedono una verifica visiva durante la riproduzione.
- Nel repository non è ancora presente un Animator Controller di produzione né un Avatar Humanoid verificato per il modello definitivo.

## Modifiche già presenti nel codice

- Le associazioni esplicite di `PlayerVisualRig` sono state rese prioritarie rispetto alla ricerca euristica.
- È stata aggiunta l'associazione tramite la convenzione stabile dei nomi delle parti.
- Sono stati aggiunti test PlayMode per la precedenza delle associazioni esplicite e per la ricerca tramite gerarchia.
- È stato aggiunto il controllo dei tipi dei parametri Animator e il rilevamento delle sostituzioni del controller.
- Le clip di accovacciamento sono state escluse dalla selezione degli stati di locomozione ordinaria.
- La documentazione dei formati PINOC è stata corretta distinguendo gli export Splat da quelli Mixamo.

## Verifiche

Al momento della revisione del 17 novembre, i controlli autonomi C# associati ai commit esaminati risultavano superati. I test Unity eseguiti in precedenza avevano superato la baseline, ma le modifiche successive dovevano ancora essere verificate nell'Editor.

Aggiornamento dei risultati disponibili al 18 novembre 2026:

- Test autonomi C#: 6 superati su 6.
- Test Unity PlayMode: 7 superati su 7.
- Test Unity EditMode: 0 eseguiti.
- Ispezione visiva del modello e della scena: non effettuata.

Il completamento del Player non può essere dichiarato finché i prefab maschile e femminile non dispongono di un rig Humanoid verificato, parti di testa e arti separatamente indirizzabili, animazioni compatibili, importazione Unity corretta, test superati e verifica visiva.
