# Esecuzione dei test

UnderGrind dispone di test standalone .NET e di test Unity EditMode e PlayMode. I due gruppi verificano livelli diversi del progetto e devono essere interpretati separatamente.

## Test standalone

Eseguire dalla root del repository:

```powershell
./Tools/Testing/run-tests.ps1
```

Lo script compila il progetto diagnostico e individua i progetti `*.Standalone.Tests.csproj`. I timeout predefiniti sono 180 secondi per la compilazione e 180 secondi per ciascun progetto di test. Possono essere modificati con `-BuildTimeoutSeconds` e `-TestTimeoutSeconds`.

## Test Unity

Per eseguire solo EditMode:

```powershell
./Tools/Testing/run-unity-tests.ps1 -Platform EditMode
```

Per eseguire solo PlayMode:

```powershell
./Tools/Testing/run-unity-tests.ps1 -Platform PlayMode
```

Per eseguire entrambe le piattaforme:

```powershell
./Tools/Testing/run-unity-tests.ps1 -Platform All
```

Lo script cerca l'Editor Unity corrispondente alla versione dichiarata in `ProjectSettings/ProjectVersion.txt`, limitando la ricerca alle directory standard di Unity Hub. Se necessario, il percorso può essere indicato con `-UnityEditorPath` oppure con la variabile d'ambiente `UNITY_EDITOR_PATH`.

Il timeout predefinito è di 600 secondi per piattaforma e può essere modificato con `-TimeoutSeconds`. Il runner avvia Unity tramite `System.Diagnostics.Process`, drena stdout e stderr in modo asincrono e legge `ExitCode` dalla stessa istanza del processo dopo `WaitForExit()`. Alla scadenza termina l'albero dei processi Unity e registra l'esecuzione come fallita. Se EditMode rileva errori di compilazione, PlayMode non viene avviato: in presenza degli stessi errori non potrebbe essere eseguito e produrrebbe soltanto un secondo avvio Unity inutile.

## Esecuzione completa

Per eseguire standalone, EditMode e PlayMode in sequenza:

```powershell
./Tools/Testing/run-all-tests.ps1
```

Il timeout complessivo predefinito è di 1800 secondi. I timeout delle singole fasi e quello complessivo sono configurabili tramite i parametri dello script. L'executor avvia ogni script PowerShell in un processo separato e attende la terminazione del solo processo worker con `Process.WaitForExit(timeout)`; non usa `Start-Process -Wait`, che può attendere anche processi discendenti dopo la conclusione del runner. L'output del worker resta visibile nella console.

## Risultati

I report correnti vengono scritti in `TestResults/test/`; l'esecuzione precedente viene conservata in `TestResults/test old/`. I report includono stato, durata, conteggi, errori, dettagli dei singoli test Unity falliti, log e file XML/TRX. I log .NET vengono salvati in `TestResults/test/`; Unity salva stdout, stderr, log Editor e XML per ciascuna piattaforma in `TestResults/test/Unity/`. I report in `TestResults/test/` e `TestResults/test old/` sono attualmente versionati intenzionalmente per consentire la revisione remota delle esecuzioni locali. Il primo contiene l'esecuzione corrente, il secondo quella precedente. I log possono includere percorsi assoluti e informazioni dell'ambiente della macchina che ha eseguito i test: sono metadati diagnostici, non percorsi richiesti dal progetto.

## Limiti della CI

Il workflow `Standalone Diagnostics Tests` esegue la compilazione e i test standalone .NET su Windows e verifica la sintassi degli script PowerShell. Non avvia Unity e quindi non certifica la compilazione degli assembly Unity né il superamento dei test EditMode o PlayMode. Queste verifiche devono essere eseguite con l'Editor della versione richiesta dal progetto.

La GitHub Action esegue anche `Tools/Testing/test-process-runner.ps1`, che verifica un processo figlio con exit code noto e l'acquisizione separata di stdout e stderr.
