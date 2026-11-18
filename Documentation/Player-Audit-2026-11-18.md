# Audit tecnico UnderGrind — 18 novembre 2026

## Riferimento verificato

- Repository: `158999/UnderGrind`
- Branch: `main`
- Commit analizzato prima delle modifiche di questo ciclo: `17a67e25346fe779e55cd135c2e34b8cf1306515`
- Unity: `6000.6.7f1`
- Tree alla base dell'audit: 301 elementi complessivi, 256 file, 46 sorgenti C#, 4 assembly definition, 3 scene Unity e 5 documenti Markdown sotto `Documentation`.
- Prefab Unity versionati: nessuno.
- Scena di test: `Assets/UnderGrind/Scenes/LocomotionTest.unity`, presente e abilitata in `ProjectSettings/EditorBuildSettings.asset`.

I conteggi si riferiscono al tree di base prima dell'aggiunta del nucleo Agent Manager e di questo report. Il branch può avanzare durante il lavoro.

## Stato per area

| Area | Stato verificato | Limite residuo |
|---|---|---|
| Locomozione | Controller, impostazioni ScriptableObject e test presenti | Nessuna prova visiva sul rig finale |
| Input | Input System e rebinding runtime con persistenza JSON presenti | Non è stata individuata una schermata UI dedicata alla riconfigurazione |
| Inventario | Logica, impostazioni e UI presenti; test EditMode/PlayMode | Integrazione sulla scena da riverificare dopo aggiornamenti |
| Interazione | Controller e impostazioni dedicati presenti | Interazioni da provare nella scena completa |
| Animazione | Driver, settings e builder Animator presenti | Nessuna clip FBX importata nella cartella Animations |
| Modello umanoide | Derivati testa/corpo e armature con nomi corretti in Blendstra | Pesi, ossa, Avatar Humanoid, materiali e deformazioni non validati |
| Scena di test | Scena presente e inserita nelle Build Settings | Non rigenerata con prefab riggati; non dimostra la pipeline definitiva |
| Agent Manager | Nucleo .NET/SQLite aggiunto in questo ciclo | Nessun worker MCP/Unity, autenticazione o scheduler autonomo |
| CI | Workflow standalone diagnostics e build Agent Manager | Non esegue Unity Editor né test EditMode/PlayMode |

## Verifica dei test versionati

I risultati archiviati nel repository non sono tutti riferiti allo stesso run:

- Standalone diagnostics: report `TestResults/latest.json`, 6 test passati su 6, build diagnostics passata.
- EditMode: report XML `TestResults/UnderGrind/EditMode_Latest.xml`, 12 passati, 0 falliti.
- PlayMode: il report XML `TestResults/UnderGrind/PlayMode_Latest.xml` registra 7 passati, 0 falliti, ma è del run precedente (08:27 UTC).
- Il run Unity più recente archiviato in `TestResults/Unity-20261118-103717-325` non ha prodotto i file XML attesi; il log PlayMode termina con codice di uscita 1. Il JSON `TestResults/unity-latest.json` marca quindi il run come Failed con zero test validi. Non va interpretato come 0 test falliti: è un'esecuzione non valida/incompleta.

Questi file sono report già presenti nel repository. In questa sessione non è stato possibile lanciare Unity o .NET perché il container non dispone né di Unity Editor né di .NET SDK.

## Difetti e rischi prioritari

1. **Rig non approvato:** la presenza di nomi armature e mesh testa/corpo non prova che le mesh siano skinnate correttamente. L'ispezione Blendstra non espone la configurazione dei modificatori né i dati completi delle ossa/pesi.
2. **Animazioni non integrate:** PINOC conferma otto asset e il formato `mixamo-fbx`, ma i file non sono ancora presenti nel repository. I link firmati sono temporanei e non devono essere salvati nella documentazione.
3. **Prefab assenti:** nessun prefab è versionato; la scena può usare un modello statico di fallback, che non è una verifica del rig animato.
4. **Scena non rigenerata:** la scena esistente è abilitata, ma non è stata rigenerata né aperta in questa sessione. Il builder richiede prefab e settings e può sostituire la scena solo dopo conferma.
5. **Test runner Unity:** il run più recente ha esito non valido. Occorre diagnosticare l'uscita PlayMode e ottenere XML e log validi per entrambe le piattaforme.
6. **Agent Manager:** il primo nucleo SQLite è solo un task store/lease manager. Non è ancora un agente autonomo e non deve essere descritto come tale.

## Prossimi criteri di accettazione

- Importare e ispezionare i file FBX maschile e femminile configurati separatamente in `PlayerRigSettings`, verificando entrambi gli Avatar Humanoid e il mapping delle ossa.
- Verificare pesi e deformazioni con pose per spalle, gomiti, polsi, anche, ginocchia e caviglie.
- Importare gli otto FBX Mixamo, validare loop/root motion e generare l'Animator Controller.
- Generare prefab maschile e femminile, aggiornare la scena test senza perdere modifiche manuali e controllare prima/terza persona.
- Eseguire Unity EditMode e PlayMode con output XML/log validi sul commit aggiornato.
- Aggiungere adapter e policy di sicurezza all'Agent Manager solo dopo aver compilato e testato il nucleo SQLite.
