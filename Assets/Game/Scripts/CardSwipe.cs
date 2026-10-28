// 카드 긁기 미니게임 (Chapter 3 관찰실 문) — 어몽어스의 "Swipe Card" 같은 퍼즐
//  - 카드를 손가락으로 왼쪽에서 오른쪽 끝까지 끌면 됩니다.
//  - 너무 빠르면 TOO FAST, 너무 느리면 TOO SLOW. 약 0.35 ~ 1.8초 사이가 정답.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject panel;
    public RectTransform body;
    public RectTransform card;
    public Text display;
    public Image lamp;
    public Button closeButton;
    public float startX = -120f, endX = 120f;
    public float minTime = 0.35f, maxTime = 1.8f;

    public Action onSuccess;
    public Action<string> onFail;     // "fast" / "slow" / "short"

    static readonly Color Green = new Color(0.29f, 0.88f, 0.48f);
    static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    static readonly Color Idle = new Color(0.9f, 0.85f, 0.7f);

    bool dragging, busy, ok;
    float t0, offset;
    bool timing;

    void Awake()
    {
        panel.SetActive(false);
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        panel.SetActive(true);
        busy = false;
        ResetCard();
        display.text = "PLEASE SWIPE CARD";
        display.color = Idle;
        lamp.color = new Color(0.3f, 0.3f, 0.3f);
        StartCoroutine(UIFx.Punch(body, 0.04f, 0.2f));
    }

    public void Close()
    {
        if (busy || dragging) return;
        SoundManager.Play("click", 0.5f);
        panel.SetActive(false);
    }

    void ResetCard() { card.anchoredPosition = new Vector2(startX, card.anchoredPosition.y); timing = false; }

    float LocalX(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)card.parent, screen, null, out var p);
        return p.x;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (busy || ok) return;
        dragging = true;
        offset = card.anchoredPosition.x - LocalX(e.pressPosition);   // 처음 누른 곳 기준 → 카드가 손가락을 정확히 따라옴
        display.text = "READING…";
        display.color = Idle;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        float x = Mathf.Clamp(LocalX(e.position) + offset, startX, endX);
        if (!timing && x > startX + 12f) { timing = true; t0 = Time.time; SoundManager.Play("cart", 0.25f); }
        card.anchoredPosition = new Vector2(x, card.anchoredPosition.y);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        bool reachedEnd = card.anchoredPosition.x >= endX - 12f;
        float dt = timing ? Time.time - t0 : 0f;
        if (!reachedEnd) StartCoroutine(Result(false, "BAD READ. TRY AGAIN.", "short"));
        else if (dt < minTime) StartCoroutine(Result(false, "TOO FAST. TRY AGAIN.", "fast"));
        else if (dt > maxTime) StartCoroutine(Result(false, "TOO SLOW. TRY AGAIN.", "slow"));
        else StartCoroutine(Result(true, "ACCEPTED. THANK YOU.", null));
    }

    IEnumerator Result(bool success, string msg, string why)
    {
        busy = true;
        display.text = msg;
        display.color = success ? Green : Red;
        lamp.color = success ? Green : Red;
        SoundManager.Play(success ? "success" : "error", 0.8f);
        if (!success) StartCoroutine(UIFx.Shake(body, 0.3f, 8f));
        yield return new WaitForSeconds(success ? 1.0f : 0.9f);
        if (success)
        {
            ok = true;
            panel.SetActive(false);
            onSuccess?.Invoke();
        }
        else
        {
            ResetCard();
            display.text = "PLEASE SWIPE CARD";
            display.color = Idle;
            lamp.color = new Color(0.3f, 0.3f, 0.3f);
            onFail?.Invoke(why);
        }
        busy = false;
    }
}
