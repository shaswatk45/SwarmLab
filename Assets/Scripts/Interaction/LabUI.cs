using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SwarmLab
{
    /// <summary>
    /// A real on-screen dashboard built entirely from code (uGUI): a titled glass
    /// panel with the live metrics, clickable buttons for the five experiments, and
    /// the fault-injection controls. Nothing to wire in the editor. The keyboard
    /// shortcuts in SwarmInteractor still work; this just makes it presentable and
    /// click-driven for a demo.
    /// </summary>
    public class LabUI : MonoBehaviour
    {
        private SwarmManager _swarm;
        private DataLogger _logger;
        private ScenarioManager _scenario;
        private Font _font;
        private float _fps;

        private Text _stats, _expName, _expDesc, _use, _metric, _conn, _hint, _exportInfo;
        private Button[] _expButtons;
        private string[] _expNames;
        private Image _jamBtnImg;

        private static readonly Color Panel = new Color(0.03f, 0.06f, 0.10f, 0.86f);
        private static readonly Color Accent = new Color(0.10f, 0.95f, 0.95f);
        private static readonly Color BtnIdle = new Color(0.10f, 0.16f, 0.22f, 0.95f);
        private static readonly Color BtnActive = new Color(0.10f, 0.55f, 0.6f, 1f);
        private static readonly Color Ink = new Color(0.86f, 0.95f, 0.97f);

        private void Start()
        {
            _swarm = FindFirstObjectByType<SwarmManager>();
            _logger = FindFirstObjectByType<DataLogger>();
            _scenario = FindFirstObjectByType<ScenarioManager>();
            _font = GetFont();
            try { Build(); }
            catch (Exception e) { Debug.LogWarning("LabUI build failed: " + e.Message); }
        }

        private void Build()
        {
            // Canvas + event system (new Input System module so buttons are clickable).
            var canvasGo = new GameObject("LabCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                var module = es.AddComponent<InputSystemUIInputModule>();
                // Without this the module has no actions and clicks do nothing.
                try { module.AssignDefaultActions(); } catch { }
            }

            // Left dashboard panel.
            var panel = Panelled(canvasGo.transform, Panel);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = new Vector2(18, -18);
            prt.sizeDelta = new Vector2(440, 10);
            var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 5;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperLeft;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Label(panel.transform, "SWARMLAB", 30, Accent, FontStyle.Bold);
            Label(panel.transform, "Autonomous Robot Swarm Laboratory", 13, new Color(0.6f, 0.75f, 0.8f), FontStyle.Italic);
            Divider(panel.transform);

            _stats = Label(panel.transform, "", 15, Ink, FontStyle.Bold);
            _expName = Label(panel.transform, "", 17, Accent, FontStyle.Bold);
            _expDesc = Label(panel.transform, "", 12, new Color(0.7f, 0.8f, 0.85f), FontStyle.Italic);
            _use = Label(panel.transform, "", 12, new Color(1f, 0.78f, 0.4f), FontStyle.Bold);
            _metric = Label(panel.transform, "", 15, new Color(0.6f, 1f, 0.7f), FontStyle.Bold);
            _conn = Label(panel.transform, "", 13, Ink, FontStyle.Normal);

            Divider(panel.transform);
            Label(panel.transform, "EXPERIMENTS", 12, new Color(0.5f, 0.65f, 0.7f), FontStyle.Bold);

            _expNames = new[] { "Flocking (Boids)", "Rendezvous (Consensus)", "Area Coverage",
                                "Foraging (Pheromone)", "Formation Control", "Search & Rescue" };
            string[] labels = { "1 · Flock", "2 · Gather", "3 · Cover", "4 · Forage", "5 · Shape", "6 · Rescue" };
            Func<SwarmBehaviour>[] makers =
            {
                () => new FlockingBehaviour(), () => new RendezvousBehaviour(),
                () => new CoverageBehaviour(), () => new ForagingBehaviour(),
                () => new FormationBehaviour(), () => new SearchRescueBehaviour()
            };
            _expButtons = new Button[6];
            var rowA = Row(panel.transform);
            var rowB = Row(panel.transform);
            for (int i = 0; i < 6; i++)
            {
                var maker = makers[i];
                var parent = i < 3 ? rowA.transform : rowB.transform;
                _expButtons[i] = MakeButton(parent, labels[i], 12, () => _swarm.SetBehaviour(maker()));
            }

            Divider(panel.transform);
            Label(panel.transform, "FAULT INJECTION", 12, new Color(0.9f, 0.7f, 0.4f), FontStyle.Bold);
            var row2 = Row(panel.transform);
            MakeButton(row2.transform, "Kill 30%", 12, () => _swarm.KillFraction(0.3f), new Color(0.4f, 0.12f, 0.12f, 0.95f));
            var jamBtn = MakeButton(row2.transform, "Jam Comms", 12, () => _swarm.ToggleJam(), new Color(0.4f, 0.25f, 0.1f, 0.95f));
            _jamBtnImg = jamBtn.GetComponent<Image>();
            MakeButton(row2.transform, "Morph", 12, () => { if (_swarm.ActiveBehaviour != null) _swarm.ActiveBehaviour.OnExtraKey(_swarm); });
            MakeButton(row2.transform, "Reset", 12, () => _swarm.Respawn());

            Divider(panel.transform);
            Label(panel.transform, "ENVIRONMENT & DATA", 12, new Color(0.5f, 0.8f, 0.9f), FontStyle.Bold);
            var row3 = Row(panel.transform);
            MakeButton(row3.transform, "Export CSV", 12, ExportCsv, new Color(0.12f, 0.35f, 0.3f, 0.95f));
            MakeButton(row3.transform, "+ Obstacle", 12, () => { if (_scenario != null) _scenario.AddObstacle(RandomArenaPoint(), 2.5f); });
            MakeButton(row3.transform, "Clear Obs", 12, () => { if (_scenario != null) _scenario.ClearObstacles(); });
            _exportInfo = Label(panel.transform, "", 10, new Color(0.5f, 0.75f, 0.65f), FontStyle.Normal);

            _hint = Label(panel.transform, "Left-drag attract · Right-drag repel · Middle-drag orbit · Scroll zoom · O auto-orbit",
                          11, new Color(0.55f, 0.65f, 0.7f), FontStyle.Normal);
        }

        private void Update()
        {
            if (_swarm == null || _stats == null) return;
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(Time.deltaTime, 1e-5f), 0.1f);
            var b = _swarm.ActiveBehaviour;

            _stats.text = _swarm.ActiveCount + " / " + _swarm.Count + " agents     " + Mathf.RoundToInt(_fps) + " FPS";
            _expName.text = b != null ? b.DisplayName : "-";
            _expDesc.text = b != null ? b.Description : "";
            _use.text = (b != null && !string.IsNullOrEmpty(b.RealWorldUse)) ? "Real-world use: " + b.RealWorldUse : " ";
            string m = b != null ? b.Metrics(_swarm) : null;
            _metric.text = string.IsNullOrEmpty(m) ? " " : m;
            _conn.text = "Connectivity: " + _swarm.Connectivity.ToString("F1") + " neighbours/agent"
                       + (_swarm.CommsJammed ? "    ⚠ COMMS JAMMED" : "");
            _conn.color = _swarm.CommsJammed ? new Color(1f, 0.45f, 0.45f) : Ink;

            if (_jamBtnImg != null)
                _jamBtnImg.color = _swarm.CommsJammed ? new Color(0.8f, 0.35f, 0.1f, 1f) : new Color(0.4f, 0.25f, 0.1f, 0.95f);

            // Highlight the active experiment button.
            if (_expButtons != null && b != null)
                for (int i = 0; i < _expButtons.Length; i++)
                {
                    var img = _expButtons[i].GetComponent<Image>();
                    if (img != null) img.color = (b.DisplayName == _expNames[i]) ? BtnActive : BtnIdle;
                }
        }

        private void ExportCsv()
        {
            if (_logger == null) { if (_exportInfo != null) _exportInfo.text = "No data logger found."; return; }
            string path = _logger.Export();
            if (_exportInfo != null)
                _exportInfo.text = path != null
                    ? "Saved " + _logger.SampleCount + " samples → " + System.IO.Path.GetFileName(path)
                    : "Export failed (see Console).";
        }

        private Vector3 RandomArenaPoint()
        {
            Vector3 c = _swarm != null ? _swarm.transform.position : Vector3.zero;
            float r = _swarm != null ? _swarm.arenaRadius * 0.7f : 15f;
            Vector2 p = UnityEngine.Random.insideUnitCircle * r;
            return new Vector3(c.x + p.x, c.y, c.z + p.y);
        }

        // ---- tiny uGUI builders ---------------------------------------------
        private Image Panelled(Transform parent, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private Text Label(Transform parent, string text, int size, Color color, FontStyle style)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        private void Divider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.55f, 0.5f);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 2; le.preferredHeight = 2;
        }

        private RectTransform Row(Transform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 5; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 32; le.preferredHeight = 32;
            return go.GetComponent<RectTransform>();
        }

        private Button MakeButton(Transform parent, string text, int size, UnityEngine.Events.UnityAction onClick, Color? bg = null)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg ?? BtnIdle;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.color = Ink;
            t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            return btn;
        }

        private static Font GetFont()
        {
            Font f = null;
            try { f = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf"); } catch { }
            if (f == null) try { f = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf"); } catch { }
            if (f == null) try { f = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { }
            return f;
        }
    }
}
