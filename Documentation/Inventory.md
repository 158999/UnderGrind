# Inventario del giocatore

## Modello dei dati

`ItemDefinition` è un asset ScriptableObject che definisce un tipo di oggetto:

- ID stabile dell'oggetto.
- Nome visualizzato.
- Icona.
- Numero massimo di unità per stack.
- Peso unitario in chilogrammi.

Il peso deve essere finito e non negativo. Gli oggetti a peso zero sono ammessi. La quantità è memorizzata nello slot, non nella definizione dell'oggetto.

`InventorySettings` è l'asset ScriptableObject che configura la capacità dell'inventario e la penalità da sovraccarico. L'asset iniziale si trova in `Assets/UnderGrind/Settings/PlayerInventorySettings.asset`.

## Capacità e sovraccarico

La capacità in slot e il peso massimo sono due regole distinte:

- La capacità in slot limita il numero di posizioni disponibili e la quantità di oggetti raccoglibili in base alle dimensioni massime degli stack.
- Il peso massimo è una soglia di penalità, non un blocco alla raccolta. L'inventario può superarla.
- Il peso corrente è derivato dal contenuto degli slot e non può essere modificato direttamente.

Quando `CurrentWeightKg` è maggiore di `MaxCarryWeightKg`, `PlayerInventory.IsOverloaded` diventa vero. Il controller del personaggio disabilita corsa e salto e riduce la velocità di camminata della percentuale definita in `OverloadedWalkSpeedReductionPercent`. La riduzione è fissa e non cresce con il peso eccedente. Raggiungere esattamente la soglia non costituisce sovraccarico.

Il valore iniziale della soglia è 50 kg e quello della penalità è 25%; entrambi sono modificabili dall'Inspector nell'asset di configurazione. La capacità iniziale è di 24 slot.

## Operazioni disponibili

`PlayerInventory` espone:

- `AddItem(item, quantity)`: aggiunge quanto possibile in base agli slot e agli stack; non rifiuta oggetti per superamento del peso massimo. Restituisce quantità richiesta, aggiunta e residua.
- `RemoveItem(item, quantity)`: rimuove l'oggetto dagli stack disponibili e restituisce il risultato effettivo.
- `MoveStack(source, destination, quantity)`: sposta una quantità verso uno slot vuoto o uno stack dello stesso tipo.
- `SplitStack(source, destination, quantity)`: separa una parte di uno stack in uno slot vuoto.
- `Changed`: evento emesso quando un'operazione modifica il contenuto.

Il peso totale viene ricalcolato dal contenuto reale. Spostare o dividere gli oggetti non cambia il peso complessivo.

## Ambito

Questa versione comprende il modello dati, gli slot, la capacità, il calcolo del peso, le operazioni fondamentali e l'integrazione delle penalità nella locomozione.

Non comprende interfaccia grafica, raccolta dal mondo, equipaggiamento, utilizzo degli oggetti o salvataggio persistente. Questi sistemi richiedono specifiche e implementazioni successive.
