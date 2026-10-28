// 확대 화면 (알람시계, 사진, 메모 등을 크게 보여줌)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZoomView : MonoBehaviour
{
    public static ZoomView I;
    public GameObject panel;
    public Image image;
    public Text paperText;     // 메모 종이 위 글씨
    public TapCatcher catcher;

    bool tapped;

    void Awake()
    {
        I = this;
        panel.SetActive(false);
        catcher.onTap += () => tapped = true;
    }

    // 종이 위 글씨가 줄바꿈되거나 넘치지 않도록, 한 줄이 칸에 들어갈 때까지 글자 크기를 줄임
    static readonly int[] Sizes = { 24, 22, 20, 18, 16, 14, 12 };
    static void FitText(Text t)
    {
        Vector2 box = t.rectTransform.sizeDelta;
        foreach (int size in Sizes)
        {
            t.fontSize = size;
            if (t.preferredWidth <= box.x - 4f && t.preferredHeight <= box.y + 8f) return;
        }
    }

    // lines 가 있으면 대사를 다 넘긴 뒤 닫히고, 없으면 한 번 탭하면 닫힘
    public IEnumerator Show(Sprite s, string paper, params string[] lines)
    {
        panel.SetActive(true);
        image.sprite = s;
        float scale = Mathf.Max(1f, Mathf.Floor(Mathf.Min(420f / s.rect.width, 380f / s.rect.height)));
        image.rectTransform.sizeDelta = new Vector2(s.rect.width * scale, s.rect.height * scale);
        bool hasPaper = !string.IsNullOrEmpty(paper);
        paperText.gameObject.SetActive(hasPaper);
        if (hasPaper)
        {
            paperText.text = paper;
            paperText.rectTransform.sizeDelta = image.rectTransform.sizeDelta - new Vector2(32, 24);
            FitText(paperText);
        }
        SoundManager.Play("tap", 0.7f);
        image.transform.localScale = Vector3.one * 0.92f;
        StartCoroutine(UIFx.Scale(image.transform, Vector3.one, 0.18f));

        tapped = false;
        if (lines != null && lines.Length > 0)
            yield return DialogueManager.I.Sequence(lines);
        else
        {
            yield return null;
            tapped = false;
            while (!tapped) yield return null;
            SoundManager.Play("click", 0.3f);
        }
        panel.SetActive(false);
    }
}
