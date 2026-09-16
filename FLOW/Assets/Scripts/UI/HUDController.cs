using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Flow
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private ParkourController player;
        [SerializeField] private TouchInputManager input;
        [SerializeField] private RunSession session;
        private RectTransform safeArea;
        private RectTransform runPanel;
        private RectTransform menuPanel;
        private RectTransform pausePanel;
        private RectTransform stickPanel;
        private Text timer;
        private Text shardText;
        private Text speedText;
        private Text jumpText;
        private Text slideText;
        private Text assistText;
        private Text autoText;
        private Text toast;
        private Text menuTitle;
        private Text menuDetail;
        private Image flowMeter;
        private float toastUntil;
        private float nextRefresh;
        private int lastWidth;
        private int lastHeight;
        private static readonly Color Ink = new Color(0.055f, 0.085f, 0.11f, 0.96f);
        private static readonly Color Accent = new Color(1f, 0.46f, 0.18f);
        private static readonly Color White = new Color(0.92f, 0.95f, 0.98f);
        private Font font;
        public void Configure(ParkourController runner, TouchInputManager controls, RunSession run) { player = runner; input = controls; session = run; }
        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = new GameObject("Touch HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            if (EventSystem.current == null)
            {
                GameObject events = new GameObject("Touch Event System", typeof(EventSystem));
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            safeArea = Panel("Safe area", canvas.transform, Color.clear);
            UpdateSafeArea();
            runPanel = Panel("Run", safeArea, Color.clear);
            menuPanel = Panel("Menu", safeArea, Ink);
            pausePanel = Panel("Pause", safeArea, Ink);
            BuildRun(); BuildMenu(); BuildPause();
            runPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            menuPanel.gameObject.SetActive(false);
        }
        private RectTransform Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (color.a > 0f) { Image image = rect.gameObject.AddComponent<Image>(); image.color = color; }
            return rect;
        }
        private RectTransform At(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private Text Label(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Text label = At(text, parent, anchor, position, size).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = fontSize; label.color = White;
            label.alignment = alignment; label.raycastTarget = false;
            return label;
        }
        private Text Button(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, UnityAction action, bool accent = false)
        {
            RectTransform rect = At(text, parent, anchor, position, size);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = accent ? Accent : new Color(0.13f, 0.19f, 0.23f, 0.9f);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = background; button.onClick.AddListener(action);
            Text label = Label(rect, text, new Vector2(0.5f, 0.5f), Vector2.zero, size, 19);
            if (accent) label.color = Ink;
            return label;
        }
        private void BuildRun()
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            timer = Label(runPanel, "", new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(200f, 45f), 24);
            shardText = Label(runPanel, "DATA 0 / 3", new Vector2(0f, 1f), new Vector2(120f, -35f), new Vector2(180f, 45f), 19);
            Button(runPanel, "PAUSE", new Vector2(1f, 1f), new Vector2(-105f, -35f), new Vector2(145f, 45f), session.Pause);
            autoText = Button(runPanel, "AUTO-RUN ON", new Vector2(0f, 1f), new Vector2(135f, -95f), new Vector2(220f, 42f), () => { RunSettings.Current.autoRun = !RunSettings.Current.autoRun; RunSettings.Save(); RefreshSettings(); });
            assistText = Button(runPanel, "FLOW ASSIST ON", new Vector2(1f, 1f), new Vector2(-155f, -95f), new Vector2(250f, 42f), () => { RunSettings.Current.flowAssist = !RunSettings.Current.flowAssist; RunSettings.Save(); RefreshSettings(); });
            stickPanel = Panel("Floating steering zone", runPanel, Color.clear);
            stickPanel.anchorMin = new Vector2(0f, 0.02f); stickPanel.anchorMax = new Vector2(0.42f, 0.78f);
            stickPanel.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform disc = At("Analog stick", stickPanel, center, Vector2.zero, new Vector2(148f, 148f));
            Image discImage = disc.gameObject.AddComponent<Image>(); discImage.color = new Color(0.12f, 0.19f, 0.24f, 0.55f); discImage.raycastTarget = false;
            RectTransform knob = At("Thumb", disc, center, Vector2.zero, new Vector2(48f, 48f));
            Image knobImage = knob.gameObject.AddComponent<Image>(); knobImage.color = Accent; knobImage.raycastTarget = false;
            stickPanel.gameObject.AddComponent<VirtualStick>().Configure(input, knob);
            Label(disc, "STEER", center, new Vector2(0f, -95f), new Vector2(100f, 25f), 14);
            jumpText = Pad("JUMP", new Vector2(1f, 0f), input.Jump);
            slideText = Pad("SLIDE", new Vector2(1f, 0f), input.Slide);
            RectTransform meterBackground = At("Momentum track", runPanel, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(245f, 8f));
            meterBackground.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.17f, 0.23f, 0.8f);
            RectTransform bar = Panel("Momentum", meterBackground, Accent);
            flowMeter = bar.GetComponent<Image>(); flowMeter.raycastTarget = false;
            speedText = Label(runPanel, "", new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(350f, 30f), 17);
            toast = Label(runPanel, "", new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(780f, 75f), 22);
        }
        private Text Pad(string title, Vector2 anchor, System.Action action)
        {
            RectTransform pad = At(title, runPanel, anchor, Vector2.zero, new Vector2(122f, 92f));
            pad.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.18f, 0.23f, 0.7f);
            pad.gameObject.AddComponent<ActionPad>().Configure(action);
            return Label(pad, title, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 85f), 24);
        }
        private void BuildMenu()
        {
            Vector2 middle = new Vector2(0.5f, 0.5f);
            menuTitle = Label(menuPanel, "FLOW", middle, new Vector2(0f, 155f), new Vector2(900f, 110f), 84);
            menuDetail = Label(menuPanel, "SECTOR 7 / FIRST LINE\nTwo thumbs. No limits.", middle, new Vector2(0f, 55f), new Vector2(1000f, 85f), 23);
            Button(menuPanel, "FIND YOUR FLOW", middle, new Vector2(0f, -45f), new Vector2(330f, 65f), session.Begin, true);
            Button(menuPanel, "RUN SETTINGS", middle, new Vector2(0f, -130f), new Vector2(330f, 50f), () => { menuPanel.gameObject.SetActive(false); ShowPause(); });
            Label(menuPanel, "Source vertical slice / Offline / No ads / No accounts", middle, new Vector2(0f, -225f), new Vector2(900f, 40f), 17);
        }
        private void BuildPause()
        {
            Vector2 middle = new Vector2(0.5f, 0.5f);
            Label(pausePanel, "YOUR RUN. YOUR RULES.", middle, new Vector2(0f, 230f), new Vector2(1000f, 70f), 40);
            SettingButton("AUTO-RUN", 140f, () => RunSettings.Current.autoRun, value => RunSettings.Current.autoRun = value);
            SettingButton("FLOW ASSIST", 80f, () => RunSettings.Current.flowAssist, value => RunSettings.Current.flowAssist = value);
            SettingButton("ONE-HANDED", 20f, () => RunSettings.Current.oneHanded, value => RunSettings.Current.oneHanded = value);
            SettingButton("REDUCED MOTION", -40f, () => RunSettings.Current.reducedMotion, value => RunSettings.Current.reducedMotion = value);
            SettingButton("TIME TRIAL / NEXT RUN", -100f, () => RunSettings.Current.timeTrial, value => RunSettings.Current.timeTrial = value);
            Button(pausePanel, "CONTINUE", middle, new Vector2(0f, -185f), new Vector2(350f, 57f), () => { if (session.HasActiveRun) session.Resume(); else session.Begin(); }, true);
        }
        private void SettingButton(string title, float y, System.Func<bool> read, System.Action<bool> write)
        {
            Text label = null;
            label = Button(pausePanel, title, new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(450f, 48f), () =>
            { write(!read()); RunSettings.Save(); label.text = title + (read() ? "  ON" : "  OFF"); });
            settingsRefresh += () => label.text = title + (read() ? "  ON" : "  OFF");
        }
        private System.Action settingsRefresh;
        private void RefreshSettings()
        {
            autoText.text = "AUTO-RUN " + (RunSettings.Current.autoRun ? "ON" : "OFF");
            assistText.text = "FLOW ASSIST " + (RunSettings.Current.flowAssist ? "ON" : "OFF");
            bool oneHanded = RunSettings.Current.oneHanded;
            stickPanel.anchorMin = new Vector2(0f, oneHanded ? 0.32f : 0.02f);
            stickPanel.anchorMax = new Vector2(0.42f, 0.78f);
            Vector2 anchor = new Vector2(oneHanded ? 0f : 1f, 0f);
            RectTransform jumpPad = (RectTransform)jumpText.transform.parent;
            RectTransform slidePad = (RectTransform)slideText.transform.parent;
            jumpPad.anchorMin = jumpPad.anchorMax = slidePad.anchorMin = slidePad.anchorMax = anchor;
            jumpPad.anchoredPosition = new Vector2(oneHanded ? 90f : -220f, 120f);
            slidePad.anchoredPosition = new Vector2(oneHanded ? 230f : -80f, 120f);
        }
        public void ShowRun() { menuPanel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(false); runPanel.gameObject.SetActive(true); RefreshSettings(); }
        public void ShowPause() { runPanel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(true); settingsRefresh?.Invoke(); }
        public void ShowMenu(bool completed, float duration, float best)
        {
            runPanel.gameObject.SetActive(false); pausePanel.gameObject.SetActive(false); menuPanel.gameObject.SetActive(true);
            menuTitle.text = completed ? "LINE COMPLETE" : "FLOW";
            menuDetail.text = completed ? "SECTOR 7 / " + duration.ToString("F2") + "s / DATA " + session.Shards + "/3" + (best > 0f ? "\nPERSONAL BEST " + best.ToString("F2") + "s" : "") : "SECTOR 7 / FIRST LINE\nTwo thumbs. No limits.";
        }
        public void Notify(string text, float duration) { toast.text = text; toastUntil = Time.unscaledTime + duration; }
        private void Update()
        {
            if (lastWidth != Screen.width || lastHeight != Screen.height) UpdateSafeArea();
            if (!player.Playing || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            timer.text = session.IsTimeTrial ? session.Elapsed.ToString("F2") : "";
            shardText.text = "DATA " + session.Shards + " / 3";
            speedText.text = player.Momentum.InFlow ? "FLOW / " + (player.Momentum.Speed * 3.6f).ToString("F0") + " KM/H" : (player.Momentum.Speed * 3.6f).ToString("F0") + " KM/H";
            flowMeter.rectTransform.anchorMax = new Vector2(player.Momentum.Normalized, 1f);
            jumpText.text = player.Context.Kind == ContextType.None ? "JUMP" : player.Context.Kind.ToString().ToUpperInvariant();
            slideText.text = player.State == ParkourState.Jump || player.State == ParkourState.Fall ? "ROLL" : "SLIDE";
            if (Time.unscaledTime > toastUntil) toast.text = "";
        }
        private void UpdateSafeArea()
        {
            Rect safe = Screen.safeArea;
            safeArea.anchorMin = safe.position / new Vector2(Screen.width, Screen.height);
            safeArea.anchorMax = (safe.position + safe.size) / new Vector2(Screen.width, Screen.height);
            lastWidth = Screen.width; lastHeight = Screen.height;
        }
    }
}
