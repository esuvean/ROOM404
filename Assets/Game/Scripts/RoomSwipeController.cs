// 스와이프를 통한 방 시점 변경
//  침대 → 창문 → 출입문 → 거울/책상 → 침대 (순환)
//  손가락을 왼쪽으로 밀면 오른쪽 벽으로, 오른쪽으로 밀면 왼쪽 벽으로 돌아봅니다.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RoomSwipeController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform[] walls;
    public int current = 0;
    public float slideTime = 0.3f;
    [Range(0.03f, 0.3f)] public float minSwipe = 0.08f; // 화면 너비 대비 최소 이동 거리
    public bool locked;
    public bool wrap = true;          // false 면 끝에서 멈춤 (복도)
    public Action<int> onBlocked;     // 끝에서 더 가려고 할 때 (방향)

    public Action<int> onWallChanged;
    bool sliding;
    Vector2 start;

    void Start()
    {
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(i == current);
            walls[i].anchoredPosition = Vector2.zero;
        }
        onWallChanged?.Invoke(current);
    }

    public void OnBeginDrag(PointerEventData e) { start = e.pressPosition; }
    public void OnDrag(PointerEventData e) { }

    public void OnEndDrag(PointerEventData e)
    {
        Vector2 d = e.position - start;
        if (Mathf.Abs(d.x) > Screen.width * minSwipe && Mathf.Abs(d.x) > Mathf.Abs(d.y))
            Go(d.x < 0 ? 1 : -1);
    }

    public void Go(int dir)
    {
        if (locked || sliding) return;
        if (!wrap && (current + dir < 0 || current + dir >= walls.Length)) { onBlocked?.Invoke(dir); return; }
        StartCoroutine(Slide(dir));
    }

    // 연출 없이 바로 특정 화면으로 (복도 되돌아오기)
    public void JumpTo(int index)
    {
        StopAllCoroutines();
        sliding = false;
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].gameObject.SetActive(i == index);
            walls[i].anchoredPosition = Vector2.zero;
        }
        current = index;
        onWallChanged?.Invoke(current);
    }

    IEnumerator Slide(int dir)
    {
        sliding = true;
        int n = walls.Length;
        int next = (current + dir + n) % n;
        RectTransform a = walls[current], b = walls[next];
        float w = a.rect.width;
        b.gameObject.SetActive(true);
        b.anchoredPosition = new Vector2(dir * w, 0);
        SoundManager.Play("step", 0.25f);
        for (float t = 0; t < slideTime; t += Time.deltaTime)
        {
            float k = t / slideTime; k = k * k * (3f - 2f * k);
            a.anchoredPosition = new Vector2(-dir * w * k, 0);
            b.anchoredPosition = new Vector2(dir * w * (1f - k), 0);
            yield return null;
        }
        b.anchoredPosition = Vector2.zero;
        a.anchoredPosition = Vector2.zero;
        a.gameObject.SetActive(false);
        current = next;
        sliding = false;
        onWallChanged?.Invoke(current);
    }
}
