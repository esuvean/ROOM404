// 배선 연결 미니게임 (Chapter 2 배전함) — 어몽어스의 "Fix Wiring" 같은 퍼즐
//  - 왼쪽 전선을 손가락으로 끌어서, 같은 색의 오른쪽 단자에 놓으면 연결됩니다.
//  - 빨간 선은 끊어져 있어서, 인벤토리의 '전선(WIRE)' 을 고른 채 끊어진 곳을 탭해야 이어집니다.
//  - 4가닥을 다 이으면 onComplete 가 불립니다.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WiringPanel : MonoBehaviour
{
    public GameObject panel;
    public RectTransform body;
    public RectTransform[] left;      // 왼쪽 전선 끝 (빨강 · 파랑 · 노랑 · 초록 순서)
    public RectTransform[] right;     // 오른쪽 단자 (자리만 4개, 색은 열 때마다 섞임)
    public Image[] rightImages;
    public RectTransform[] lines;     // 왼쪽에서 뻗어 나가는 전선 그림
    public GameObject cutMark;        // 빨간 선의 끊어진 표시
    public Text status;
    public Button closeButton;
    public Color[] colors;

    public bool repaired;             // 빨간 선을 이었는지
    public bool done;
    public Func<bool> wireSelected;   // 인벤토리에서 WIRE 를 골랐는지
    public Action onRepaired, onNeedWire, onComplete;

    readonly int[] rightColor = new int[4];
    readonly bool[] linked = new bool[4];
    int dragging = -1;

    void Awake()
    {
        panel.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            var d = left[i].gameObject.AddComponent<WireDrag>();
            d.panel = this; d.index = i;
            lines[i].gameObject.SetActive(false);
        }
        closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        panel.SetActive(true);
        if (!done)
        {
            // 오른쪽 색 순서를 섞기
            for (int i = 0; i < 4; i++) rightColor[i] = i;
            for (int i = 3; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (rightColor[i], rightColor[j]) = (rightColor[j], rightColor[i]); }
            if (rightColor[0] == 0 && rightColor[1] == 1) (rightColor[0], rightColor[1]) = (rightColor[1], rightColor[0]);
            for (int i = 0; i < 4; i++)
            {
                rightImages[i].color = colors[rightColor[i]];
                linked[i] = false;
                lines[i].gameObject.SetActive(false);
            }
        }
        cutMark.SetActive(!repaired);
        status.text = done ? "배선 완료" : (repaired ? "같은 색끼리 이어 주세요" : "빨간 선이 끊어져 있다");
        StartCoroutine(UIFx.Punch(body, 0.04f, 0.2f));
    }

    public void Close()
    {
        if (dragging >= 0) return;
        SoundManager.Play("click", 0.5f);
        panel.SetActive(false);
    }

    Vector2 Local(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(body, screen, null, out var p);
        return p;
    }

    Vector2 LocalOf(RectTransform rt) => (Vector2)body.InverseTransformPoint(rt.TransformPoint(rt.rect.center));

    void Stretch(int i, Vector2 to)
    {
        Vector2 from = LocalOf(left[i]);
        var line = lines[i];
        line.gameObject.SetActive(true);
        line.anchoredPosition = from;
        Vector2 d = to - from;
        line.sizeDelta = new Vector2(d.magnitude, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // ---------- WireDrag 에서 불림 ----------
    public bool Begin(int i)
    {
        if (done || linked[i]) return false;
        if (i == 0 && !repaired)
        {
            TryRepair();
            return false;
        }
        dragging = i;
        SoundManager.Play("tap", 0.5f);
        return true;
    }

    public void TryRepair()
    {
        if (repaired) return;
        if (wireSelected != null && wireSelected())
        {
            repaired = true;
            cutMark.SetActive(false);
            SoundManager.Play("pickup", 0.8f);
            status.text = "같은 색끼리 이어 주세요";
            StartCoroutine(UIFx.Punch(left[0], 0.3f, 0.3f));
            onRepaired?.Invoke();
        }
        else
        {
            SoundManager.Play("error", 0.4f);
            StartCoroutine(UIFx.Shake(left[0], 0.25f, 4f));
            onNeedWire?.Invoke();
        }
    }

    public void Drag(int i, Vector2 screen) { if (dragging == i) Stretch(i, Local(screen)); }

    public void End(int i, Vector2 screen)
    {
        if (dragging != i) return;
        dragging = -1;
        for (int j = 0; j < 4; j++)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(right[j], screen, null)) continue;
            if (rightColor[j] == i)
            {
                linked[i] = true;
                Stretch(i, LocalOf(right[j]));
                SoundManager.Play("switch", 0.8f);
                StartCoroutine(UIFx.Punch(right[j], 0.25f, 0.2f));
                CheckDone();
                return;
            }
            break;
        }
        lines[i].gameObject.SetActive(false);
        SoundManager.Play("buzz", 0.3f);
    }

    void CheckDone()
    {
        for (int k = 0; k < 4; k++) if (!linked[k]) return;
        done = true;
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        status.text = "배선 완료";
        SoundManager.Play("success", 0.8f);
        yield return new WaitForSeconds(0.9f);
        panel.SetActive(false);
        onComplete?.Invoke();
    }
}

// 왼쪽 전선 끝에 붙어서 끌기를 전달
public class WireDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public WiringPanel panel;
    public int index;
    bool active;

    public void OnBeginDrag(PointerEventData e) { active = panel.Begin(index); }
    public void OnDrag(PointerEventData e) { if (active) panel.Drag(index, e.position); }
    public void OnEndDrag(PointerEventData e) { if (active) panel.End(index, e.position); active = false; }
    public void OnPointerClick(PointerEventData e) { if (index == 0 && !panel.repaired && !e.dragging) panel.TryRepair(); }
}
