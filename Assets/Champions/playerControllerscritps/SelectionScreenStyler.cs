using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restyles the champion select canvas at runtime (no scene wiring needed; CharacterSelection adds it).
/// Dark slate panels with gold trim, highlighted selected champion, proper champion name, an ability row
/// with icons + key badges taken from the champion's HUD loadout, and a large LOCK IN button.
/// </summary>
[DisallowMultipleComponent]
public class SelectionScreenStyler : MonoBehaviour
{
    public CharacterSelection selection;

    private RectTransform _canvasRT;
    private readonly List<(Button button, Image frame, Image fill, TMP_Text text, int index)> _champButtons = new List<(Button, Image, Image, TMP_Text, int)>();
    private TMP_Text _nameText;
    private TMP_Text _subtitle;
    private RectTransform _abilityRow;
    private readonly List<GameObject> _abilityCells = new List<GameObject>();
    private TMP_FontAsset _font;
    private bool _styled;

    static readonly AbilityKey[] Order = { AbilityKey.LeftClick, AbilityKey.RightClick, AbilityKey.One, AbilityKey.Two, AbilityKey.Ctrl };

    private void Start()
    {
        if (selection == null) selection = GetComponent<CharacterSelection>();
        TryStyle();
    }

    private void OnEnable()
    {
        if (selection == null) selection = GetComponent<CharacterSelection>();
        if (selection != null) selection.SelectionChanged += Refresh;
        if (_styled) Refresh();
    }

    private void OnDisable()
    {
        if (selection != null) selection.SelectionChanged -= Refresh;
    }

    // ------------------------------------------------------------------ build

    private void TryStyle()
    {
        if (_styled || selection == null) return;
        var canvasGO = FindCanvas();
        if (canvasGO == null) return;
        _canvasRT = canvasGO.GetComponent<RectTransform>();
        _font = selection.label != null ? selection.label.font : null;

        var background = Find("Background");
        var listPanel = Find("CharacterListPanel");
        var previewPanel = Find("PreviewPanel");
        var previewImage = Find("PreviewImage");
        var infoPanel = Find("InfoPanel");
        var startButton = Find("StartButton");

        if (background) SetImage(background, UIStyle.White, new Color(0.03f, 0.05f, 0.07f, 1f), sliced: false);
        if (previewPanel) SetImage(previewPanel, UIStyle.White, new Color(0.04f, 0.07f, 0.10f, 1f), sliced: false);

        if (listPanel)
        {
            SetImage(listPanel, UIStyle.White, new Color(0.02f, 0.035f, 0.05f, 1f), sliced: false);
            // thin gold divider on the right edge
            var divider = NewRect("GoldDivider", listPanel);
            divider.anchorMin = new Vector2(1f, 0f); divider.anchorMax = new Vector2(1f, 1f);
            divider.pivot = new Vector2(1f, 0.5f); divider.sizeDelta = new Vector2(2f, 0f);
            AddImage(divider, UIStyle.White, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.6f), false);

            var header = NewRect("ListHeader", listPanel);
            header.anchorMin = new Vector2(0f, 1f); header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f); header.sizeDelta = new Vector2(0f, 90f);
            var h = AddText(header, "CHAMPIONS", 30, UIStyle.Gold, FontStyles.Bold, TextAlignmentOptions.Center);
            h.characterSpacing = 8f;

