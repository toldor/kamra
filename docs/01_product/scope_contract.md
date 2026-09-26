# Scope Contract

Ez a dokumentum rögzíti, mi épül meg a projekt keretén belül, és mi nem.
Hatókörön kívüli fejlesztés csak explicit kérésre indulhat (lásd `AGENTS.md` 11. pont).

## Hatókörben

- Kamra-kezelés: tételek hozzáadása, szerkesztése, törlése
- Természetes nyelvű bevitel LLM-mel (név, mennyiség, lejárat kinyerése): az eredmény tételjavaslat, amely csak jóváhagyás után kerül a készletbe
- Lejárati dátum becslés kategória alapján
- Receptkezelés: induló receptkészlet (20–40 magyar hétköznapi recept) és kézi receptfelvitel
- Ajánlás: a mentett receptek determinisztikus illesztése a jelenlegi készlethez, a hamarosan lejáró tételeket előre sorolva
- AI-receptötlet: AI által generált recept, amely szerkeszthető űrlapon jóváhagyás után menthető a receptek közé („AI-javasolt” forrásjelöléssel)
- AI-vezérelt adagszámítás és alapanyag-helyettesítés
- Automatikus készletcsökkentés főzés után
- Kézi készletcsökkentés kötelező csökkenési okkal (elfogyott / kidobtam / hibás rögzítés); minden készletváltozás naplózva
- Metrika-lekérdezések a [metrics.md](metrics.md) szerint (North Star + 3 guardrail), szintetikus seed-adaton tesztekkel ellenőrizve; ehhez a tételjavaslat beküldési és jóváhagyási időbélyegének tárolása
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
- Táplálkozás- és kalóriakövetés
- Egyéni ízlés- és allergiaprofilok
- Online rendelés, bolti vagy webshop-integráció
