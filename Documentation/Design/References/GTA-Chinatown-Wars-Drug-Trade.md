# Studio di riferimento — economia di spaccio in GTA: Chinatown Wars

**Stato:** analisi comparativa e proposta preliminare. Nessuna meccanica descritta in questo documento è approvata per l'implementazione in UnderGrind.

## 1. Cosa fa Chinatown Wars

Le fonti consultate descrivono un'attività economica strutturata, non una semplice interazione di vendita:

- Il giocatore compra merce a un prezzo e cerca contatti disposti a pagarla di più. Ogni gruppo criminale ha preferenze di acquisto e vendita e opera in territori specifici.
- La rete è ampia: la guida consultata elenca nove gang coinvolte nel mercato e ottanta dealer. La varietà nasce dalle preferenze e dalla posizione dei contatti, non da un singolo menu universale.
- I contatti possono inviare offerte temporanee: un dealer può proporre un prezzo migliore per una merce per due giorni di gioco.
- Esistono due livelli di stoccaggio: una riserva permanente nella safehouse e una borsa trasportata dal personaggio. La riserva non viene persa durante un arresto; la merce trasportata può essere confiscata.
- Oltre all'acquisto, la merce può arrivare da attività di recupero e furto di carichi, incursioni ai magazzini, cassonetti e missioni.
- Il rischio della transazione dipende anche dalla quantità: gli arresti durante uno scambio possono attivare una risposta della polizia e la probabilità aumenta con il volume trattato.
- La sorveglianza urbana modifica il rapporto fra rischio e profitto. GameSpot descrive le telecamere come un fattore che aumenta il rischio e la redditività degli scambi nelle loro vicinanze; distruggerle riduce il rischio ma anche il guadagno ottenibile. GTABase dettaglia il cambiamento di prezzo associato alla distruzione delle telecamere. Le fonti concordano quindi sul principio di design: la sorveglianza collega direttamente rischio e convenienza economica.

