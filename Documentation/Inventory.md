# Inventario del giocatore

## Modello dei dati

Il sistema dell'inventario è composto da quattro elementi:

- `ItemDefinition`: asset `ScriptableObject` che descrive un tipo di oggetto.
- `InventorySlot`: coppia oggetto/quantità conservata in uno slot.
- `PlayerInventory`: componente che gestisce slot, operazioni, peso ed eventi.
- `InventoryOperationResult`: risultato di un'operazione con quantità richiesta, elaborata e residua.

`ItemDefinition` contiene:

- `ItemId`: identificativo stabile.
- `DisplayName`: nome mostrato nell'interfaccia.
- `Icon`: icona dello slot.
- `WorldPrefab`: prefab istanziato quando l'oggetto viene lasciato nel mondo.
- `MaxStackSize`: quantità massima per stack.
- `UnitWeightKg`: peso unitario in chilogrammi.

La validazione dell'asset impone uno stack massimo di almeno un'unità e un peso unitario finito e non negativo. Gli oggetti a peso zero sono ammessi. La quantità appartiene allo slot, non alla definizione dell'oggetto.

Uno slot è vuoto quando non contiene una definizione valida oppure quando la quantità è zero. Il peso dello slot è il prodotto tra peso unitario e quantità.

## Configurazione

`InventorySettings` è l'asset `ScriptableObject` che regola capacità e sovraccarico. L'asset presente nel progetto è `Assets/UnderGrind/Settings/PlayerInventorySettings.asset`.

| Proprietà | Significato |
|---|---|
| `MaxSlots` | Numero massimo configurato di slot |
| `MaxCarryWeightKg` | Soglia di peso oltre la quale si applicano le penalità |
| `OverloadedWalkSpeedReductionPercent` | Percentuale sottratta alla velocità di camminata |

I valori dell'asset attuale sono 24 slot, 50 kg e riduzione del 25%. Sono valori iniziali modificabili dall'Inspector.

`PlayerInteractionSettings` configura distanza e raggio della ricerca degli oggetti, layer ammessi, posizione di rilascio, impulso applicato al corpo fisico e quantità rilasciata per azione.

`InventoryUISettings` configura testi, font, dimensioni del pannello, risoluzione di riferimento, numero di colonne, dimensioni e spaziatura degli slot, ancoraggi di icone e testo, colori, padding, altezza dell'intestazione e numero massimo di slot visibili. Il generatore dell'Editor crea questo asset quando manca; non è presente come file versionato nella cartella Settings del repository.

## Capacità e sovraccarico

La capacità in slot e il peso trasportato sono regole distinte:

- La capacità limita il numero di posizioni disponibili e la quantità contenibile in base alla dimensione massima degli stack.
- Il peso massimo è una soglia di penalità, non un limite alla raccolta. Un oggetto può essere aggiunto anche se porta il peso totale oltre la soglia, purché ci sia spazio negli slot.
- Il peso corrente è derivato dagli oggetti e dalle quantità presenti negli slot; non è modificabile direttamente.
- Il sovraccarico si verifica soltanto quando `CurrentWeightKg` è strettamente maggiore di `MaxCarryWeightKg`. Raggiungere esattamente la soglia non attiva penalità.

Quando l'inventario è sovraccarico, `HumanLocomotionController` disabilita corsa e salto e riduce la velocità di camminata della percentuale configurata. La stessa riduzione si applica alla camminata accovacciata. La penalità è fissa: non aumenta in funzione dei chilogrammi eccedenti. Un salto già iniziato non viene interrotto a mezz'aria.

Quando il peso torna uguale o inferiore alla soglia, corsa, salto e velocità di camminata vengono ripristinati automaticamente.

## Operazioni di PlayerInventory

`PlayerInventory` espone le seguenti operazioni:

- `Configure(settings)`: assegna la configurazione e adegua il numero di slot, preservando gli slot occupati che non possono essere rimossi.
- `GetSlot(index)`: restituisce lo slot richiesto; genera `ArgumentOutOfRangeException` per un indice non valido.
- `AddItem(item, quantity)`: riempie prima gli stack esistenti dello stesso tipo e poi gli slot vuoti. Rispetta `MaxStackSize` e restituisce la quantità effettivamente aggiunta.
- `RemoveItem(item, quantity)`: rimuove l'oggetto dagli stack disponibili.
- `RemoveFromSlot(index, quantity)`: rimuove una quantità da uno slot specifico.
- `MoveStack(source, destination, quantity)`: sposta una quantità verso uno slot vuoto o verso uno stack dello stesso tipo, se la capacità residua è sufficiente.
- `SplitStack(source, destination, quantity)`: trasferisce una parte dello stack in uno slot vuoto senza svuotare quello sorgente.
- `SelectSlot(index)`: seleziona uno slot valido e segnala se la selezione è cambiata.

