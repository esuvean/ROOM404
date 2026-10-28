// 엔딩 화면 (ENDING A / ENDING B) + 총 플레이 시간 + 크레딧 → 처음으로
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingScreen : MonoBehaviour
{
    public Image background;
    public Sprite bgA, bgB;
    public Text label, title, body, record, credits, other;
    public RectTransform creditsRoll;
    public Button backButton;
    public Button retryButton;     // 아직 못 본 결말이 있으면 → 관제실 선택 장면으로
    public GlitchEffect glitch;

    static readonly string[] Scenes = { "Chapter01_Room404", "Chapter02_Corridor", "Chapter03_Observation", "Chapter04_WakeUp" };

    void Start()
    {
        SoundManager.StopLoop();
        bool b = GameState.ending == "B";
        background.sprite = b ? bgB : bgA;
        label.text = b ? "ENDING B" : "ENDING A";
        title.text = b ? "04:05" : "OBSERVER";
        title.color = b ? new Color(1f, 0.93f, 0.78f) : new Color(1f, 0.23f, 0.19f);
        body.text = b
            ? "시계가 처음으로 다음 시각을 가리켰다.\n\n문밖에는, 아침이 와 있었다."
            : "관찰은 계속된다.\n\nROOM 404 에서, 누군가 또 눈을 뜬다.";

        float total = 0f; int n = 0;
        foreach (var s in Scenes) if (GameState.records.TryGetValue(s, out var t)) { total += t; n++; }
        if (n > 0)
        {
            int m = Mathf.FloorToInt(total / 60f), sec = Mathf.FloorToInt(total % 60f);
            record.text = (n == Scenes.Length ? "모든 챕터를 지나는 데 " : "이번에는 ") + (m > 0 ? $"{m}분 {sec}초" : $"{sec}초") + " 걸렸다.";
        }
        else record.text = "";

        seenOther = PlayerPrefs.GetInt(b ? "0404_endingA" : "0404_endingB", 0) == 1;
        other.text = seenOther ? "두 개의 결말을 모두 보았습니다." : (b ? "다른 결말이 하나 더 있습니다." : "다른 결말이 하나 더 있습니다.\n…시간을 움직일 수 있다면.");
        retryButton.gameObject.SetActive(false);
        retryButton.onClick.AddListener(() =>
        {
            SoundManager.Play("click");
            GameState.retryChoice = true;
            SceneLoader.I.Load("Chapter04_WakeUp", 1.0f);
        });

        foreach (var g in new Graphic[] { background, label, title, body, record, other }) Hide(g);
        credits.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
        backButton.onClick.AddListener(() => { SoundManager.Play("click"); SceneLoader.I.Load("Title", 1.2f); });
        if (b) SoundManager.Amb("amb_facility", 0.25f); else SoundManager.Amb("amb_room", 0.35f);
        StartCoroutine(Play(b));
    }

    bool seenOther;

    static void Hide(Graphic g) { var c = g.color; c.a = 0f; g.color = c; }

    IEnumerator Play(bool b)
    {
        yield return new WaitForSeconds(1.0f);
        yield return UIFx.FadeGraphic(background, b ? 0.5f : 0.45f, 2.0f);   // 글자가 잘 보이게 배경은 어둡게
        SoundManager.Play(b ? "chime" : "buzz", 0.7f);
        yield return UIFx.FadeGraphic(label, 1f, 0.8f);
        if (!b) StartCoroutine(glitch.Burst(0.4f, 0.5f));
        yield return UIFx.FadeGraphic(title, 1f, 1.2f);
        yield return new WaitForSeconds(0.6f);
        yield return UIFx.FadeGraphic(body, 1f, 1.2f);
        yield return new WaitForSeconds(1.2f);
        yield return UIFx.FadeGraphic(record, 0.8f, 1.0f);
        yield return new WaitForSeconds(2.0f);

        // 글자를 치우고, 크레딧이 아래에서 위로 올라감 (다시 선택해서 본 엔딩이면 크레딧은 건너뜀)
        bool skipCredits = GameState.fromRetry;
        GameState.fromRetry = false;
        foreach (var g in new Graphic[] { label, body, record }) StartCoroutine(UIFx.FadeGraphic(g, 0f, 0.8f));
        yield return UIFx.FadeGraphic(title, 0f, 0.8f);
        credits.gameObject.SetActive(!skipCredits);
        Vector2 from = new Vector2(0, -900), to = new Vector2(0, 260);
        creditsRoll.anchoredPosition = from;
        for (float t = 0; t < 11f && !skipCredits; t += Time.deltaTime)
        {
            creditsRoll.anchoredPosition = Vector2.Lerp(from, to, t / 11f);
            yield return null;
        }
        creditsRoll.anchoredPosition = to;
        yield return UIFx.FadeGraphic(credits, 0f, 0.8f);
        StartCoroutine(UIFx.FadeGraphic(label, 1f, 0.8f));
        yield return UIFx.FadeGraphic(title, 1f, 1.0f);
        yield return UIFx.FadeGraphic(other, 0.85f, 1.0f);
        yield return new WaitForSeconds(0.6f);
        if (!seenOther)
        {
            retryButton.gameObject.SetActive(true);
            var rcg = retryButton.GetComponent<CanvasGroup>();
            rcg.alpha = 0f;
            StartCoroutine(UIFx.Fade(rcg, 1f, 0.6f));
        }
        backButton.gameObject.SetActive(true);
        var cg = backButton.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        yield return UIFx.Fade(cg, 1f, 0.6f);
    }
}
