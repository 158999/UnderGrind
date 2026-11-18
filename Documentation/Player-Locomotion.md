# Player e locomozione umanoide

## Personaggio di riferimento e parametri di movimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m. Il modello femminile presente nella sorgente misura 1,70 m. Entrambi i modelli originali sono input immutabili: scheletri, pesi e modifiche visive devono essere realizzati su derivati separati.

Valori iniziali della locomozione:

- Camminata: 1,40 m/s.
- Corsa: camminata × 4/3 = 1,8667 m/s.
- Altezza massima teorica del salto: 0,40 m rispetto al punto di partenza.
- Gravità: 9,81 m/s².
- Altezza del CharacterController: 1,78 m.
- Raggio della capsula: 0,28 m.
- Altezza della capsula accovacciata: 1,12 m.

L'altezza del salto è un parametro di bilanciamento del gioco e non una misurazione biomeccanica.

## Componenti di runtime

`HumanLocomotionController` gestisce il movimento e il componente Unity `CharacterController`. Comprende input da tastiera, mouse e gamepad, accelerazione e decelerazione, salto con input bufferizzato e coyote time, verifica dello spazio disponibile per rialzarsi e gestione della telecamera in prima e terza persona.

`PlayerVisualRig` gestisce la visibilità dei renderer:

- In prima persona nasconde i renderer della testa.
- In terza persona nasconde gli eventuali renderer destinati esclusivamente alle braccia e alle mani in prima persona.
- Gli altri renderer mantengono lo stato di abilitazione originale.

`PlayerAnimationDriver` aggiorna soltanto i parametri effettivamente presenti nell'Animator Controller assegnato. Disabilita il root motion, evitando che l'Animator applichi uno spostamento aggiuntivo rispetto a quello gestito dal controller.

## Associazione delle parti del modello

Dopo l'importazione di un modello riggato, il prefab deve configurare il componente tramite `PlayerVisualRig.ConfigureFromHierarchy(rigRoot)`.

La convenzione dei nomi è:

- `*_Head`: testa, nascosta in prima persona e ripristinata in terza persona.
- `*_FP_Arms`: braccia dedicate alla prima persona.
- `*_FP_Hands`: mani dedicate alla prima persona.

Le parti `*_FP_Arms` e `*_FP_Hands` sono visibili in prima persona e nascoste in terza persona. Gli altri renderer restano visibili in entrambe le modalità, salvo configurazione esplicita.

Se non viene trovata una testa separata, `ConfigureFromHierarchy` restituisce `false` e non sostituisce la configurazione esistente. È inoltre disponibile `Configure`, che accetta gruppi espliciti di renderer. Le associazioni basate su parole presenti nei nomi sono una soluzione di ripiego per i prototipi, non il criterio previsto per i prefab di produzione.

La visuale in prima persona può nascondere soltanto la testa quando questa è un renderer indipendente o appartiene a un gruppo di renderer assegnato esplicitamente. Il corpo e le braccia devono rimanere visibili. In terza persona la testa viene ripristinata e la telecamera si posiziona dietro il personaggio, verificando gli ostacoli tramite uno sphere cast.

La sorgente attualmente ispezionata contiene una sola mesh statica completa per ciascun personaggio. Non dispone di una testa separata, di uno scheletro o di pesi di deformazione. Per questo motivo i test basati su gerarchie artificiali dimostrano il comportamento del componente, ma non dimostrano che la visuale in prima persona funzioni sul modello reale.

## Comandi

| Azione | Tastiera e mouse | Gamepad |
|---|---|---|
| Movimento | WASD / frecce | Stick sinistro |
| Visuale | Mouse | Stick destro |
| Corsa | Shift | — |
| Salto | Spazio | Pulsante sud |
| Accovacciamento | C | Pulsante est |
| Cambio telecamera | F5 | Pulsante nord |
| Sblocco cursore | Esc | — |
| Blocco cursore | Clic sinistro | — |

## Parametri dell'Animator

`PlayerAnimationDriver` può aggiornare i seguenti parametri:

- `Speed`: velocità in metri al secondo.
- `MoveX` e `MoveY`: direzione dell'input.
- `Grounded`: contatto con il terreno.
- `VerticalSpeed`: velocità verticale.
- `Running`, `Crouching`, `Jumping` e `Falling`: stati del movimento.

Prima di aggiornare un parametro, il componente controlla che esista e che abbia il tipo corretto. La sostituzione del Runtime Animator Controller viene rilevata durante l'esecuzione.

Il generatore della scena di test crea i parametri e gli stati idle, camminata, corsa, salto e accovacciamento quando trova clip compatibili e un Avatar valido. Non crea animazioni o Avatar fittizi quando la sorgente Blender non li contiene.

## Pipeline delle animazioni

I sei movimenti selezionati dalla libreria PINOC sono Standing Idle, Walking, Running, Jump, Crouching Idle e Crouch Walk Forward.

Gli export Mixamo contengono una mesh X Bot e uno scheletro con nomi Mixamo: sono candidati per il retargeting sullo scheletro UnderGrind tramite uno strumento DCC. X Bot non è il corpo del giocatore. Gli export Splat sono invece animazioni senza mesh, basate su uno scheletro MetaHuman di 441 ossa; per Unity sono destinati al pacchetto Splat Engine e al rig Generic, non al normale retargeting Humanoid.

Nessuna delle due famiglie di file è ancora importata nel repository o collegata al personaggio UnderGrind. Prima della distribuzione degli export devono essere verificate le condizioni d'uso del fornitore.

## Stato delle verifiche

Il modello sorgente maschile e quello femminile sono stati ispezionati e risultano distinti, statici e privi di rig. Il progetto Blendstra dei derivati non ha prodotto una versione utilizzabile: l'ultimo tentativo è terminato con l'errore `DUPLICATE_OR_INVALID_OBJECT_ID`. La generazione del rig, la separazione della testa e la verifica dei pesi sono quindi ancora da completare.

Il codice del controller, la gestione della visibilità dei renderer, il collegamento con l'Animator e i relativi test sono presenti nel repository.

Ultima esecuzione disponibile, 18 novembre 2026:

- Test autonomi C#: 6 superati su 6.
- Test Unity PlayMode: 7 superati su 7.
- Test Unity EditMode: risultato non verificabile. Il riepilogo JSON riporta zero test e codice di uscita 0, ma il log di Unity termina con codice di uscita 1 e non è stato prodotto il file XML dei risultati.
- Verifica visiva nell'Editor: non effettuata.

I risultati dei test automatici non sostituiscono l'ispezione del modello reale, la verifica delle animazioni nell'Editor e il controllo della scena durante il gioco.