Fonti: [GameSpot — Hands-On, 22 gennaio 2009](https://www.gamespot.com/articles/grand-theft-auto-chinatown-wars-hands-on/1100-6203423/) e [GTABase — Drug Dealing Guide](https://www.gtabase.com/side-missions/gta-chinatown-wars/drug-dealing).

## 2. Confronto GameGene

GameGene non restituisce al momento un record dedicato a *GTA: Chinatown Wars*. Non vengono quindi attribuite a GameGene informazioni che non contiene. La ricerca ha trovato precedenti utili in altri titoli:

| Gioco | Meccanica documentata | Possibile lezione di design | Confidenza del record |
|---|---|---|---|
| [Scarface: The World Is Yours — Delivery runs test greed](https://gamegene.app/library/records/claims/e848ae11-c114-4a84-82bb-55f9f8de1090) | Una consegna a tempo permette di scegliere una rotta più pericolosa per ottenere un premio maggiore; se il carico viene perso, si perde il guadagno. | La scelta della rotta deve essere una decisione economica, non soltanto navigazione. | Moderate |
| [Scarface — Open world from “be Tony” focus tests](https://gamegene.app/library/records/claims/dd149e71-076e-40e0-b587-b20ca8a4ee12) | L'open world sostiene la fantasia di costruire un impero criminale. | Il commercio può essere il primo gradino di una progressione più ampia. | Moderate |
| [The Godfather II — Builds a family instead of a lone wolf](https://gamegene.app/library/records/claims/ed80dddf-060b-47ff-aba9-0790f6624857) | Il giocatore recluta membri, li promuove e sostituisce quelli che non sono più utili. | Una rete di collaboratori può evolvere insieme al giocatore. | Strong |
| [The Godfather II — Crime rings worth defending](https://gamegene.app/library/records/claims/d4a80bfb-7d96-456e-b310-6daffc8cb0b0) | Il controllo completo di gruppi di attività concede bonus, ma rende quelle attività obiettivi da difendere. | Il controllo territoriale deve generare vantaggi e nuovi costi di mantenimento. | Moderate |
| [The Godfather II — Rival families play the same game](https://gamegene.app/library/records/claims/ef558d3f-da2d-441e-a599-7eb8f68e25fc) | Le famiglie rivali contendono gli stessi obiettivi e reagiscono alle mosse del giocatore. | Il mercato e il territorio non dovrebbero restare statici mentre il giocatore agisce. | Strong |
| [Prison Architect — Every addition carries a danger](https://gamegene.app/library/records/claims/8bacf089-216c-4203-8a6d-186e6905ad0a) | Ogni scelta vantaggiosa introduce anche una conseguenza negativa da gestire. | Ogni espansione commerciale dovrebbe aumentare opportunità e superficie di rischio. | Strong |
| [Schedule I — Farming loop as a drug business](https://gamegene.app/library/records/claims/5934d2ba-0825-4b0f-80aa-21f7e4e857bd) | Un ciclo di produzione, raccolta e reinvestimento sostiene la crescita dell'attività. | Se UnderGrind include produzione, questa deve essere un ciclo distinto dalla distribuzione. | Weak |

Le etichette di confidenza sono quelle riportate dai record GameGene; non equivalgono a una verifica indipendente di ogni dettaglio.

## 3. Proposta preliminare per UnderGrind

La parte più interessante da trasferire non è il numero di sostanze o di dealer, ma la combinazione di quattro sistemi:

1. **Mercato locale:** ogni zona ha domanda, disponibilità e prezzi propri. I contatti hanno preferenze e condizioni diverse.
2. **Informazioni temporanee:** messaggi, voci e contatti rivelano opportunità che scadono. Il giocatore decide se cambiare piano per sfruttarle.
3. **Rischio logistico:** il rischio dipende da quantità trasportata, zona, sorveglianza, presenza della polizia e modalità della transazione.
4. **Progressione della rete:** reputazione, fiducia e controllo di punti di distribuzione sbloccano nuovi contatti e opportunità, ma generano rivalità e costi di difesa.

### Loop da valutare

`Informazioni → approvvigionamento → deposito → carico trasportato → scelta del contatto e della rotta → transazione → risposta del mondo → reinvestimento`

La transazione dovrebbe essere il punto di verifica del rischio, non una vendita automatica al semplice contatto con un NPC. Il giocatore deve poter scegliere se accettare il prezzo, trattare, rinunciare o allontanarsi.

### Integrazione con i sistemi Player già presenti

- `ItemDefinition` può rappresentare i beni commerciabili, con identificativo, nome, icona, stack e peso unitario.
- `PlayerInventory` può contenere la merce trasportata e calcolarne il peso.
- La regola di sovraccarico già implementata trasforma il carico in una scelta di mobilità: superata la soglia configurata, corsa e salto vengono disabilitati e la camminata rallenta. Non serve introdurre un secondo limite rigido di peso.
- `PlayerInteractionController` è il punto d'ingresso per l'interazione con dealer, casse e carichi nel mondo.
- Un futuro sistema di deposito deve essere separato dall'inventario trasportato: la merce custodita in una safehouse non dovrebbe essere trattata come se fosse nello zaino.
- Il graybox può essere usato per verificare tempi di percorrenza, vie di fuga, ostacoli e conseguenze del sovraccarico prima di costruire una mappa commerciale completa.

## 4. Architettura da valutare, non ancora approvata

Se il concept viene confermato, la separazione consigliata è:

- `MarketProductDefinition`: identità del prodotto, peso, valore base e parametri di mercato.
- `DistrictMarketDefinition`: domanda, offerta, variazione e collegamento con gang o gruppi locali.
- `DealerDefinition`: preferenze, prezzi, quantità acquistabili, fiducia e condizioni d'accesso.
- `MarketSimulationSettings`: frequenza degli aggiornamenti, limiti di prezzo, durata delle offerte e volatilità.
- `DealerNetwork`: contatti scoperti, reputazione e progressione.
- `SafehouseStash`: deposito separato dall'inventario trasportato.
- `TransactionRiskSettings`: rischio di arresto, quantità, sorveglianza, zona e risposta della polizia.

Tutti i parametri di gameplay dovranno essere configurabili nell'Inspector tramite ScriptableObject o asset equivalenti; i sorgenti non dovranno contenere valori di bilanciamento hardcoded.

## 5. Decisioni ancora aperte

Prima di implementare servono le regole del sistema che hai già in mente. In particolare:

- Il giocatore acquista soltanto, produce, ruba carichi o combina queste attività?
- I dealer sono NPC fissi, contatti mobili o entrambi?
- Il mercato è guidato da zone e fazioni, da domanda/offerta simulata o da eventi narrativi?
- La merce confiscata è persa, recuperabile o collegata a una missione?
- La rete commerciale deve portare al controllo del territorio, a un'attività gestita da collaboratori o restare un sistema personale?
- Quanto deve essere simulato il processo di vendita: scelta rapida da menu, trattativa, incontro nel mondo o sequenza interattiva?

**Nessuna di queste decisioni viene considerata approvata.** Il passo successivo è confrontare questo schema con il tuo loop già progettato e selezionare le meccaniche da conservare, modificare o scartare.
