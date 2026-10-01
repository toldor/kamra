# Versenytárs-elemzés

Hasonló recept-, kamra- és bevásárlólista-alkalmazások funkciónkénti összehasonlítása. Minden cella a hivatkozott forráson alapul (ellenőrizve: 2026-09-26).

**Jelölések:** ✓ a forrás szerint van · ✗ a forrás szerint nincs · **n.e.** a vizsgált forrás nem említi (ez **nem** jelenti azt, hogy nincs)

## Összehasonlítás

| Alkalmazás | Készlet-nyilvántartás | Lejárat-követés | Recept a készletből | Lejárat szerinti rangsor | Bevásárlólista | Közös háztartás | Web | Magyar nyelv | Bevitel módja |
|---|---|---|---|---|---|---|---|---|---|
| **Samsung Food** [1][2] | ✓ (Food List) | ✓ | ✓ | ✓ (Food+ előfizetéssel) | ✓ | ✓ (megosztott bevásárlólista) | ✓ | ✗ | fotó, kézi |
| **KitchenPal** [3] | ✓ | ✓ (értesítéssel) | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | vonalkód, hang, szöveg |
| **NoWaste.ai** [4] | ✓ | ✓ (értesítéssel) | ✓ (AI) | n.e. | ✓ (szokásokból generált) | ✓ (Family csomag) | n.e. | n.e. | blokkszkennelés, vonalkód |
| **SuperCook** [5][6] | ✓ (hozzávalólista) | ✗ | ✓ | ✗ | n.e. | n.e. | ✓ | n.e. | lista, hangdiktálás |
| **Paprika** [7] | ✓ | n.e. | n.e. | n.e. | ✓ | n.e. | n.e. (iOS, Android, Mac, Windows) | n.e. | n.e. |
| **Bring!** [8] | n.e. | n.e. | n.e. (heti receptválogatás) | n.e. | ✓ | ✓ | n.e. | n.e. | n.e. |
| **AnyList** [9] | n.e. | n.e. | n.e. | n.e. | ✓ | ✓ | ✓ | n.e. | n.e. |
| **Pantry Vault AI** (nyílt forrású projekt) [10] | ✓ | ✓ | n.e. | n.e. | n.e. | n.e. | n.e. | n.e. | bemásolt lista → LLM kinyeri a tételeket, mennyiséget és lejáratot |
| **Kamra** *(tervezett, még nincs implementálva)* | ✓ | ✓ (kategória alapú becsléssel) | ✓ | ✓ | ✓ | ✗ (non-goal) | ✓ | ✓ | form, egy mondatos szabad szöveg jóváhagyással |

## Megkülönböztető érték

**Tudott hiányosság:** a jelenlegi terv szerint a Kamrának nincs olyan funkciója vagy értéke, amelyről forrással igazolható, hogy más alkalmazásban nincs meg. A táblázat alapján minden tervezett fő funkció (lejárat-követés, lejárat szerint rangsorolt receptajánlás, főzés utáni levonás, bevásárlólista) legalább egy versenytársban megtalálható. Az egy mondatos, szabad szöveges bevitel is létezik [10]. Negatív állítás, például „más app nem magyar nyelvű” vagy „más app nem indokolja az ajánlást”, a teljes piac átvizsgálása nélkül nem igazolható. Az ellenőrzés menete: [verification_log V-01](../07_ai/verification_log.md).

Ami a táblázatból igazolhatóan kiolvasható: a magyar nyelvet a Samsung Food és a KitchenPal a saját nyelvlistája szerint nem támogatja [2][3]. Ez két konkrét alkalmazásra igaz, nem a piac egészére.

## Források

1. Samsung Newsroom, IFA 2024 – Samsung Food+ (Food List, lejárat közeli tételek priorizálása, fotós bevitel): https://news.samsung.com/mx/samsung-food-mejora-para-elevar-el-nivel-de-las-experiencias-gastronomicas-en-ifa-2024
2. Samsung Food – App Store leírás és nyelvlista: https://apps.apple.com/us/app/samsung-food-meal-planner/id1133637674 · Web: https://samsungfood.com/
3. KitchenPal – hivatalos oldal (funkciók, platformok, nyelvek): https://kitchenpalapp.com/en/
4. NoWaste.ai – hivatalos oldal: https://nowaste.ai/
5. SuperCook – App Store leírás: https://apps.apple.com/us/app/supercook-recipe-by-ingredient/id1477747816 · Web: https://www.supercook.com/
6. SuperCook App Review 2026 (lejárat-követés hiánya): https://quickdishcookbook.com/supercook-the-best-app-for-using-up-the-ingredients-in-your-pantry/
7. Paprika – hivatalos oldal: https://www.paprikaapp.com/
8. Bring! – hivatalos oldal: https://www.getbring.com/en/home
9. AnyList – hivatalos oldal: https://www.anylist.com/
10. Pantry Vault AI – GitHub README: https://github.com/hardijain26/pantry_vault_ai
