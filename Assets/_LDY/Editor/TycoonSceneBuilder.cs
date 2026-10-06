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

    [MenuItem("Tools/Tycoon/Build Scene")]
    public static void Build()
    {
        var items = CreateItems();
        var buttonPrefab = CreateButtonPrefab();

        var canvas = CreateCanvas();
        var moneyText = CreateText("MoneyText", canvas.transform, new Vector2(20, -20), 36, TextAlignmentOptions.TopLeft);
        var powerText = CreateText("PowerText", canvas.transform, new Vector2(20, -70), 36, TextAlignmentOptions.TopLeft);

        var shopRoot = new GameObject("Shop", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ShopView));
        shopRoot.transform.SetParent(canvas.transform, false);
        var shopRect = (RectTransform)shopRoot.transform;
        shopRect.anchorMin = new Vector2(0.5f, 0);
        shopRect.anchorMax = new Vector2(0.5f, 1);
        shopRect.pivot = new Vector2(0.5f, 1);
        shopRect.sizeDelta = new Vector2(500, -140);
        shopRect.anchoredPosition = new Vector2(0, -130);
        var layout = shopRoot.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var hud = canvas.gameObject.AddComponent<HudView>();
        SetFields(hud, ("moneyText", moneyText), ("powerText", powerText));
        SetFields(shopRoot.GetComponent<ShopView>(), ("buttonPrefab", buttonPrefab), ("container", shopRoot.transform));

        var systems = new GameObject("GameSystems");
        var ticker = systems.AddComponent<GameTicker>();
        var bootstrapper = systems.AddComponent<GameBootstrapper>();
        SetFields(bootstrapper, ("ticker", ticker), ("hudView", hud), ("shopView", shopRoot.GetComponent<ShopView>()));
        SetArray(bootstrapper, "items", items);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tycoon scene built. Save the scene (Ctrl+S).");
    }

    private static ItemData[] CreateItems()
    {
        Directory.CreateDirectory($"{Root}/Data");
        var defs = new (string name, int price, int power, int income, float mult)[]
        {
            ("Lamp", 20, 20, 3, 1f),
            ("Fan", 50, 40, 8, 1f),
            ("Computer", 120, 60, 25, 1.5f),
        };

        var result = new ItemData[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            string path = $"{Root}/Data/{defs[i].name}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
                var so = new SerializedObject(item);
                so.FindProperty("displayName").stringValue = defs[i].name;
                so.FindProperty("price").intValue = defs[i].price;
                so.FindProperty("powerConsumption").intValue = defs[i].power;
                so.FindProperty("baseIncome").intValue = defs[i].income;
                so.FindProperty("incomeMultiplier").floatValue = defs[i].mult;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
            }
            result[i] = item;
        }
        AssetDatabase.SaveAssets();
        return result;
    }

    private static ItemButtonView CreateButtonPrefab()
    {
        var go = new GameObject("ItemButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ItemButtonView));
        go.GetComponent<LayoutElement>().preferredHeight = 80;

        var label = CreateText("Label", go.transform, Vector2.zero, 28, TextAlignmentOptions.Center);
        label.color = Color.black;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        SetFields(go.GetComponent<ItemButtonView>(), ("button", go.GetComponent<Button>()), ("label", label));

        string path = $"{Root}/ItemButton.prefab";
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved.GetComponent<ItemButtonView>();
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        return canvas;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 pos, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(600, 50);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = align;
        return text;
    }

    private static void SetFields(Object target, params (string field, Object value)[] fields)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in fields)
            so.FindProperty(field).objectReferenceValue = value;
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
