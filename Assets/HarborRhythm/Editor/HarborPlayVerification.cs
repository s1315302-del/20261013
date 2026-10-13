using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using HarborRhythm;

[InitializeOnLoad]
public static class HarborPlayVerification
{
    const string Request = "Assets/HarborRhythm/verify.request";
    static double started;
    static bool running;
    static HarborPlayVerification()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += StateChanged;
    }
    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (File.Exists("Assets/HarborRhythm/build.request") || !File.Exists("Assets/Scenes/KaohsiungHarbor.unity")) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/KaohsiungHarbor.unity") return;
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying || !running) return;
        var game = UnityEngine.Object.FindAnyObjectByType<HarborRhythmGame>();
        if (!game) { Finish("FAIL: runtime controller missing."); return; }
        if (!game.Running)
        {
            bool pass = game.Hits == 64 && game.Misses == 0 && game.Score == 13825;
            string report = (pass ? "PASS" : "FAIL") + ": full 64-note autoplay. Hits=" + game.Hits + ", Misses=" + game.Misses + ", Score=" + game.Score;
            game.autoPilot = false; game.StartRun();
            bool reset = game.Score == 0 && game.Hits == 0 && game.Misses == 0 && game.Running;
            report += "\nRestart state: " + (reset ? "PASS" : "FAIL");
            Finish(report);
        }
        else if (EditorApplication.timeSinceStartup - started > 60) Finish("FAIL: runtime verification exceeded 60 seconds.");
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !File.Exists(Request)) return;
        var game = UnityEngine.Object.FindAnyObjectByType<HarborRhythmGame>();
        if (!game) { Finish("FAIL: no game controller."); return; }
        game.autoPilot = true; game.StartRun(); Time.timeScale = 4;
        started = EditorApplication.timeSinceStartup; running = true;
    }
    static void Finish(string report)
    {
        File.WriteAllText("Assets/HarborRhythm/PlayVerification.txt", report);
        File.Delete(Request); Time.timeScale = 1; running = false;
        EditorApplication.isPlaying = false; Debug.Log("Harbor Rhythm verification: " + report);
    }
}