            var container = listPanel.Find("ButtonContainer") as RectTransform;
            if (container != null)
            {
                container.offsetMax = new Vector2(container.offsetMax.x, -95f); // leave room for header
                var layout = container.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.childAlignment = TextAnchor.UpperCenter;
                    layout.spacing = 12f;
                    layout.padding = new RectOffset(30, 30, 10, 10);
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = true;
                    layout.childControlHeight = false;
                    layout.childForceExpandHeight = false;
                }
                StyleChampionButtons(container);
            }
        }

        if (previewImage)
        {
            var frame = NewRect("PreviewFrame", previewImage.parent);
            frame.SetSiblingIndex(previewImage.GetSiblingIndex());
            frame.anchorMin = previewImage.anchorMin; frame.anchorMax = previewImage.anchorMax;
            frame.offsetMin = previewImage.offsetMin - new Vector2(3f, 3f);
            frame.offsetMax = previewImage.offsetMax + new Vector2(3f, 3f);
            AddImage(frame, UIStyle.Rounded, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.75f), true);
        }

        if (infoPanel)
        {
            SetImage(infoPanel, UIStyle.Rounded, UIStyle.Slate, sliced: true);
            // Hide the old placeholder widgets (empty square, unused description labels)
            foreach (Transform child in infoPanel)
            {
                if (child.name == "CharacterNameLabel") continue;
                child.gameObject.SetActive(false);
            }
            if (selection.label != null) selection.label.gameObject.SetActive(false);

            var nameRT = NewRect("ChampionName", infoPanel);
            nameRT.anchorMin = new Vector2(0f, 1f); nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.pivot = new Vector2(0.5f, 1f); nameRT.sizeDelta = new Vector2(-60f, 80f);
            nameRT.anchoredPosition = new Vector2(0f, -18f);
            _nameText = AddText(nameRT, "", 58, UIStyle.GoldBright, FontStyles.Bold, TextAlignmentOptions.Left);
            _nameText.characterSpacing = 6f;

            var subRT = NewRect("Subtitle", infoPanel);
            subRT.anchorMin = new Vector2(0f, 1f); subRT.anchorMax = new Vector2(1f, 1f);
            subRT.pivot = new Vector2(0.5f, 1f); subRT.sizeDelta = new Vector2(-60f, 30f);
            subRT.anchoredPosition = new Vector2(0f, -96f);
            _subtitle = AddText(subRT, "ABILITIES", 20, new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.6f), FontStyles.Bold, TextAlignmentOptions.Left);
            _subtitle.characterSpacing = 6f;

            _abilityRow = NewRect("AbilityRow", infoPanel);
            _abilityRow.anchorMin = new Vector2(0f, 0f); _abilityRow.anchorMax = new Vector2(1f, 0f);
            _abilityRow.pivot = new Vector2(0f, 0f);
            _abilityRow.sizeDelta = new Vector2(-60f, 165f);
            _abilityRow.anchoredPosition = new Vector2(30f, 22f);
            var hl = _abilityRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 26f; hl.childAlignment = TextAnchor.LowerLeft;
            hl.childControlWidth = false; hl.childControlHeight = false;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        }

        if (startButton) StyleStartButton(startButton);

        _styled = true;
        Refresh();
    }

    private void StyleChampionButtons(RectTransform container)
    {
        _champButtons.Clear();
        foreach (Transform t in container)
        {
            var button = t.GetComponent<Button>();
            if (button == null) continue;
            var rt = (RectTransform)t;
            var le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76f; le.minHeight = 76f;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 76f);

            // Button graphic becomes the gold frame; an inner fill sits on top of it
            var frame = t.GetComponent<Image>();
            frame.sprite = UIStyle.Rounded; frame.type = Image.Type.Sliced; frame.color = UIStyle.GoldDim;
            var fillRT = NewRect("Fill", rt);
            fillRT.SetAsFirstSibling();
            Stretch(fillRT, 2f);
            var fill = AddImage(fillRT, UIStyle.Rounded, UIStyle.SlateLight, true);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1.5f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = frame;

            var text = t.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = text.text.ToUpperInvariant();
                text.fontSize = 28f;
                text.fontStyle = FontStyles.Bold;
                text.characterSpacing = 4f;
                text.color = UIStyle.Cream;
                text.transform.SetAsLastSibling();
            }

            _champButtons.Add((button, frame, fill, text, IndexForButton(t.name)));
        }
    }

    private void StyleStartButton(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.36f, 0.012f);
        rt.anchorMax = new Vector2(0.64f, 0.075f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var frame = rt.GetComponent<Image>();
        frame.sprite = UIStyle.Rounded; frame.type = Image.Type.Sliced; frame.color = UIStyle.GoldBright;
        var fillRT = NewRect("Fill", rt);
        fillRT.SetAsFirstSibling();
        Stretch(fillRT, 3f);
        AddImage(fillRT, UIStyle.Rounded, new Color(0.05f, 0.30f, 0.33f, 1f), true);

        var button = rt.GetComponent<Button>();
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.colorMultiplier = 1.5f;
            button.colors = colors;
        }

        var text = rt.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "LOCK IN";
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 10f;
            text.color = UIStyle.Cream;
            text.enableAutoSizing = false;
            text.transform.SetAsLastSibling();
        }
    }

    // ------------------------------------------------------------------ refresh

    private void Refresh()
    {
        if (!_styled || selection == null) return;
        int sel = selection.selectedIndex;

        foreach (var b in _champButtons)
        {
            bool on = b.index == sel;
            b.frame.color = on ? UIStyle.GoldBright : UIStyle.GoldDim;
            b.fill.color = on ? new Color(0.10f, 0.20f, 0.24f, 1f) : UIStyle.SlateLight;
            if (b.text != null) b.text.color = on ? UIStyle.GoldBright : UIStyle.Cream;
        }

        var e = sel >= 0 && sel < selection.characters.Count ? selection.characters[sel] : null;
        if (e == null) return;

        string champName = !string.IsNullOrEmpty(e.id) ? e.id : (e.gameplayPrefab != null ? e.gameplayPrefab.name : "");
        if (_nameText != null) _nameText.text = champName.ToUpperInvariant();
        BuildAbilityRow(e);
    }

    private void BuildAbilityRow(CharacterSelection.CharacterEntry e)
    {
        if (_abilityRow == null) return;
        foreach (var go in _abilityCells) Destroy(go);
        _abilityCells.Clear();

        var icons = new Dictionary<AbilityKey, Sprite>();
        var present = new HashSet<AbilityKey> { AbilityKey.LeftClick, AbilityKey.RightClick, AbilityKey.One, AbilityKey.Two };
        bool hasDash = false;
        var carrier = e.gameplayPrefab != null ? e.gameplayPrefab.GetComponentInChildren<CooldownUIManager>(true) : null;
        if (carrier != null)
        {
            foreach (var a in carrier.defaultAbilities)
            {
                if (a == null) continue;
                if (a.present) { present.Add(a.key); icons[a.key] = a.icon; } else present.Remove(a.key);
            }
            if (carrier.defaultDash != null && carrier.defaultDash.present) { hasDash = true; icons[AbilityKey.Ctrl] = carrier.defaultDash.icon; }
        }
        var control = e.gameplayPrefab != null ? e.gameplayPrefab.GetComponentInChildren<CharacterControl>(true) : null;
        if (control != null && control.canDash) hasDash = true;
        if (hasDash) present.Add(AbilityKey.Ctrl);

        foreach (var key in Order)
        {
            if (!present.Contains(key)) continue;
            icons.TryGetValue(key, out var icon);
            _abilityCells.Add(CreateAbilityCell(key, icon, AbilityName(e, key)));
        }
    }

    private static string AbilityName(CharacterSelection.CharacterEntry e, AbilityKey key)
    {
        string n;
        switch (key)
        {
            case AbilityKey.LeftClick: n = e.lmbName; break;
            case AbilityKey.RightClick: n = e.rmbName; break;
            case AbilityKey.One: n = e.oneName; break;
            case AbilityKey.Two: n = e.twoName; break;
            default: return "Dash";
        }
        // Ignore the CharacterEntry placeholder defaults
        if (n == "Left Click" || n == "Right Click" || n == "1" || n == "2") return null;
        return n;
    }

    private GameObject CreateAbilityCell(AbilityKey key, Sprite icon, string abilityName)
    {
        const float size = 112f;
        var cell = NewRect($"Ability_{key}", _abilityRow);
        cell.sizeDelta = new Vector2(size + 24f, size + 46f);

        var slot = NewRect("Slot", cell);
        slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 1f);
        slot.pivot = new Vector2(0.5f, 1f);
        slot.sizeDelta = new Vector2(size, size);
        AddImage(slot, UIStyle.Rounded, UIStyle.Gold, true);
        var back = NewRect("Back", slot); Stretch(back, 3f);
        AddImage(back, UIStyle.Rounded, UIStyle.Ink, true);
        var iconRT = NewRect("Icon", slot); Stretch(iconRT, 6f);
        var img = AddImage(iconRT, icon != null ? icon : UIStyle.White, icon != null ? Color.white : new Color(0.16f, 0.2f, 0.24f, 1f), false);
        img.preserveAspect = true;

        // key badge across the bottom edge of the slot
        string label = UIStyle.KeyLabel(key);
        var badge = NewRect("KeyBadge", slot);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0f);
        badge.pivot = new Vector2(0.5f, 0.5f);
        badge.sizeDelta = new Vector2(Mathf.Max(40f, label.Length * 16f + 16f), 32f);
        AddImage(badge, UIStyle.Rounded, UIStyle.Gold, true);
        var badgeIn = NewRect("In", badge); Stretch(badgeIn, 2f);
        AddImage(badgeIn, UIStyle.Rounded, UIStyle.Ink, true);
        var keyText = AddText(NewRect("Key", badge), label, 20, UIStyle.Cream, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch((RectTransform)keyText.transform, 0f);

        if (!string.IsNullOrWhiteSpace(abilityName))
        {
            var nameRT = NewRect("Name", cell);
            nameRT.anchorMin = new Vector2(0f, 0f); nameRT.anchorMax = new Vector2(1f, 0f);
            nameRT.pivot = new Vector2(0.5f, 0f); nameRT.sizeDelta = new Vector2(30f, 26f);
            var n = AddText(nameRT, abilityName, 18, new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.8f), FontStyles.Normal, TextAlignmentOptions.Center);
            n.textWrappingMode = TextWrappingModes.NoWrap;
            n.overflowMode = TextOverflowModes.Ellipsis;
        }
        return cell.gameObject;
    }

    // ------------------------------------------------------------------ helpers

    private int IndexForButton(string buttonName)
    {
        string n = buttonName.Replace("Button", "");
        if (n.Equals("Cait", System.StringComparison.OrdinalIgnoreCase)) n = "Caitlyn";
        for (int i = 0; i < selection.characters.Count; i++)
        {
            var c = selection.characters[i];
            if (c == null) continue;
            if (string.Equals(c.id, n, System.StringComparison.OrdinalIgnoreCase)) return i;
            if (c.gameplayPrefab != null && string.Equals(c.gameplayPrefab.name, n, System.StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    private GameObject FindCanvas()
    {
        if (selection.label != null)
        {
            var c = selection.label.GetComponentInParent<Canvas>(true);
            if (c != null) return c.rootCanvas.gameObject;
        }
        var go = GameObject.Find("SelectionCanvas");
        return go;
    }

    private RectTransform Find(string name)
    {
        foreach (var t in _canvasRT.GetComponentsInChildren<RectTransform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool sliced)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void SetImage(RectTransform rt, Sprite sprite, Color color, bool sliced)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) return;
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
    }

    private TMP_Text AddText(RectTransform rt, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }
}
