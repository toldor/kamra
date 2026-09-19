# AI Prompt Napló

Minden érdemi AI-munkamenet végén javasolj egy bejegyzést (lásd `AGENTS.md` 10. pont).
A **„Mit változtattam / döntésem"** mezőt a fejlesztő tölti ki – az AI nem találja ki.

## Formátum

```
### P-XX – [rövid cím]
- **Dátum:** ÉÉÉÉ-HH-NN
- **Cél:** ...
- **Eszköz:** Claude Code / Gemini CLI / stb.
- **Prompt összefoglaló:** ...
- **AI javaslat összefoglaló:** ...
- **Érintett fájlok:** ...
- **Mit változtattam / döntésem:** [fejlesztő tölti ki]
```

---

### P-01 – Projekt alapdokumentáció létrehozása
- **Dátum:** 2026-09-19
- **Cél:** AGENTS.md, CLAUDE.md és a docs/ könyvtárstruktúra skeleton doksikkal
- **Eszköz:** Claude Code (claude-sonnet-4-6)
- **Prompt összefoglaló:** Hozz létre agents.md-t és claude.md-t; majd hozz létre a docs mappába alap doksikat, az AGENTS.md hivatkozzon rájuk.
- **AI javaslat összefoglaló:** Létrehozta a teljes docs/ struktúrát (00_index, 01_product, 02_architecture/adr, 03_design, 04_quality, 05_security_ops, 07_ai), frissítette az AGENTS.md 9. és 10. szekcióját markdown linkekkel.
- **Érintett fájlok:** `agents.md`, `CLAUDE.md`, `docs/` (11 fájl)
- **Mit változtattam / döntésem:** [fejlesztő tölti ki]
