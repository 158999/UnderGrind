# Skill — Debugging e performance

**Attivazione:** eccezioni, log, crash, input, bug intermittenti, lag, GC, frame drop, memoria e regressioni.

**Fonte omgskills:** [debugging-strategies](https://github.com/wshobson/agents/tree/0874a27b17a22e2b7c2e2bcd725768e0803cad06).

## Debugging

1. Acquisire log completo, stack trace, versione Unity, scena, passi di riproduzione e frequenza.
2. Separare sintomo, ipotesi e causa dimostrata.
3. Individuare la prima violazione dell'invariante, non solo l'ultima eccezione.
4. Tracciare ownership, lifecycle, nullability, ordine eventi e persistenza.
5. Confrontare commit/diff e test correlati.
6. Formulare ipotesi falsificabili e verificarle una per volta.
7. Aggiungere test di regressione insieme alla correzione.
8. Ripetere la stessa riproduzione e verificare i casi limite.

## Performance

1. Definire target e carico; non imporre 60 FPS senza piattaforma/obiettivo confermati.
2. Acquisire dati Profiler/Memory Profiler/Frame Debugger o benchmark ripetibile.
3. Separare CPU, GPU, GC, memoria, fisica, rendering, asset loading e pathfinding.
4. Identificare hot path e misurare baseline.
5. Applicare una modifica mirata e ripetere la misura.
6. Non introdurre pooling, Jobs/Burst, ECS o Addressables senza evidenza.
7. Documentare dispositivo, scena, entità, risoluzione, durata e metriche.

**Accettazione:** causa supportata da evidenza, test anti-regressione e confronto misurato prima/dopo per le ottimizzazioni.
