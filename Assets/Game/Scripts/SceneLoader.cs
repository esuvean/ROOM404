// Title / Chapter / Clear 씬 전환 (검은 화면 페이드 포함)
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader I;
    public CanvasGroup fade;          // 화면 전체를 덮는 검은 판
    public bool fadeInOnStart = true; // 씬이 시작될 때 밝아질지
    public float fadeInTime = 1.0f;
    bool loading;

    void Awake()
    {
        I = this;
        if (fade != null)
        {
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            if (fadeInOnStart) StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        yield return null;
        yield return UIFx.Fade(fade, 0f, fadeInTime);
        fade.blocksRaycasts = false;
    }

    public void ShowImmediate(bool black)
    {
        fade.alpha = black ? 1f : 0f;
        fade.blocksRaycasts = black;
    }

    public void Load(string sceneName, float fadeTime = 0.8f)
    {
        if (loading) return;
        loading = true;
        StartCoroutine(LoadCo(sceneName, fadeTime));
    }

    IEnumerator LoadCo(string sceneName, float fadeTime)
    {
        fade.blocksRaycasts = true;
        if (fade.alpha < 1f) yield return UIFx.Fade(fade, 1f, fadeTime);
        SceneManager.LoadScene(sceneName);
    }
}
