# Anteprima e ispezione delle animazioni Crouch Walk

## Apertura

In Unity aprire **UnderGrind → Animation → Crouch Walk Candidate Preview**.

La finestra carica:
- la clip Crouch Walk attualmente assegnata in `PlayerAnimationSettings.asset`;
- i candidati FBX presenti in `Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk`;
- i prefab generati `Human_Male_Rigged` e `Human_Female_Rigged`.

La prima voce della selezione animazione è la clip originale assegnata; le successive sono i candidati importati dalla cartella.

## Anteprima

- **Personaggio:** seleziona il prefab maschile o femminile.
- **Animazione:** seleziona la clip originale o un candidato.
- **Inquadratura:** frontale, tre quarti, laterale o posteriore.
- **Distanza camera:** regola l'inquadratura.
- **Pausa / Riproduci**, **Inizio**, **Loop**, **Velocità** e **Tempo normalizzato:** controllano la riproduzione e l'ispezione del ciclo.

La clip viene collegata direttamente a un `AnimationClipPlayable` su un `PlayableGraph` manuale e riprodotta sull'Animator Humanoid del prefab selezionato.

## Esportazione del report diagnostico

Espandere **Report diagnostico ultra dettagliato**.

Sono disponibili due operazioni:

1. **Esporta clip selezionata — entrambi i personaggi:** produce il report della clip corrente sui prefab maschile e femminile.
2. **Esporta originale + tutti i candidati — entrambi i personaggi:** produce un confronto completo della clip assegnata e di tutti i candidati trovati nella cartella, sui due Avatar Player.

Scegliere una cartella di destinazione. Unity crea un archivio ZIP pronto da allegare alla conversazione.

### Parametri configurabili

- **Campionamento pose (Hz):** frequenza uniforme dei campioni; i tempi di keyframe possono aggiungere campioni intermedi.
- **Istantanee per clip:** numero di istanti rappresentativi usati per contact sheet e diagnostica geometrica.
- **Larghezza / altezza immagine:** risoluzione di ogni riquadro.
- **Limite campioni posa:** limite di sicurezza per il numero complessivo di pose registrate.
- **Includi tempi di tutti i keyframe:** aggiunge i tempi delle curve float e object-reference alla sequenza di campionamento.
- **Analizza mesh skinned deformate:** esegue `SkinnedMeshRenderer.BakeMesh` agli istanti delle immagini e registra bounds, distribuzione dei vertici e blend shape.
- **Tolleranza altezza contatto** e **Velocità massima contatto:** soglie euristiche per stimare il contatto dei piedi.

Il valore iniziale è 30 Hz, con 24 istantanee (96 viste per personaggio/clip), 192×320 pixel per riquadro e inclusione dei tempi keyframe. Il limite dei campioni evita che clip con moltissime curve producano report eccessivi; se viene raggiunto, il JSON lo segnala esplicitamente.

## Struttura del pacchetto

L'archivio contiene `manifest.json`, `index.html`, `README.txt` e una cartella per ciascuna clip e variante del personaggio.

Per ogni combinazione clip/personaggio:

| File | Contenuto |
|---|---|
| `report.json` | Metadati completi della clip, importer, Avatar sorgente e Player, prefab, renderer, curve, eventi, campioni e metriche aggregate. |
| `pose-samples.csv` | Una riga per ogni osso Humanoid a ogni campione temporale. |
| `curve-keys.csv` | Tutte le curve float e object-reference, con tempi, valori, tangenti, pesi e tipo di binding. |
| `geometry-samples.csv` | Diagnostica delle mesh skinned agli istanti della contact sheet, inclusi bounds, centroidi, min/max dei vertici, vertici non finiti e pesi delle blend shape. |
| `contact-sheet.png` | Quattro viste del personaggio per ogni istante: frontale, tre quarti, laterale e posteriore. |

### Dati registrati

**Identità e provenienza**
- versione Unity, sistema operativo, CPU, GPU, API grafica, render pipeline e color space;
- percorso asset, GUID, dependency hash, SHA-256 e dimensione in byte di FBX e prefab;
- nome, durata, frame rate, numero di frame stimato, wrap mode, clip vuota/valida, flag Humanoid e flag delle curve root motion.

**Importazione e Avatar**
- tipo e configurazione del ModelImporter, Avatar Setup, Avatar sorgente, compressione, resampling, root motion node, scala e impostazioni di importazione;
- proprietà serializzate visibili dell'importer, incluse le impostazioni di clip;
- parametri HumanDescription, mapping Mecanim e gerarchia scheletrica;
- mapping Player risolto dall'Animator, gerarchia e percorso dei Transform.

