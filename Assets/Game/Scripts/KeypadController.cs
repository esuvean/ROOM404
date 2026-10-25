// 번호 입력 및 정답 확인 (출입문 번호키)
//  - 숫자 버튼(UI Button)을 누르면 화면 위쪽에 숫자가 표시됩니다.
//  - 0404 : ERROR + 노이즈 + 04:04 깜빡임 (함정)
//  - 0214 : UNLOCKED (정답)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KeypadController : MonoBehaviour
{
    public GameObject panel;
    public RectTransform body;
    public Text display;
    public Button[] digits;          // 0 ~ 9
    public Button backButton, enterButton, closeButton;
    public TapCatcher dimCatcher;
    public GlitchEffect glitch;

    public string answer = "0214";
    public string trap = "0404";
    public int length = 4;
    public string trapFlash = "04:04";
    public bool unlocked;
    public Action onUnlocked;
    public Action onTrap;

    static readonly Color Green = new Color(0.29f, 0.88f, 0.48f);
    static readonly Color Red = new Color(1f, 0.25f, 0.2f);

    string input = "";
    bool busy;

    void Awake()
    {
        for (int i = 0; i < digits.Length; i++)
        {
            string d = i.ToString();
            digits[i].onClick.AddListener(() => Press(d));
        }
        backButton.onClick.AddListener(Back);
        enterButton.onClick.AddListener(Enter);
        closeButton.onClick.AddListener(Close);
        if (dimCatcher != null) dimCatcher.onTap += Close;
        panel.SetActive(false);
    }

    public void Open()
    {
        panel.SetActive(true);
        input = "";
        busy = false;
        UpdateDisplay();
        StartCoroutine(UIFx.Punch(body, 0.05f, 0.2f));
    }

    public void Close()
    {
        if (busy) return;
        SoundManager.Play("click", 0.5f);
        panel.SetActive(false);
    }

    void Press(string d)
    {
        if (busy || unlocked) return;
        SoundManager.Play("beep", 0.8f);
        if (input.Length >= length) return;
        input += d;
        UpdateDisplay();
    }

    void Back()
    {
        if (busy || unlocked || input.Length == 0) return;
        SoundManager.Play("click", 0.5f);
        input = input.Substring(0, input.Length - 1);
        UpdateDisplay();
    }

    void Enter()
    {
        if (busy || unlocked) return;
        SoundManager.Play("beep", 0.8f);
        if (input == answer) StartCoroutine(Success());
        else if (input == trap) StartCoroutine(Trap());
        else StartCoroutine(Error());
    }

    void UpdateDisplay()
    {
        display.color = Green;
        display.text = input.PadRight(length, '_');
    }

    IEnumerator Success()
    {
        busy = true;
        unlocked = true;
        yield return new WaitForSeconds(0.15f);
        SoundManager.Play("success");
        display.color = Green;
        display.text = "UNLOCKED";
        yield return UIFx.Punch(body, 0.04f, 0.25f);
        yield return new WaitForSeconds(1.1f);
        busy = false;
        panel.SetActive(false);
        onUnlocked?.Invoke();
    }

    IEnumerator Error()
    {
        busy = true;
        SoundManager.Play("error");
        display.color = Red;
        display.text = "ERROR";
        yield return UIFx.Shake(body, 0.35f, 10f);
        yield return new WaitForSeconds(0.5f);
        input = "";
        UpdateDisplay();
        busy = false;
    }

    IEnumerator Trap()
    {
        busy = true;
        SoundManager.Play("error");
        SoundManager.Play("static", 0.8f);
        display.color = Red;
        display.text = "ERROR";
        if (glitch != null) StartCoroutine(glitch.Burst(0.9f, 0.6f));
        StartCoroutine(UIFx.Shake(body, 0.6f, 14f));
        for (int i = 0; i < 6; i++)
        {
            display.text = i % 2 == 0 ? trapFlash : "ERROR";
            yield return new WaitForSeconds(i % 2 == 0 ? 0.12f : 0.18f);
        }
        display.text = trapFlash;
        yield return new WaitForSeconds(0.7f);
        input = "";
        UpdateDisplay();
        busy = false;
        onTrap?.Invoke();
    }
}
