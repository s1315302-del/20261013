using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborRhythm
{
    public class HarborRhythmGame : MonoBehaviour
    {
        public Transform wave;
        public LineRenderer waveLine;
        public Transform vehicle;
        public Material noteMaterial;
        public Material goldMaterial;
        public AudioSource audioSource;
        public float bpm = 100f;
        public int totalNotes = 64;
        public float approachSeconds = 3f;
        public float minHeight = 1.4f;
        public float maxHeight = 6.4f;
        public bool autoPilot;
        public int Score { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public bool Running { get; private set; }
        public float Throttle { get; private set; } = .5f;
        public static string Judge(float distance) => distance <= .32f ? "PERFECT" : distance <= .72f ? "GOOD" : "MISS";
        class Note { public Transform body; public float time, height; public int index; }
        readonly List<Note> notes = new List<Note>();
        readonly List<Transform> particles = new List<Transform>();
        float songTime, nextBeat, feedbackUntil, pulse;
        int spawned, combo, bestCombo, perfect;
        string feedback = "READY";
        bool paused, finished;
        Font font;
        AudioClip[] tones;
        GUIStyle label, title, small, button;
        readonly float[] pattern = { .2f, .2f, .5f, .5f, .8f, .65f, .35f, .5f, .8f, .8f, .5f, .2f, .35f, .65f, .5f, .5f };

        void Awake()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft JhengHei", "Noto Sans CJK TC", "Arial" }, 22);
            tones = new AudioClip[8];
            int[] steps = { 0, 2, 4, 7, 9, 12, 14, 16 };
            for (int n = 0; n < tones.Length; n++)
            {
                const int rate = 22050;
                var data = new float[rate / 5];
                float freq = 220 * Mathf.Pow(2, steps[n] / 12f);
                for (int i = 0; i < data.Length; i++)
                {
                    float t = (float)i / rate;
                    data[i] = (Mathf.Sin(2 * Mathf.PI * freq * t) + .25f * Mathf.Sin(4 * Mathf.PI * freq * t)) * .15f * Mathf.Exp(-t * 24);
                }
                tones[n] = AudioClip.Create("Harbor synth " + n, data.Length, 1, rate, false);
                tones[n].SetData(data, 0);
            }
            UpdateWave(0);
        }

        public void StartRun()
        {
            foreach (var n in notes) if (n.body) Destroy(n.body.gameObject);
            foreach (var p in particles) if (p) Destroy(p.gameObject);
            notes.Clear(); particles.Clear();
            Score = Hits = Misses = spawned = combo = bestCombo = perfect = 0;
            songTime = 0; nextBeat = 0; Throttle = .5f;
            Running = true; paused = finished = false; feedback = "GO"; feedbackUntil = 1;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame && !Running) StartRun();
                if (kb.rKey.wasPressedThisFrame) StartRun();
                if (kb.escapeKey.wasPressedThisFrame && Running) paused = !paused;
            }
            if (!paused)
            {
                float direction = kb == null ? 0 : (kb.wKey.isPressed || kb.upArrowKey.isPressed || kb.spaceKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
                Throttle = Mathf.Clamp01(Throttle + direction * Time.deltaTime * .6f);
                if (Gamepad.current != null && (Gamepad.current.rightTrigger.ReadValue() > .01f || Gamepad.current.leftTrigger.ReadValue() > .01f))
                    Throttle = Mathf.Clamp01(Gamepad.current.rightTrigger.ReadValue() - Gamepad.current.leftTrigger.ReadValue());
                if (Running)
                {
                    songTime += Time.deltaTime;
                    float interval = 60f / bpm;
                    while (spawned < totalNotes && songTime >= spawned * interval)
                    {
                        Spawn(spawned, spawned * interval + approachSeconds);
                        spawned++;
                    }
                    if (songTime >= nextBeat)
                    {
                        pulse = 1;
                        audioSource.PlayOneShot(tones[((int)(nextBeat / interval)) % tones.Length], .7f);
                        nextBeat += interval;
                    }
                    if (autoPilot && notes.Count > 0) Throttle = Mathf.InverseLerp(minHeight, maxHeight, notes[0].height);
                    for (int i = notes.Count - 1; i >= 0; i--)
                    {
                        Note n = notes[i];
                        float remaining = n.time - songTime;
                        n.body.position = new Vector3(0, n.height + Mathf.Max(0, remaining) * 3.1f, 2 + Mathf.Max(0, remaining) * 8);
                        n.body.Rotate(0, 100 * Time.deltaTime, 50 * Time.deltaTime);
                        if (remaining <= 0)
                        {
                            float error = Mathf.Abs(n.height - Mathf.Lerp(minHeight, maxHeight, Throttle));
                            feedback = Judge(error); feedbackUntil = songTime + .5f;
                            if (feedback == "MISS") { Misses++; combo = 0; }
                            else
                            {
                                Hits++; combo++; bestCombo = Mathf.Max(bestCombo, combo);
                                if (feedback == "PERFECT") perfect++;
                                Score += (feedback == "PERFECT" ? 100 : 60) + Mathf.Min(combo, 30) * 5;
                                audioSource.PlayOneShot(tones[n.index % 8], 1);
                                Burst(n.body.position);
                            }
                            Destroy(n.body.gameObject); notes.RemoveAt(i);
                        }
                    }
                    if (spawned == totalNotes && notes.Count == 0) { Running = false; finished = true; }
                }
                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    var p = particles[i];
                    p.localScale *= Mathf.Exp(-3 * Time.deltaTime);
                    p.position += p.up * Time.deltaTime * 3;
                    if (p.localScale.x < .03f) { Destroy(p.gameObject); particles.RemoveAt(i); }
                }
                pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 3);
            }
            UpdateWave(paused ? songTime : Time.time);
        }

        void Spawn(int index, float hitTime)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "Rhythm note " + index;
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one * .7f;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = index % 4 == 0 ? goldMaterial : noteMaterial;
            notes.Add(new Note { body = obj.transform, time = hitTime, height = Mathf.Lerp(minHeight, maxHeight, pattern[index % pattern.Length]), index = index });
        }

        void Burst(Vector3 pos)
        {
            for (int i = 0; i < 8; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(p.GetComponent<Collider>());
                p.transform.SetParent(transform); p.transform.position = pos;
                p.transform.rotation = Quaternion.Euler(0, 0, i * 45);
                p.transform.localScale = Vector3.one * .22f;
                p.GetComponent<Renderer>().sharedMaterial = goldMaterial;
                particles.Add(p.transform);
            }
        }

        void UpdateWave(float t)
        {
            float y = Mathf.Lerp(minHeight, maxHeight, Throttle);
            wave.position = new Vector3(0, y, 2);
            wave.localScale = Vector3.one * (1 + pulse * .12f);
            for (int i = 0; i < waveLine.positionCount; i++)
            {
                float x = -4.5f + 9f * i / (waveLine.positionCount - 1);
                waveLine.SetPosition(i, new Vector3(x, y + Mathf.Sin(x * 3 - t * 5) * .14f * Mathf.Abs(x), 2));
            }
            if (vehicle) vehicle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2) * 2);
        }

        void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 23, normal = { textColor = Color.white } };
            title = new GUIStyle(label) { fontSize = 44, fontStyle = FontStyle.Bold };
            small = new GUIStyle(label) { fontSize = 16, normal = { textColor = new Color(.65f, .81f, .87f) } };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 22, padding = new RectOffset(15, 15, 12, 12) };
        }

        void OnGUI()
        {
            Styles();
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            Panel(new Rect(24, 22, 365, 105));
            GUI.Label(new Rect(44, 34, 330, 30), "KAOHSIUNG / HARBOR FREQUENCY", small);
            GUI.Label(new Rect(44, 61, 330, 45), "港都・音浪駕駛", new GUIStyle(title) { fontSize = 30 });
            Panel(new Rect(936, 22, 320, 105));
            GUI.Label(new Rect(955, 34, 285, 32), "SCORE  " + Score.ToString("000000"), label);
            GUI.Label(new Rect(955, 72, 285, 30), "COMBO " + combo + "   /   " + bpm + " BPM", small);
            Panel(new Rect(24, 572, 360, 124));
            GUI.Label(new Rect(44, 583, 320, 30), "油門深度  " + Mathf.RoundToInt(Throttle * 100) + "%", label);
            GUI.enabled = !paused;
            Throttle = GUI.HorizontalSlider(new Rect(44, 628, 320, 25), Throttle, 0, 1);
            GUI.enabled = true;
            GUI.Label(new Rect(44, 657, 320, 25), "W / ↑ 加深   S / ↓ 放鬆   或拖動油門", small);
            Panel(new Rect(880, 607, 376, 89));
            GUI.Label(new Rect(900, 619, 340, 26), "駁二藝術特區 × 高雄流行音樂中心", small);
            GUI.Label(new Rect(900, 649, 340, 26), "ESC 暫停   R 重玩   手把 RT 類比油門", small);
            if (Running)
            {
                float progress = (Hits + Misses) / (float)totalNotes;
                Draw(new Rect(430, 36, 420, 5), new Color(.15f, .25f, .32f));
                Draw(new Rect(430, 36, 420 * progress, 5), new Color(.2f, 1, .85f));
                if (songTime < feedbackUntil) GUI.Label(new Rect(490, 140, 340, 60), feedback, new GUIStyle(title) { alignment = TextAnchor.MiddleCenter, normal = { textColor = feedback == "MISS" ? new Color(1,.35f,.4f) : new Color(.35f,1,.85f) } });
                if (notes.Count > 0) GUI.Label(new Rect(445, 665, 400, 35), "下一球油門 " + Mathf.RoundToInt(Mathf.InverseLerp(minHeight, maxHeight, notes[0].height) * 100) + "%", new GUIStyle(label) { alignment = TextAnchor.MiddleCenter });
            }
            if (!Running || paused)
            {
                Panel(new Rect(396, 183, 488, 358));
                GUI.Label(new Rect(425, 208, 435, 55), paused ? "演出暫停" : finished ? "港都演出完成" : "把城市，開成一首歌。", new GUIStyle(title) { fontSize = 30, alignment = TextAnchor.MiddleCenter });
                string text = finished ? "得分 " + Score + "   命中 " + Hits + "/" + totalNotes + "\nPERFECT " + perfect + "   最高連擊 " + bestCombo + "\n準確率 " + Mathf.RoundToInt(Hits * 100f / totalNotes) + "%" : "油門越深，青綠音浪越高。\n節奏球落到前方接球位置的瞬間，\n讓音浪中心與球的高度重合。\n音樂節拍與目標油門會協助你掌握節奏。";
                GUI.Label(new Rect(426, 280, 428, 150), text, new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true });
                if (GUI.Button(new Rect(468, 457, 344, 55), paused ? "繼續演出" : finished ? "再演一次 / ENTER" : "開始演出 / ENTER", button)) { if (paused) paused = false; else StartRun(); }
            }
            GUI.matrix = oldMatrix;
        }
        static void Panel(Rect rect) => Draw(rect, new Color(.025f, .065f, .10f, .93f));
        static void Draw(Rect rect, Color color) { var previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
        void OnDestroy() { if (tones != null) foreach (var tone in tones) if (tone) Destroy(tone); if (font) Destroy(font); }
    }
}
