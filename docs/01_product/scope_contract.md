# Scope Contract

Ez a dokumentum rögzíti, mi épül meg a projekt keretén belül, és mi nem.
Hatókörön kívüli fejlesztés csak explicit kérésre indulhat (lásd `AGENTS.md` 11. pont).

## Hatókörben

- Kamra-kezelés: tételek hozzáadása, szerkesztése, törlése
- Természetes nyelvű bevitel LLM-mel (név, mennyiség, lejárat kinyerése)
- Lejárati dátum becslés kategória alapján
- Receptjavaslat a jelenlegi készletből, lejárat-prioritással
- AI-vezérelt adagszámítás és alapanyag-helyettesítés
- Automatikus készletcsökkentés főzés után
- Bevásárlólista generálás alacsony készlet esetén
- Chat asszisztens MCP eszköztáron keresztül
- REST API (ASP.NET Core) + React frontend
- Docker Compose-alapú fejlesztői és éles környezet

## Hatókörön kívül

- Mobil natív alkalmazás (iOS / Android)
- Vonalkód-/QR-kód olvasó
- Bolti árak vagy kedvezmények integrálása
- Valós idejű többfelhasználós kollaboráció (websocket sync)
- Gépi tanulású saját modell betanítása
- E-mail / push értesítések
- Szerepkör-alapú hozzáférés-kezelés (RBAC) több felhasználó részére
