# Pipeline di preparazione dei modelli umanoidi

## Modelli sorgente e conservazione

Il progetto Blendstra [UnderGrind — Humans Source Separation](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2cdfaa98-3ea2-4a49-892a-b96488e12bc3) contiene la versione sorgente ispezionata dei due personaggi:

- `Human_Male`: altezza modellata di 1,78 m; 260 vertici e 486 triangoli nella mesh sorgente.
- `Human_Female`: altezza modellata di 1,70 m; 284 vertici e 529 triangoli nella mesh sorgente.

Ogni personaggio è rappresentato da un'unica mesh statica completa. Le mesh sorgenti non hanno uno scheletro, pesi di deformazione o un renderer separato per la testa. I due modelli originali devono rimanere invariati: scheletri, pesi, suddivisioni e varianti vanno realizzati su derivati distinti.

L'ispezione della versione sorgente ha rilevato anche modificatori e dati UV la cui configurazione non è coperta dall'esportazione di verifica utilizzata. Non è quindi possibile considerare verificati questi aspetti sulla sola base dei conteggi geometrici.

## Stato della generazione dei derivati

Il progetto separato [UnderGrind — Rigged Human Derivatives](https://blendstra.com/studio?workspace=wsp_fcfd842ff566eb960d6d885ff219b4f02b624b23&project=2493fca7-8542-4d61-8a81-12487e6a3999) è stato predisposto per non modificare il progetto sorgente.

L'ultimo tentativo di generazione è terminato con errore durante la risoluzione degli identificativi degli oggetti (`DUPLICATE_OR_INVALID_OBJECT_ID`). Il progetto dei derivati non contiene una versione salvata utilizzabile e non ha una sorgente associata nella versione attualmente aperta. Di conseguenza, non esiste ancora un modello riggato da importare in Unity.

Il tentativo previsto comprendeva:

1. Duplicazione delle mesh sorgenti e applicazione delle trasformazioni ai derivati.
2. Sistemazione del contatto con il terreno e del centraggio, senza alterare le proporzioni.
3. Creazione di scheletri distinti, `UG_Male_Armature` e `UG_Female_Armature`, con nomi delle ossa compatibili con il riconoscimento Humanoid di Unity.
4. Separazione della testa in un renderer indipendente.
5. Assegnazione automatica dei pesi alle mesh del corpo e della testa.

La soglia geometrica ipotizzata per separare la testa è soltanto una stima iniziale: la linea di separazione deve essere controllata sulle viste frontale, laterale e obliqua. Anche i pesi automatici devono essere verificati attraverso la deformazione delle articolazioni. Nessuno dei due passaggi è attualmente approvato.

## Importazione in Unity e generazione dei prefab

Dopo aver ottenuto e verificato un FBX derivato:

1. Importare il file FBX nel progetto Unity.
2. Selezionare l'asset nella finestra Project.
3. Eseguire **UnderGrind → Player → Build Prefabs From Selected Rigged FBX**.
4. Verificare che Unity riconosca il modello come Humanoid e generi un Avatar valido.
5. Controllare i prefab prodotti:
   - `Assets/UnderGrind/Generated/Prefabs/Human_Male_Rigged.prefab`
   - `Assets/UnderGrind/Generated/Prefabs/Human_Female_Rigged.prefab`

Il generatore richiede le radici `UG_Male_Armature` e `UG_Female_Armature`. I renderer della testa devono seguire la convenzione `*_Head` e vengono associati tramite `PlayerVisualRig.ConfigureFromHierarchy`.

Il generatore non produce ancora una geometria dedicata per braccia e mani in prima persona. In assenza di tale asset, la visuale in prima persona non può essere considerata completa: il personaggio non dispone ancora di un modello specifico per le braccia e le mani in camera.

## Sorgenti delle animazioni

Sono stati individuati e acquisiti dalla libreria PINOC sei movimenti:

- Standing Idle
- Walking
- Running
- Jump
- Crouching Idle
- Crouch Walk Forward

Per ciascun movimento sono disponibili due famiglie di esportazione FBX, con finalità differenti:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprensivo delle ossa facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con il pacchetto Unity `com.viggle.splat-engine` dalla versione 0.2.2 e richiede il rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomenclatura Mixamo. È il formato candidato per il retargeting tramite Blender o altri strumenti DCC verso lo scheletro umanoide di UnderGrind.

X Bot è un modello di supporto per il retargeting e non deve sostituire il corpo del personaggio UnderGrind. I sei movimenti non sono ancora presenti nel repository né sono stati trasferiti sullo scheletro definitivo. Prima di distribuire o includere gli export nel prodotto occorre verificare le condizioni d'uso applicabili del fornitore.

## Criteri di completamento

- [ ] Generazione dei derivati Blendstra completata e versione salvata identificata.
- [ ] Integrità delle mesh maschile e femminile originali confrontata con i derivati.
- [ ] Separazione della testa controllata da più angolazioni.
- [ ] Pesi verificati su tutte le principali articolazioni.
- [ ] Avatar Humanoid Unity valido per entrambi i personaggi.
- [ ] Prefab maschile e femminile generati.
- [ ] Mesh dedicate a braccia e mani in prima persona create e posate.
- [ ] Animazioni PINOC in formato Mixamo trasferite sullo scheletro UnderGrind.
- [ ] Cicli di camminata e corsa verificati per scivolamento dei piedi e movimento della radice.
- [ ] Test PlayMode superati e resa visiva controllata nell'Editor Unity.
