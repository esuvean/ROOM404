// 비상 전원 레버 (홀드)
//  - 길게 누르고 있으면 게이지가 0% → 25% → 50% → 75% → 100% 로 올라갑니다. (2초)
//  - 중간에 손을 떼면 레버가 튕겨 올라가고 게이지가 0 으로 돌아갑니다.
//  - 짧게 탭하면 tapId 를 지금 챕터 관리자에게 보냅니다.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldLever : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float holdTime = 2f;
    public RectTransform handle;        // 눌리는 동안 아래로 내려가는 레버 그림
    public float travel = 48f;          // 레버가 내려가는 거리 (픽셀)
    public GameObject gaugeRoot;
    public Image fill;                  // Filled 이미지
    public Text percent;
    public string tapId = "lever";
    public string label = "POWER";      // 게이지 글자 (Chapter 3 지문 스캐너는 "SCAN")
    public bool done;

    public Func<bool> canHold;
    public Action onDone;
    public Action onRelease;            // 끝까지 못 버티고 손을 뗐을 때

    float progress, downTime;
    bool holding;
    Vector2 downPos, home;

    void Start()
    {
        if (handle != null) home = handle.anchoredPosition;
        if (gaugeRoot != null) gaugeRoot.SetActive(false);
        UpdateUI();
    }

    public void OnPointerDown(PointerEventData e)
    {
        downTime = Time.time;
        downPos = e.position;
        if (done) return;
        if (canHold != null && !canHold()) return;
        holding = true;
        if (gaugeRoot != null) gaugeRoot.SetActive(true);
        SoundManager.Loop("lever_charge", 0.6f);
    }

    public void OnPointerUp(PointerEventData e)
    {
        bool shortTap = Time.time - downTime < 0.3f && (e.position - downPos).magnitude < Screen.height * 0.03f;
        Release();
        if (shortTap) TapRouter.Send(tapId);
    }

    public void OnPointerExit(PointerEventData e) { Release(); }

    void Release()
    {
        if (!holding) return;
        holding = false;
        SoundManager.StopLoop();
        if (!done && progress > 0.35f)
        {
            SoundManager.Play("switch", 0.6f);
            onRelease?.Invoke();
        }
    }

    void Update()
    {
        if (done) return;
        if (holding)
        {
            progress += Time.deltaTime;
            if (progress >= holdTime)
            {
                progress = holdTime;
                holding = false;
                done = true;
                SoundManager.StopLoop();
                UpdateUI();
                onDone?.Invoke();
                return;
            }
        }
        else progress = Mathf.Max(0f, progress - Time.deltaTime * 4f);   // 손을 떼면 빠르게 튕겨 올라감
        UpdateUI();
    }

    void UpdateUI()
    {
        float k = progress / holdTime;
        if (fill != null) fill.fillAmount = k;
        if (percent != null) percent.text = label + " " + (Mathf.FloorToInt(k * 4f + 0.0001f) * 25) + "%";
        if (handle != null && !done)
        {
            Vector2 shake = holding ? new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), 0) : Vector2.zero;
            handle.anchoredPosition = home + new Vector2(0, -travel * k) + shake;
        }
    }

    public void ResetHandle() { if (handle != null) handle.anchoredPosition = home; }
}
