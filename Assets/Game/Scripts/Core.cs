// =============================================================
//  04:04  -  공용 도구 모음
//  GameState    : 플레이 시간 기록
//  UIFx         : 페이드, 이동, 흔들림 같은 연출 함수
// =============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 오브젝트를 탭하면 지금 챕터의 관리자(Chapter01Manager / Chapter02Manager)에게 id 를 전달
public static class TapRouter
{
    public static Action<string> Handler;
    public static void Send(string id) { Handler?.Invoke(id); }
}

public static class GameState
{
    public static float startTime;
    public static float clearSeconds = -1f;
    public static string ending = "";   // 마지막 챕터에서 고른 결말 (A / B)
    public static bool retryChoice;     // 엔딩 화면의 [다른 결말 보기] → 관제실 선택 장면부터 다시
    public static bool fromRetry;       // 방금 본 엔딩이 '다시 선택' 으로 본 것인지
    public static void StartRun() { startTime = Time.time; clearSeconds = -1f; }
    // 챕터별 클리어 기록 (씬 이름 → 걸린 초). Chapter 3 출입 기록표에 쓰임
    public static readonly Dictionary<string, float> records = new Dictionary<string, float>();
    public static void EndRun()
    {
        clearSeconds = Time.time - startTime;
        records[SceneManager.GetActiveScene().name] = clearSeconds;
    }
    public static string Format(float sec)
    {
        int m = Mathf.FloorToInt(sec / 60f), s = Mathf.FloorToInt(sec % 60f);
        return m > 0 ? $"{m}분 {s}초" : $"{s}초";
    }
}

public static class UIFx
{
    static float Smooth(float t) => t * t * (3f - 2f * t);

    public static IEnumerator Fade(CanvasGroup g, float to, float dur)
    {
        float from = g.alpha;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            g.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        g.alpha = to;
    }

    public static IEnumerator FadeGraphic(Graphic g, float to, float dur)
    {
        Color c = g.color; float from = c.a;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / dur); g.color = c;
            yield return null;
        }
        c.a = to; g.color = c;
    }

    public static IEnumerator Move(RectTransform rt, Vector2 to, float dur)
    {
        Vector2 from = rt.anchoredPosition;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, Smooth(t / dur));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    public static IEnumerator MoveLinear(RectTransform rt, Vector2 to, float dur)
    {
        Vector2 from = rt.anchoredPosition;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, t / dur);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    public static IEnumerator Scale(Transform tr, Vector3 to, float dur)
    {
        Vector3 from = tr.localScale;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            tr.localScale = Vector3.Lerp(from, to, Smooth(t / dur));
            yield return null;
        }
        tr.localScale = to;
    }

    public static IEnumerator Shake(RectTransform rt, float dur, float mag)
    {
        Vector2 basePos = rt.anchoredPosition;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = 1f - t / dur;
            rt.anchoredPosition = basePos + UnityEngine.Random.insideUnitCircle * mag * k;
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    public static IEnumerator Punch(Transform tr, float amount = 0.15f, float dur = 0.2f)
    {
        Vector3 b = Vector3.one;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / dur * Mathf.PI);
            tr.localScale = b * (1f + amount * k);
            yield return null;
        }
        tr.localScale = b;
    }
}

