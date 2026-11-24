# Skill — Unity e C# engineering

**Attivazione:** feature Unity/C#, componenti, ScriptableObject, input, fisica, rendering, architettura e refactoring.

**Fonti omgskills:** [unity-developer](https://github.com/rmyndharis/antigravity-skills/tree/364ea63c190fcf7cac035f7fa91f0406fa392584), [game-developer](https://github.com/Jeffallan/claude-skills/tree/078a25cc5bfd8011459be7b91d004e900c1d02d3), [unity](https://github.com/Mindrally/skills/tree/7682ca77710e0971eab4e0ae5dddfa281aea0ba5).

## Flusso operativo

1. Leggere START-HERE, Systems Guide e Architecture.
2. Individuare assembly, interfacce, servizi, implementazioni analoghe, test e proprietario dei dati.
3. Separare responsabilità: Domain (regole/stato puro), Gameplay (casi d'uso/contratti), Infrastructure (storage/configurazione), Presentation (Unity/input/UI), Core (lifecycle/orchestrazione).
4. Progettare cambiamento minimo includendo errori, lifecycle, persistenza e compatibilità serializzata.
5. Seguire naming, namespace, asmdef e pattern già presenti. Non imporre ECS, singleton, service locator o DI senza necessità provata.
6. Usare asset configurabili già previsti per i parametri di gameplay.
7. Aggiungere test deterministici al Domain e PlayMode per integrazioni Unity.
8. Aggiornare XML docs e guide italiane.
9. Controllare il diff, .meta, scene, prefab e asset.

## Vincoli

Domain non conosce GameObject/Transform/scene/input. Non duplicare PlayerState/NPCState in componenti. Non spostare prezzi, transazioni, stock o inventario nei MonoBehaviour. Non introdurre un secondo Input System. Evitare allocazioni per frame e ricerche ripetute nei loop. Usare decimal per il denaro. Verificare compatibilità prima di aggiungere package.

## Output

File/classi interessate, flusso dati, codice completo se modificabile, test aggiunti, rischi di serializzazione/lifecycle e verifiche realmente svolte.

**Accettazione:** ownership unico, layer corretto, nessuna regressione API/asset, test e documentazione coerenti.
