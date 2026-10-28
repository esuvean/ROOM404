// 타이틀 화면 : TAP TO START
using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public Text tapText;
    public TapCatcher catcher;
    public Image bg;
    public Text logo;        // ENDING B 를 본 뒤에는 04:05 로 바뀜
    bool started;

    void Start()
    {
        SoundManager.Amb("amb_corridor", 0.45f);
        if (logo != null && PlayerPrefs.GetInt("0404_endingB", 0) == 1) logo.text = "04:05";
        catcher.onTap += OnTap;
    }

    void Update()
    {
        var c = tapText.color;
        c.a = started ? 0f : 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 2.2f));
        tapText.color = c;
        // 복도 조명이 가끔 깜빡임
        if (bg != null) bg.color = Random.value < 0.015f ? new Color(0.55f, 0.55f, 0.55f) : Color.white;
    }

    void OnTap()
    {
        if (started) return;
        started = true;
        SoundManager.Play("click");
        SoundManager.Play("heartbeat", 0.6f);
        SceneLoader.I.Load("Chapter01_Room404", 1.2f);
    }
}
