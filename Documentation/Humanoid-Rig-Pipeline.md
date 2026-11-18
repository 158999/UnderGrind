# Pipeline di preparazione dei modelli umanoidi

## Modelli sorgente e conservazione

Il progetto Blendstra [UnderGrind — Humans Source Separation](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3) contiene le sorgenti maschile e femminile:

- `Human_Male`: altezza modellata 1,78 m; 260 vertici e 486 triangoli.
- `Human_Female`: altezza modellata 1,70 m; 284 vertici e 529 triangoli.

Le due mesh originali devono rimanere conservate. Le modifiche di rigging e le varianti sono derivate e non devono sostituire le sorgenti.

L'esportazione di verifica non copre la configurazione dei modificatori e le coordinate UV valutate. Questi aspetti non sono certificati dai conteggi geometrici.

## Stato del prototipo di rig

La versione salvata `version_9333bec316691d50b66236e6ed6e172d9dbb5921f28563f3d3ae9b2b96ff0591` contiene:

- Le mesh originali `Human_Male` e `Human_Female`, conservate e nascoste dal rendering.
- Copie `Human_Male_Body` e `Human_Female_Body`, con gli stessi conteggi di vertici e triangoli delle rispettive sorgenti.
- Le radici armature `UG_Male_Armature` e `UG_Female_Armature`.

Il codice che ha generato il prototipo colloca le ossa usando proporzioni ricavate dai bounding box e prova ad assegnare i pesi automatici di Blender, con una distribuzione per prossimità come fallback. Questi passaggi non sono ancora verificati con test di deformazione. L'esportazione nativa disponibile non espone il dettaglio delle ossa né la configurazione dei pesi. La versione risulta inoltre priva di canali di animazione.

Il prototipo non è quindi un rig Humanoid approvato per la produzione. In particolare, ogni personaggio ha ancora una sola mesh del corpo: non è presente un renderer separato per testa/viso. Non è possibile nascondere soltanto la testa in prima persona.

Il progetto separato [UnderGrind — Rigged Human Derivatives](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2493fca7-8542-4d61-8a81-12487e6a3999) non contiene una versione salvata: il tentativo precedente è terminato con `DUPLICATE_OR_INVALID_OBJECT_ID`.

Una revisione successiva è stata avviata per separare soltanto le isole geometriche già disconnesse, senza classificare automaticamente una parte come testa in base alla posizione. Il risultato deve essere ispezionato prima di assegnare nomi semantici o considerarlo utilizzabile.

## Importazione in Unity e generazione dei prefab

Dopo aver ottenuto e verificato un FBX derivato:

1. Importare il file FBX nel progetto Unity.
2. Selezionare l'asset nella finestra Project.
3. Eseguire **UnderGrind → Player → Build Prefabs From Selected Rigged FBX**.
4. Verificare che Unity riconosca il modello come Humanoid e generi un Avatar valido.
5. Controllare i prefab prodotti nella cartella `Assets/UnderGrind/Generated/Prefabs`.

Il generatore legge `PlayerRigSettings.asset` e `PlayerLocomotionSettings.asset`. Il FBX selezionato deve contenere entrambe le radici configurate, `UG_Male_Armature` e `UG_Female_Armature`, e deve produrre un Avatar Humanoid valido.

La configurazione iniziale richiede un renderer separato per la testa tramite la convenzione `*_Head`. Se la testa non è separata, il generatore rifiuta il prefab invece di applicare una divisione geometrica automatica non verificata. I renderer `*_FP_Arms` e `*_FP_Hands` sono opzionali nella configurazione attuale; se non esistono, il corpo riggato resta visibile in prima persona.

La scena di test preferisce i prefab riggati configurati. Se il prefab maschile non esiste, può usare la mesh statica maschile originale come riferimento visivo; in quel caso il personaggio si muove ma non dispone di un rig animabile.

## Sorgenti delle animazioni

Sono stati individuati nella libreria PINOC sei movimenti:

- Standing Idle
- Walking
- Running
- Jump
- Crouching Idle
- Crouch Walk Forward

Le due famiglie di esportazione FBX hanno finalità diverse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese le ossa facciali, senza mesh. È destinato ai personaggi PINOC con Splat Engine e rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomenclatura Mixamo. È il formato candidato per il retargeting tramite Blender o altri strumenti DCC verso il rig UnderGrind.

X Bot è un modello di supporto per il retargeting e non deve sostituire il corpo del personaggio UnderGrind. I sei movimenti non sono ancora importati nel repository né trasferiti sul rig definitivo. Prima di distribuire o includere gli export nel prodotto occorre verificare le condizioni d'uso applicabili.

Dopo l'importazione delle clip nella cartella `Assets/UnderGrind/Animations`, **UnderGrind → Player → Refresh Clips and Build Animator Controller** aggiorna i riferimenti e genera il controller usando `PlayerAnimationSettings.asset`. Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono richieste; Fall e Land sono opzionali.

## Criteri di completamento

- [ ] Mesh sorgenti maschile e femminile preservate e confrontate con i derivati.
- [ ] Isole geometriche disconnesse separate e ispezionate.
- [ ] Testa separata in un renderer indipendente, verificata da più angolazioni.
- [ ] Posizione delle ossa verificata su pose di riferimento.
- [ ] Pesi verificati con deformazioni di spalle, gomiti, polsi, anche, ginocchia e caviglie.
- [ ] Avatar Humanoid Unity valido per entrambi i personaggi.
- [ ] Prefab maschile e femminile generati e controllati.
- [ ] Mesh dedicate a braccia e mani in prima persona create e posate, se richieste dal modello finale.
- [ ] Animazioni PINOC in formato Mixamo trasferite sullo scheletro UnderGrind.
- [ ] Cicli di camminata e corsa controllati per scivolamento dei piedi e movimento della radice.
- [ ] Test PlayMode superati sul rig importato.
- [ ] Visuale in prima e terza persona controllata nella scena Unity.
