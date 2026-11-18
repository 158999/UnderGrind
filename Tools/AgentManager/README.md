# UnderGrind Agent Manager

Prima implementazione del nucleo persistente locale in .NET 10 e SQLite. Il componente gestisce la coda, le dipendenze, i lease dei worker e l'audit delle transizioni. Non invoca ancora provider MCP o processi Unity: gli adapter vanno aggiunti e autenticati separatamente.

## Requisiti

- .NET SDK 10
- Windows, Linux o macOS
- NuGet accessibile al primo restore

## Comandi

Dalla root del repository:

```powershell
dotnet run --project Tools/AgentManager -- init
dotnet run --project Tools/AgentManager -- enqueue "Audit Player" "Verificare input, locomozione e test" 10 github
dotnet run --project Tools/AgentManager -- list
dotnet run --project Tools/AgentManager -- lease worker-local-01
dotnet run --project Tools/AgentManager -- complete <task-id> worker-local-01 "Verifiche concluse"
dotnet run --project Tools/AgentManager -- recover
```

È possibile passare un file di configurazione alternativo con `--settings <path>` prima del comando. Il database predefinito è `Tools/AgentManager/Data/undergrind-agent-manager.db`; la cartella viene creata automaticamente.

## Garanzie implementate

- Persistenza SQLite con journal WAL e foreign key.
- Coda ordinata per priorità e data di creazione.
- Un task è assegnabile solo se tutti i prerequisiti sono conclusi con stato `Succeeded`.
- Lease con proprietario e scadenza; un worker diverso non può completare il task.
- Recupero dei lease scaduti dopo un crash.
- Audit delle transizioni principali.
- Query parametrizzate e validazione dei parametri essenziali.

## Limiti attuali

- Nessun worker provider è collegato; il comando `lease` assegna un task ma non lo esegue.
- Non sono ancora implementati OAuth, Windows Credential Manager/DPAPI, policy di approvazione, limiti di concorrenza per provider, ciclo scheduler continuo, retry con backoff o verifica Git HEAD.
- Le dipendenze possono essere fornite dall'API interna `Enqueue`; il CLI iniziale non espone ancora un'opzione per dichiararle.
- Non è stata eseguita una build in questo ambiente: il container disponibile non include .NET SDK. Eseguire `dotnet restore` e `dotnet build` sulla macchina di sviluppo prima di usare il database reale.

Questo è il primo nucleo eseguibile, non un orchestratore autonomo completo. Le integrazioni vanno abilitate una alla volta dopo test d'integrazione e configurazione dei segreti.
