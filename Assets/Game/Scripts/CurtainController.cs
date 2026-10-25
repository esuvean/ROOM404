// 커튼 스와이프
//  - 탭으로는 열리지 않고, 커튼 위에서 좌우로 밀어야 열립니다.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CurtainController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public RectTransform left, right;
    public float openOffset = 144f;   // 열렸을 때 커튼이 옆으로 이동하는 거리
    public bool isOpen;
    public Action onOpened;

    Vector2 lStart, rStart;
    float startX, amount;
    Canvas canvas;

    void Start()
    {
        lStart = left.anchoredPosition;
        rStart = right.anchoredPosition;
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (isOpen) return;
        if ((e.pressPosition - e.position).magnitude > Screen.height * 0.03f) return;
        TapRouter.Send("curtain");
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (isOpen) return;
        startX = e.position.x;
        SoundManager.Play("curtain", 0.6f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (isOpen) return;
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        float dx = (e.position.x - startX) / scale;
        amount = Mathf.Clamp01(Mathf.Abs(dx) / openOffset);
        Apply(amount);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (isOpen) return;
        if (amount > 0.35f) StartCoroutine(OpenCo());
        else StartCoroutine(CloseCo());
    }

    void Apply(float k)
    {
        left.anchoredPosition = lStart + new Vector2(-openOffset * k, 0);
        right.anchoredPosition = rStart + new Vector2(openOffset * k, 0);
    }

    IEnumerator OpenCo()
    {
        isOpen = true;
        float from = amount;
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            Apply(Mathf.Lerp(from, 1f, t / 0.35f));
            yield return null;
        }
        Apply(1f);
        var img = GetComponent<Image>();
        if (img != null) img.raycastTarget = false;   // 열린 뒤엔 뒤쪽(창문)을 탭할 수 있게
        onOpened?.Invoke();
    }

    IEnumerator CloseCo()
    {
        float from = amount;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            Apply(Mathf.Lerp(from, 0f, t / 0.2f));
            yield return null;
        }
        Apply(0f);
        amount = 0f;
    }
}
