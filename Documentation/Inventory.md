# Inventario del giocatore

## Modello dei dati

`ItemDefinition` è un asset ScriptableObject che definisce un tipo di oggetto:

- ID stabile dell'oggetto.
- Nome visualizzato e icona.
- Prefab da istanziare nel mondo quando l'oggetto viene rilasciato.
- Numero massimo di unità per stack.
- Peso unitario in chilogrammi.

Il peso deve essere finito e non negativo. Gli oggetti a peso zero sono ammessi. La quantità è memorizzata nello slot, non nella definizione dell'oggetto.

`InventorySettings` configura capacità e sovraccarico. L'asset iniziale è `Assets/UnderGrind/Settings/PlayerInventorySettings.asset`.

## Capacità e sovraccarico

La capacità in slot e il peso massimo sono due regole distinte:

- La capacità in slot limita le posizioni utilizzabili per nuove unità. Se viene ridotta mentre gli slot oltre il nuovo limite contengono oggetti, tali contenuti vengono preservati come overflow e non ricevono nuovi oggetti finché non rientrano nella capacità configurata.
- Il peso massimo è una soglia di penalità, non un blocco alla raccolta. L'inventario può superarla.
- Il peso corrente è derivato dal contenuto degli slot e non può essere modificato direttamente.

Quando `CurrentWeightKg` è maggiore di `MaxCarryWeightKg`, `PlayerInventory.IsOverloaded` diventa vero. Il controller disabilita corsa e salto e riduce la velocità di camminata della percentuale definita in `OverloadedWalkSpeedReductionPercent`. La riduzione è fissa e non cresce con il peso eccedente. Raggiungere esattamente la soglia non costituisce sovraccarico.

I valori iniziali sono 24 slot, 50 kg e una riduzione del 25%. Sono modificabili nell'Inspector.

## Operazioni disponibili

`PlayerInventory` espone:

- `AddItem(item, quantity)`: riempie prima gli stack dello stesso tipo e poi gli slot vuoti. Non rifiuta oggetti per superamento del peso massimo. Restituisce quantità richiesta, aggiunta e residua.
- `RemoveItem(item, quantity)`: rimuove l'oggetto dagli stack disponibili.
- `RemoveFromSlot(index, quantity)`: rimuove una quantità da uno slot specifico.
- `SelectSlot(index)`: cambia lo slot attivo.
- `MoveStack(source, destination, quantity)`: sposta una quantità verso uno slot vuoto o uno stack dello stesso tipo.
- `SplitStack(source, destination, quantity)`: separa una parte di uno stack in uno slot vuoto.
- `Changed`: evento emesso quando un'operazione modifica contenuto o selezione.

Le operazioni che modificano il contenuto restituiscono un `InventoryOperationResult` con quantità richiesta, elaborata e residua. Il peso totale viene derivato dagli slot; spostare o dividere oggetti non cambia il peso complessivo.

## Interfaccia dell'inventario

`PlayerInventoryUI` crea una Canvas Screen Space Overlay durante l'inizializzazione del Player. Visualizza:

- Gli slot configurati, fino al limite visibile.
- L'icona, il nome e la quantità di ogni stack.
- Lo slot selezionato.
- Il peso trasportato e la soglia di sovraccarico.

`InventoryUISettings` si trova in `Assets/UnderGrind/Settings/PlayerInventoryUISettings.asset`. Dimensioni, spaziatura, numero di colonne, colori, font, testi, risoluzione di riferimento e aree di icona/etichetta sono modificabili nell'Inspector. Con `MaxVisibleSlots = 0` vengono mostrati tutti gli slot; con un valore maggiore la navigazione si limita agli slot visibili. Il componente aggiorna la vista tramite l'evento `PlayerInventory.Changed`.

La UI si apre e si chiude con Tab. Quando è aperta, il cursore viene liberato; alla chiusura viene ripristinato lo stato precedente. Le parentesi quadre destra e sinistra cambiano lo slot selezionato. Il gamepad usa Select per aprire l'inventario e il D-pad orizzontale per cambiare slot.

## Raccolta e rilascio nel mondo

`WorldItemPickup` rappresenta un oggetto raccoglibile nella scena. Contiene un riferimento a `ItemDefinition` e la quantità disponibile. La raccolta trasferisce nell'inventario la quantità che trova posto; se lo spazio non basta, il pickup conserva la quantità residua.

`PlayerInteractionController` esegue una SphereCast dalla camera del giocatore. Distanza, raggio e layer interrogati sono configurati da `PlayerInteractionSettings` in `Assets/UnderGrind/Settings/PlayerInteractionSettings.asset`.

- **E** (gamepad: pulsante ovest) raccoglie il pickup più vicino intercettato dal raggio.
- **G** (gamepad: D-pad giù) rilascia dallo slot selezionato la quantità configurata.
- Il prefab mondo è assegnato nella relativa `ItemDefinition`. Se manca, il rilascio viene rifiutato e l'inventario non viene modificato.
- Il rilascio istanzia il prefab, configura il componente `WorldItemPickup` e solo dopo rimuove la quantità dall'inventario. Se il trasferimento non si completa, l'operazione viene annullata e la quantità elaborata viene restituita all'inventario.
- Distanza, altezza, impulso fisico e quantità per azione sono configurabili nell'Inspector.

La raccolta viene ignorata mentre l'interfaccia è aperta. Il rilascio resta disponibile anche con l'interfaccia aperta, per consentire di rilasciare lo slot selezionato.

## Limiti attuali

Non sono ancora implementati equipaggiamento, utilizzo degli oggetti, trascinamento con mouse, ordinamento automatico, contenitori esterni, commercio o salvataggio persistente. Gli oggetti rilasciati richiedono un prefab mondo assegnato alla definizione. L'interazione è basata sulla camera e non su un sistema di prossimità indipendente dalla visuale.
