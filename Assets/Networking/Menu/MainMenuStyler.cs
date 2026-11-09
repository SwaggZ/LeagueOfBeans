using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Restyles the main menu canvas at runtime (installed by MainMenuController), keeping all existing
/// wiring (buttons, input fields used by ServerBrowserController). Adds a champion splash showcase,
/// the League of Beans logo, a styled server browser (host / join cards) and a working Settings page.
/// Art comes from Resources/MenuArt.asset.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuStyler : MonoBehaviour
{
    const float Margin = 120f;
    const float SlideSeconds = 7f;
    const float FadeSeconds = 1.2f;

    private RectTransform _canvas;
    private TMP_FontAsset _font;
    private MenuArt _art;

    // showcase
    private RectTransform _showcase;
    private RawImage _slideA, _slideB;
    private TMP_Text _featuredName;
    private int _slideIndex;
    private float _slideStart;
    private float _fadeStart = -1f;

    // server browser
    private RectTransform _serverList;
    private GameObject _emptyListText;
    private readonly HashSet<int> _styledListItems = new HashSet<int>();

    // settings
    private TMP_Text _volumeValue, _sensValue, _fullscreenValue, _qualityValue;

    private bool _styled;

    private void Start() => Style();

    // ------------------------------------------------------------------ build

    private void Style()
    {
        if (_styled) return;
        _canvas = GetComponent<RectTransform>();
        _art = MenuArt.Load();
        var anyText = GetComponentInChildren<TMP_Text>(true);
        _font = anyText != null ? anyText.font : null;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        BuildBackdrop();
        StyleMainPanel(Child("MainPanel"));
        StyleServerBrowser(Child("ServerBrowserPanel"));
        StyleSettings(Child("SettingsPanel"));
        _styled = true;
    }

    private void BuildBackdrop()
    {
        var backdrop = NewRect("MenuBackdrop", _canvas);
        backdrop.SetAsFirstSibling();
        Stretch(backdrop);
        AddImage(backdrop, UIStyle.White, new Color(0.02f, 0.03f, 0.05f, 1f), false);

        // champion showcase on the right
        _showcase = NewRect("Showcase", backdrop);
        _showcase.anchorMin = new Vector2(0.30f, 0f);
        _showcase.anchorMax = new Vector2(1f, 1f);
        _showcase.offsetMin = _showcase.offsetMax = Vector2.zero;
        _showcase.gameObject.AddComponent<RectMask2D>();
        _slideA = NewRaw("SlideA", _showcase);
        _slideB = NewRaw("SlideB", _showcase);
        _slideB.color = new Color(1f, 1f, 1f, 0f);

        // fades: left edge into the base colour, plus a bottom fade and an overall tint
        var left = NewRect("FadeLeft", _showcase);
        left.anchorMin = new Vector2(0f, 0f); left.anchorMax = new Vector2(0.55f, 1f);
        left.offsetMin = left.offsetMax = Vector2.zero;
        var lr = left.gameObject.AddComponent<RawImage>();
        lr.texture = Gradient(true); lr.color = new Color(0.02f, 0.03f, 0.05f, 1f); lr.raycastTarget = false;

        var bottom = NewRect("FadeBottom", _showcase);
        bottom.anchorMin = new Vector2(0f, 0f); bottom.anchorMax = new Vector2(1f, 0.45f);
        bottom.offsetMin = bottom.offsetMax = Vector2.zero;
        var br = bottom.gameObject.AddComponent<RawImage>();
        br.texture = Gradient(false); br.color = new Color(0.02f, 0.03f, 0.05f, 0.95f); br.raycastTarget = false;

        var tint = NewRect("Tint", _showcase); Stretch(tint);
        AddImage(tint, UIStyle.White, new Color(0.02f, 0.05f, 0.08f, 0.25f), false);

        // featured champion caption, bottom-right
        var cap = NewRect("FeaturedCaption", backdrop);
        cap.anchorMin = cap.anchorMax = cap.pivot = new Vector2(1f, 0f);
        cap.sizeDelta = new Vector2(700f, 130f);
        cap.anchoredPosition = new Vector2(-80f, 60f);
        var small = AddText(NewRect("Label", cap), "FEATURED CHAMPION", 20, new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.6f), FontStyles.Bold, TextAlignmentOptions.BottomRight);
        Place((RectTransform)small.transform, new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(700f, 30f));
        small.characterSpacing = 6f;
        _featuredName = AddText(NewRect("Name", cap), "", 72, UIStyle.GoldBright, FontStyles.Bold, TextAlignmentOptions.BottomRight);
        Place((RectTransform)_featuredName.transform, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(700f, 90f));
        _featuredName.characterSpacing = 8f;

        // gold hairline across the bottom
        var line = NewRect("GoldLine", backdrop);
        line.anchorMin = new Vector2(0f, 0f); line.anchorMax = new Vector2(1f, 0f);
        line.pivot = new Vector2(0.5f, 0f); line.sizeDelta = new Vector2(0f, 2f); line.anchoredPosition = new Vector2(0f, 36f);
        AddImage(line, UIStyle.White, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.35f), false);

        _slideIndex = 0;
        ShowSlide(_slideA, 0);
        _slideStart = Time.unscaledTime;
    }

    private void StyleMainPanel(RectTransform panel)
    {
        if (panel == null) return;
        PrepPanel(panel, 0f);

        // logo
        var logoText = panel.Find("Logo");
        if (_art != null && _art.wordmark != null)
        {
            if (logoText) logoText.gameObject.SetActive(false);
            if (_art.emblem != null)
            {
                var emblem = NewRect("Emblem", panel);
                PlaceTopLeft(emblem, new Vector2(Margin + 220f, -70f), new Vector2(150f, 150f));
                var e = AddImage(emblem, _art.emblem, UIStyle.Gold, false); e.preserveAspect = true;
            }
            var word = NewRect("Wordmark", panel);
            PlaceTopLeft(word, new Vector2(Margin, -235f), new Vector2(600f, 223f));
            var w = AddImage(word, _art.wordmark, UIStyle.GoldBright, false); w.preserveAspect = true;
        }
        else if (logoText)
        {
            var t = logoText.GetComponent<TMP_Text>();
            t.text = "LEAGUE OF BEANS"; t.color = UIStyle.GoldBright; t.fontSize = 72; t.alignment = TextAlignmentOptions.Left;
            PlaceTopLeft((RectTransform)logoText, new Vector2(Margin, -200f), new Vector2(800f, 100f));
        }

        var divider = NewRect("Divider", panel);
        PlaceTopLeft(divider, new Vector2(Margin, -500f), new Vector2(420f, 2f));
        AddImage(divider, UIStyle.White, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.5f), false);

        var play = panel.Find("PlayButton") as RectTransform;
        var settings = panel.Find("SettingsButton") as RectTransform;
        float y = -540f;
        if (play) { PlaceTopLeft(play, new Vector2(Margin, y), new Vector2(420f, 88f)); StyleButton(play, "PLAY", primary: true); y -= 108f; }
        if (settings) { PlaceTopLeft(settings, new Vector2(Margin, y), new Vector2(420f, 64f)); StyleButton(settings, "SETTINGS", primary: false); y -= 80f; }

        // Quit (new)
        var quit = MakeButton("QuitButton", panel, "QUIT", primary: false, QuitGame);
        PlaceTopLeft(quit, new Vector2(Margin, y), new Vector2(420f, 64f));

        var version = AddText(NewRect("Version", panel), $"v{Application.version}  ·  Unity {Application.unityVersion}", 18,
                              new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.4f), FontStyles.Normal, TextAlignmentOptions.BottomLeft);
        var vrt = (RectTransform)version.transform;
        vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0f, 0f);
        vrt.sizeDelta = new Vector2(600f, 30f); vrt.anchoredPosition = new Vector2(Margin, 50f);
    }

    private void StyleServerBrowser(RectTransform panel)
    {
        if (panel == null) return;
        PrepPanel(panel, 0.55f);

        var header = panel.Find("ServerHeader");
        if (header) StyleHeader(header.GetComponent<TMP_Text>(), "PLAY");
        AddSubtitle(panel, "Host your own game or join one on your network");

        // Host card
        var host = Card(panel, "HostCard", new Vector2(Margin, -230f), new Vector2(680f, 640f), "HOST A GAME");
        float y = -110f;
        y = Field(host, panel.Find("HostSection/ServerName"), "SERVER NAME", 40f, y, 600f);
        y = Field(host, panel.Find("HostSection/HostPort"), "PORT", 40f, y, 600f);
        y = Field(host, panel.Find("HostSection/HostPassword"), "PASSWORD (OPTIONAL)", 40f, y, 600f);
        var hostBtn = panel.Find("HostSection/HostButton") as RectTransform;
        if (hostBtn)
        {
            hostBtn.SetParent(host, false);
            PlaceBottomLeft(hostBtn, new Vector2(40f, 40f), new Vector2(600f, 80f));
            StyleButton(hostBtn, "CREATE SERVER", primary: true);
        }
        var hostSection = panel.Find("HostSection"); if (hostSection) hostSection.gameObject.SetActive(false);

        // Join card
        var join = Card(panel, "JoinCard", new Vector2(Margin + 720f, -230f), new Vector2(960f, 640f), "JOIN A GAME");
        var lanLabel = AddLabel(join, "LOCAL NETWORK", new Vector2(40f, -100f));
        var listBox = NewRect("ListBox", join);
        PlaceTopLeft(listBox, new Vector2(40f, -134f), new Vector2(880f, 190f));
        AddImage(listBox, UIStyle.Rounded, new Color(0f, 0.01f, 0.02f, 0.85f), true);
        listBox.gameObject.AddComponent<RectMask2D>();
        _serverList = panel.Find("ServerList") as RectTransform;
        if (_serverList)
        {
            _serverList.SetParent(listBox, false);
            _serverList.anchorMin = new Vector2(0f, 1f); _serverList.anchorMax = new Vector2(1f, 1f);
            _serverList.pivot = new Vector2(0.5f, 1f);
            _serverList.offsetMin = new Vector2(10f, _serverList.offsetMin.y); _serverList.offsetMax = new Vector2(-10f, -10f);
            var vlg = _serverList.GetComponent<VerticalLayoutGroup>();
            if (vlg) { vlg.spacing = 8f; vlg.childControlWidth = true; vlg.childForceExpandWidth = true; }
        }
        var empty = AddText(NewRect("EmptyState", listBox), "Searching for games on your network…", 22,
                            new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.45f), FontStyles.Italic, TextAlignmentOptions.Center);
        Stretch((RectTransform)empty.transform);
        _emptyListText = empty.gameObject;

        AddLabel(join, "DIRECT CONNECT", new Vector2(40f, -350f)).fontSize = 20;
        Field(join, panel.Find("JoinSection/DirectAddress"), "ADDRESS", 40f, -390f, 400f);
        Field(join, panel.Find("JoinSection/DirectPort"), "PORT", 460f, -390f, 160f);
        Field(join, panel.Find("JoinSection/JoinPassword"), "PASSWORD", 640f, -390f, 280f);
        var joinBtn = panel.Find("JoinSection/JoinButton") as RectTransform;
        if (joinBtn)
        {
            joinBtn.SetParent(join, false);
            PlaceBottomLeft(joinBtn, new Vector2(520f, 40f), new Vector2(400f, 80f));
            StyleButton(joinBtn, "JOIN", primary: true);
        }
        var joinSection = panel.Find("JoinSection"); if (joinSection) joinSection.gameObject.SetActive(false);
        lanLabel.fontSize = 20;

        StyleBack(panel.Find("BackButton") as RectTransform);
    }

    private void StyleSettings(RectTransform panel)
    {
        if (panel == null) return;
        PrepPanel(panel, 0.55f);
        var header = panel.Find("SettingsHeader");
        if (header) StyleHeader(header.GetComponent<TMP_Text>(), "SETTINGS");
        AddSubtitle(panel, "Saved automatically");

        var card = Card(panel, "SettingsCard", new Vector2(Margin, -230f), new Vector2(900f, 480f), "GAME");
        float y = -120f;

        _volumeValue = SettingRow(card, "MASTER VOLUME", ref y);
        AddSlider(card, new Vector2(330f, y + 85f), 0f, 1f, GameSettings.MasterVolume, v =>
        {
            GameSettings.MasterVolume = v;
            _volumeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        });
        _volumeValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";

        _sensValue = SettingRow(card, "MOUSE SENSITIVITY", ref y);
        AddSlider(card, new Vector2(330f, y + 85f), 0.1f, 3f, GameSettings.MouseSensitivity, v =>
        {
            GameSettings.MouseSensitivity = v;
            _sensValue.text = GameSettings.MouseSensitivity.ToString("0.00") + "x";
        });
        _sensValue.text = GameSettings.MouseSensitivity.ToString("0.00") + "x";

        _fullscreenValue = SettingRow(card, "FULLSCREEN", ref y);
        var fsButton = MakeButton("FullscreenToggle", card, "", primary: false, () =>
        {
            GameSettings.Fullscreen = !GameSettings.Fullscreen;
            _fullscreenValue.text = GameSettings.Fullscreen ? "ON" : "OFF";
        });
        PlaceTopLeft(fsButton, new Vector2(330f, y + 80f), new Vector2(160f, 50f));
        fsButton.GetComponentInChildren<TMP_Text>().text = "TOGGLE";
        fsButton.GetComponentInChildren<TMP_Text>().fontSize = 20;
        _fullscreenValue.text = GameSettings.Fullscreen ? "ON" : "OFF";

        _qualityValue = SettingRow(card, "GRAPHICS QUALITY", ref y);
        var prev = MakeButton("QualityPrev", card, "<", primary: false, () => StepQuality(-1));
        PlaceTopLeft(prev, new Vector2(330f, y + 80f), new Vector2(60f, 50f));
        var next = MakeButton("QualityNext", card, ">", primary: false, () => StepQuality(1));
        PlaceTopLeft(next, new Vector2(400f, y + 80f), new Vector2(60f, 50f));
        _qualityValue.text = QualitySettings.names.Length > 0 ? QualitySettings.names[GameSettings.Quality].ToUpperInvariant() : "-";

        StyleBack(panel.Find("BackButton") as RectTransform);
    }

    private void StepQuality(int dir)
    {
        int count = QualitySettings.names.Length;
        if (count == 0) return;
        GameSettings.Quality = (GameSettings.Quality + dir + count) % count;
        _qualityValue.text = QualitySettings.names[GameSettings.Quality].ToUpperInvariant();
    }

    private void OnDisable() => GameSettings.Save();

    private static void QuitGame()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ update

    private void Update()
    {
        if (!_styled) return;
        UpdateShowcase();

        if (_serverList != null)
        {
            int visible = 0;
            foreach (Transform item in _serverList)
            {
                if (!item.gameObject.activeSelf) continue;
                visible++;
                if (_styledListItems.Add(item.GetInstanceID())) StyleListItem((RectTransform)item);
            }
            if (_emptyListText != null) _emptyListText.SetActive(visible == 0);
        }
    }

    private void UpdateShowcase()
    {
        if (_art == null || _art.featured == null || _art.featured.Count == 0) return;
        float now = Time.unscaledTime;
        float t = now - _slideStart;

        if (_fadeStart < 0f && t > SlideSeconds && _art.featured.Count > 1)
        {
            _fadeStart = now;
            ShowSlide(_slideB, (_slideIndex + 1) % _art.featured.Count);
        }

        ApplyKenBurns(_slideA, t);
        if (_fadeStart >= 0f)
        {
            float f = Mathf.Clamp01((now - _fadeStart) / FadeSeconds);
            _slideB.color = new Color(1f, 1f, 1f, f);
            ApplyKenBurns(_slideB, now - _fadeStart);
            if (f >= 1f)
            {
                // B becomes the new A
                _slideIndex = (_slideIndex + 1) % _art.featured.Count;
                ShowSlide(_slideA, _slideIndex);
                _slideB.color = new Color(1f, 1f, 1f, 0f);
                _slideStart = _fadeStart;
                _fadeStart = -1f;
            }
        }
    }

    private void ShowSlide(RawImage target, int index)
    {
        if (_art == null || _art.featured == null || _art.featured.Count == 0) return;
        var f = _art.featured[index];
        target.texture = f.splash != null ? f.splash.texture : null;
        target.color = target == _slideA ? Color.white : target.color;
        if (target == _slideA || _fadeStart >= 0f)
            _featuredName.text = (f.championName ?? "").ToUpperInvariant();
    }

    // Cover-crop the square splash to the showcase aspect, with a slow zoom.
    private void ApplyKenBurns(RawImage img, float t)
    {
        if (img.texture == null || _showcase == null) return;
        float aspect = _showcase.rect.width / Mathf.Max(1f, _showcase.rect.height);
        float texAspect = (float)img.texture.width / img.texture.height;
        float w = 1f, h = 1f;
        if (aspect > texAspect) h = texAspect / aspect; else w = aspect / texAspect;
        float zoom = Mathf.Lerp(1f, 0.9f, Mathf.Clamp01(t / (SlideSeconds + FadeSeconds)));
        w *= zoom; h *= zoom;
        float x = (1f - w) * 0.5f;
        float y = (1f - h) * 0.72f; // bias toward the top (faces)
        img.uvRect = new Rect(x, y, w, h);
    }

    // ------------------------------------------------------------------ widgets

    private RectTransform Card(RectTransform parent, string name, Vector2 topLeft, Vector2 size, string title)
    {
        var edge = NewRect(name, parent);
        PlaceTopLeft(edge, topLeft, size);
        AddImage(edge, UIStyle.Rounded, new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.55f), true);
        var fill = NewRect("Fill", edge); Stretch(fill, 2f);
        AddImage(fill, UIStyle.Rounded, new Color(0.03f, 0.05f, 0.07f, 0.94f), true);
        var t = AddText(NewRect("Title", edge), title, 30, UIStyle.Gold, FontStyles.Bold, TextAlignmentOptions.Left);
        t.characterSpacing = 6f;
        PlaceTopLeft((RectTransform)t.transform, new Vector2(40f, -30f), new Vector2(size.x - 80f, 44f));
        return edge;
    }

    private TMP_Text AddLabel(RectTransform parent, string text, Vector2 topLeft)
    {
        var t = AddText(NewRect("Label", parent), text, 18, new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.65f), FontStyles.Bold, TextAlignmentOptions.Left);
        t.characterSpacing = 4f;
        PlaceTopLeft((RectTransform)t.transform, topLeft, new Vector2(600f, 28f));
        return t;
    }

    // Moves an existing TMP_InputField into the card under a label. Returns the next row's y.
    private float Field(RectTransform card, Transform inputT, string label, float x, float y, float width)
    {
        AddLabel(card, label, new Vector2(x, y));
        var input = inputT as RectTransform;
        if (input == null) return y - 110f;
        input.SetParent(card, false);
        var le = input.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = true;
        PlaceTopLeft(input, new Vector2(x, y - 32f), new Vector2(width, 58f));
        StyleInput(input.GetComponent<TMP_InputField>());
        return y - 116f;
    }

    private void StyleInput(TMP_InputField field)
    {
        if (field == null) return;
        var frame = field.GetComponent<Image>();
        if (frame) { frame.sprite = UIStyle.Rounded; frame.type = Image.Type.Sliced; frame.color = UIStyle.GoldDim; }
        var fill = NewRect("Fill", field.transform); fill.SetAsFirstSibling(); Stretch(fill, 2f);
        AddImage(fill, UIStyle.Rounded, new Color(0.01f, 0.02f, 0.03f, 1f), true);

        var colors = field.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.5f, 1.4f, 1.2f, 1f);
        colors.selectedColor = new Color(2.2f, 2f, 1.5f, 1f);
        colors.colorMultiplier = 2f;
        field.colors = colors;
        field.caretColor = UIStyle.GoldBright;
        field.customCaretColor = true;
        field.selectionColor = new Color(UIStyle.Gold.r, UIStyle.Gold.g, UIStyle.Gold.b, 0.35f);

        if (field.textViewport != null)
        {
            field.textViewport.anchorMin = Vector2.zero; field.textViewport.anchorMax = Vector2.one;
            field.textViewport.offsetMin = new Vector2(18f, 8f); field.textViewport.offsetMax = new Vector2(-18f, -8f);
            field.textViewport.SetAsLastSibling();
        }
        if (field.textComponent != null)
        {
            field.textComponent.fontSize = 24; field.textComponent.color = UIStyle.Cream;
            field.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (field.placeholder is TMP_Text ph)
        {
            ph.fontSize = 24; ph.color = new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.3f);
            ph.fontStyle = FontStyles.Italic; ph.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private void StyleButton(RectTransform rt, string label, bool primary)
    {
        var le = rt.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = true;
        var frame = rt.GetComponent<Image>();
        frame.sprite = UIStyle.Rounded; frame.type = Image.Type.Sliced;
        frame.color = primary ? UIStyle.GoldBright : UIStyle.GoldDim;
        var fill = NewRect("Fill", rt); fill.SetAsFirstSibling(); Stretch(fill, primary ? 3f : 2f);
        AddImage(fill, UIStyle.Rounded, primary ? new Color(0.05f, 0.30f, 0.33f, 1f) : UIStyle.SlateLight, true);

        var button = rt.GetComponent<Button>();
        if (button)
        {
            var c = button.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.3f, 1.25f, 1.1f, 1f);
            c.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            c.selectedColor = Color.white;
            c.colorMultiplier = 1.6f;
            c.fadeDuration = 0.08f;
            button.colors = c;
            button.targetGraphic = frame;
        }

        var text = rt.GetComponentInChildren<TMP_Text>(true);
        if (text == null) text = AddText(NewRect("Text", rt), "", 24, UIStyle.Cream, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch((RectTransform)text.transform);
        text.transform.SetAsLastSibling();
        text.text = label;
        text.fontSize = primary ? 34 : 24;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = primary ? 10f : 6f;
        text.color = UIStyle.Cream;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
    }

    private RectTransform MakeButton(string name, RectTransform parent, string label, bool primary, UnityEngine.Events.UnityAction onClick)
    {
        var rt = NewRect(name, parent);
        rt.gameObject.AddComponent<Image>();
        var b = rt.gameObject.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        StyleButton(rt, label, primary);
        return rt;
    }

    private void StyleBack(RectTransform back)
    {
        if (back == null) return;
        back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 0f);
        back.sizeDelta = new Vector2(240f, 64f);
        back.anchoredPosition = new Vector2(Margin, 70f);
        StyleButton(back, "BACK", primary: false);
    }

    private void StyleHeader(TMP_Text t, string text)
    {
        if (t == null) return;
        var le = t.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = true;
        t.text = text; t.fontSize = 72; t.fontStyle = FontStyles.Bold; t.color = UIStyle.GoldBright;
        t.characterSpacing = 10f; t.alignment = TextAlignmentOptions.Left;
        PlaceTopLeft((RectTransform)t.transform, new Vector2(Margin, -70f), new Vector2(1000f, 90f));
    }

    private void AddSubtitle(RectTransform panel, string text)
    {
        var t = AddText(NewRect("Subtitle", panel), text, 24, new Color(UIStyle.Cream.r, UIStyle.Cream.g, UIStyle.Cream.b, 0.6f), FontStyles.Normal, TextAlignmentOptions.Left);
        PlaceTopLeft((RectTransform)t.transform, new Vector2(Margin + 4f, -160f), new Vector2(1000f, 36f));
    }

    private TMP_Text SettingRow(RectTransform card, string label, ref float y)
    {
        var l = AddText(NewRect(label, card), label, 22, UIStyle.Cream, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        l.characterSpacing = 3f;
        PlaceTopLeft((RectTransform)l.transform, new Vector2(40f, y), new Vector2(280f, 50f));
        var v = AddText(NewRect(label + "_Value", card), "", 22, UIStyle.GoldBright, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        PlaceTopLeft((RectTransform)v.transform, new Vector2(700f, y), new Vector2(160f, 50f));
        y -= 90f;
        return v;
    }

    private void AddSlider(RectTransform parent, Vector2 topLeft, float min, float max, float value, System.Action<float> onChange)
    {
        var root = NewRect("Slider", parent);
        PlaceTopLeft(root, topLeft, new Vector2(340f, 40f));
        var bg = NewRect("Background", root);
        bg.anchorMin = new Vector2(0f, 0.5f); bg.anchorMax = new Vector2(1f, 0.5f); bg.sizeDelta = new Vector2(0f, 10f);
        AddImage(bg, UIStyle.Rounded, new Color(0.01f, 0.02f, 0.03f, 1f), true);
        var fillArea = NewRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f); fillArea.sizeDelta = new Vector2(-20f, 10f);
        var fill = NewRect("Fill", fillArea); fill.sizeDelta = new Vector2(10f, 0f);
        AddImage(fill, UIStyle.Rounded, UIStyle.Gold, true);
        var handleArea = NewRect("Handle Slide Area", root); Stretch(handleArea); handleArea.offsetMin = new Vector2(10f, 7f); handleArea.offsetMax = new Vector2(-10f, -7f);
        var handle = NewRect("Handle", handleArea); handle.sizeDelta = new Vector2(26f, 0f);
        var handleImg = AddImage(handle, UIStyle.Rounded, UIStyle.GoldBright, true);
        handleImg.raycastTarget = true;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.value = value;
        var c = slider.colors; c.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f); c.colorMultiplier = 1.5f; slider.colors = c;
        slider.onValueChanged.AddListener(v => onChange(v));
        // make the whole bar clickable
        var hit = root.gameObject.AddComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0f);
    }

    private void StyleListItem(RectTransform item)
    {
        var le = item.GetComponent<LayoutElement>() ?? item.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 56f; le.minHeight = 56f;
        var bg = item.GetComponent<Image>();
        if (bg) { bg.sprite = UIStyle.Rounded; bg.type = Image.Type.Sliced; bg.color = UIStyle.SlateLight; }
        foreach (var t in item.GetComponentsInChildren<TMP_Text>(true))
        {
            t.color = UIStyle.Cream; t.fontSize = 22;
        }
        foreach (var b in item.GetComponentsInChildren<Button>(true))
        {
            var rt = (RectTransform)b.transform;
            var label = b.GetComponentInChildren<TMP_Text>(true);
            StyleButton(rt, label != null && !string.IsNullOrEmpty(label.text) ? label.text.ToUpperInvariant() : "JOIN", primary: false);
            var txt = b.GetComponentInChildren<TMP_Text>(true); if (txt) txt.fontSize = 20;
        }
    }

    // ------------------------------------------------------------------ helpers

    private void PrepPanel(RectTransform panel, float dim)
    {
        var layout = panel.GetComponent<LayoutGroup>(); if (layout) layout.enabled = false;
        var fitter = panel.GetComponent<ContentSizeFitter>(); if (fitter) fitter.enabled = false;
        var img = panel.GetComponent<Image>();
        if (img) { img.sprite = UIStyle.White; img.color = new Color(0.01f, 0.02f, 0.03f, dim); }
        foreach (Transform c in panel)
        {
            var le = c.GetComponent<LayoutElement>(); if (le) le.ignoreLayout = true;
        }
    }

    private RectTransform Child(string name)
    {
        foreach (Transform t in _canvas) if (t.name == name) return t as RectTransform;
        return null;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static RawImage NewRaw(string name, Transform parent)
    {
        var rt = NewRect(name, parent); Stretch(rt);
        var r = rt.gameObject.AddComponent<RawImage>(); r.raycastTarget = false;
        return r;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void PlaceTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
    }

    private static void PlaceBottomLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
    }

    private static void Place(RectTransform rt, Vector2 anchorPivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPivot;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
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

    private TMP_Text AddText(RectTransform rt, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    // Horizontal (alpha 1 -> 0, left to right) or vertical (alpha 1 at bottom -> 0 at top) fade texture.
    private static Texture2D Gradient(bool horizontal)
    {
        const int n = 128;
        var tex = new Texture2D(horizontal ? n : 1, horizontal ? 1 : n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < n; i++)
        {
            float a = 1f - i / (float)(n - 1);
            a = a * a * (3f - 2f * a); // smoothstep
            if (horizontal) tex.SetPixel(i, 0, new Color(1f, 1f, 1f, a)); else tex.SetPixel(0, i, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }
}
