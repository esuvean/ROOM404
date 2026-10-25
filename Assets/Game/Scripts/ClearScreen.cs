// 27. Chapter 1 결과 화면 + 다음 챕터 → 추후 제작 → 돌아가기
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClearScreen : MonoBehaviour
{
    public CanvasGroup clearGroup;
    public Text chapterText, clearText, timeCodeText, recordText;
    public Button nextButton;

    public CanvasGroup walkGroup;
    public Image corridor;
    public Text walkTitle;

    public GlitchEffect glitch;

    public CanvasGroup soonGroup;
    public Text soonTitle, soonSub;
    public Button backButton;

    bool goingNext;

    void Start()
    {
        SoundManager.StopAmb();
        SoundManager.StopLoop();
        clearGroup.alpha = 1f;
        walkGroup.alpha = 0f; walkGroup.gameObject.SetActive(false);
        soonGroup.alpha = 0f; soonGroup.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        Hide(chapterText); Hide(clearText); Hide(timeCodeText); Hide(recordText);

        float sec = GameState.clearSeconds;
        if (sec >= 0)
        {
            int m = Mathf.FloorToInt(sec / 60f), s = Mathf.FloorToInt(sec % 60f);
            recordText.text = m > 0 ? $"이번에는 {m}분 {s}초 걸렸다." : $"이번에는 {s}초 걸렸다.";
        }
        else recordText.text = "";

        nextButton.onClick.AddListener(() => { if (!goingNext) StartCoroutine(GoNext()); });
        backButton.onClick.AddListener(() =>
        {
            SoundManager.Play("click");
            SceneLoader.I.Load("Title", 1.0f);
        });
        StartCoroutine(ShowClear());
    }

    static void Hide(Graphic g) { var c = g.color; c.a = 0f; g.color = c; }

    IEnumerator ShowClear()
    {
        yield return new WaitForSeconds(1.2f);
        SoundManager.Play("heartbeat", 0.5f);
        yield return UIFx.FadeGraphic(chapterText, 1f, 0.8f);
        yield return UIFx.FadeGraphic(clearText, 1f, 1.0f);
        yield return new WaitForSeconds(0.8f);
        SoundManager.Play("blip", 1f);
        yield return UIFx.FadeGraphic(timeCodeText, 1f, 0.6f);
        yield return new WaitForSeconds(0.6f);
        yield return UIFx.FadeGraphic(recordText, 0.6f, 1.2f);
        yield return new WaitForSeconds(1.0f);
        nextButton.gameObject.SetActive(true);
        var cg = nextButton.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        yield return UIFx.Fade(cg, 1f, 0.6f);
    }

    void Update()
    {
        if (nextButton.gameObject.activeSelf && !goingNext)
        {
            var t = nextButton.GetComponentInChildren<Text>();
            var c = t.color; c.a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 2f)); t.color = c;
        }
    }

    // 복도를 따라 다음 챕터로 향하지만…
    IEnumerator GoNext()
    {
        goingNext = true;
        SoundManager.Play("click");
        nextButton.interactable = false;
        yield return UIFx.Fade(clearGroup, 0f, 0.8f);
        clearGroup.gameObject.SetActive(false);

        walkGroup.gameObject.SetActive(true);
        corridor.transform.localScale = Vector3.one;
        SoundManager.Amb("amb_corridor", 0.6f);
        yield return UIFx.Fade(walkGroup, 1f, 1.0f);

        // 걸어가는 연출
        StartCoroutine(UIFx.Scale(corridor.transform, Vector3.one * 1.45f, 4.2f));
        for (int i = 0; i < 7; i++)
        {
            SoundManager.Play("step", 0.8f);
            if (i == 4) corridor.color = new Color(0.4f, 0.4f, 0.4f);
            if (i == 5) corridor.color = Color.white;
            yield return new WaitForSeconds(0.55f);
        }

        // 갑자기 화면이 깨짐
        SoundManager.StopAmb();
        SoundManager.Play("static", 1f);
        SoundManager.Play("error", 0.6f);
        StartCoroutine(glitch.Burst(1.1f, 1f));
        for (int i = 0; i < 8; i++)
        {
            corridor.color = i % 2 == 0 ? new Color(1f, 0.3f, 0.3f) : Color.white;
            walkTitle.text = i % 2 == 0 ? "CHAPTER 2" : "CH4PT3R_?";
            yield return new WaitForSeconds(0.08f);
        }
        walkGroup.alpha = 0f;
        walkGroup.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.9f);

        // 추후 제작
        soonGroup.gameObject.SetActive(true);
        SoundManager.Play("beep", 0.6f);
        yield return UIFx.Fade(soonGroup, 1f, 0.5f);
    }
}
