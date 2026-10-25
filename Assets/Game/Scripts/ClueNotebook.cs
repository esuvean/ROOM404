// 단서 노트 (오른쪽 위 노트 아이콘)
//  - 지금까지 발견한 단서를 정리해서 보여줍니다. 추리할 때 참고!
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClueNotebook : MonoBehaviour
{
    public static ClueNotebook I;
    public GameObject panel;
    public Text listText;
    public Button openButton, closeButton;
    public TapCatcher dimCatcher;
    public RectTransform icon;
    public Image newDot;

    readonly List<string> keys = new List<string>();
    readonly List<string> lines = new List<string>();

    void Awake()
    {
        I = this;
        panel.SetActive(false);
        newDot.enabled = false;
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        if (dimCatcher != null) dimCatcher.onTap += Close;
    }

    public void Add(string key, string line)
    {
        if (keys.Contains(key)) return;
        keys.Add(key);
        lines.Add(line);
        newDot.enabled = true;
        StartCoroutine(UIFx.Punch(icon, 0.35f, 0.35f));
    }

    public bool HasClue(string key) => keys.Contains(key);

    void Open()
    {
        SoundManager.Play("click", 0.6f);
        newDot.enabled = false;
        listText.text = lines.Count == 0 ? "아직 기록된 단서가 없다." : "· " + string.Join("\n· ", lines);
        panel.SetActive(true);
    }

    void Close()
    {
        SoundManager.Play("click", 0.4f);
        panel.SetActive(false);
    }
}