Le operazioni che modificano il contenuto o la selezione emettono l'evento `Changed`. `InventoryOperationResult` espone `Requested`, `Processed`, `Remaining` e `Completed`; le operazioni di aggiunta e rimozione possono quindi segnalare un risultato parziale senza perdere la quantità residua.

## Raccolta e rilascio nel mondo

`WorldItemPickup` rappresenta un oggetto raccoglibile nella scena. Contiene una definizione e una quantità. Quando `TryPickup` riceve un inventario valido, trasferisce la quantità che trova posto negli slot:

- se viene raccolta tutta la quantità, il GameObject viene distrutto;
- se l'inventario accetta soltanto una parte, il pickup rimane nella scena con la quantità residua;
- se non viene trasferita alcuna unità, il pickup non viene modificato.

`PlayerInteractionController` collega input, camera, inventario e pickup. L'azione di interazione esegue uno `SphereCastAll` dalla camera, filtra i layer configurati, ignora i collider appartenenti al Player e ordina i risultati per distanza. Il primo pickup valido trovato viene utilizzato.

Il rilascio riguarda lo stack selezionato. Richiede che `ItemDefinition.WorldPrefab` sia assegnato. Il componente crea il prefab davanti al giocatore, configura il relativo `WorldItemPickup` e rimuove dall'inventario la quantità definita in `DropQuantityPerAction`. Se la configurazione del pickup o la rimozione non riescono, l'oggetto creato viene eliminato e l'eventuale quantità già rimossa viene restituita all'inventario. Se il prefab contiene un `Rigidbody`, può ricevere l'impulso configurato.

Le azioni di interazione e rilascio sono risolte tramite `PlayerInputSettings`. La raccolta non dipende dal peso massimo: il peso produce una penalità di locomozione, non il rifiuto dell'oggetto.

## Interfaccia

`PlayerInventoryUI` crea a runtime un Canvas `ScreenSpaceOverlay` e un pannello con intestazione, peso trasportato e griglia degli slot. Ogni slot mostra l'icona, quando presente, il nome e la quantità dell'oggetto. Lo slot selezionato usa il colore dedicato; il peso viene evidenziato quando l'inventario supera la soglia.

La griglia è generata in base alla capacità effettiva e al limite di slot visibili. Layout, font, dimensioni, ancoraggi, testi e colori provengono da `InventoryUISettings`. L'interfaccia usa i componenti `Text`, `Image`, `GridLayoutGroup` e `GraphicRaycaster` di Unity UI.

I controlli disponibili sono:

- `ToggleInventory`: apre o chiude il pannello.
- `NextSlot`: seleziona lo slot successivo.
- `PreviousSlot`: seleziona lo slot precedente.

All'apertura, l'interfaccia salva lo stato di cattura del cursore, sopprime l'input di locomozione e rende libero il cursore. Alla chiusura ripristina la cattura se era attiva prima dell'apertura. La UI si iscrive all'evento `PlayerInventory.Changed` e aggiorna peso, icone, testi e selezione dopo le modifiche. Il metodo `Configure` rimuove la sottoscrizione precedente e si collega al nuovo inventario assegnato.

La versione attuale non implementa selezione tramite click, trascinamento degli stack o menu contestuali degli oggetti.

## Copertura dei test

I test EditMode dell'inventario verificano le operazioni sui dati e gli invarianti degli slot. I test PlayMode comprendono:

- penalità di sovraccarico e ripristino delle capacità di corsa e salto;
- raccolta di un pickup nella scena;
- rilascio di una quantità configurata dello stack selezionato;
- aggiornamento dell'interfaccia quando l'inventario viene assegnato dopo la creazione del componente.

La presenza dei test nel repository non certifica da sola l'esecuzione sulla revisione corrente. I risultati validi devono essere riferiti al commit e alla specifica esecuzione Unity.

## Funzionalità non implementate

Il sistema descritto non comprende ancora:

- salvataggio e caricamento persistente dell'inventario;
- equipaggiamento e slot dedicati;
- utilizzo degli oggetti;
- crafting e consumo di risorse;
- categorie, filtri e ricerca;
- interazione con gli slot tramite mouse, drag-and-drop o menu contestuali.
