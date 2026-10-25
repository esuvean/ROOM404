// 거울 이동 및 비밀 장치 공개
//  - 손자국 1번 탭: 거울이 살짝 움직임
//  - 손자국 2번 탭: 거울이 옆으로 밀리면서 뒤의 POWER 스위치가 나타남
using System.Collections;
using UnityEngine;

public class MirrorController : MonoBehaviour
{
    public RectTransform mirror;
    public float slideDistance = 160f;
    Vector2 home;
    public bool opened;

    void Start() { home = mirror.anchoredPosition; }

    public IEnumerator Nudge()
    {
        SoundManager.Play("mirror_slide", 0.4f);
        yield return UIFx.Move(mirror, home + new Vector2(12f, 0), 0.15f);
        yield return UIFx.Shake(mirror, 0.25f, 3f);
    }

    public IEnumerator SlideOpen()
    {
        opened = true;
        SoundManager.Play("mirror_slide", 0.9f);
        yield return UIFx.Move(mirror, home + new Vector2(slideDistance, 0), 0.9f);
    }
}
