using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class TycoonSceneBuilder
{
    private const string Root = "Assets/_LDY";
    private const string FontPath = "Assets/_JCY/07 Font/DOSIyagiBoldface SDF.asset";
    private static readonly string[] GeneratedRootNames = { "Canvas", "GameSystems", "EventSystem" };
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.6f);

    private static TMP_FontAsset font;

    [MenuItem("Tools/Tycoon/Build Scene")]
    public static void Build()
    {
        RemovePreviousObjects();
        font = LoadFont();

        var items = CreateItems();
        var upgrades = CreateUpgrades();
        AssignIcons(items, upgrades);
        var sprites = CreateCharacterSprites();
        var itemCardPrefab = CreateItemCardPrefab();
        var upgradeCardPrefab = CreateUpgradeCardPrefab();
        var popupPrefab = CreateCoinPopupPrefab();

        var canvas = CreateCanvas();
        var hud = CreateHud(canvas.transform);
        CreateCharacterArea(canvas.transform, sprites, popupPrefab, out var characterView, out var characterClickView, out var coinPopupView);
        CreateShop(canvas.transform, itemCardPrefab, upgradeCardPrefab, out var itemShopView, out var upgradeShopView);

        var systems = new GameObject("GameSystems");
        var ticker = systems.AddComponent<GameTicker>();
        var binder = systems.AddComponent<UiBinder>();
        SetFields(binder, ("hudView", hud), ("itemShopView", itemShopView), ("upgradeShopView", upgradeShopView),
            ("characterView", characterView), ("characterClickView", characterClickView), ("coinPopupView", coinPopupView));

        var bootstrapper = systems.AddComponent<GameBootstrapper>();
        SetFields(bootstrapper, ("ticker", ticker), ("uiBinder", binder));
        SetValues(bootstrapper, ("startingMoney", 50), ("powerLimit", 80), ("cautionThreshold", 0.7f));
        SetArray(bootstrapper, "items", items);
        SetArray(bootstrapper, "upgrades", upgrades);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tycoon scene built. Save the scene (Ctrl+S) and press Play.");
    }

    private static void RemovePreviousObjects()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (System.Array.IndexOf(GeneratedRootNames, root.name) >= 0)
                Object.DestroyImmediate(root);
        }
    }

    private static TMP_FontAsset LoadFont()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (asset == null) Debug.LogWarning($"Font not found at '{FontPath}'. Default TMP font will be used.");
        return asset;
    }

    private static ItemData[] CreateItems()
    {
        Directory.CreateDirectory($"{Root}/Data");
        var defs = new (string asset, string name, string description, int price, int power, int income)[]
        {
            ("electric bulb", "전구", "저렴하고 전력도 적게 쓰는 입문용", 30, 10, 1),
            ("Computer", "컴퓨터", "본격적인 수익의 시작", 150, 30, 5),
            ("Fan", "선풍기", "전력을 꽤 먹지만 수익이 좋음", 600, 60, 15),
            ("Server", "서버", "막대한 전력, 막대한 수익", 2500, 120, 50),
            ("MiningRig", "채굴기", "밤낮없이 돌아가는 수익 기계", 8000, 180, 120),
            ("FactoryEquipment", "공장 설비", "대량 생산으로 큰 수익", 30000, 320, 400),
            ("DataCenter", "데이터센터", "압도적인 전력, 압도적인 수익", 120000, 560, 1500),
        };

        var result = new ItemData[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            string path = $"{Root}/Data/{defs[i].asset}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);

                var so = new SerializedObject(item);
                so.FindProperty("displayName").stringValue = defs[i].name;
                so.FindProperty("description").stringValue = defs[i].description;
                so.FindProperty("price").intValue = defs[i].price;
                so.FindProperty("powerConsumption").intValue = defs[i].power;
                so.FindProperty("baseIncome").intValue = defs[i].income;
                so.FindProperty("incomeMultiplier").floatValue = 1f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            result[i] = item;
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    private static UpgradeData[] CreateUpgrades()
    {
        var defs = new (string asset, UpgradeType type, string name, int cost, float growth, float effect, int max)[]
        {
            ("PowerLimitUpgrade", UpgradeType.PowerLimit, "전력 한도 업", 200, 1.6f, 20f, 10),
            ("IncomeMultiplierUpgrade", UpgradeType.IncomeMultiplier, "수익 배율 업", 300, 1.8f, 0.2f, 10),
            ("IncomeIntervalUpgrade", UpgradeType.IncomeInterval, "수익 주기 단축", 1000, 2f, 0.9f, 8),
            ("ClickIncomeUpgrade", UpgradeType.ClickIncome, "클릭 수익 업", 100, 1.5f, 2f, 15),
        };

        var result = new UpgradeData[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            string path = $"{Root}/Data/{defs[i].asset}.asset";
            var data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(data, path);

                var so = new SerializedObject(data);
                so.FindProperty("type").enumValueIndex = (int)defs[i].type;
                so.FindProperty("displayName").stringValue = defs[i].name;
                so.FindProperty("baseCost").intValue = defs[i].cost;
                so.FindProperty("costGrowth").floatValue = defs[i].growth;
                so.FindProperty("effectPerLevel").floatValue = defs[i].effect;
                so.FindProperty("maxLevel").intValue = defs[i].max;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
            result[i] = data;
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    private static void AssignIcons(ItemData[] items, UpgradeData[] upgrades)
    {
        var palette = new[]
        {
            new Color(1f, 0.9f, 0.4f), new Color(0.4f, 0.7f, 1f), new Color(0.6f, 0.9f, 0.9f),
            new Color(0.7f, 0.5f, 1f), new Color(1f, 0.6f, 0.3f), new Color(0.6f, 0.6f, 0.65f),
            new Color(0.3f, 0.9f, 0.6f),
        };
        for (int i = 0; i < items.Length; i++)
            AssignIcon(items[i], CreateIconSprite($"Item_{items[i].name}", palette[i % palette.Length]));

        var upgradePalette = new[]
        {
            new Color(0.95f, 0.8f, 0.2f), new Color(0.3f, 0.85f, 0.4f),
            new Color(0.3f, 0.8f, 1f), new Color(1f, 0.5f, 0.5f),
        };
        for (int i = 0; i < upgrades.Length; i++)
            AssignIcon(upgrades[i], CreateIconSprite($"Upgrade_{upgrades[i].name}", upgradePalette[i % upgradePalette.Length]));
        AssetDatabase.SaveAssets();
    }

    private static void AssignIcon(Object target, Sprite sprite)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty("icon");
        if (prop.objectReferenceValue != null) return;

        prop.objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static Sprite CreateIconSprite(string name, Color color)
    {
        Directory.CreateDirectory($"{Root}/Sprites/Icons");
        string path = $"{Root}/Sprites/Icons/{name}.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Dictionary<ReactionType, Sprite> CreateCharacterSprites()
    {
        var colors = new Dictionary<ReactionType, Color>
        {
            { ReactionType.Idle, new Color(0.85f, 0.85f, 0.9f) },
            { ReactionType.Income, new Color(1f, 0.85f, 0.25f) },
            { ReactionType.Purchase, new Color(0.3f, 0.85f, 0.4f) },
            { ReactionType.NearLimit, new Color(1f, 0.6f, 0.2f) },
            { ReactionType.Blackout, new Color(0.2f, 0.2f, 0.3f) },
            { ReactionType.Recovery, new Color(0.3f, 0.8f, 1f) },
        };

        Directory.CreateDirectory($"{Root}/Sprites");
        var result = new Dictionary<ReactionType, Sprite>();
        foreach (var pair in colors)
        {
            string path = $"{Root}/Sprites/Character_{pair.Key}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64);
                var pixels = new Color[64 * 64];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = pair.Value;
                texture.SetPixels(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);

                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            result[pair.Key] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return result;
    }

    private static ItemCardView CreateItemCardPrefab()
    {
        var go = CreateCardRoot("ItemCard", 72, typeof(ItemCardView));

        var icon = CreateImage("Icon", go.transform, Color.white);
        Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(56, 56));
        icon.raycastTarget = false;

        var name = CreateText("Name", go.transform, 22, TextAlignmentOptions.TopLeft, Color.white);
        PlaceTopLeft(name.rectTransform, new Vector2(74, -6), new Vector2(200, 28));
        var desc = CreateText("Description", go.transform, 16, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.85f));
        PlaceTopLeft(desc.rectTransform, new Vector2(280, -9), new Vector2(480, 24));
        var price = CreateText("Price", go.transform, 20, TextAlignmentOptions.TopLeft, Color.white);
        PlaceTopLeft(price.rectTransform, new Vector2(74, -38), new Vector2(150, 26));
        var power = CreateText("Power", go.transform, 20, TextAlignmentOptions.TopLeft, Color.white);
        PlaceTopLeft(power.rectTransform, new Vector2(234, -38), new Vector2(150, 26));

        var income = CreateText("Income", go.transform, 22, TextAlignmentOptions.TopRight, new Color(1f, 0.9f, 0.4f));
        PlaceAnchored(income.rectTransform, new Vector2(1, 1), new Vector2(-12, -6), new Vector2(220, 28));
        var state = CreateText("State", go.transform, 22, TextAlignmentOptions.BottomRight, Color.white);
        PlaceAnchored(state.rectTransform, new Vector2(1, 0), new Vector2(-12, 6), new Vector2(160, 28));
        var warning = CreateText("Warning", go.transform, 16, TextAlignmentOptions.Right, new Color(1f, 0.6f, 0.25f));
        PlaceAnchored(warning.rectTransform, new Vector2(1, 0.5f), new Vector2(-245, 0), new Vector2(220, 24));

        SetFields(go.GetComponent<ItemCardView>(),
            ("button", go.GetComponent<Button>()), ("background", go.GetComponent<Image>()), ("iconImage", icon),
            ("nameText", name), ("descriptionText", desc), ("priceText", price), ("powerText", power),
            ("incomeText", income), ("stateText", state), ("warningText", warning));

        return SavePrefab(go, "ItemCard").GetComponent<ItemCardView>();
    }

    private static UpgradeCardView CreateUpgradeCardPrefab()
    {
        var go = CreateCardRoot("UpgradeCard", 72, typeof(UpgradeCardView));

        var icon = CreateImage("Icon", go.transform, Color.white);
        Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(56, 56));
        icon.raycastTarget = false;

        var name = CreateText("Name", go.transform, 22, TextAlignmentOptions.TopLeft, Color.white);
        PlaceTopLeft(name.rectTransform, new Vector2(74, -6), new Vector2(380, 28));
        var effect = CreateText("Effect", go.transform, 20, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.85f));
        PlaceTopLeft(effect.rectTransform, new Vector2(74, -38), new Vector2(460, 26));
        var level = CreateText("Level", go.transform, 22, TextAlignmentOptions.TopRight, Color.white);
        PlaceAnchored(level.rectTransform, new Vector2(1, 1), new Vector2(-12, -6), new Vector2(260, 28));
        var cost = CreateText("Cost", go.transform, 22, TextAlignmentOptions.BottomRight, new Color(1f, 0.9f, 0.4f));
        PlaceAnchored(cost.rectTransform, new Vector2(1, 0), new Vector2(-12, 6), new Vector2(200, 28));

        SetFields(go.GetComponent<UpgradeCardView>(),
            ("button", go.GetComponent<Button>()), ("background", go.GetComponent<Image>()), ("iconImage", icon),
            ("nameText", name), ("levelText", level), ("effectText", effect), ("costText", cost));

        return SavePrefab(go, "UpgradeCard").GetComponent<UpgradeCardView>();
    }

    private static CoinPopupItem CreateCoinPopupPrefab()
    {
        var go = new GameObject("CoinPopup", typeof(RectTransform), typeof(CoinPopupItem));
        Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 50));

        var label = CreateText("Label", go.transform, 36, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));
        Fill(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        SetFields(go.GetComponent<CoinPopupItem>(), ("label", label));
        return SavePrefab(go, "CoinPopup").GetComponent<CoinPopupItem>();
    }

    private static GameObject CreateCardRoot(string name, float height, System.Type viewType)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), viewType);
        go.GetComponent<LayoutElement>().preferredHeight = height;

        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        return go;
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/{name}.prefab");
        Object.DestroyImmediate(go);
        return saved;
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static HudView CreateHud(Transform parent)
    {
        var go = new GameObject("Hud", typeof(RectTransform), typeof(Image), typeof(HudView));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = PanelColor;
        Fill((RectTransform)go.transform, new Vector2(0, 0.85f), Vector2.one, Vector2.zero, Vector2.zero);

        var money = CreateText("MoneyText", go.transform, 28, TextAlignmentOptions.MidlineLeft, Color.white, true);
        Fill(money.rectTransform, new Vector2(0, 0.5f), new Vector2(0.3f, 1), new Vector2(16, 0), new Vector2(0, -4));
        var income = CreateText("IncomeText", go.transform, 26, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.4f), true);
        Fill(income.rectTransform, new Vector2(0.3f, 0.833f), new Vector2(0.6f, 1), Vector2.zero, new Vector2(0, -4));
        var perSecond = CreateText("PerSecondText", go.transform, 24, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.4f), true);
        Fill(perSecond.rectTransform, new Vector2(0.3f, 0.667f), new Vector2(0.6f, 0.833f), Vector2.zero, Vector2.zero);
        var clickIncome = CreateText("ClickIncomeText", go.transform, 24, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.4f), true);
        Fill(clickIncome.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.6f, 0.667f), Vector2.zero, Vector2.zero);
        var power = CreateText("PowerText", go.transform, 24, TextAlignmentOptions.MidlineLeft, Color.white, true);
        Fill(power.rectTransform, new Vector2(0, 0), new Vector2(0.25f, 0.5f), new Vector2(16, 4), Vector2.zero);

        var gaugeBg = CreateImage("GaugeBg", go.transform, new Color(0.15f, 0.15f, 0.15f));
        Fill(gaugeBg.rectTransform, new Vector2(0.25f, 0.1f), new Vector2(0.97f, 0.4f), Vector2.zero, Vector2.zero);
        var gaugeFill = CreateImage("GaugeFill", gaugeBg.transform, Color.green);
        Fill(gaugeFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var warning = CreateImage("BlackoutWarning", go.transform, new Color(0.75f, 0.1f, 0.1f, 0.9f));
        Fill(warning.rectTransform, new Vector2(0.62f, 0.55f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
        var warningText = CreateText("Text", warning.transform, 26, TextAlignmentOptions.Center, Color.white, true);
        Fill(warningText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        warningText.text = "정전! 수익 중단";

        var view = go.GetComponent<HudView>();
        SetFields(view, ("moneyText", money), ("powerText", power), ("incomeText", income), ("perSecondText", perSecond), ("clickIncomeText", clickIncome),
            ("gaugeFill", gaugeFill), ("blackoutWarning", warning.gameObject));
        return view;
    }

    private static void CreateCharacterArea(Transform parent, Dictionary<ReactionType, Sprite> sprites,
        CoinPopupItem popupPrefab, out CharacterView characterView, out CharacterClickView characterClickView, out CoinPopupView coinPopupView)
    {
        var area = new GameObject("CharacterArea", typeof(RectTransform));
        area.transform.SetParent(parent, false);
        Fill((RectTransform)area.transform, new Vector2(0, 0.5f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);

        var character = CreateImage("Character", area.transform, Color.white);
        Place(character.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200));
        characterClickView = character.gameObject.AddComponent<CharacterClickView>();
        characterView = character.gameObject.AddComponent<CharacterView>();
        SetFields(characterView, ("characterImage", character));
        SetVisuals(characterView, sprites);

        var popups = new GameObject("CoinPopups", typeof(RectTransform));
        popups.transform.SetParent(area.transform, false);
        Place((RectTransform)popups.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(10, 10));
        coinPopupView = popups.AddComponent<CoinPopupView>();
        SetFields(coinPopupView, ("popupPrefab", popupPrefab), ("container", popups.transform));
    }

    private static void SetVisuals(CharacterView view, Dictionary<ReactionType, Sprite> sprites)
    {
        var defs = new (ReactionType type, ReactionMotion motion, float duration)[]
        {
            (ReactionType.Idle, ReactionMotion.Breathe, 0f),
            (ReactionType.Income, ReactionMotion.Bounce, 0.6f),
            (ReactionType.Purchase, ReactionMotion.Bounce, 0.6f),
            (ReactionType.NearLimit, ReactionMotion.Shake, 0f),
            (ReactionType.Blackout, ReactionMotion.Sink, 0f),
            (ReactionType.Recovery, ReactionMotion.Bounce, 1.5f),
        };

        var so = new SerializedObject(view);
        var array = so.FindProperty("visuals");
        array.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
        {
            var element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("type").enumValueIndex = (int)defs[i].type;
            element.FindPropertyRelative("sprite").objectReferenceValue = sprites[defs[i].type];
            element.FindPropertyRelative("tint").colorValue = Color.white;
            element.FindPropertyRelative("motion").enumValueIndex = (int)defs[i].motion;
            element.FindPropertyRelative("duration").floatValue = defs[i].duration;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateShop(Transform parent, ItemCardView itemCard, UpgradeCardView upgradeCard,
        out ItemShopView itemShopView, out UpgradeShopView upgradeShopView)
    {
        var shop = new GameObject("Shop", typeof(RectTransform), typeof(Image), typeof(ShopTabsView));
        shop.transform.SetParent(parent, false);
        shop.GetComponent<Image>().color = PanelColor;
        Fill((RectTransform)shop.transform, Vector2.zero, new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);

        var tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tabs.transform.SetParent(shop.transform, false);
        Place((RectTransform)tabs.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-24, 40));
        var tabsLayout = tabs.GetComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 10;
        tabsLayout.childControlWidth = tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = tabsLayout.childForceExpandHeight = true;

        var itemTab = CreateTabButton("ItemTab", "아이템", tabs.transform);
        var upgradeTab = CreateTabButton("UpgradeTab", "업그레이드", tabs.transform);

        var itemScroll = CreateScroll("ItemScroll", shop.transform, out var itemContent);
        var upgradeScroll = CreateScroll("UpgradeScroll", shop.transform, out var upgradeContent);

        itemShopView = itemScroll.AddComponent<ItemShopView>();
        SetFields(itemShopView, ("cardPrefab", itemCard), ("container", itemContent));
        upgradeShopView = upgradeScroll.AddComponent<UpgradeShopView>();
        SetFields(upgradeShopView, ("cardPrefab", upgradeCard), ("container", upgradeContent));

        SetFields(shop.GetComponent<ShopTabsView>(), ("itemTabButton", itemTab), ("upgradeTabButton", upgradeTab),
            ("itemPanel", itemScroll), ("upgradePanel", upgradeScroll));
    }

    private static GameObject CreateScroll(string name, Transform parent, out Transform content)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = Color.clear;
        Fill((RectTransform)root.transform, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -52));

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(root.transform, false);
        Fill((RectTransform)viewport.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewport.transform, false);
        Place((RectTransform)contentGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = (RectTransform)contentGo.transform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        content = contentGo.transform;
        return root;
    }

    private static Button CreateTabButton(string name, string label, Transform parent)
    {
        var image = CreateImage(name, parent, new Color(0.3f, 0.3f, 0.4f));
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = CreateText("Label", image.transform, 22, TextAlignmentOptions.Center, Color.white);
        Fill(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.text = label;
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions align,
        Color color, bool autoSize = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        if (autoSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = size;
        }
        return text;
    }

    private static void PlaceTopLeft(RectTransform rect, Vector2 pos, Vector2 size) =>
        Place(rect, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, size);

    private static void PlaceAnchored(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size) =>
        Place(rect, anchor, anchor, anchor, pos, size);

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    private static void Fill(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetFields(Object target, params (string field, Object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in fields)
            so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetValues(Object target, params (string field, object value)[] values)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in values)
        {
            var prop = so.FindProperty(field);
            if (value is int i) prop.intValue = i;
            else if (value is float f) prop.floatValue = f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
