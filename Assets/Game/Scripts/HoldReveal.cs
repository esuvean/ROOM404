// UV 홀드 시간 계산 및 메시지 표시
//  - UV가 선택된 상태에서 거울을 길게 누르면 글자가 점점 나타납니다.
//    0.0초 희미한 흔적 → 0.5초 일부 → 1.0초 거의 보임 → 1.5초 전체 공개
//  - 중간에 손을 떼면 UV가 꺼지고 글자가 다시 흐려집니다.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldReveal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public CanvasGroup revealGroup;   // THE CLOCK LIES 글자
    public Image glow;                // 보라색 UV 빛
    public float holdTime = 1.5f;
    public string tapId = "mirror";   // 짧게 탭했을 때 보낼 id
    public bool revealed;

    public Func<bool> canHold;        // UV가 선택되어 있는지
    public Action onRevealed;

    float progress, downTime;
    bool holding;
    Vector2 downPos;

    public float Progress => progress;

    public void OnPointerDown(PointerEventData e)
    {
        downTime = Time.time;
        downPos = e.position;
        if (!revealed && canHold != null && canHold())
        {
            holding = true;
            SoundManager.Play("uv_on", 0.8f);
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        bool shortTap = Time.time - downTime < 0.35f && (e.position - downPos).magnitude < Screen.height * 0.03f;
        holding = false;
        if (shortTap) TapRouter.Send(tapId);
    }

    public void OnPointerExit(PointerEventData e) { holding = false; }

    void Update()
    {
        if (!revealed)
        {
            if (holding)
            {
                progress += Time.deltaTime;
                if (progress >= holdTime)
                {
                    revealed = true;
                    holding = false;
                    progress = holdTime;
                    onRevealed?.Invoke();
                }
            }
            else progress = Mathf.Max(0f, progress - Time.deltaTime * 2.5f);
        }

        // 단계별 표시 (기획서 18번 표)
        float k = progress / holdTime;
        float a;
        if (revealed) a = 1f;
        else if (k <= 0f) a = 0f;
        else if (k < 0.333f) a = 0.12f;      // 희미한 흔적
        else if (k < 0.666f) a = 0.4f;       // 일부 등장
        else if (k < 1f) a = 0.75f;          // 거의 보임
        else a = 1f;
        if (holding) a *= UnityEngine.Random.Range(0.85f, 1f); // 손전등 떨림
        revealGroup.alpha = Mathf.MoveTowards(revealGroup.alpha, a, Time.deltaTime * 6f);

        if (glow != null)
        {
            float g = holding ? 0.55f + 0.15f * Mathf.Sin(Time.time * 20f) : (revealed ? 0.12f : 0f);
            var c = glow.color;
            c.a = Mathf.MoveTowards(c.a, g, Time.deltaTime * 4f);
            glow.color = c;
        }
    }
}
