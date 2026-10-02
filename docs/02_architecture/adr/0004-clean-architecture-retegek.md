# 0004 - Clean Architecture rétegek használati eset osztályokkal és aggregátumonkénti repositoryval

Dátum: 2026-10-02
Státusz: Proposed

## Context

- **Probléma:** a backend szerkezetének egyszerre kell biztosítania, hogy (1) a REST API és az MCP-szerver ugyanazokat a használati eseteket hívja, duplikált logika nélkül (scope_contract US-6); (2) minden LLM-hívás interfész mögött legyen és tesztben mockolható; (3) a determinisztikus szabályok (ajánlás és rangsor, FEFO, adagskálázás, becsült lejárat, bevásárlójavaslat) adatbázis nélkül unit-tesztelhetők legyenek; (4) egy háztartás adata más háztartásból ne legyen elérhető.
- **Kényszerek:** a stack adott a témavezetői iránymutatás és a tématerv alapján: .NET API, React frontend, PostgreSQL, külső AI-szolgáltatás, MCP-réteg. Kb. 200 óra fejlesztési idő, egy fejlesztő. A kód nagy részét AI-ágensek generálják, ezért a szerkezeti szabályoknak gépileg ellenőrizhetőnek kell lenniük, nem csak konvenciónak.
- **Érintett minőségi attribútumok:** [QA-2 helyesség, QA-6 modularitás és tesztelhetőség, QA-1 adatelkülönítés](../quality_attributes.md).

## Decision

- A backend Clean Architecture szerint négy rétegre (Domain, Application, Infrastructure, Api) és egy külön MCP-hostra bomlik; az Application használati esetenként egy osztályból áll, az adatot aggregátumonkénti repository-interfészeken éri el, a háztartás azonosítója minden háztartáshoz kötött lekérdezés kötelező paramétere, az üzleti szabályok pedig a Domain entitásaiban és domain szolgáltatásaiban élnek.

## Alternatives

**Architektúra-minta**

1) **Klasszikus 3 rétegű (Api / Business / Data)** – előny: egyszerűbb, ismerős; hátrány: az üzleti réteg közvetlenül EF Core-tól függ, így a szabályok nem tesztelhetők adatbázis nélkül, és az LLM-interfész csak konvenció.
2) **Vertical Slice (feature-mappák egy közös könyvtárban)** – előny: a legkevesebb kód, egy funkció egy helyen; hátrány: a rétegszabályok projekthivatkozással nem kényszeríthetők ki, ezért a rétegszabály-teszt nem működik.
3) **Moduláris monolit (feature-modulok saját rétegekkel)** – előny: erős modulhatárok; hátrány: 6 story és egy fejlesztő mellett túlméretezett.

**Adatelérés és hívási mód**

4) **`IKamraDbContext` interfész repository nélkül** – előny: a legkevesebb kód; hátrány: az Application az EF Core csomagtól függ, és a használati esetek tesztjéhez adatbázis kell (in-memory adatbázist az AGENTS.md tilt).
5) **MediatR közvetítő pipeline-nal** – előny: a validáció és a naplózás egységesen, egy helyen; hátrány: új függőség, a 13-as verziótól kettős (RPL-1.5 / kereskedelmi) licenc nyilatkozattal és kulccsal, kb. 20 használati esetnél nincs valódi haszna, és nehezíti a kód követését.

**Háztartás szerinti szűrés**

6) **EF Core globális query filter** – előny: nem lehet elfelejteni; hátrány: az Application kódjából nem látszik, `IgnoreQueryFilters`-szel csendben kikapcsolható, fake repositoryval nem tesztelhető, és a „rendszer-elem vagy saját” szabály nehezen fejezhető ki.

**Validáció és domain modell**

7) **FluentValidation** – előny: olvasható szabályok, kényelmes mezők közötti szabályok; hátrány: új függőség, a szabályaink többsége egyszerű.
8) **Vérszegény entitások, logika az Application rétegben** – előny: egyszerűbb EF-leképezés; hátrány: a szabályok szétszóródnak, az invariánsok megkerülhetők, és a Domain réteg szinte üres marad.

## Consequences

- **Pozitív:**
  - A függőségi irány (Domain → semmi; Application → Domain; Infrastructure → Application, Domain; Api és McpServer → Application, plusz Infrastructure a DI-hoz) projekthivatkozással kikényszerített.
  - Az API és az MCP-host ugyanazt a használatieset-osztályt hívja.
  - A Domain szabályai és a használati esetek fake repositoryval, adatbázis nélkül unit-tesztelhetők.
  - A háztartás szerinti szűrés a metódus-szignatúrában látszik, paraméter nélkül nem lehet lekérdezni.
- **Negatív / kockázatok:**
  - A négy közül ez a minta hozza a legtöbb kódot (rétegközi DTO-k, kb. 5–6 repository-interfész és -implementáció).
  - A DTO ↔ entitás leképezés kézzel készül (AutoMapper nélkül, ugyanazon licencváltozás miatt).
  - A gazdag entitások EF-leképezése privát settereket és konstruktorokat igényel.
- **Figyelni kell a megvalósítás során:**
  - Nem lehet generikus repository; aggregátumonként csak a használati esetekhez szükséges metódusok kellenek.
  - A háztartás azonosítóját a használati eset az Application rétegben definiált `ICurrentHousehold` interfészből veszi; a feltöltése az authentikációs ADR része.
  - A validáció beépített DataAnnotations-szel (mezők közötti szabálynál `IValidatableObject`) az Application rétegben történik; ha a mezők közötti szabályok elszaporodnak, a FluentValidationre váltás csak a validátorokat érinti.
  - A Domain invariánsai (például nem negatív készlet) az entitásokban maradnak a validációtól függetlenül.

## Verification

- **Hogyan ellenőrizzük?**
  - Reflexiós rétegszabály-teszt: a Domain és az Application assembly hivatkozásai között nincs tiltott projekt- vagy csomaghivatkozás.
  - A Domain és az Application réteg sorlefedettsége ≥ 80% a CI-ban (QA-6).
  - Az S-2 paraméterezett integrációs teszt minden háztartáshoz kötött végpontra (QA-1).
  - Tervezési validáció: [P-12](../../07_ai/prompt_log.md); az architektúra-kapu keresztvalidációjának eredménye ide kerül.
- **Evidence link:** a walking skeleton és az 1. lépcső tesztjei; a link az implementációval együtt kerül ide.
