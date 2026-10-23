using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KamraApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Ingredients",
                columns: new[] { "Id", "DefaultCategory", "Dimension", "HouseholdId", "Name", "NormalizedName", "RetiredAt", "SeedKey" },
                values: new object[,]
                {
                    { new Guid("01a14b8b-9fb8-7d94-a937-28da98afaa55"), "FreshMeatFish", "Mass", null, "marhalábszár", "marhalábszár", null, "marhalabszar" },
                    { new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"), "Vegetables", "Mass", null, "petrezselyemgyökér", "petrezselyemgyökér", null, "petrezselyemgyoker" },
                    { new Guid("01a14b8b-9fba-722b-bd18-071f08b0a1d4"), "DryGoods", "Mass", null, "köménymag", "köménymag", null, "komenymag" },
                    { new Guid("01a14b8b-9fbb-7108-b43a-0619e7bc9097"), "FreshMeatFish", "Mass", null, "csirkecomb", "csirkecomb", null, "csirkecomb" },
                    { new Guid("01a14b8b-9fbc-781e-9084-50717d7a7926"), "DryGoods", "Mass", null, "zsemlemorzsa", "zsemlemorzsa", null, "zsemlemorzsa" },
                    { new Guid("01a14b8b-9fbd-7439-9bca-70a478075d66"), "Vegetables", "Mass", null, "petrezselyemzöld", "petrezselyemzöld", null, "petrezselyemzold" },
                    { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), "DryGoods", "Count", null, "babérlevél", "babérlevél", null, "baberlevel" },
                    { new Guid("01a14b8b-9fbf-7bb6-bf48-090b5d6be2b2"), "DryGoods", "Mass", null, "szárazbab", "szárazbab", null, "szarazbab" },
                    { new Guid("01a14b8b-9fc0-7682-9163-2771298f3e69"), "DryGoods", "Mass", null, "búzadara", "búzadara", null, "buzadara" },
                    { new Guid("01a14b8b-9fc1-765b-9843-49e3c5a0fedc"), "DryGoods", "Mass", null, "kakaópor", "kakaópor", null, "kakaopor" },
                    { new Guid("01a14b8b-9fc2-70ce-be70-6e1540e0553c"), "Vegetables", "Mass", null, "csiperkegomba", "csiperkegomba", null, "csiperkegomba" },
                    { new Guid("01a14b8b-9fc3-71e0-a769-8e6806da8751"), "DryGoods", "Mass", null, "lencse", "lencse", null, "lencse" },
                    { new Guid("01a14b8b-9fc4-7604-8519-03132846423c"), "Dairy", "Volume", null, "kefir", "kefir", null, "kefir" },
                    { new Guid("01a14b8b-9fc5-734c-86d7-490e6314c360"), "Dairy", "Volume", null, "író", "író", null, "iro" },
                    { new Guid("01a14b8b-9fc6-7e83-8043-668b7428fdfc"), "Dairy", "Mass", null, "görög joghurt", "görög joghurt", null, "gorog-joghurt" },
                    { new Guid("01a14b8b-9fc7-7dd5-950b-fc03f854fca1"), "Dairy", "Mass", null, "margarin", "margarin", null, "margarin" },
                    { new Guid("01a14b8b-9fc8-7ac2-8aa0-16773dcb500c"), "Dairy", "Volume", null, "laktózmentes tej", "laktózmentes tej", null, "laktozmentes-tej" },
                    { new Guid("01a14b8b-9fc9-7d4c-90e0-8afaf46fd6de"), "Cheese", "Mass", null, "mozzarella", "mozzarella", null, "mozzarella" },
                    { new Guid("01a14b8b-9fca-7a7d-9d84-8656f58e741c"), "Cheese", "Mass", null, "parmezán", "parmezán", null, "parmezan" },
                    { new Guid("01a14b8b-9fcb-7a6a-b2ef-39e22c6b8c70"), "Cheese", "Mass", null, "feta", "feta", null, "feta" },
                    { new Guid("01a14b8b-9fcc-7eb1-a4b4-de9e80e305f0"), "Cheese", "Mass", null, "gouda sajt", "gouda sajt", null, "gouda-sajt" },
                    { new Guid("01a14b8b-9fcd-7579-8861-619572b13526"), "Cheese", "Mass", null, "füstölt sajt", "füstölt sajt", null, "fustolt-sajt" },
                    { new Guid("01a14b8b-9fce-7a65-abd1-002ace82f53a"), "Cheese", "Mass", null, "krémsajt", "krémsajt", null, "kremsajt" },
                    { new Guid("01a14b8b-9fcf-73fb-8b90-0706c234f795"), "Cheese", "Mass", null, "camembert", "camembert", null, "camembert" },
                    { new Guid("01a14b8b-9fd0-7916-9584-553f57ca4459"), "Eggs", "Count", null, "fürjtojás", "fürjtojás", null, "furjtojas" },
                    { new Guid("01a14b8b-9fd1-7dda-925a-a379cdc065a3"), "FreshMeatFish", "Mass", null, "sertéskaraj", "sertéskaraj", null, "serteskaraj" },
                    { new Guid("01a14b8b-9fd2-7982-8ec6-fc5496eb59f5"), "FreshMeatFish", "Mass", null, "sertéstarja", "sertéstarja", null, "sertestarja" },
                    { new Guid("01a14b8b-9fd3-7959-9985-50cc29f105b0"), "FreshMeatFish", "Mass", null, "csirkeszárny", "csirkeszárny", null, "csirkeszarny" },
                    { new Guid("01a14b8b-9fd4-7bb7-9d80-55e0c08866e7"), "FreshMeatFish", "Mass", null, "pulykamell", "pulykamell", null, "pulykamell" },
                    { new Guid("01a14b8b-9fd5-7f81-ad20-140f4a45078c"), "FreshMeatFish", "Mass", null, "darált marhahús", "darált marhahús", null, "daralt-marhahus" },
                    { new Guid("01a14b8b-9fd6-7f80-a261-6913e3d33cc0"), "FreshMeatFish", "Mass", null, "darált pulykahús", "darált pulykahús", null, "daralt-pulykahus" },
                    { new Guid("01a14b8b-9fd7-7ad7-bf01-070af95592e1"), "FreshMeatFish", "Mass", null, "lazacfilé", "lazacfilé", null, "lazacfile" },
                    { new Guid("01a14b8b-9fd8-77f1-bfb5-7dffd142f68a"), "FreshMeatFish", "Mass", null, "hekk", "hekk", null, "hekk" },
                    { new Guid("01a14b8b-9fd9-70c5-bb30-c437b1648340"), "ProcessedMeat", "Mass", null, "virsli", "virsli", null, "virsli" },
                    { new Guid("01a14b8b-9fda-7be8-bc6d-1106cd0be464"), "ProcessedMeat", "Mass", null, "párizsi", "párizsi", null, "parizsi" },
                    { new Guid("01a14b8b-9fdb-79b4-a2fd-ad339e92eabc"), "ProcessedMeat", "Mass", null, "szalámi", "szalámi", null, "szalami" },
                    { new Guid("01a14b8b-9fdc-7a7b-b49e-1dfef8c68166"), "ProcessedMeat", "Mass", null, "bacon", "bacon", null, "bacon" },
                    { new Guid("01a14b8b-9fdd-799f-afdc-9f8799a0dfcc"), "ProcessedMeat", "Mass", null, "főtt tarja", "főtt tarja", null, "fott-tarja" },
                    { new Guid("01a14b8b-9fde-7d90-8344-caf1476cc97e"), "Vegetables", "Mass", null, "uborka", "uborka", null, "uborka" },
                    { new Guid("01a14b8b-9fdf-772e-8ece-9fd7ab082bb6"), "Vegetables", "Mass", null, "cukkini", "cukkini", null, "cukkini" },
                    { new Guid("01a14b8b-9fe0-7216-97e2-23760c372ffc"), "Vegetables", "Mass", null, "padlizsán", "padlizsán", null, "padlizsan" },
                    { new Guid("01a14b8b-9fe1-7fac-b0a3-0abe2a1c390a"), "Vegetables", "Mass", null, "brokkoli", "brokkoli", null, "brokkoli" },
                    { new Guid("01a14b8b-9fe2-774c-afe2-9e345305568d"), "Vegetables", "Mass", null, "karfiol", "karfiol", null, "karfiol" },
                    { new Guid("01a14b8b-9fe3-73d6-9129-d810b1085734"), "Vegetables", "Mass", null, "fejes káposzta", "fejes káposzta", null, "fejes-kaposzta" },
                    { new Guid("01a14b8b-9fe4-7096-baed-7c0c578af435"), "Vegetables", "Mass", null, "kelkáposzta", "kelkáposzta", null, "kelkaposzta" },
                    { new Guid("01a14b8b-9fe5-7b24-a651-345536885c5f"), "Vegetables", "Mass", null, "lilahagyma", "lilahagyma", null, "lilahagyma" },
                    { new Guid("01a14b8b-9fe6-79a8-87ef-f4d86b7d1fc3"), "Vegetables", "Count", null, "újhagyma", "újhagyma", null, "ujhagyma" },
                    { new Guid("01a14b8b-9fe7-7c74-8b15-498bade992d3"), "Vegetables", "Count", null, "póréhagyma", "póréhagyma", null, "porehagyma" },
                    { new Guid("01a14b8b-9fe8-78b4-b4f2-dd205aded85c"), "Vegetables", "Mass", null, "zellergumó", "zellergumó", null, "zellergumo" },
                    { new Guid("01a14b8b-9fe9-716a-b2db-90cf247d9ba0"), "Vegetables", "Mass", null, "cékla", "cékla", null, "cekla" },
                    { new Guid("01a14b8b-9fea-7c77-9efb-d220150bd556"), "Vegetables", "Mass", null, "spenót", "spenót", null, "spenot" },
                    { new Guid("01a14b8b-9feb-7c24-857c-4dd3d6aefc81"), "Vegetables", "Count", null, "jégsaláta", "jégsaláta", null, "jegsalata" },
                    { new Guid("01a14b8b-9fec-7120-a25c-461eb47c6a0b"), "Vegetables", "Mass", null, "retek", "retek", null, "retek" },
                    { new Guid("01a14b8b-9fed-75a8-adf6-a937e74344ca"), "Vegetables", "Mass", null, "zöldbab", "zöldbab", null, "zoldbab" },
                    { new Guid("01a14b8b-9fee-7392-a523-2448e4ac3088"), "Vegetables", "Mass", null, "sütőtök", "sütőtök", null, "sutotok" },
                    { new Guid("01a14b8b-9fef-734c-90dc-9789e1789aa4"), "Vegetables", "Mass", null, "édesburgonya", "édesburgonya", null, "edesburgonya" },
                    { new Guid("01a14b8b-9ff0-749f-9374-fddfb810fe55"), "Vegetables", "Mass", null, "kapor", "kapor", null, "kapor" },
                    { new Guid("01a14b8b-9ff1-7339-94f5-18a7ecfafeaa"), "Vegetables", "Mass", null, "snidling", "snidling", null, "snidling" },
                    { new Guid("01a14b8b-9ff2-7631-b24f-e26f456a5011"), "Vegetables", "Mass", null, "gyömbér", "gyömbér", null, "gyomber" },
                    { new Guid("01a14b8b-9ff3-7e16-9113-cbc044b3a3f3"), "Fruit", "Count", null, "körte", "körte", null, "korte" },
                    { new Guid("01a14b8b-9ff4-79b7-8e59-ed88e3c0d2c8"), "Fruit", "Count", null, "narancs", "narancs", null, "narancs" },
                    { new Guid("01a14b8b-9ff5-7612-8561-09e35cf6de4f"), "Fruit", "Count", null, "mandarin", "mandarin", null, "mandarin" },
                    { new Guid("01a14b8b-9ff6-730d-b867-a562f03ce85b"), "Fruit", "Count", null, "kiwi", "kiwi", null, "kiwi" },
                    { new Guid("01a14b8b-9ff7-7f9c-a433-3eb150069ca9"), "Fruit", "Count", null, "őszibarack", "őszibarack", null, "oszibarack" },
                    { new Guid("01a14b8b-9ff8-7777-b595-f44a5f1d8da8"), "Fruit", "Count", null, "avokádó", "avokádó", null, "avokado" },
                    { new Guid("01a14b8b-9ff9-7836-9d3d-6a648dd6c265"), "Fruit", "Count", null, "lime", "lime", null, "lime" },
                    { new Guid("01a14b8b-9ffa-70b7-9376-789903025e23"), "Fruit", "Count", null, "ananász", "ananász", null, "ananasz" },
                    { new Guid("01a14b8b-9ffb-75b1-a879-e2bc79de1d39"), "Fruit", "Count", null, "görögdinnye", "görögdinnye", null, "gorogdinnye" },
                    { new Guid("01a14b8b-9ffc-75a8-a701-f555efeddca1"), "Fruit", "Mass", null, "szőlő", "szőlő", null, "szolo" },
                    { new Guid("01a14b8b-9ffd-7474-9a65-d84efbfe68a4"), "Fruit", "Mass", null, "eper", "eper", null, "eper" },
                    { new Guid("01a14b8b-9ffe-7dee-84c5-832578d291bb"), "Fruit", "Mass", null, "málna", "málna", null, "malna" },
                    { new Guid("01a14b8b-9fff-7b03-8822-ea3769c5b1aa"), "Fruit", "Mass", null, "szilva", "szilva", null, "szilva" },
                    { new Guid("01a14b8b-a000-7f08-9a84-df6acf733a32"), "Fruit", "Mass", null, "meggy", "meggy", null, "meggy" },
                    { new Guid("01a14b8b-a001-76fd-8541-04aaaf7e3f53"), "Bakery", "Count", null, "zsemle", "zsemle", null, "zsemle" },
                    { new Guid("01a14b8b-a002-795d-9c8b-0bce0f4b87d4"), "Bakery", "Count", null, "kalács", "kalács", null, "kalacs" },
                    { new Guid("01a14b8b-a003-70dd-9c05-17a74df128ff"), "Bakery", "Count", null, "bagett", "bagett", null, "bagett" },
                    { new Guid("01a14b8b-a004-7783-bb49-47e81d030551"), "Bakery", "Count", null, "rozskenyér", "rozskenyér", null, "rozskenyer" },
                    { new Guid("01a14b8b-a005-7a26-8752-9da48ddad226"), "Bakery", "Count", null, "tortilla lap", "tortilla lap", null, "tortilla-lap" },
                    { new Guid("01a14b8b-a006-7f2c-84ac-ff417150b8d2"), "Bakery", "Count", null, "croissant", "croissant", null, "croissant" },
                    { new Guid("01a14b8b-a007-71c0-a02d-56c5f6e69195"), "DryGoods", "Mass", null, "zabpehely", "zabpehely", null, "zabpehely" },
                    { new Guid("01a14b8b-a008-794b-bbb3-fcf98f1a1ad8"), "DryGoods", "Mass", null, "teljes kiőrlésű liszt", "teljes kiőrlésű liszt", null, "teljes-kiorlesu-liszt" },
                    { new Guid("01a14b8b-a009-78dc-a770-5022b03eee29"), "DryGoods", "Mass", null, "sütőpor", "sütőpor", null, "sutopor" },
                    { new Guid("01a14b8b-a00a-7d3a-a8a1-9bcf42e2d5dc"), "DryGoods", "Mass", null, "szárított élesztő", "szárított élesztő", null, "szaritott-eleszto" },
                    { new Guid("01a14b8b-a00b-78ac-a1e9-c19a448491be"), "DryGoods", "Mass", null, "porcukor", "porcukor", null, "porcukor" },
                    { new Guid("01a14b8b-a00c-7ee9-822f-69c85de782eb"), "DryGoods", "Mass", null, "vaníliás cukor", "vaníliás cukor", null, "vanilias-cukor" },
                    { new Guid("01a14b8b-a00d-7e3c-aae9-bdd24efe90e7"), "DryGoods", "Mass", null, "csicseriborsó", "csicseriborsó", null, "csicseriborso" },
                    { new Guid("01a14b8b-a00e-77aa-a1dc-17ebe6cb7431"), "DryGoods", "Mass", null, "kuszkusz", "kuszkusz", null, "kuszkusz" },
                    { new Guid("01a14b8b-a00f-735f-b84e-7935033bcaa2"), "DryGoods", "Mass", null, "tarhonya", "tarhonya", null, "tarhonya" },
                    { new Guid("01a14b8b-a010-747e-ae52-c39a3a7d4a85"), "DryGoods", "Mass", null, "müzli", "müzli", null, "muzli" },
                    { new Guid("01a14b8b-a011-751b-bd8f-4d507d9b93e0"), "DryGoods", "Mass", null, "keksz", "keksz", null, "keksz" },
                    { new Guid("01a14b8b-a012-72ee-84de-de5b1d8e2ddf"), "DryGoods", "Mass", null, "dió", "dió", null, "dio" },
                    { new Guid("01a14b8b-a013-7a5c-85ba-285b8b0faec9"), "DryGoods", "Mass", null, "mandula", "mandula", null, "mandula" },
                    { new Guid("01a14b8b-a014-7c6d-a531-8294214fbb39"), "DryGoods", "Mass", null, "mazsola", "mazsola", null, "mazsola" },
                    { new Guid("01a14b8b-a015-7f8d-81c7-290508754b65"), "DryGoods", "Mass", null, "napraforgómag", "napraforgómag", null, "napraforgomag" },
                    { new Guid("01a14b8b-a016-73af-a9a6-09c2d9570a37"), "DryGoods", "Volume", null, "olívaolaj", "olívaolaj", null, "olivaolaj" },
                    { new Guid("01a14b8b-a017-7aa4-ae35-df0a4f093337"), "DryGoods", "Mass", null, "majoranna", "majoranna", null, "majoranna" },
                    { new Guid("01a14b8b-a018-7707-8fd1-e7075b717e54"), "DryGoods", "Mass", null, "oregánó", "oregánó", null, "oregano" },
                    { new Guid("01a14b8b-a019-7856-a508-e008752fb31f"), "DryGoods", "Mass", null, "bazsalikom (szárított)", "bazsalikom (szárított)", null, "bazsalikom" },
                    { new Guid("01a14b8b-a01a-7db5-9fc7-1a5d136b1c97"), "DryGoods", "Mass", null, "fahéj", "fahéj", null, "fahej" },
                    { new Guid("01a14b8b-a01b-7d1a-af85-3f187b20f07b"), "DryGoods", "Mass", null, "kávé", "kávé", null, "kave" },
                    { new Guid("01a14b8b-a01c-7b85-8544-9c50185d4f32"), "DryGoods", "Count", null, "filteres tea", "filteres tea", null, "filteres-tea" },
                    { new Guid("01a14b8b-a01d-7b87-9c2e-aa7ce5bb4202"), "CannedAndSauces", "Mass", null, "ketchup", "ketchup", null, "ketchup" },
                    { new Guid("01a14b8b-a01e-7346-87c2-acaa3432ee65"), "CannedAndSauces", "Mass", null, "mustár", "mustár", null, "mustar" },
                    { new Guid("01a14b8b-a01f-77a4-9e21-ef64d2c0d141"), "CannedAndSauces", "Mass", null, "majonéz", "majonéz", null, "majonez" },
                    { new Guid("01a14b8b-a020-73a0-970f-d4c6f4d47c3a"), "CannedAndSauces", "Mass", null, "lekvár", "lekvár", null, "lekvar" },
                    { new Guid("01a14b8b-a021-7cb8-91f2-bb150a4cd4e9"), "CannedAndSauces", "Mass", null, "méz", "méz", null, "mez" },
                    { new Guid("01a14b8b-a022-7e52-b005-6f92f590ebb8"), "CannedAndSauces", "Mass", null, "csemegekukorica (konzerv)", "csemegekukorica (konzerv)", null, "csemegekukorica" },
                    { new Guid("01a14b8b-a023-7143-be77-d1c06cf90ab6"), "CannedAndSauces", "Mass", null, "tonhalkonzerv", "tonhalkonzerv", null, "tonhalkonzerv" },
                    { new Guid("01a14b8b-a024-7fdb-a3ac-5a8a9175f979"), "CannedAndSauces", "Mass", null, "hámozott paradicsom (konzerv)", "hámozott paradicsom (konzerv)", null, "hamozott-paradicsom" },
                    { new Guid("01a14b8b-a025-7fc0-8e75-fe73347fe208"), "CannedAndSauces", "Volume", null, "szójaszósz", "szójaszósz", null, "szojaszosz" },
                    { new Guid("01a14b8b-a026-7d8a-af60-c1d6ec50ee59"), "CannedAndSauces", "Volume", null, "ecet", "ecet", null, "ecet" },
                    { new Guid("01a14b8b-a027-70f4-a30c-7097901c5ff4"), "CannedAndSauces", "Mass", null, "csemegeuborka", "csemegeuborka", null, "csemegeuborka" },
                    { new Guid("01a14b8b-a028-7919-afdd-0366fdaff468"), "CannedAndSauces", "Mass", null, "sűrített tej", "sűrített tej", null, "suritett-tej" },
                    { new Guid("01a14b8b-a029-7789-9ba2-024ba8cd5e00"), "Frozen", "Mass", null, "fagyasztott spenót", "fagyasztott spenót", null, "fagyasztott-spenot" },
                    { new Guid("01a14b8b-a02a-7aef-819d-d70f7c004360"), "Frozen", "Mass", null, "fagyasztott vegyes zöldség", "fagyasztott vegyes zöldség", null, "fagyasztott-vegyes-zoldseg" },
                    { new Guid("01a14b8b-a02b-7465-9466-47818fce4a2e"), "Frozen", "Mass", null, "fagyasztott hasábburgonya", "fagyasztott hasábburgonya", null, "fagyasztott-hasabburgonya" },
                    { new Guid("01a14b8b-a02c-74c3-9ffe-43fb07d79541"), "Frozen", "Mass", null, "halrudacska", "halrudacska", null, "halrudacska" },
                    { new Guid("01a14b8b-a02d-75d3-968e-0fc7373183b4"), "Beverages", "Volume", null, "ásványvíz", "ásványvíz", null, "asvanyviz" },
                    { new Guid("01a14b8b-a02e-765b-b2b9-c81a987e13f1"), "Beverages", "Volume", null, "almalé", "almalé", null, "almale" },
                    { new Guid("01a14b8b-a02f-7bf0-a330-510520918764"), "Beverages", "Volume", null, "szörp", "szörp", null, "szorp" },
                    { new Guid("01a14b8b-a030-7b2b-babe-d00949f6fa5c"), "PreparedFood", "Mass", null, "hummusz", "hummusz", null, "hummusz" }
                });

            migrationBuilder.InsertData(
                table: "Recipes",
                columns: new[] { "Id", "CreatedAt", "HouseholdId", "Instructions", "Name", "RetiredAt", "SeedKey", "Servings", "Source" },
                values: new object[,]
                {
                    { new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A burgonyát héjában, a tojást keményre főzzük, majd mindkettőt megtisztítjuk és felkarikázzuk. Kivajazott tálba rétegezzük a burgonyát, a tojást és a karikára vágott kolbászt, minden réteget megsózunk és tejföllel kenünk meg; a tetejére burgonya és tejföl kerül. Előmelegített sütőben kb. 30 percig sütjük, amíg a teteje megpirul.", "Rakott krumpli", null, "rakott-krumpli", 4, "Seed" },
                    { new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A hagymát az olajon megdinszteljük, a kockára vágott húst hozzáadva fehéredésig pirítjuk. Lehúzzuk a tűzről, megszórjuk pirospaprikával és köménymaggal, felöntjük vízzel, és fedő alatt kb. másfél órán át főzzük. Hozzáadjuk a kockára vágott zöldségeket, a paprikát és a paradicsomot, megsózzuk, és addig főzzük, amíg minden megpuhul.", "Gulyásleves", null, "gulyasleves", 4, "Seed" },
                    { new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A hagymát az olajon üvegesre pároljuk, levesszük a tűzről, és elkeverjük benne a pirospaprikát. Beletesszük a csirkecombot, a felaprított paprikát és paradicsomot, megsózzuk, kevés vízzel felöntjük, és fedő alatt puhára pároljuk. A tejfölt a liszttel csomómentesre keverjük, a szaftba forgatjuk, és még néhány percig forraljuk.", "Csirkepaprikás", null, "csirkepaprikas", 4, "Seed" },
                    { new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A finomra vágott hagymát, fokhagymát és reszelt répát az olajon megfuttatjuk, hozzáadjuk a darált húst, és morzsásra pirítjuk. Belekeverjük a paradicsompürét és kevés vizet, sózzuk, borsozzuk, és lassú tűzön kb. 30 percig főzzük. A tésztát sós vízben kifőzzük, a szósszal tálaljuk, a tetejére reszelt sajtot szórunk.", "Bolognai spagetti", null, "bolognai-spagetti", 4, "Seed" },
                    { new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A csirkemellet szeletekre vágjuk, kissé kiklopfoljuk és megsózzuk. Lisztbe, felvert tojásba, majd zsemlemorzsába forgatjuk, és forró olajban mindkét oldalát aranybarnára sütjük. A rizst kevés olajon átforgatjuk, kétszeres mennyiségű sós vízzel felöntjük, és fedő alatt puhára pároljuk.", "Rántott csirkemell rizzsel", null, "rantott-csirkemell-rizzsel", 4, "Seed" },
                    { new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A borsót a vajon néhány percig pároljuk, megszórjuk cukorral és sóval, majd kevés vízzel felöntve puhára főzzük. A lisztet a tejjel simára keverjük, a főzelékhez adjuk, és sűrűre forraljuk. Végül belekeverjük az apróra vágott petrezselymet.", "Zöldborsófőzelék", null, "zoldborsofozelek", 4, "Seed" },
                    { new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A kockára vágott burgonyát a babérlevéllel sós vízben puhára főzzük. Az olajon a finomra vágott hagymát megdinszteljük, rászórjuk a lisztet és a pirospaprikát, majd hideg vízzel csomómentes rántást készítünk. A rántással besűrítjük a főzeléket, végül a tejföllel elkeverjük; a babérlevelet tálalás előtt kivesszük.", "Krumplifőzelék", null, "krumplifozelek", 4, "Seed" },
                    { new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A tésztát sós vízben kifőzzük és leszűrjük. A vajon a felkockázott sonkát átforgatjuk, hozzáadjuk a tésztát és a tejfölt. A reszelt sajt felét belekeverjük, a többit a tetejére szórjuk, és tálalás előtt fedő alatt hagyjuk megolvadni.", "Sajtos-sonkás tészta", null, "sajtos-sonkas-teszta", 4, "Seed" },
                    { new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A babot előző este beáztatjuk, majd friss vízben a babérlevéllel félpuhára főzzük. Hozzáadjuk a felkarikázott zöldségeket és kolbászt, és együtt puhára főzzük. Az olajon a hagymát és a fokhagymát megfuttatjuk, rászórjuk a pirospaprikát, a tejfölös-lisztes habarással együtt a leveshez adjuk, megsózzuk, és még néhány percig forraljuk.", "Bableves", null, "bableves", 4, "Seed" },
                    { new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A rizst a vajon üvegesre pirítjuk, majd kétszeres mennyiségű sós vízzel felöntjük. Amikor felforrt, hozzáadjuk a zöldborsót, és fedő alatt, kis lángon puhára pároljuk. Tálalás előtt villával átlazítjuk.", "Rizibizi", null, "rizibizi", 4, "Seed" },
                    { new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A tejet a cukorral és a vajjal felforraljuk. Folyamatos keverés mellett beleszórjuk a búzadarát, és kis lángon addig főzzük, amíg besűrűsödik. Tálaláskor a tetejét kakaóporral szórjuk meg.", "Tejbegríz", null, "tejbegriz", 4, "Seed" },
                    { new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A hagymát az olajon megdinszteljük, levesszük a tűzről, és elkeverjük benne a pirospaprikát. Hozzáadjuk a felszeletelt gombát, megsózzuk, és saját levében puhára pároljuk. A tejfölt a liszttel elkeverjük, a gombához adjuk, és néhány perc alatt összeforraljuk.", "Gombapaprikás", null, "gombapaprikas", 4, "Seed" },
                    { new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A vajon a lisztet világosra pirítjuk, hozzáadjuk a paradicsompürét, majd felöntjük vízzel, és csomómentesre keverjük. Cukorral és sóval ízesítjük, és kb. 10 percig főzzük. Közben a tésztát külön kifőzzük, és a tányérban a levesre szedjük.", "Paradicsomleves", null, "paradicsomleves", 4, "Seed" },
                    { new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A darált húst összekeverjük a nyers rizzsel, a tojással, az apróra vágott hagymával, sóval és borssal. A paprikák magházát kivágjuk, és lazán megtöltjük a masszával. A paradicsompürét vízzel felhígítjuk, cukorral és sóval ízesítjük, beletesszük a paprikákat, és fedő alatt kb. egy órán át főzzük; végül a mártást lisztes habarással besűrítjük.", "Töltött paprika", null, "toltott-paprika", 4, "Seed" },
                    { new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A lencsét átválogatjuk, és a babérlevéllel sós vízben puhára főzzük. Az olajon a hagymát megdinszteljük, a lisztet rászórva világos rántást készítünk, és a főzelékhez adjuk. Néhány perc forralás után a tejföllel elkeverjük; a babérlevelet tálalás előtt kivesszük.", "Lencsefőzelék", null, "lencsefozelek", 4, "Seed" }
                });

            migrationBuilder.InsertData(
                table: "RecipeIngredients",
                columns: new[] { "IngredientId", "RecipeId", "EnteredUnit", "Quantity" },
                values: new object[,]
                {
                    { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "Kg", 1000m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "G", 400m },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "Db", 4m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "G", 30m },
                    { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "G", 200m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"), "G", 10m },
                    { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 100m },
                    { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 100m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 150m },
                    { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 500m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "Ml", 30m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 10m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 10m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "L", 1500.0m },
                    { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 150m },
                    { new Guid("01a14b8b-9fb8-7d94-a937-28da98afaa55"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 500m },
                    { new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 100m },
                    { new Guid("01a14b8b-9fba-722b-bd18-071f08b0a1d4"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"), "G", 2m },
                    { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 100m },
                    { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 100m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 150m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "Ml", 30m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 200m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 20m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 10m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "G", 8m },
                    { new Guid("01a14b8b-9fbb-7108-b43a-0619e7bc9097"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"), "Kg", 1000m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 100m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "Ml", 30m },
                    { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 400m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 8m },
                    { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 1m },
                    { new Guid("01a126a0-f4ac-74eb-8f00-38d37814a809"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 100m },
                    { new Guid("01a126a0-f4ae-7bad-a26d-3db0c8e0bd77"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 500m },
                    { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 100m },
                    { new Guid("01a126a0-f4b6-72dc-9d01-ddf60e84df77"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "Db", 2m },
                    { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"), "G", 200m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "Ml", 300m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "G", 100m },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "Db", 2m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "G", 8m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "Ml", 600m },
                    { new Guid("01a126a0-f4ad-73e7-857a-7396a56d6d75"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "G", 600m },
                    { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "G", 300m },
                    { new Guid("01a14b8b-9fbc-781e-9084-50717d7a7926"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"), "G", 150m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 30m },
                    { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "Ml", 200m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 30m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 5m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "Ml", 300m },
                    { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 10m },
                    { new Guid("01a126a0-f4b8-70a8-92e7-887762859205"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 500m },
                    { new Guid("01a14b8b-9fbd-7439-9bca-70a478075d66"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"), "G", 10m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 100m },
                    { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 800m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "Ml", 30m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 200m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 30m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 5m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "G", 8m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "Ml", 600m },
                    { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587"), "Db", 2m },
                    { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 400m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 200m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 20m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 8m },
                    { new Guid("01a126a0-f4ac-74eb-8f00-38d37814a809"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 150m },
                    { new Guid("01a126a0-f4af-7d94-a469-1140c47e4814"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"), "G", 200m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 100m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "Ml", 20m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 200m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 20m },
                    { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 150m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 5m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 10m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "L", 2000m },
                    { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 150m },
                    { new Guid("01a126a0-f4b6-72dc-9d01-ddf60e84df77"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "Db", 2m },
                    { new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 100m },
                    { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "Db", 2m },
                    { new Guid("01a14b8b-9fbf-7bb6-bf48-090b5d6be2b2"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"), "G", 300m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), "G", 20m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), "G", 5m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), "Ml", 600m },
                    { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), "G", 300m },
                    { new Guid("01a126a0-f4b8-70a8-92e7-887762859205"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"), "G", 200m },
                    { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), "L", 1000m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), "G", 20m },
                    { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), "G", 60m },
                    { new Guid("01a14b8b-9fc0-7682-9163-2771298f3e69"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), "G", 150m },
                    { new Guid("01a14b8b-9fc1-765b-9843-49e3c5a0fedc"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"), "G", 10m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 150m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "Ml", 30m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 200m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 20m },
                    { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 10m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 6m },
                    { new Guid("01a14b8b-9fc2-70ce-be70-6e1540e0553c"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"), "G", 600m },
                    { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 80m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 20m },
                    { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 20m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 6m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "L", 1200.0m },
                    { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 40m },
                    { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"), "G", 200m },
                    { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 800m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 100m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 20m },
                    { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "Db", 1m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 10m },
                    { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 1m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "Ml", 800m },
                    { new Guid("01a126a0-f4ae-7bad-a26d-3db0c8e0bd77"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 500m },
                    { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 100m },
                    { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 30m },
                    { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"), "G", 300m },
                    { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "G", 100m },
                    { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "Ml", 30m },
                    { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "G", 100m },
                    { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "G", 30m },
                    { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "G", 8m },
                    { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "L", 1000m },
                    { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "Db", 2m },
                    { new Guid("01a14b8b-9fc3-71e0-a769-8e6806da8751"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"), "G", 300m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc4-7604-8519-03132846423c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc5-734c-86d7-490e6314c360"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc6-7e83-8043-668b7428fdfc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc7-7dd5-950b-fc03f854fca1"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc8-7ac2-8aa0-16773dcb500c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc9-7d4c-90e0-8afaf46fd6de"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fca-7a7d-9d84-8656f58e741c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fcb-7a6a-b2ef-39e22c6b8c70"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fcc-7eb1-a4b4-de9e80e305f0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fcd-7579-8861-619572b13526"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fce-7a65-abd1-002ace82f53a"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fcf-73fb-8b90-0706c234f795"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd0-7916-9584-553f57ca4459"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd1-7dda-925a-a379cdc065a3"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd2-7982-8ec6-fc5496eb59f5"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd3-7959-9985-50cc29f105b0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd4-7bb7-9d80-55e0c08866e7"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd5-7f81-ad20-140f4a45078c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd6-7f80-a261-6913e3d33cc0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd7-7ad7-bf01-070af95592e1"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd8-77f1-bfb5-7dffd142f68a"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fd9-70c5-bb30-c437b1648340"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fda-7be8-bc6d-1106cd0be464"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fdb-79b4-a2fd-ad339e92eabc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fdc-7a7b-b49e-1dfef8c68166"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fdd-799f-afdc-9f8799a0dfcc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fde-7d90-8344-caf1476cc97e"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fdf-772e-8ece-9fd7ab082bb6"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe0-7216-97e2-23760c372ffc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe1-7fac-b0a3-0abe2a1c390a"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe2-774c-afe2-9e345305568d"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe3-73d6-9129-d810b1085734"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe4-7096-baed-7c0c578af435"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe5-7b24-a651-345536885c5f"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe6-79a8-87ef-f4d86b7d1fc3"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe7-7c74-8b15-498bade992d3"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe8-78b4-b4f2-dd205aded85c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fe9-716a-b2db-90cf247d9ba0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fea-7c77-9efb-d220150bd556"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9feb-7c24-857c-4dd3d6aefc81"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fec-7120-a25c-461eb47c6a0b"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fed-75a8-adf6-a937e74344ca"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fee-7392-a523-2448e4ac3088"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fef-734c-90dc-9789e1789aa4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff0-749f-9374-fddfb810fe55"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff1-7339-94f5-18a7ecfafeaa"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff2-7631-b24f-e26f456a5011"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff3-7e16-9113-cbc044b3a3f3"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff4-79b7-8e59-ed88e3c0d2c8"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff5-7612-8561-09e35cf6de4f"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff6-730d-b867-a562f03ce85b"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff7-7f9c-a433-3eb150069ca9"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff8-7777-b595-f44a5f1d8da8"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ff9-7836-9d3d-6a648dd6c265"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ffa-70b7-9376-789903025e23"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ffb-75b1-a879-e2bc79de1d39"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ffc-75a8-a701-f555efeddca1"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ffd-7474-9a65-d84efbfe68a4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9ffe-7dee-84c5-832578d291bb"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fff-7b03-8822-ea3769c5b1aa"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a000-7f08-9a84-df6acf733a32"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a001-76fd-8541-04aaaf7e3f53"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a002-795d-9c8b-0bce0f4b87d4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a003-70dd-9c05-17a74df128ff"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a004-7783-bb49-47e81d030551"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a005-7a26-8752-9da48ddad226"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a006-7f2c-84ac-ff417150b8d2"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a007-71c0-a02d-56c5f6e69195"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a008-794b-bbb3-fcf98f1a1ad8"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a009-78dc-a770-5022b03eee29"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00a-7d3a-a8a1-9bcf42e2d5dc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00b-78ac-a1e9-c19a448491be"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00c-7ee9-822f-69c85de782eb"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00d-7e3c-aae9-bdd24efe90e7"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00e-77aa-a1dc-17ebe6cb7431"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a00f-735f-b84e-7935033bcaa2"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a010-747e-ae52-c39a3a7d4a85"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a011-751b-bd8f-4d507d9b93e0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a012-72ee-84de-de5b1d8e2ddf"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a013-7a5c-85ba-285b8b0faec9"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a014-7c6d-a531-8294214fbb39"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a015-7f8d-81c7-290508754b65"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a016-73af-a9a6-09c2d9570a37"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a017-7aa4-ae35-df0a4f093337"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a018-7707-8fd1-e7075b717e54"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a019-7856-a508-e008752fb31f"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01a-7db5-9fc7-1a5d136b1c97"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01b-7d1a-af85-3f187b20f07b"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01c-7b85-8544-9c50185d4f32"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01d-7b87-9c2e-aa7ce5bb4202"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01e-7346-87c2-acaa3432ee65"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a01f-77a4-9e21-ef64d2c0d141"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a020-73a0-970f-d4c6f4d47c3a"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a021-7cb8-91f2-bb150a4cd4e9"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a022-7e52-b005-6f92f590ebb8"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a023-7143-be77-d1c06cf90ab6"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a024-7fdb-a3ac-5a8a9175f979"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a025-7fc0-8e75-fe73347fe208"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a026-7d8a-af60-c1d6ec50ee59"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a027-70f4-a30c-7097901c5ff4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a028-7919-afdd-0366fdaff468"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a029-7789-9ba2-024ba8cd5e00"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02a-7aef-819d-d70f7c004360"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02b-7465-9466-47818fce4a2e"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02c-74c3-9ffe-43fb07d79541"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02d-75d3-968e-0fc7373183b4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02e-765b-b2b9-c81a987e13f1"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a02f-7bf0-a330-510520918764"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a030-7b2b-babe-d00949f6fa5c"));

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fb8-7d94-a937-28da98afaa55"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fba-722b-bd18-071f08b0a1d4"), new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f497-711a-a974-dff897cec5ad"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbb-7108-b43a-0619e7bc9097"), new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4ac-74eb-8f00-38d37814a809"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4ae-7bad-a26d-3db0c8e0bd77"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b6-72dc-9d01-ddf60e84df77"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4ad-73e7-857a-7396a56d6d75"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbc-781e-9084-50717d7a7926"), new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b8-70a8-92e7-887762859205"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbd-7439-9bca-70a478075d66"), new Guid("01a14b8b-a036-7313-af94-a38a4c73d060") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f499-70f0-baca-644f79523c11"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a037-7238-b442-81754ef8d587") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4ac-74eb-8f00-38d37814a809"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4af-7d94-a469-1140c47e4814"), new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a3-761c-ba7a-3ba388ba03e9"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b5-7e6f-80b9-dbcb7048edaa"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b6-72dc-9d01-ddf60e84df77"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbf-7bb6-bf48-090b5d6be2b2"), new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b8-70a8-92e7-887762859205"), new Guid("01a14b8b-a03a-70da-a649-24b7b3176829") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a0-7528-a0fa-827975890a2c"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fc0-7682-9163-2771298f3e69"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fc1-765b-9843-49e3c5a0fedc"), new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a4-748b-acd6-1ce2382afd1e"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fc2-70ce-be70-6e1540e0553c"), new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49b-7dc6-bf52-af8623e29f9b"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a2-7ba6-9efe-59bd7fd8e1bc"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a03d-7517-9607-4b88976269ba") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f496-7926-80c2-fffa4640a3c7"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a1-76ae-8300-46253511172a"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a6-79c2-a6b4-083b402ee44a"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4ae-7bad-a26d-3db0c8e0bd77"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b0-7909-8e1b-b4a28be99399"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b1-7548-9a34-96e76ef4d804"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4b7-7546-a5a1-9b55e78e6dc0"), new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f498-7ceb-818b-3d4613bec488"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49a-71be-8b17-cb68391f337b"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49d-7056-b903-76a6f2b64b57"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f49f-7974-9214-d4c1336a00ff"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a5-7dae-b0c3-91a860a253f3"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a126a0-f4a7-746d-8f8a-d6173bc02904"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "RecipeIngredients",
                keyColumns: new[] { "IngredientId", "RecipeId" },
                keyValues: new object[] { new Guid("01a14b8b-9fc3-71e0-a769-8e6806da8751"), new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917") });

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fb8-7d94-a937-28da98afaa55"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fb9-7f41-842e-0b53a40e84f9"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fba-722b-bd18-071f08b0a1d4"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fbb-7108-b43a-0619e7bc9097"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fbc-781e-9084-50717d7a7926"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fbd-7439-9bca-70a478075d66"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fbe-7b14-932f-c7f01a0dadf0"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fbf-7bb6-bf48-090b5d6be2b2"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc0-7682-9163-2771298f3e69"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc1-765b-9843-49e3c5a0fedc"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc2-70ce-be70-6e1540e0553c"));

            migrationBuilder.DeleteData(
                table: "Ingredients",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-9fc3-71e0-a769-8e6806da8751"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a031-7595-9a6f-cde8f2db92f1"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a032-7b7c-a567-4e863defc98a"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a033-7f8d-ae8a-afbcfce714ee"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a034-7d8f-a4f9-9755074455b3"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a035-77e0-ae1d-8d93fb45ecc1"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a036-7313-af94-a38a4c73d060"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a037-7238-b442-81754ef8d587"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a038-71ec-b471-4e969a7271fa"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a039-7c94-bd89-5668e36dc0d1"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03a-70da-a649-24b7b3176829"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03b-7d4e-9200-9ef52d9aefcf"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03c-7a15-8165-ad6d3575227b"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03d-7517-9607-4b88976269ba"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03e-76e3-82dc-b015066f8b07"));

            migrationBuilder.DeleteData(
                table: "Recipes",
                keyColumn: "Id",
                keyValue: new Guid("01a14b8b-a03f-7536-9668-c84e89ce0917"));
        }
    }
}