**Curve ed eventi**
- binding path, proprietà, tipo componente, curve root/motion, numero di chiavi, primo/ultimo tempo, range valori e wrap mode;
- tutte le chiavi float con tangenti, pesi e weighted mode;
- tutte le chiavi object-reference;
- Animation Events con tempo, funzione e parametri.

**Pose e cinematica**
- posizione, rotazione e scala locale di ogni osso Humanoid disponibile;
- posizione e rotazione world, gerarchia e parent;
- velocità lineare, accelerazione lineare e velocità angolare stimate tra campioni;
- posizione/rotazione del root, forward/right effettivi del modello, deriva del bacino nel root e inclinazione del busto;
- angoli proxy di anche e ginocchia;
- distanza tridimensionale e separazione laterale firmata di piedi e ginocchia, misurate sia sull'asse anatomico dinamico delle anche (ricalcolato a ogni campione, come nel test Unity) sia sull'asse statico della posa iniziale;
- altezza dei piedi rispetto al root, altezza relativa minima osservata, velocità, accelerazione, contatto stimato e possibile scivolamento;
- lunghezza della traiettoria dei piedi, velocità massima, percentuale dei campioni in contatto, incroci e relativi tempi;
- errore di posizione dei piedi relativi alle anche tra il campione iniziale e l'ultimo campione prima della fine, più errore world di ossa selezionate e rotazione relativa dei piedi.

**Geometria deformata**
- numero vertici, triangoli, blend shape e materiali dei renderer;
- bounds world e local;
- bounds della mesh dopo BakeMesh;
- centroide, min/max, raggio massimo e conteggio dei vertici non finiti;
- valore corrente di ogni blend shape.

## Interpretazione e limiti

Il report campiona il retargeting Humanoid direttamente sui due prefab Player, ma non esegue la scena reale.

- Le posizioni world sono riferite all'istanza temporanea nel Preview Scene; le posizioni local sono relative al parent Transform.
- Velocità e accelerazioni sono differenze finite. La precisione dipende dalla frequenza e dalla distribuzione dei campioni.
- Gli incroci dinamici sono conteggi di campioni in cui la separazione firmata rispetto all'asse delle anche correnti scende sotto la tolleranza negativa configurata. Il conteggio è allineato al test `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars`; non rappresenta il numero di eventi distinti.
- Il report include anche l'asse statico iniziale: può evidenziare falsi incroci quando il bacino ruota, perciò il valore dinamico è quello da confrontare con il test.
- Il contatto è una stima euristica basata su altezza minima osservata e velocità. Non equivale al grounded state del controller.
- La percentuale di contatto è una frazione dei campioni, non una misura temporale ponderata.
- Gli angoli di anca sono proxy geometrici tra bacino, femore e gamba; non sono angoli anatomici certificati.
- L'errore di loop dei piedi confronta le posizioni relative alle anche a tempo 0 e a `clip.length - 0.0001 s`, come nel test esistente. È riportato separatamente anche l'errore world di ossa selezionate. Va interpretato insieme alle impostazioni di loop e alle immagini: non tutte le clip sono concepite per terminare nella posa iniziale.
- Le immagini sono render statici del Preview Scene; non rappresentano il Game View con materiali, luci, camera, controller e post-processing di gioco.
- Controller Animator, Blend Tree, transizioni, root motion applicato, input, collisioni, fisica e `PlayerFootSeparationIK` non vengono eseguiti. Le curve root motion sono comunque registrate.
- La diagnostica BakeMesh descrive la geometria deformata, ma non certifica shading, normali, materiali o qualità estetica.
- Se `sampleLimitReached` è true, la sequenza dei campioni è stata ridotta per rispettare il limite configurato; alcuni tempi keyframe potrebbero non essere presenti.

Il pacchetto consente un'analisi tecnica molto più completa di un singolo screenshot o di un test numerico, ma non sostituisce la verifica finale in Play Mode del controller di produzione e l'osservazione umana del movimento.

## Isolamento e sicurezza

La finestra e l'esportatore creano istanze temporanee nei Preview Scene dell'Editor. Le componenti MonoBehaviour dei prefab vengono disabilitate nell'istanza temporanea; i renderer FP sono esclusi dalla visualizzazione di terza persona.

Non vengono modificati o salvati:
- `PlayerAnimationSettings.asset`;
- Animator Controller di produzione;
- prefab maschile e femminile;
- FBX originali e candidati;
- scene del progetto;
- impostazioni d'importazione degli asset.

Al termine vengono distrutti Playable Graph, mesh temporanee, texture e istanze di preview.

La clip 08 resta un candidato diagnostico finché il controllo visivo sul report e la successiva verifica Play Mode non ne autorizzano la promozione.
