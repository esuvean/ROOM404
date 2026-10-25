// =============================================================
//  Chapter 1 - ROOM 404 전체 퍼즐 진행 상태 관리
//
//  진행 순서 (기획서 28번)
//   잠긴 문 확인 → 커튼 스와이프 → 02:14 발견 → 열쇠 획득
//   → 열쇠 선택 후 서랍 오픈 → UV 손전등 획득 → UV 선택 후 거울 홀드
//   → THE CLOCK LIES → 손자국 → 거울 이동 → POWER 스위치 ON
//   → 전화 이벤트 → 번호키 0214 → 문 열림 → 복도 → 모든 방이 404
//
//  오브젝트를 탭하면 InteractableObject 가 OnTap("id") 를 부릅니다.
//  아래 switch 문에서 id 별로 무슨 일이 일어나는지 정합니다.
// =============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chapter01Manager : MonoBehaviour
{
    public static Chapter01Manager I;

    [Header("그림 (자동 연결됨)")]
    public List<Sprite> sprites = new List<Sprite>();

    // ---- 연결되는 부품들 ----
    RoomSwipeController room;
    CurtainController curtains;
    HoldReveal mirrorHold;
    MirrorController mirror;
    KeypadController keypad;
    DialogueManager dlg;
    InventoryManager inv;
    ZoomView zoom;
    ClueNotebook notes;
    GlitchEffect glitch;

    readonly Dictionary<string, Transform> map = new Dictionary<string, Transform>();

    // ---- 진행 상태 ----
    bool busy;
    bool curtainOpen, keyTaken, drawerOpen, uvTaken, memoTaken;
    bool uvRevealed, mirrorOpened, powerOn, phoneRinging, callDone, doorUnlocked;
    int clockTaps, photoTaps, mirrorTaps, uvShortTaps, doorTaps, handTaps, curtainTaps, drawerTaps, trapCount;

    // =========================================================
    void Awake()
    {
        I = this;
        TapRouter.Handler = OnTap;
        var canvas = FindFirstObjectByType<Canvas>();
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) map[t.name] = t;

        room = Get<RoomSwipeController>("RoomView");
        curtains = Get<CurtainController>("Curtains");
        mirrorHold = Get<HoldReveal>("MirrorGlass");
        mirror = Get<MirrorController>("Mirror");
        keypad = GetComponent<KeypadController>();
        dlg = GetComponent<DialogueManager>();
        inv = GetComponent<InventoryManager>();
        zoom = GetComponent<ZoomView>();
        notes = GetComponent<ClueNotebook>();
        glitch = GetComponent<GlitchEffect>();

        // 각 장치가 끝났을 때 알려주도록 연결
        curtains.onOpened = () => StartCoroutine(OnCurtainOpened());
        mirrorHold.canHold = () => inv.IsSelected("UV");
        mirrorHold.onRevealed = () => StartCoroutine(OnUVRevealed());
        keypad.onUnlocked = OnDoorUnlocked;
        keypad.onTrap = OnKeypadTrap;
        room.onWallChanged = OnWallChanged;

        Get<Button>("ArrowLeft").onClick.AddListener(() => room.Go(-1));
        Get<Button>("ArrowRight").onClick.AddListener(() => room.Go(1));
        Get<Button>("GearButton").onClick.AddListener(ToggleSound);
        UpdateGearIcon();
    }

    T Get<T>(string n) where T : Component => map[n].GetComponent<T>();
    GameObject GO(string n) => map[n].gameObject;
    Sprite S(string n) => sprites.Find(s => s != null && s.name == n);
    void Say(string line) => dlg.Say(line);

    void Start()
    {
        GameState.StartRun();
        SoundManager.Amb("amb_room", 0.5f);
        StartCoroutine(Intro());
    }

    // ---------------- 7. 시작 연출 ----------------
    IEnumerator Intro()
    {
        busy = true;
        var intro = Get<CanvasGroup>("IntroPanel");
        var introText = Get<Text>("IntroText");
        intro.gameObject.SetActive(true);
        intro.alpha = 1f;
        var c = introText.color; c.a = 0f; introText.color = c;
        yield return new WaitForSeconds(0.6f);
        yield return UIFx.FadeGraphic(introText, 1f, 1.2f);
        SoundManager.Play("heartbeat", 0.7f);
        yield return new WaitForSeconds(1.4f);
        yield return UIFx.FadeGraphic(introText, 0f, 0.8f);
        yield return UIFx.Fade(intro, 0f, 1.8f);
        intro.gameObject.SetActive(false);
        busy = false;
        Say("나|…여기가 어디지?");
    }

    void OnWallChanged(int index)
    {
        var dots = Get<Text>("WallDots");
        string s = "";
        for (int i = 0; i < 4; i++) s += (i == index ? "■" : "□") + (i < 3 ? " " : "");
        dots.text = s;
    }

    void ToggleSound()
    {
        SoundManager.SetMuted(!SoundManager.Muted);
        SoundManager.Play("click", 0.5f);
        UpdateGearIcon();
    }

    void UpdateGearIcon()
    {
        var img = Get<Image>("GearIcon");
        img.color = SoundManager.Muted ? new Color(1, 1, 1, 0.3f) : Color.white;
    }

    // =========================================================
    //  탭 처리
    // =========================================================
    public void OnTap(string id)
    {
        if (busy || dlg.IsBlocking) return;

        switch (id)
        {
            // ---------- 화면 1 : 침대 ----------
            case "clock":
                clockTaps++;
                notes.Add("clock", "알람시계 — 04:04 에서 멈춰 있다.");
                if (uvRevealed) Run(zoom.Show(S("zoom_clock"), null, "여전히 04:04.", "나|…거울은 시계를 믿지 말라고 했다."));
                else if (clockTaps == 1) Run(zoom.Show(S("zoom_clock"), null, "04시 04분에서 멈춰 있다."));
                else Run(zoom.Show(S("zoom_clock"), null, "고장 난 건가?"));
                break;

            case "photo":
                photoTaps++;
                notes.Add("photo", "가족사진 — 세 사람. 가운데 사람의 얼굴만 긁혀 있다. (APR. 14)");
                if (photoTaps == 1) Run(zoom.Show(S("zoom_photo"), null, "오래된 사진이다."));
                else if (photoTaps == 2) Run(zoom.Show(S("zoom_photo"), null, "가운데 사람의 얼굴만 심하게 긁혀 있다.", "나|…누구지?"));
                else Run(zoom.Show(S("zoom_photo"), null, "사진 아래에 작은 글씨로 APR. 14 라고 적혀 있다."));
                break;

            case "bed":
                Say("시트가 차갑다. 누군가 방금 일어난 것처럼 구겨져 있다.");
                break;

            case "lamp":
                Say("스탠드 불빛이 아주 미세하게 떨리고 있다.");
                break;

            // ---------- 화면 2 : 창문 ----------
            case "curtain":
                if (curtainOpen) break;
                curtainTaps++;
                if (curtainTaps == 1) Say("두꺼운 커튼이다.");
                else Say("두꺼운 커튼이다. …옆으로 걷어낼 수 있을 것 같다.");
                break;

            case "window":
                Say("창문인데 밖이 없다.");
                break;

            case "scratch":
                notes.Add("scratch", "창문 뒤 콘크리트 벽 — 누군가 긁어서 적은 숫자 02:14");
                Run(zoom.Show(S("zoom_scratch"), null, "누군가 벽을 긁어서 적어놓은 것 같다."));
                break;

            case "key":
                if (keyTaken) break;
                keyTaken = true;
                GO("Key").SetActive(false);
                inv.Add("KEY");
                SoundManager.Play("pickup");
                Say("작은 열쇠를 얻었다.");
                break;

            case "radiator":
                Say("라디에이터가 얼음처럼 차갑다. 한 번도 켜진 적이 없는 것 같다.");
                break;

            // ---------- 화면 4 : 출입문 ----------
            case "door":
                if (doorUnlocked) { StartCoroutine(Escape()); break; }
                doorTaps++;
                SoundManager.Play("tap", 0.8f);
                if (doorTaps == 1) Say("잠겨 있다.");
                else if (!powerOn) Say("잠겨 있다. 문 옆에 번호키가 달려 있다.");
                else Say("번호키에 비밀번호를 입력해야 열릴 것 같다.");
                break;

            case "keypad":
                if (doorUnlocked) Say("번호키에 UNLOCKED 라고 표시되어 있다.");
                else if (!powerOn) Say("전원이 들어오지 않는다.");
                else keypad.Open();
                break;

            case "evac":
                notes.Add("evac", "비상 대피도 — 이 층의 방 번호가 전부 404 로 적혀 있다.");
                Run(zoom.Show(S("zoom_evac"), null, "비상 대피도다.", "…이 층의 방 번호가 전부 404 다.", "나|인쇄가 잘못된 거겠지."));
                break;

            case "switchplate":
                Say(powerOn ? "방 불은 이미 켜져 있다." : "눌러봐도 아무 반응이 없다.");
                break;

            // ---------- 화면 3 : 거울 / 책상 ----------
            case "drawer":
                if (drawerOpen) break;
                drawerTaps++;
                if (inv.IsSelected("KEY")) { StartCoroutine(OpenDrawer()); break; }
                if (inv.Selected != null) { Say("여기엔 " + inv.Selected + "를 쓸 수 없을 것 같다."); break; }
                SoundManager.Play("tap", 0.8f);
                if (drawerTaps >= 2 && inv.Has("KEY")) Say("잠겨 있다. …아래쪽 인벤토리에서 열쇠를 선택해볼까?");
                else if (drawerTaps >= 2) Say("잠겨 있다. 작은 열쇠 구멍이 있다.");
                else Say("잠겨 있다.");
                break;

            case "desk":
                Say("책상 위에는 먼지만 쌓여 있다.");
                break;

            case "memo":
                if (!memoTaken)
                {
                    memoTaken = true;
                    notes.Add("memo", "서랍 속 메모 — \"거울은 거짓말하지 않는다.\"");
                }
                Run(zoom.Show(S("zoom_paper"), "거울은\n거짓말하지 않는다.", "낡은 메모다. 다른 설명은 없다."));
                break;

            case "uvitem":
                if (uvTaken) break;
                uvTaken = true;
                GO("UVItem").SetActive(false);
                inv.Add("UV");
                SoundManager.Play("pickup");
                Say("UV 손전등을 얻었다.");
                break;

            case "note":
                notes.Add("note", "거울에 붙은 쪽지 — \"Same Room. Different You.\"");
                Run(zoom.Show(S("zoom_paper"), "Same Room.\nDifferent You.", "거울 모서리에 꽂혀 있던 쪽지다.", "나|같은 방… 다른 나?"));
                break;

            case "mirror":
                if (uvRevealed) Say("거울에 보랏빛 글자가 남아 있다. THE CLOCK LIES");
                else if (inv.IsSelected("UV"))
                {
                    uvShortTaps++;
                    if (uvShortTaps >= 2) Say("특별한 건 보이지 않는다. …조금 더 오래 비춰봐야 할까?");
                    else Say("특별한 건 보이지 않는다.");
                }
                else if (inv.Selected != null) Say("여기엔 " + inv.Selected + "를 쓸 수 없을 것 같다.");
                else
                {
                    mirrorTaps++;
                    if (mirrorTaps == 1) Say("낡은 거울이다.");
                    else if (mirrorTaps == 2) Say("…거울 속 내가 아주 조금 늦게 따라 움직인 것 같다.");
                    else Say("나|기분 탓이겠지.");
                }
                break;

            case "handprint":
                StartCoroutine(Handprint());
                break;

            case "switch":
                if (!mirrorOpened) break;
                if (!powerOn) StartCoroutine(PowerOn());
                else Say("스위치는 켜져 있다.");
                break;

            case "phone":
                if (phoneRinging) StartCoroutine(PhoneCall());
                else if (callDone) Say("뚜— 뚜— 신호음만 들린다.");
                else Say("수화기를 들어봐도 아무 소리도 들리지 않는다.");
                break;
        }
    }

    void Run(IEnumerator co) { StartCoroutine(co); }

    // =========================================================
    //  퍼즐 이벤트
    // =========================================================
    IEnumerator OnCurtainOpened()
    {
        curtainOpen = true;
        notes.Add("window", "창문 — 밖이 없다. 바로 앞이 콘크리트 벽으로 막혀 있다.");
        yield return new WaitForSeconds(0.3f);
        SoundManager.Play("heartbeat", 0.5f);
        yield return dlg.Sequence("나|…?", "창문인데 밖이 없다.");
    }

    IEnumerator OpenDrawer()
    {
        busy = true;
        drawerOpen = true;
        inv.Remove("KEY");
        SoundManager.Play("unlock");
        Say("철컥.");
        yield return new WaitForSeconds(0.6f);
        SoundManager.Play("drawer");
        GO("DrawerClosed").SetActive(false);
        GO("DrawerOpen").SetActive(true);
        yield return UIFx.Punch(map["DrawerOpen"], 0.06f, 0.2f);
        busy = false;
        Say("서랍이 열렸다. 안에 무언가 들어 있다.");
    }

    IEnumerator OnUVRevealed()
    {
        uvRevealed = true;
        busy = true;
        SoundManager.Play("stinger", 0.6f);
        notes.Add("uv", "거울 (UV) — THE CLOCK LIES : 시계를 믿지 마.");
        yield return new WaitForSeconds(0.6f);
        yield return dlg.Sequence("THE CLOCK LIES", "…시계를 믿지 마.");
        inv.Deselect();
        // 거울 오른쪽 아래에 손자국이 새로 나타남
        var hand = Get<Image>("Handprint");
        hand.gameObject.SetActive(true);
        var c = hand.color; c.a = 0f; hand.color = c;
        SoundManager.Play("heartbeat", 0.6f);
        yield return UIFx.FadeGraphic(hand, 1f, 1.2f);
        busy = false;
        Say("…거울 오른쪽 아래에 작은 손자국이 떠올랐다.");
    }

    IEnumerator Handprint()
    {
        if (mirrorOpened) yield break;
        handTaps++;
        busy = true;
        if (handTaps == 1)
        {
            yield return mirror.Nudge();
            busy = false;
            Say("거울이 약간 움직인다.");
        }
        else
        {
            mirrorOpened = true;
            Get<Image>("Handprint").raycastTarget = false;
            Get<Image>("Note").raycastTarget = false;
            yield return mirror.SlideOpen();
            notes.Add("switch", "거울 뒤 — 숨겨진 POWER 스위치");
            busy = false;
            Say("거울 뒤에 무언가 숨겨져 있다.");
        }
    }

    IEnumerator PowerOn()
    {
        busy = true;
        powerOn = true;
        SoundManager.Play("switch");
        Get<Image>("Switch").sprite = S("obj_switch_on");
        Say("딸깍.");
        yield return new WaitForSeconds(0.4f);

        // 방의 조명이 한 번 꺼졌다 켜진다
        var dark = Get<Image>("DarkOverlay");
        SoundManager.Play("buzz");
        float[] seq = { 0.92f, 0.1f, 0.0f, 0.06f, 0.95f, 0.5f, 0.0f, 0.0f };
        for (int i = 0; i < seq.Length; i += 2)
        {
            SetAlpha(dark, seq[i]);
            yield return new WaitForSeconds(0.07f + seq[i + 1]);
        }
        SetAlpha(dark, 0.97f);
        SoundManager.StopAmb();
        yield return new WaitForSeconds(0.8f);
        SoundManager.Play("buzz", 0.6f);
        SetAlpha(dark, 0f);
        SoundManager.Amb("amb_room", 0.5f);
        GO("KeypadScreen").SetActive(true);
        yield return new WaitForSeconds(0.9f);

        // 전화벨이 울린다
        phoneRinging = true;
        SoundManager.Loop("phone_ring", 0.7f);
        StartCoroutine(PhoneWiggle());
        busy = false;
        Say("RRRRRR—");
    }

    IEnumerator PhoneWiggle()
    {
        var phone = Get<Image>("Phone");
        var rt = phone.rectTransform;
        Vector2 home = rt.anchoredPosition;
        while (phoneRinging)
        {
            phone.sprite = S("obj_phone_ring");
            for (float t = 0; t < 0.9f && phoneRinging; t += Time.deltaTime)
            {
                rt.anchoredPosition = home + new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f));
                yield return null;
            }
            phone.sprite = S("obj_phone");
            rt.anchoredPosition = home;
            for (float t = 0; t < 0.4f && phoneRinging; t += Time.deltaTime) yield return null;
        }
        phone.sprite = S("obj_phone");
        rt.anchoredPosition = home;
    }

    IEnumerator PhoneCall()
    {
        busy = true;
        phoneRinging = false;
        SoundManager.StopLoop();
        SoundManager.Play("phone_line");
        SoundManager.StopAmb();
        yield return new WaitForSeconds(0.3f);
        yield return dlg.Sequence(
            "…………",
            "잠시 아무 소리도 들리지 않는다.",
            "???|…또 찾았네.",
            "나|누구야?",
            "???|이번에는 얼마나 걸렸어?");
        SoundManager.Play("hangup");
        yield return dlg.Sequence("뚜— 뚜— 전화가 끊어졌다.");
        SoundManager.Amb("amb_room", 0.5f);
        callDone = true;
        notes.Add("phone", "전화 — \"…또 찾았네.\"  \"이번에는 얼마나 걸렸어?\"");
        busy = false;
        Say("나|…문 옆 번호키에 불이 들어온 것 같아.");
    }

    void OnKeypadTrap()
    {
        trapCount++;
        notes.Add("trap", "번호키에 0404 입력 — ERROR. 화면에 04:04 가 번쩍였다.");
        if (trapCount == 1) Say("나|…04:04 가 아니야.");
        else Say("나|시계를 믿지 말라고 했지.");
    }

    void OnDoorUnlocked()
    {
        doorUnlocked = true;
        GO("KeypadScreen").GetComponent<Image>().color = new Color(0.29f, 0.88f, 0.48f, 1f);
        SoundManager.Play("unlock");
        Say("철컥— 출입문의 잠금이 해제되었다.");
    }

    static void SetAlpha(Graphic g, float a) { var c = g.color; c.a = a; g.color = c; }

    // =========================================================
    //  25 ~ 26. 방 탈출 + 반전 연출
    // =========================================================
    IEnumerator Escape()
    {
        busy = true;
        room.locked = true;
        GO("InputBlocker").SetActive(true);
        dlg.Hide();

        // 문이 천천히 열린다
        SoundManager.Play("door_open");
        var door = Get<Image>("Door");
        door.sprite = S("obj_door_open");
        yield return new WaitForSeconds(1.4f);

        // 복도로 이동
        var cut = Get<CanvasGroup>("CutscenePanel");
        var persp = Get<Image>("CorridorPersp");
        var side = Get<Image>("CorridorSide");
        var flash = Get<Image>("Flash");
        cut.gameObject.SetActive(true);
        cut.alpha = 0f;
        persp.gameObject.SetActive(true);
        side.gameObject.SetActive(false);
        persp.transform.localScale = Vector3.one;
        SoundManager.Amb("amb_corridor", 0.6f);
        yield return UIFx.Fade(cut, 1f, 1.2f);
        yield return dlg.Auto("나|드디어…", 1.2f);
        dlg.Hide();

        // 복도 쪽으로 걸어 나감
        StartCoroutine(UIFx.Scale(persp.transform, Vector3.one * 1.18f, 2.2f));
        for (int i = 0; i < 4; i++) { SoundManager.Play("step", 0.8f); yield return new WaitForSeconds(0.5f); }
        yield return new WaitForSeconds(0.3f);

        // 방문이 뒤에서 자동으로 닫힌다 - 쾅
        SoundManager.Play("door_slam");
        SetAlpha(flash, 0.6f);
        StartCoroutine(UIFx.FadeGraphic(flash, 0f, 0.4f));
        StartCoroutine(UIFx.Shake((RectTransform)cut.transform, 0.5f, 14f));
        yield return dlg.Auto("쾅.", 1.0f);
        dlg.Hide();

        // 뒤를 돌아 방금 나온 문을 본다
        SetAlpha(flash, 1f); flash.color = Color.black;
        persp.gameObject.SetActive(false);
        side.gameObject.SetActive(true);
        var sideRt = side.rectTransform;
        sideRt.anchoredPosition = new Vector2(0, sideRt.anchoredPosition.y);
        yield return new WaitForSeconds(0.5f);
        yield return UIFx.FadeGraphic(flash, 0f, 0.8f);
        flash.color = new Color(1, 1, 1, 0);
        yield return dlg.Auto("방금 나온 문. ROOM 404.", 1.4f);

        // 카메라가 옆으로 이동 - 옆방도, 다음 방도 404
        float endX = -(sideRt.rect.width - 480f);
        StartCoroutine(UIFx.MoveLinear(sideRt, new Vector2(endX, sideRt.anchoredPosition.y), 6.5f));
        SoundManager.Play("heartbeat", 0.6f);
        yield return dlg.Auto("옆방도… 404.", 1.0f);
        SoundManager.Play("heartbeat", 0.7f);
        yield return dlg.Auto("다음 방도 404.", 0.9f);
        SoundManager.Play("heartbeat", 0.8f);
        yield return dlg.Auto("그다음 방도…… 404.", 0.8f);
        SoundManager.Play("stinger", 0.9f);
        yield return dlg.Auto("복도 끝까지, 모든 문이 ROOM 404 다.", 1.4f);
        dlg.Hide();
        yield return new WaitForSeconds(0.6f);
        yield return dlg.Auto("나|…뭐야.", 1.6f);
        dlg.Hide();

        // 잠시 정적 → 화면 암전
        SoundManager.StopAmb();
        yield return new WaitForSeconds(1.0f);
        GameState.EndRun();
        SceneLoader.I.Load("Chapter01_Clear", 1.6f);
    }
}
