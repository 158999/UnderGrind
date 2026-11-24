# Anteprima dei candidati Crouch Walk

## Apertura

In Unity aprire **UnderGrind → Animation → Crouch Walk Candidate Preview**.

La finestra carica i prefab generati Human_Male_Rigged e Human_Female_Rigged e le clip FBX presenti in:

`Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk`

La prima voce della selezione animazione è la clip Crouch Walk attualmente assegnata in `PlayerAnimationSettings.asset`; le successive sono i candidati importati dalla cartella.

## Controlli

- **Personaggio:** seleziona il prefab maschile o femminile.
- **Animazione:** confronta la clip originale con i candidati disponibili.
- **Inquadratura:** frontale, tre quarti, laterale o posteriore.
- **Distanza camera:** regola l'inquadratura del personaggio.
- **Pausa / Riproduci:** arresta o riprende la riproduzione.
- **Inizio:** riporta la clip al primo fotogramma.
- **Loop:** ripete il ciclo.
- **Velocità:** regola la velocità di anteprima.
- **Tempo normalizzato:** consente di ispezionare un punto preciso del ciclo.

Usare almeno le inquadrature frontale, laterale e tre quarti per osservare piedi, ginocchia, bacino, postura e deformazioni su entrambi i personaggi.

## Isolamento e sicurezza

La finestra crea un'istanza temporanea del prefab nel contesto di anteprima dell'Editor e collega direttamente la clip selezionata a un Playable Graph manuale. Le componenti MonoBehaviour del prefab vengono disabilitate nell'istanza temporanea; i renderer FP vengono nascosti per mostrare la configurazione di terza persona.

La finestra non salva né modifica:
- `PlayerAnimationSettings.asset`;
- l'Animator Controller di produzione;
- i prefab maschile e femminile;
- le clip FBX sorgenti o candidate;
- le scene del progetto.

Chiudendo la finestra vengono distrutti il Playable Graph e l'istanza temporanea.

## Ambito della verifica

La clip viene riprodotta direttamente sull'Animator Humanoid del prefab selezionato: il test permette di osservare il retargeting sul rig Player e confrontare i due Avatar.

L'anteprima non esegue il controller di locomozione di produzione, le sue transizioni, il movimento del personaggio, il root motion o il componente di foot-separation IK. Non sostituisce quindi la verifica in Play Mode della clip nel controller reale. Serve a selezionare visivamente il candidato prima di qualsiasi modifica agli asset di produzione.

La promozione della clip richiede ancora:
1. assenza di incroci e contatti innaturali in tutto il ciclo;
2. postura accovacciata coerente su entrambi gli Avatar;
3. deformazioni accettabili di bacino, gambe e piedi;
4. continuità del loop;
5. successiva verifica Play Mode nel controller di produzione.

La clip 08 resta un candidato diagnostico finché questi controlli non sono completati.