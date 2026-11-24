# Skill — Testing e QA

**Attivazione:** feature, bugfix, regressioni, modifiche player/NPC/UI, scene, prefab e asset import.

## Matrice di verifica

| Livello | Dimostra | Non dimostra |
|---|---|---|
| Diff/source | Coerenza statica e test presenti | Compilazione |
| Standalone .NET | Build/test standalone | Compilazione Unity |
| Unity EditMode | Test Editor/asset deterministici | Runtime |
| Unity PlayMode | Comportamento runtime testato | Qualità visiva generale |
| Blender export | Validazioni previste dallo script | Deformazione in Unity |
| Visual QA | Aspetto nei casi osservati | Copertura automatica completa |

## Runner esistenti

- `Tools/Testing/run-tests.ps1`
- `Tools/Testing/run-unity-tests.ps1 -Platform EditMode`
- `Tools/Testing/run-unity-tests.ps1 -Platform PlayMode`
- `Tools/Testing/run-unity-tests.ps1 -Platform All`
- `Tools/Testing/run-all-tests.ps1`

Consultare `Documentation/Testing.md`. Il runner usa l'Editor della versione dichiarata dal progetto.

## Procedura

1. Definire comportamento atteso e casi limite.
2. Leggere test, asmdef e runner esistenti.
3. Testare outcome e invarianti, non dettagli implementativi fragili.
4. Usare tempo simulato e dati deterministici dove possibile.
5. Aggiungere PlayMode per input, fisica, Animator, lifecycle, scene e UI.
6. Eseguire i runner su una macchina con .NET/Unity. ChatGPT via GitHub non li esegue automaticamente.
7. Leggere report, stack trace, log, exit code, timeout e test discovery.
8. Verificare casi manuali/visivi pertinenti.
9. Riportare pass/fail/skipped/inconclusive e fasi non eseguite.

Non scrivere "tutto verde" se è stata eseguita una sola suite. Test source non equivale a test eseguito. Senza Unity: compilazione/EditMode/PlayMode NON ESEGUITI. Senza Game View: visual QA NON ESEGUITA.

**Accettazione:** copertura pertinente, report reale, limiti espliciti, passaggi riproducibili.
