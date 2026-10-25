// 스와이프로 물건 밀기 (청소 카트, 배전함 먼지 덮개)
//  - 정해진 방향(direction)으로 충분히 밀면 끝까지 움직이고 onDone 이 불립니다.
//  - 짧게 탭하면 tapId 를 지금 챕터 관리자에게 보냅니다.
//  - 다 밀고 난 뒤의 스와이프는 뒤쪽(복도 화면 넘기기)으로 넘겨 줍니다.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public RectTransform target;
    public Vector2 direction = Vector2.left;   // 밀어야 하는 방향
    public float distance = 160f;             // 끝까지 움직이는 거리 (픽셀)
    public string tapId;
    public string sound = "cart";
    public bool done;
    public Action onDone;

    Vector2 home, startPt;
    float amount;
    bool passThrough;
    Canvas canvas;

    void Start()
    {
        if (target == null) target = (RectTransform)transform;
        home = target.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if ((e.pressPosition - e.position).magnitude > Screen.height * 0.03f) return;
        TapRouter.Send(tapId);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        startPt = e.pressPosition;
        // 이미 다 밀었거나, 다른 방향으로 미는 중이면 → 화면 넘기기로 전달
        Vector2 d = e.position - startPt;
        passThrough = done || Vector2.Dot(d.normalized, direction.normalized) < 0.5f;
        if (passThrough) { Forward(e, ExecuteEvents.beginDragHandler); return; }
        SoundManager.Play(sound, 0.7f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (passThrough) { Forward(e, ExecuteEvents.dragHandler); return; }
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 d = (e.position - startPt) / scale;
        amount = Mathf.Clamp01(Vector2.Dot(d, direction.normalized) / distance);
        Apply(amount);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (passThrough) { Forward(e, ExecuteEvents.endDragHandler); passThrough = false; return; }
        if (amount > 0.4f) StartCoroutine(Finish());
        else StartCoroutine(Back());
    }

    void Forward<T>(PointerEventData e, ExecuteEvents.EventFunction<T> fn) where T : IEventSystemHandler
    {
        if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, fn);
    }

    void Apply(float k) { target.anchoredPosition = home + direction.normalized * distance * k; }

    IEnumerator Finish()
    {
        done = true;
        float from = amount;
        for (float t = 0; t < 0.3f; t += Time.deltaTime) { Apply(Mathf.Lerp(from, 1f, t / 0.3f)); yield return null; }
        Apply(1f);
        onDone?.Invoke();
    }

    IEnumerator Back()
    {
        float from = amount;
        for (float t = 0; t < 0.2f; t += Time.deltaTime) { Apply(Mathf.Lerp(from, 0f, t / 0.2f)); yield return null; }
        Apply(0f);
        amount = 0f;
    }
}
