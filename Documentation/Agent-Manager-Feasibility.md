# Valutazione tecnica dell'orchestrazione multi-agente

**Data della verifica:** 18 novembre 2026  
**Repository:** `158999/UnderGrind`, branch `main`  
**Unity:** `6000.6.7f1`  
**Pacchetti principali:** Input System `1.20.1`, URP `17.6.0`, Test Framework `1.4.6`, uGUI `2.6.0`.

## Stato del repository esaminato

La struttura versionata contiene 284 elementi nel tree GitHub, tra cui 45 file C#, 4 assembly definition, 3 scene Unity e 4 documenti Markdown sotto `Documentation`. Non risultano prefab `.prefab` versionati nel tree al momento della verifica. Il progetto separa codice runtime, Editor tooling, asset generati, impostazioni, sorgenti artistiche e test.

Il Player ha sistemi runtime distinti per locomozione, input, animazione, inventario, interazione e rig visuale. I parametri principali sono esposti tramite asset di configurazione. La scena di test versionata è precedente al generatore attuale e non dimostra l'integrazione completa di input, inventario, UI e Animator.

Il workflow GitHub attivo è `.github/workflows/standalone-tests.yml`: esegue i test diagnostici .NET su Windows. Non equivale a una compilazione Unity, né a test EditMode/PlayMode.

## Architettura e prima implementazione Agent Manager

È stato aggiunto il primo nucleo locale in `Tools/AgentManager`, basato su .NET 10 e SQLite (`Microsoft.Data.Sqlite`). Non è necessario introdurre subito un sistema distribuito: un singolo processo Windows può coordinare task, processi locali e server MCP remoti.

### Componenti previsti e stato attuale

- **Coordinator:** valida richieste, scompone task, assegna priorità e verifica le dipendenze.
- **Task store SQLite — implementato:** conserva task, stato, priorità, provider, lease, risultato/errore e timestamp. Le dipendenze sono registrate in `TaskDependencies`; gli eventi sono scritti in `AuditLog`.
- **Scheduler — parziale:** `TryLeaseNext` assegna il task con priorità più alta le cui dipendenze sono concluse; non esiste ancora un loop concorrente o un limite per provider.
- **Worker host — non implementato:** il CLI può assegnare un task a un worker identificato, ma non avvia ancora processi o server MCP.
- **Provider adapters:** un adapter per API REST o server MCP; non un adapter fittizio per ogni plugin ChatGPT.
- **Git workspace manager:** legge HEAD, verifica file modificati, crea commit solo su richiesta di policy, controlla che il branch di destinazione sia quello previsto.
- **Verifier:** esegue controlli statici, test .NET, test Unity e controlli specifici per asset.
- **Audit log:** registra input, tool effettivamente invocati, output, hash degli artefatti, decisioni e motivi di blocco.

### Schema minimo dei dati

- `Task`: ID, titolo, descrizione, stato, priorità, agente logico, provider, tentativo massimo, lease, date.
- `TaskDependency`: task dipendente e task prerequisito.
- `TaskRun`: inizio/fine, esito, errore, worker, versione del prompt/configurazione.
- `Artifact`: percorso o URI, hash SHA-256, tipo, provenienza, licenza e stato di verifica.
- `RepositoryOperation`: repository, branch, base SHA, commit SHA, file toccati e controlli eseguiti.
- `Approval`: azione, motivazione, soggetto autorizzante, scadenza e stato.

Gli stati consigliati sono `Queued`, `Ready`, `Running`, `WaitingForApproval`, `Succeeded`, `Failed`, `Cancelled` e `Blocked`. Un task può essere dichiarato `Succeeded` solo se le condizioni di completamento registrate sono state verificate.

### Esecuzione e sicurezza

- Usare lease con scadenza per evitare che due worker eseguano lo stesso task dopo un crash.
- Usare idempotency key per le operazioni remote che le supportano.
- Limitare concorrenza per provider e per progetto Unity.
- Tenere le credenziali fuori da SQLite in chiaro: Windows Credential Manager o DPAPI per segreti locali; OAuth con refresh token cifrati per provider che lo supportano.
- Applicare allowlist di tool e percorsi; i task di lettura possono essere automatici, mentre cancellazioni, pubblicazioni, acquisti e operazioni Git distruttive richiedono approvazione.
- Non eseguire codice generato con accesso indiscriminato al filesystem o alla rete.
- Il branch `main` resta il branch di integrazione; prima di ogni scrittura il worker deve verificare che HEAD sia ancora quello usato per produrre la modifica.

## Integrazioni esterne verificate

