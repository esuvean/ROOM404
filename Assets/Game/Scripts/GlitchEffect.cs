// 화면 노이즈(지지직) 연출
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GlitchEffect : MonoBehaviour
{
    public RawImage noise;

    void Awake()
    {
        var c = noise.color; c.a = 0f; noise.color = c;
        noise.raycastTarget = false;
    }

    public IEnumerator Burst(float duration, float strength)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            noise.uvRect = new Rect(Random.value, Random.value, 1f + Random.value * 0.5f, Random.Range(0.05f, 0.6f));
            float a = Random.value < 0.25f ? 0f : Random.Range(0.3f, 1f) * strength;
            noise.color = new Color(Random.Range(0.7f, 1f), Random.Range(0.6f, 1f), Random.Range(0.7f, 1f), a);
            yield return null;
        }
        var c = noise.color; c.a = 0f; noise.color = c;
    }
}
