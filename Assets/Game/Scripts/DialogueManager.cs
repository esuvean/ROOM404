// 독백 / 전화 대사 출력
//  - Say("문장")           : 잠깐 보여주고 저절로 사라지는 혼잣말
//  - Sequence("나|문장"...) : 탭해서 넘기는 대사 (전화 이벤트 등)
//  - Auto("문장", 시간)     : 연출용, 탭 없이 자동으로 넘어가는 대사
//  "말하는사람|내용" 형식으로 쓰면 이름이 표시됩니다.
//  말하는 사람마다 글자 소리가 다릅니다.
//   나 / 설명        → blip (기본)
//   ???  (전화 속)   → voice_phone (낮고 두꺼운 소리, 조금 느리게)
//   방송             → voice_radio (금속성 스피커 소리)
//   404-09, 404-10  → voice_other (조금 낮고 흐릿한 '또 다른 나')
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager I;

    public CanvasGroup panel;
    public Text speakerText;
    public Text bodyText;
    public Text nextMark;
    public GameObject blocker;    // 대사를 넘길 때 화면 전체 탭을 받는 투명 판
    public float typeSpeed = 0.035f;

    public bool IsBlocking { get; private set; }

    Coroutine sayCo;
    bool tapped;

    string voice = "blip";
    int voiceVariants = 1, voiceEvery = 2;
    float voiceVolume = 0.6f, voiceSpeed = 1f;

    void SetVoice(string sp)
    {
        if (sp == "???") { voice = "voice_phone"; voiceVariants = 3; voiceEvery = 3; voiceVolume = 0.85f; voiceSpeed = 1.5f; }
        else if (sp == "방송") { voice = "voice_radio"; voiceVariants = 2; voiceEvery = 2; voiceVolume = 0.6f; voiceSpeed = 1.15f; }
        else if (sp != null && sp.StartsWith("404-")) { voice = "voice_other"; voiceVariants = 2; voiceEvery = 2; voiceVolume = 0.6f; voiceSpeed = 1.1f; }
        else { voice = "blip"; voiceVariants = 1; voiceEvery = 2; voiceVolume = 0.6f; voiceSpeed = 1f; }
    }

    void Awake()
    {
        I = this;
        panel.alpha = 0f;
        blocker.SetActive(false);
        nextMark.enabled = false;
        var tc = blocker.GetComponent<TapCatcher>();
        if (tc == null) tc = blocker.AddComponent<TapCatcher>();
        tc.onTap += () => tapped = true;
    }

    void Update()
    {
        if (nextMark.enabled)
        {
            var c = nextMark.color;
            c.a = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            nextMark.color = c;
        }
    }

    static void Parse(string raw, out string speaker, out string body)
    {
        int i = raw.IndexOf('|');
        if (i >= 0) { speaker = raw.Substring(0, i); body = raw.Substring(i + 1); }
        else { speaker = null; body = raw; }
    }

    void SetSpeaker(string sp)
    {
        SetVoice(sp);
        if (string.IsNullOrEmpty(sp)) { speakerText.text = ""; return; }
        speakerText.text = sp;
        speakerText.color = sp == "???" ? new Color(0.72f, 0.55f, 1f) : new Color(0.89f, 0.76f, 0.48f);
    }

    IEnumerator Type(string body, bool skippable)
    {
        bodyText.text = "";
        for (int i = 0; i < body.Length; i++)
        {
            if (skippable && tapped) { tapped = false; break; }
            bodyText.text = body.Substring(0, i + 1);
            if (i % voiceEvery == 0 && body[i] != ' ' && body[i] != '.' && body[i] != '…')
                SoundManager.Play(voiceVariants > 1 ? voice + Random.Range(1, voiceVariants + 1) : voice, voiceVolume);
            yield return new WaitForSeconds(typeSpeed * voiceSpeed);
        }
        bodyText.text = body;
    }

    // ---------- 잠깐 보였다 사라지는 혼잣말 ----------
    public void Say(string raw)
    {
        if (IsBlocking) return;
        if (sayCo != null) StopCoroutine(sayCo);
        sayCo = StartCoroutine(SayCo(raw));
    }

    IEnumerator SayCo(string raw)
    {
        Parse(raw, out var sp, out var body);
        SetSpeaker(sp);
        nextMark.enabled = false;
        panel.alpha = 1f;
        yield return Type(body, false);
        yield return new WaitForSeconds(1.8f + body.Length * 0.06f);
        yield return UIFx.Fade(panel, 0f, 0.3f);
        sayCo = null;
    }

    // ---------- 탭해서 넘기는 대사 ----------
    public IEnumerator Sequence(params string[] lines)
    {
        if (sayCo != null) { StopCoroutine(sayCo); sayCo = null; }
        IsBlocking = true;
        blocker.SetActive(true);
        panel.alpha = 1f;
        foreach (var raw in lines)
        {
            Parse(raw, out var sp, out var body);
            SetSpeaker(sp);
            tapped = false;
            nextMark.enabled = false;
            yield return Type(body, true);
            tapped = false;
            nextMark.enabled = true;
            while (!tapped) yield return null;
            nextMark.enabled = false;
            SoundManager.Play("click", 0.3f);
        }
        blocker.SetActive(false);
        IsBlocking = false;
        panel.alpha = 0f;
    }

    // ---------- 연출용 자동 대사 ----------
    public IEnumerator Auto(string raw, float hold)
    {
        if (sayCo != null) { StopCoroutine(sayCo); sayCo = null; }
        Parse(raw, out var sp, out var body);
        SetSpeaker(sp);
        nextMark.enabled = false;
        panel.alpha = 1f;
        yield return Type(body, false);
        yield return new WaitForSeconds(hold);
    }

    public void Hide()
    {
        if (sayCo != null) { StopCoroutine(sayCo); sayCo = null; }
        panel.alpha = 0f;
    }
}