| Provider | Integrazione pubblica rilevata | Autenticazione/costi | Stato per Agent Manager |
|---|---|---|---|
| GitHub | REST API ufficiale e GitHub Actions API | GitHub App o token con permessi minimi; i costi dipendono dal piano GitHub e dai runner utilizzati | Integrabile direttamente; il connettore GitHub di ChatGPT non è riutilizzabile automaticamente dall'app |
| Unity | Unity CLI con modalità MCP `unity mcp`; comandi `unity command` e `unity eval` | Unity dichiara CLI e MCP gratuiti; Unity 6+ | Compatibile con la versione del progetto. Serve installare/configurare CLI e verificare la connessione sulla macchina Windows |
| PINOC | MCP ufficiale `https://pinoc.mcp.viggle.ai/mcp`; Viggle API REST | OAuth con browser; consultazione e preset motion gratuiti; generazione personaggi/motion/scene consuma crediti | Integrabile come MCP remoto. Le operazioni a crediti devono avere budget e approvazione |
| Mobbin | MCP ufficiale `https://api.mobbin.com/mcp` | OAuth tramite browser | Integrabile per ricerca UI/UX, subordinato all'autorizzazione dell'account |
| Magic Patterns | MCP ufficiale `https://mcp.magicpatterns.com/mcp` | Autenticazione del provider; verificare piano e limiti prima di automatizzare | Endpoint documentato; utile per task UI, non per codice Unity runtime |
| Blendstra | Endpoint MCP `https://blendstra.com/mcp` individuato in un registro MCP di terze parti | Il registro segnala autenticazione richiesta; modalità, scope e condizioni vanno verificate con il provider | Integrazione possibile in linea di principio, ma non ancora validata da un client esterno |
| OpenAI API | Responses API supporta tool MCP remoti via Streamable HTTP o HTTP/SSE | API key; costo basato sui token di input/output e sui tool; la documentazione indica che non c'è una tariffa aggiuntiva per singola chiamata MCP | Opzione per un coordinatore LLM autonomo; non è necessaria per il solo scheduler deterministico |
| Contour3D | Nessun endpoint pubblico/API/MCP verificato in questa ricognizione | Non verificato | Non implementare un adapter finché il provider non documenta endpoint e autenticazione |
| Loupe Code Review | È stato trovato un endpoint MCP di Loupe, ma la documentazione consultata descrive gestione di progetti/documenti/review, non prova l'esistenza dello specifico agente di code review richiesto | OAuth e scope per progetto secondo la documentazione Loupe | Non equiparare il prodotto Loupe trovato al plugin "Loupe Code Review" senza conferma |
| GameGene, Game Asset Studio, AI Game Maker e skill di design | Strumenti disponibili nell'ambiente ChatGPT; non è stato verificato un endpoint pubblico esterno per ciascuno | Dipende dal provider e dal piano | Trattarli come integrazioni ChatGPT-only finché non esiste documentazione API/MCP verificabile |

I plugin collegati alla sessione ChatGPT non diventano automaticamente invocabili da un processo C#. Il Manager deve usare le API/MCP pubbliche dei rispettivi fornitori, quando esistono, con autenticazione indipendente. Se un provider espone soltanto un plugin ChatGPT, quel lavoro richiede una sessione ChatGPT attiva o un operatore umano.

## Ruolo di OpenAI

La Responses API permette di collegare server MCP remoti e di filtrare gli strumenti ammessi. Le chiamate MCP vengono eseguite dal servizio API; il costo resta quello dell'uso del modello e dei token scambiati, senza una tariffa aggiuntiva per ogni singola chiamata MCP secondo la documentazione consultata.

Un Agent Manager può quindi funzionare in due modalità:

1. **Senza modello esterno:** task e dipendenze definiti da regole C#; worker deterministici per GitHub, Unity CLI e MCP. Non pianifica autonomamente attività non strutturate.
2. **Con coordinatore LLM:** un modello via API interpreta richieste, prepara task e sintetizza risultati. Questa modalità comporta costi API e richiede controlli rigorosi su tool consentiti e approvazioni.

La sessione ChatGPT corrente e i suoi plugin non possono essere assunti come un runtime persistente accessibile dal servizio esterno.

## Stato della prima implementazione e prossimi incrementi

1. **Completato come prima base:** progetto `Tools/AgentManager`, database SQLite, coda, dipendenze, lease, recupero lease scaduti, completamento/fallimento e audit log.
2. Collegare GitHub REST e Unity CLI; aggiungere verifiche di repository, compilazione e test.
3. Aggiungere PINOC e Mobbin tramite MCP remoto, con OAuth e scope minimi.
4. Validare Blendstra e Magic Patterns con una connessione MCP indipendente prima di automatizzarli.
5. Aggiungere un coordinatore LLM solo quando la coda deterministica e il sistema di audit sono affidabili.

## Limiti della verifica

Questa ricognizione verifica struttura Git, configurazione Unity versionata, workflow e documentazione pubblica dei provider. Non verifica la disponibilità delle credenziali API dell'utente, l'accesso ai suoi account, i limiti contrattuali dei piani, né la connessione locale a Unity CLI/MCP. Non dimostra che i plugin ChatGPT possano essere chiamati dall'esterno.
 
## Riferimenti

- OpenAI Responses API, MCP: https://developers.openai.com/api/docs/guides/tools-connectors-mcp
- OpenAI tool calling: https://developers.openai.com/api/docs/guides/tools
- Unity CLI e MCP: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli
- Unity AI tools e costi CLI/MCP: https://unity.com/features/ai
- PINOC MCP: https://viggle.ai/pinoc/mcp/
- Mobbin MCP: https://github.com/mobbin/mobbin-mcp-server
- Magic Patterns MCP: https://www.magicpatterns.com/product/mcp
- Blendstra MCP registry entry: https://rmcp.dev/server/blendstra.com
- Loupe MCP: https://ubermuda.github.io/loupe/using/mcp/


## Comandi del prototipo

Dalla root del repository, con .NET SDK 10 installato:

```powershell
dotnet run --project Tools/AgentManager -- init
dotnet run --project Tools/AgentManager -- enqueue "Audit Player" "Verificare input, locomozione e test" 10 github
dotnet run --project Tools/AgentManager -- list
dotnet run --project Tools/AgentManager -- lease worker-local-01
dotnet run --project Tools/AgentManager -- complete <task-id> worker-local-01 "Verifiche concluse"
dotnet run --project Tools/AgentManager -- recover
```

Il prototipo non è ancora un orchestratore multi-agente autonomo: non contiene adapter MCP, autenticazione, gestione segreti, approvazioni, retry/backoff, esecuzione continua, isolamento dei worker o verifica del branch Git. La build non è stata eseguita in questo ambiente perché non è disponibile il .NET SDK.
