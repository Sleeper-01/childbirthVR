using System;
using System.Collections.Generic;
using 产前运动;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public static class 讲解自检
{
    [MenuItem("产前运动/运行讲解自检")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Open PrenatalDemo and enter Play mode first.");
        var c = UnityEngine.Object.FindObjectOfType<讲解控制器>();
        Require(c != null, "Controller exists");
        Require(!c.service.IsBusy, "No request in flight");
        var results = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            c.exerciseButtons[i].onClick.Invoke();
            Require(c.CurrentExercise == i && c.dialogue.CurrentText == c.catalog.exercises[i].passages[0], "Exercise selection " + i);
        }
        results.Add("Five exercise buttons");
        c.SelectExercise(0);
        for (int i = 0; i < 6; i++)
        {
            Require(c.CurrentPassage == i, "Kegel step " + i);
            while (c.dialogue.Advance()) { }
            c.Next();
        }
        results.Add("Six Kegel steps");
        c.Restart(); c.Next();
        int passage = c.CurrentPassage; string text = c.dialogue.CurrentText;
        if (!c.service.IsConfigured)
        {
            c.input.field.text = "如何开始练习？"; c.input.Submit();
            Require(c.dialogue.CurrentText.Contains("尚未配置"), "Missing key message");
            Require(c.input.field.text.Length > 0 && c.input.send.interactable, "Input retained and controls restored");
            c.Resume();
            Require(c.CurrentPassage == passage && c.dialogue.CurrentText == text, "Resume keeps progress");
            results.Add("Missing key / resume");
        }
        c.ShowSafety();
        foreach (string safety in c.catalog.safety)
        {
            Require(c.dialogue.CurrentText == safety, "Safety text");
            while (c.dialogue.Advance()) { }
            c.Next();
        }
        results.Add("All safety passages");
        c.dialogue.Show("分页测试", new string('测', 2000));
        Require(c.dialogue.PageCount > 1, "Long text has multiple pages");
        while (c.dialogue.Advance()) { }
        Require(c.dialogue.CurrentPage == c.dialogue.PageCount, "Final page reachable");
        results.Add("Long text pagination");
        c.input.field.text = ""; c.input.Submit(); Require(c.status.text == "请先输入问题", "Empty input");
        c.input.SetBusy(true); Require(!c.input.send.interactable && !c.input.field.interactable, "Busy controls"); c.input.SetBusy(false);
        c.video.Select(null);
        Require(!c.video.play.interactable && !c.video.replay.interactable, "Missing video controls");
        Require(c.video.player.audioOutputMode == VideoAudioOutputMode.None && !c.video.player.playOnAwake, "Silent manual video");
        Require(智能问答服务.FailureMessage(401).Contains("鉴权"), "Authentication error mapping");
        Require(智能问答服务.FailureMessage(429).Contains("频繁"), "Rate limit mapping");
        Require(智能问答服务.FailureMessage(0).Contains("超时"), "Network error mapping");
        results.Add("Empty input / busy controls / video defaults / failure messages");
        c.SelectExercise(0);
        Debug.Log("Prenatal smoke checks passed: " + string.Join("; ", results));
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Prenatal check failed: " + message); }
}
