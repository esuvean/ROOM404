// =============================================================
//  Chapter 3 - OBSERVATION (관찰동) 전체 퍼즐 진행 관리
//
//  화면: B4 복도 → 연구실 → 기록 보관실 → 관찰실 문 (끝에서 멈춤)
//
//  진행
//   연구실 메모 "비밀번호는 내 생일" → 가운을 옆으로 스와이프 → 가족사진 APR. 14
//   → PC 로그인 0414 (0404 는 함정) → 메일 "출입 카드는 본인 파일 서랍에" + 보관함 잠금 해제
//   → UV 손전등으로 보관함을 홀드 → 404-07 서랍에만 손자국 → 출입 카드
//   → 카드 리더기 → 지문 스캐너 2초 홀드 → "WELCOME BACK, OBSERVER 404-07"
//   → 관찰실: 모니터 속 ROOM 404 의 '나' 에게 전화를 건다 (Chapter 1 의 그 전화)
//   → CAM 00 에 비친 건 관찰실의 내 뒷모습 → "이번에는 얼마나 걸렸어?" → CHAPTER 3 CLEAR
// =============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chapter03Manager : MonoBehaviour
{
    public static Chapter03Manager I;

    [Header("그림 (자동 연결됨)")]
    public List<Sprite> sprites = new List<Sprite>();

    RoomSwipeController room;
    DialogueManager dlg;
    InventoryManager inv;
    ZoomView zoom;
    ClueNotebook notes;
    GlitchEffect glitch;
    KeypadController keypad;
    HoldReveal cabinetUV;
    HoldLever scanner;
    SwipeMover coat;

    readonly Dictionary<string, Transform> map = new Dictionary<string, Transform>();

    // ---- 진행 상태 ----
    bool busy;
    bool mirrorSeen, stickySeen, photoSeen, coatMoved, loggedIn, uvRevealed;
    bool cardTaken, cardOk, scanned, waitPhone, phoneTapped;
    int coatTaps, trapCount, uvShortTaps, lockedTaps, wrongOpened, mirrorTaps, doorTaps;
    readonly HashSet<string> opened = new HashSet<string>();

    Coroutine cctvCo, phoneCo;

    float idle;
    int hintStage = -1, hintLevel;

    // =========================================================
    void Awake()
    {
        I = this;
        TapRouter.Handler = OnTap;
        var canvas = FindFirstObjectByType<Canvas>();
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) map[t.name] = t;

        room = Get<RoomSwipeController>("LabView");
        dlg = GetComponent<DialogueManager>();
        inv = GetComponent<InventoryManager>();
        zoom = GetComponent<ZoomView>();
        notes = GetComponent<ClueNotebook>();
        glitch = GetComponent<GlitchEffect>();
        keypad = GetComponent<KeypadController>();
        cabinetUV = Get<HoldReveal>("CabinetHit");
        scanner = Get<HoldLever>("ScannerHit");
        coat = Get<SwipeMover>("Coat");

        room.onWallChanged = OnWallChanged;
        room.onBlocked = d => Say(d < 0 ? "뒤쪽은 방금 들어온 비상구뿐이다." : "더 갈 곳이 없다. …관찰실 문뿐이다.");
        coat.onDone = () => StartCoroutine(OnCoatMoved());
        cabinetUV.canHold = () => inv.IsSelected("UV");
        cabinetUV.onRevealed = () => StartCoroutine(OnUVRevealed());
        keypad.onUnlocked = () => StartCoroutine(OnLogin());
        keypad.onTrap = OnKeypadTrap;
        scanner.canHold = () => cardOk && !scanned;
        scanner.onDone = () => StartCoroutine(OnScanned());
        scanner.onRelease = () => Say("손가락을 떼자 스캔이 취소되었다.");

        Get<Button>("ArrowLeft").onClick.AddListener(() => room.Go(-1));
        Get<Button>("ArrowRight").onClick.AddListener(() => room.Go(1));
        Get<Button>("GearButton").onClick.AddListener(ToggleSound);
        Get<Button>("HintButton").onClick.AddListener(OnHint);
        UpdateGearIcon();
    }

    T Get<T>(string n) where T : Component => map[n].GetComponent<T>();
    GameObject GO(string n) => map[n].gameObject;
    Sprite S(string n) => sprites.Find(s => s != null && s.name == n);
    void Say(string line) => dlg.Say(line);
    void Run(IEnumerator co) => StartCoroutine(co);
    static void SetAlpha(Graphic g, float a) { var c = g.color; c.a = a; g.color = c; }

    void Start()
    {
        GameState.StartRun();
        SoundManager.StopLoop();
        SoundManager.Amb("amb_facility", 0.55f);
        inv.Add("UV");   // UV 손전등은 계속 가지고 있다
        cctvCo = StartCoroutine(CctvBlink());
        StartCoroutine(Intro());
    }

    // ---------------- 시작 연출 ----------------
    IEnumerator Intro()
    {
        busy = true;
        var intro = Get<CanvasGroup>("IntroPanel");
        var t1 = Get<Text>("IntroText");
        var t2 = Get<Text>("IntroSub");
        intro.gameObject.SetActive(true);
        intro.alpha = 1f;
        SetAlpha(t1, 0f); SetAlpha(t2, 0f);
        yield return new WaitForSeconds(0.5f);
        SoundManager.Play("static", 0.6f);
        StartCoroutine(glitch.Burst(0.35f, 0.5f));
        yield return new WaitForSeconds(0.5f);
        yield return UIFx.FadeGraphic(t1, 1f, 0.8f);
        yield return UIFx.FadeGraphic(t2, 1f, 0.6f);
        yield return new WaitForSeconds(1.4f);
        StartCoroutine(UIFx.FadeGraphic(t2, 0f, 0.6f));
        yield return UIFx.FadeGraphic(t1, 0f, 0.6f);
        SoundManager.Play("buzz", 0.5f);
        yield return UIFx.Fade(intro, 0f, 1.6f);
        intro.gameObject.SetActive(false);

        yield return dlg.Sequence(
            "나|…뒤에서 문이 잠겼어.",
            "나|여긴 모텔이 아니야. 무슨… 연구소 같은데.",
            "나|아까 그 모니터 속 사람들. 다 나랑 똑같이 생겼었어.",
            "나|누가… 나를 보고 있는 거지?");

        var find = Get<Text>("FindExit");
        find.gameObject.SetActive(true);
        SetAlpha(find, 0f);
        SoundManager.Play("blip", 1f);
        yield return UIFx.FadeGraphic(find, 1f, 0.5f);
        yield return new WaitForSeconds(1.6f);
        yield return UIFx.FadeGraphic(find, 0f, 0.8f);
        find.gameObject.SetActive(false);
        busy = false;
        idle = 0f;
    }

    void OnWallChanged(int index)
    {
        string s = "";
        for (int i = 0; i < 4; i++) s += (i == index ? "■" : "□") + (i < 3 ? " " : "");
        Get<Text>("WallDots").text = s;
        string[] names = { "B4 · HALL", "B4 · LAB", "B4 · ARCHIVE", "B4 · OBSERVATION" };
        Get<Text>("RoomTitle").text = names[index];
        GO("ArrowLeft").SetActive(index > 0);
        GO("ArrowRight").SetActive(index < 3);
    }

    void ToggleSound()
    {
        SoundManager.SetMuted(!SoundManager.Muted);
        SoundManager.Play("click", 0.5f);
        UpdateGearIcon();
    }

    void UpdateGearIcon()
    {
        Get<Image>("GearIcon").color = SoundManager.Muted ? new Color(1, 1, 1, 0.3f) : Color.white;
    }

    static string Rec(string scene)
    {
        if (!GameState.records.TryGetValue(scene, out var s)) return "--:--";
        return $"{Mathf.FloorToInt(s / 60f):00}:{Mathf.FloorToInt(s % 60f):00}";
    }

    // =========================================================
    //  탭 처리
    // =========================================================
    public void OnTap(string id)
    {
        // 마지막 연출에서 수화기를 드는 순간
        if (id == "obsphone") { if (waitPhone) phoneTapped = true; return; }
        if (busy || dlg.IsBlocking) return;
        idle = 0f;

        switch (id)
        {
            // ---------- 화면 1 : B4 복도 ----------
            case "backdoor":
                SoundManager.Play("tap", 0.8f);
                Say("방금 들어온 비상구다. 이쪽에는 손잡이조차 없다.");
                break;

            case "mirror":
                Run(Mirror());
                break;

            case "log":
                notes.Add("log", "출입 기록표 — 404-07 STAGE 3 통과 / 404-09 STAGE 3 진행 중");
                Run(zoom.Show(S("zoom_note"),
                    "B4 관찰동 출입 기록\n\n404-07  STAGE 3  통과\n404-08  STAGE 1  중단\n404-09  STAGE 1  " + Rec("Chapter01_Room404") +
                    "\n404-09  STAGE 2  " + Rec("Chapter02_Corridor") + "\n404-09  STAGE 3  진행 중",
                    "벽에 걸린 출입 기록표다.",
                    "나|404-09… STAGE 1, STAGE 2…",
                    GameState.records.ContainsKey("Chapter01_Room404")
                        ? "나|…적힌 시간이, 내가 그 방이랑 복도에서 보낸 시간이랑 똑같아."
                        : "나|STAGE 3 진행 중. …지금 누가 하고 있다는 거지?"));
                break;

            case "cooler":
                SoundManager.Play("tap", 0.6f);
                Say("정수기다. 옆에 쌓인 종이컵마다 '404' 가 적혀 있다.");
                break;

            case "sign":
                Say("OBSERVATION → 화살표가 복도 안쪽을 가리킨다.");
                break;

            // ---------- 화면 2 : 연구실 ----------
            case "board":
                notes.Add("board", "화이트보드 — STAGE 1 ✓ · STAGE 2 ✓ · STAGE 3 ?  (회차 표시 9개)");
                Run(zoom.Show(S("zoom_note"), "관찰 일지\n\nSTAGE 1 · ROOM    ✓\nSTAGE 2 · LOOP    ✓\nSTAGE 3 · ????    \n\n회차  ||||| ||||",
                    "화이트보드에 무언가 정리되어 있다.",
                    "나|STAGE 1 은 그 방… STAGE 2 는 그 복도.",
                    "나|그럼 STAGE 3 는… 여기야?",
                    "나|회차 표시가 아홉 개. …아홉 번째라는 거야?"));
                break;

            case "pc":
                if (loggedIn) { Run(ShowMail(false)); break; }
                SoundManager.Play("beep", 0.6f);
                keypad.Open();
                break;

            case "sticky":
                stickySeen = true;
                notes.Add("sticky", "모니터 메모 — \"비밀번호는 내 생일. 사진에 적어 뒀잖아. — 나\"");
                Run(zoom.Show(S("zoom_paper"), "비밀번호는 내 생일.\n사진에 적어 뒀잖아.\n\n— 나",
                    "모니터 옆에 붙은 노란 메모다.",
                    "나|…이 글씨.",
                    "나|내 글씨랑 똑같아."));
                break;

            case "mug":
                SoundManager.Play("tap", 0.6f);
                Say("식은 커피. 컵에 이름이 적혀 있었는데… 누군가 지워 버렸다.");
                break;

            case "desk":
                Say("서랍은 비어 있다. 연필 한 자루 없다.");
                break;

            case "coat":
                if (coatMoved) { Say("옆으로 치워 둔 연구원 가운이다."); break; }
                coatTaps++;
                SoundManager.Play("tap", 0.6f);
                if (coatTaps == 1) Say("하얀 연구원 가운이다. 명찰 자리는 비어 있다.");
                else Say("가운 뒤에 뭔가 걸려 있는 것 같다. …옆으로 밀어 볼까?");
                break;

            case "photo":
                if (!coatMoved) break;
                photoSeen = true;
                notes.Add("photo", "가운 뒤 가족사진 — 가운데 얼굴은 나. 날짜 APR. 14 (내 생일?)");
                Run(zoom.Show(S("zoom_photo3"), null,
                    "ROOM 404 에 있던 것과 똑같은 가족사진이다.",
                    "그런데 이번엔… 가운데 얼굴이 긁혀 있지 않다.",
                    "나|…이거, 나잖아.",
                    "사진 아래에 날짜가 적혀 있다. APR. 14",
                    stickySeen ? "나|내 생일… 4월 14일." : "나|4월 14일. 무슨 날이지?"));
                break;

            // ---------- 화면 3 : 기록 보관실 ----------
            case "cabinet":
                if (inv.IsSelected("UV") && !uvRevealed) { UVShort(); break; }
                Say(loggedIn ? "피험자 파일 보관함이다. 서랍마다 번호가 붙어 있다." : "피험자 파일 보관함이다. 전자 잠금이 걸려 있다.");
                break;

            case "lamp":
                Say(loggedIn ? "잠금 표시등이 초록색이다." : "잠금 표시등이 빨갛게 켜져 있다. …어딘가에서 해제해야 할 것 같다.");
                break;

            case "d4": case "d5": case "d6": case "d7": case "d8": case "d9":
                Drawer(id);
                break;

            case "box":
                SoundManager.Play("tap", 0.6f);
                Say("상자마다 '404' 라고 적혀 있다. 안에는 똑같은 파일 묶음뿐이다.");
                break;

            // ---------- 화면 4 : 관찰실 문 ----------
            case "door":
                if (scanned) { StartCoroutine(Finale()); break; }
                doorTaps++;
                SoundManager.Play("tap", 0.8f);
                if (doorTaps == 1) Say("OBSERVATION — STAFF ONLY. 굳게 잠겨 있다.");
                else Say("문 옆에 카드 리더기와 지문 스캐너가 있다.");
                break;

            case "reader":
                if (cardOk) { Say("카드는 이미 확인되었다."); break; }
                if (inv.IsSelected("CARD")) { Run(OnCardAccepted()); break; }
                if (inv.Selected != null) { Say("여기엔 " + inv.Selected + "를 쓸 수 없을 것 같다."); break; }
                Say(inv.Has("CARD") ? "출입 카드 리더기다. …아이템 칸에서 카드를 골라서 대 보자." : "출입 카드 리더기다. 카드가 필요하다.");
                break;

            case "scanner":
                if (scanned) { Say("스캐너 화면: WELCOME BACK."); break; }
                if (!cardOk) { Say("지문 스캐너다. 불이 꺼져 있다."); break; }
                Say("손가락을 대고… 꾹 누르고 있어야 할 것 같다.");
                break;

            case "cctv":
                Say("빨간 불이 깜빡인다. 아까부터 계속 나를 따라 보고 있다.");
                break;
        }
    }

    // ---------------- 거울 ----------------
    IEnumerator Mirror()
    {
        mirrorTaps++;
        if (mirrorSeen)
        {
            Say(mirrorTaps == 2 ? "나|…평범한 내 모습이야. 아까 그건 뭐였지?" : "거울은 거짓말하지 않는다… 였던가.");
            yield break;
        }
        mirrorSeen = true;
        busy = true;
        SoundManager.Play("mirror_slide", 0.4f);
        yield return zoom.Show(S("zoom_mirror"), null,
            "벽에 붙은 거울이다.",
            "거울 속의 나는… 하얀 연구원 가운을 입고 있다.",
            "나|…?");
        SoundManager.Play("static", 0.7f);
        yield return glitch.Burst(0.4f, 0.7f);
        notes.Add("mirror", "B4 복도 거울 — 거울 속 나는 연구원 가운을 입고 있었다. (명찰 07?)");
        busy = false;
        yield return dlg.Sequence("다시 보니, 평범한 내 모습이다.", "나|…피곤해서 그런 거겠지.");
    }

    // ---------------- 연구실 ----------------
    IEnumerator OnCoatMoved()
    {
        coatMoved = true;
        idle = 0f;
        yield return new WaitForSeconds(0.2f);
        Say("가운 뒤에 작은 액자가 걸려 있다.");
    }

    void OnKeypadTrap()
    {
        trapCount++;
        notes.Add("trap3", "PC 에 0404 — ACCESS DENIED. 피험자 번호로는 로그인할 수 없다.");
        Run(dlg.Sequence("ACCESS DENIED", trapCount == 1 ? "…피험자 번호로는 로그인할 수 없습니다." : "나|404 는 이제 지겹다니까."));
    }

    IEnumerator OnLogin()
    {
        loggedIn = true;
        idle = 0f;
        SoundManager.Play("chime", 0.6f);
        Get<Image>("CabinetLamp").sprite = S("obj_lamp_green");
        notes.Add("login", "PC 로그인 (0414) — 메일: 출입 카드는 '본인 파일' 서랍에. 보관함 잠금 해제.");
        yield return ShowMail(true);
    }

    IEnumerator ShowMail(bool first)
    {
        var lines = new List<string> { "PC 에 메일이 한 통 열려 있다." };
        if (first)
        {
            lines.Add("로그인한 계정 이름은 까맣게 지워져 있다. ████");
            lines.Add("나|본인 파일…? 누구의 파일인데.");
            lines.Add("어딘가에서 '철컥' 하고 잠금이 풀리는 소리가 났다.");
        }
        yield return zoom.Show(S("zoom_pc"),
            "보낸 사람: 관리팀\n\n관찰자님, 출입 카드는\n본인 파일 서랍에\n넣어 두었습니다.\n\n[기록 보관함 잠금 해제됨]",
            lines.ToArray());
    }

    // ---------------- 기록 보관실 ----------------
    void UVShort()
    {
        uvShortTaps++;
        Say(uvShortTaps >= 2 ? "특별한 건 보이지 않는다. …조금 더 오래 비춰볼까?" : "특별한 건 보이지 않는다.");
    }

    IEnumerator OnUVRevealed()
    {
        uvRevealed = true;
        busy = true;
        SoundManager.Play("stinger", 0.6f);
        yield return new WaitForSeconds(0.5f);
        yield return dlg.Sequence(
            "…보관함 위로 보랏빛 손자국이 떠올랐다.",
            "다른 서랍은 깨끗한데, 딱 한 서랍에만 손자국이 잔뜩 묻어 있다.",
            "404-07.",
            "나|누군가 이 서랍만… 몇 번이고 열었어.");
        notes.Add("uvprint", "UV 보관함 — 404-07 서랍에만 손자국이 잔뜩.");
        inv.Deselect();
        busy = false;
    }

    void Drawer(string id)
    {
        if (inv.IsSelected("UV") && !uvRevealed) { UVShort(); return; }
        if (!loggedIn)
        {
            lockedTaps++;
            SoundManager.Play("error", 0.5f);
            Say(lockedTaps == 1 ? "전자 잠금이 걸려 있다. 옆에 빨간 불이 켜져 있다." : "전자 잠금이다. …연구실 어딘가에서 풀 수 있지 않을까?");
            return;
        }
        if (opened.Contains(id))
        {
            Say(id == "d7" ? "출입 카드가 들어 있던 서랍이다." : "이미 열어 본 서랍이다.");
            return;
        }
        opened.Add(id);
        Run(OpenDrawer(id));
    }

    IEnumerator OpenDrawer(string id)
    {
        busy = true;
        SoundManager.Play("drawer", 0.9f);
        GO("DrawerOpen_" + id).SetActive(true);
        yield return new WaitForSeconds(0.3f);

        if (id == "d7") { busy = false; yield return TakeCard(); yield break; }

        var lines = new List<string>();
        switch (id)
        {
            case "d4": lines.Add("SUBJECT 404-04 파일. 'STAGE 1 실패. 기록 종료.'"); break;
            case "d5": lines.Add("SUBJECT 404-05 파일. 'STAGE 2 에서 47번 반복. 기록 종료.'"); break;
            case "d6": lines.Add("SUBJECT 404-06 파일. 메모: '전화를 받지 않음.'"); break;
            case "d8": lines.Add("SUBJECT 404-08 파일. …서류가 비어 있다. 사진 칸만 찢겨 나갔다."); break;
            case "d9":
                lines.Add("SUBJECT 404-09 파일. 라벨만 새것이다.");
                lines.Add("STAGE 1 · ROOM 통과 / STAGE 2 · LOOP 통과 / STAGE 3 · 진행 중");
                lines.Add("나|…이거, 나 아니야?");
                notes.Add("file09", "404-09 파일 — STAGE 1, 2 통과 / STAGE 3 진행 중. …나?");
                break;
        }
        if (id != "d8" && id != "d9")
        {
            wrongOpened++;
            lines.Add(wrongOpened == 1 ? "사진 칸에 붙은 얼굴은… 나다." : "여기도… 내 얼굴이다.");
            if (wrongOpened == 1) lines.Add("나|왜 다른 번호 파일에 내 사진이 있는 거야?");
            notes.Add("faces", "보관함 파일 — 번호는 다른데 사진은 전부 내 얼굴.");
        }
        if (uvRevealed && id != "d9") lines.Add("나|…손자국이 묻어 있던 건 07 서랍이었지.");
        yield return dlg.Sequence(lines.ToArray());
        busy = false;
    }

    IEnumerator TakeCard()
    {
        busy = true;
        yield return dlg.Sequence(
            "SUBJECT 404-07 파일.",
            "맨 위에 빨간 도장이 찍혀 있다. 'STAGE 3 통과 → 관찰자 전환.'",
            "파일 사이에 출입 카드가 끼워져 있다.");
        cardTaken = true;
        inv.Add("CARD");
        SoundManager.Play("pickup");
        notes.Add("card", "404-07 서랍 — 관찰자 출입 카드. 'STAGE 3 통과 → 관찰자 전환.'");
        yield return zoom.Show(S("zoom_card"), null,
            "출입 카드를 얻었다.",
            "OBSERVER · 404-07",
            "카드 사진 속 얼굴은… 역시 나다.",
            "나|번호만 다르고, 다 나야. 대체 나는 몇 명이야…");
        busy = false;
        idle = 0f;
    }

    // ---------------- 관찰실 문 ----------------
    IEnumerator OnCardAccepted()
    {
        busy = true;
        cardOk = true;
        inv.Deselect();
        SoundManager.Play("beep", 1f);
        yield return new WaitForSeconds(0.2f);
        SoundManager.Play("success", 0.8f);
        Get<Image>("ReaderLamp").sprite = S("obj_lamp_green");
        GO("ScanGlow").SetActive(true);
        StartCoroutine(UIFx.Punch(map["ScanGlow"], 0.2f, 0.3f));
        yield return dlg.Sequence("삑— CARD ACCEPTED.", "옆의 지문 스캐너에 불이 들어왔다. PLACE YOUR FINGER.");
        notes.Add("reader", "카드 리더기 — 카드 확인. 지문 스캐너를 길게 눌러야 한다.");
        busy = false;
        idle = 0f;
    }

    IEnumerator OnScanned()
    {
        busy = true;
        scanned = true;
        room.locked = true;
        SoundManager.Play("success", 0.8f);
        GO("Gauge").SetActive(false);
        yield return dlg.Auto("SCAN COMPLETE.", 0.6f);
        SoundManager.Play("static", 0.6f);
        StartCoroutine(glitch.Burst(0.5f, 0.5f));
        yield return dlg.Auto("IDENTITY CONFIRMED.", 0.6f);
        SoundManager.Play("chime", 0.8f);
        yield return dlg.Sequence(
            "방송|WELCOME BACK, OBSERVER 404-07.",
            "방송|다시 오신 것을 환영합니다, 관찰자 404-07 님.",
            "나|…07?",
            "나|이건 07 의 카드잖아. 그런데 왜 내 지문이…",
            "나|…'다시' 오신 걸 환영한다고?");
        SoundManager.Play("unlock");
        SoundManager.Play("door_open", 0.8f);
        GO("ObsDoorOpen").SetActive(true);
        notes.Add("scan", "지문 스캐너 — \"WELCOME BACK, OBSERVER 404-07.\" …내 지문이 07 이다?");
        yield return dlg.Sequence("철컥— 관찰실 문이 열렸다.");
        room.locked = false;
        busy = false;
        idle = 0f;
    }

    IEnumerator CctvBlink()
    {
        var cam = Get<Image>("CCTV");
        while (true)
        {
            cam.sprite = S("obj_cctv_on");
            yield return new WaitForSeconds(0.6f);
            cam.sprite = S("obj_cctv_off");
            yield return new WaitForSeconds(0.4f);
        }
    }

    // =========================================================
    //  마지막 반전 — 관찰실
    // =========================================================
    IEnumerator ShowMonitor(Image monitor, string sprite)
    {
        SoundManager.Play("static", 0.5f);
        monitor.gameObject.SetActive(true);
        monitor.sprite = S(sprite);
        monitor.transform.localScale = new Vector3(1f, 0.05f, 1f);
        yield return UIFx.Scale(monitor.transform, Vector3.one, 0.2f);
    }

    IEnumerator SwapMonitor(Image monitor, string sprite)
    {
        SoundManager.Play("static", 0.4f);
        StartCoroutine(glitch.Burst(0.2f, 0.4f));
        monitor.color = new Color(1, 1, 1, 0.4f);
        yield return new WaitForSeconds(0.08f);
        monitor.sprite = S(sprite);
        monitor.color = Color.white;
    }

    IEnumerator PhoneBlink(Image phone)
    {
        while (true)
        {
            phone.sprite = S("obj_phone_ring");
            yield return new WaitForSeconds(0.35f);
            phone.sprite = S("obj_phone");
            yield return new WaitForSeconds(0.35f);
        }
    }

    IEnumerator Finale()
    {
        busy = true;
        room.locked = true;
        dlg.Hide();
        if (cctvCo != null) StopCoroutine(cctvCo);

        // CutscenePanel 이 화면 전체를 덮어서 뒤쪽 터치를 막아 줌 (InputBlocker 는 대사 넘기기까지 막으므로 쓰지 않음)
        var cut = Get<CanvasGroup>("CutscenePanel");
        var bg = Get<Image>("ObsRoom");
        var flash = Get<Image>("Flash");
        var monitor = Get<Image>("Monitor");
        var phone = Get<Image>("ObsPhone");
        cut.gameObject.SetActive(true);
        cut.alpha = 0f;
        monitor.gameObject.SetActive(false);
        phone.gameObject.SetActive(false);
        flash.color = new Color(0, 0, 0, 1);
        SoundManager.Play("step", 0.8f);
        SoundManager.StopAmb();
        yield return UIFx.Fade(cut, 1f, 0.6f);
        yield return UIFx.FadeGraphic(flash, 0f, 1.2f);
        SoundManager.Amb("amb_room", 0.35f);

        yield return dlg.Auto("관찰실.", 0.8f);
        yield return dlg.Auto("벽 가득 모니터가 켜져 있다. 아무도 없다.", 1.2f);
        dlg.Hide();

        // CAM 01 : ROOM 404
        yield return ShowMonitor(monitor, "cam_room404");
        yield return dlg.Auto("CAM 01 · ROOM 404.", 0.9f);
        SoundManager.Play("heartbeat", 0.7f);
        yield return dlg.Auto("침대 위에 누군가 누워 있다. 화면 구석의 시계는 04:04.", 1.4f);
        yield return dlg.Auto("나|…저 방이야. 내가 처음 깨어난 방.", 1.2f);
        dlg.Hide();

        // 책상 위 수칙
        yield return dlg.Sequence(
            "책상 위에 종이 한 장이 놓여 있다.",
            "관찰자 수칙  1. 피험자가 POWER 를 켜면 전화를 걸 것.",
            "2. 정해진 대사만 말할 것.",
            "3. 피험자에게 절대 이름을 알려주지 말 것.");

        // 피험자가 일어나 방을 돌아다닌다 → POWER → 전화
        yield return SwapMonitor(monitor, "cam_phone");
        yield return dlg.Auto("화면 속 그 사람이 일어나, 거울 뒤의 스위치를 켠다.", 1.2f);
        SoundManager.Play("switch", 0.8f);
        yield return dlg.Auto("딸깍.", 0.6f);
        dlg.Hide();

        // 수화기 불이 깜빡인다 — 직접 들어야 한다
        phone.gameObject.SetActive(true);
        phoneCo = StartCoroutine(PhoneBlink(phone));
        SoundManager.Loop("phone_ring", 0.5f);
        var hint = Get<Text>("PhoneHint");
        hint.gameObject.SetActive(true);
        waitPhone = true; phoneTapped = false;
        float t = 0f;
        while (!phoneTapped)
        {
            t += Time.deltaTime;
            SetAlpha(hint, 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 3f)));
            yield return null;
        }
        waitPhone = false;
        hint.gameObject.SetActive(false);
        StopCoroutine(phoneCo);
        SoundManager.StopLoop();
        phone.sprite = S("obj_phone");
        SoundManager.Play("pickup", 0.8f);
        SoundManager.Play("phone_line", 0.6f);
        SoundManager.StopAmb();

        // Chapter 1 의 그 전화 — 이번엔 내가 건다
        yield return dlg.Sequence(
            "수화기를 들자, 입이 멋대로 움직였다.",
            "나|…또 찾았네.",
            "404-09|누구야?",
            "나|이번에는 얼마나 걸렸어?");
        SoundManager.Play("hangup", 0.8f);
        yield return new WaitForSeconds(0.6f);
        SoundManager.Amb("amb_room", 0.35f);
        yield return dlg.Sequence(
            "나|…방금 그 말.",
            "나|그 방에서 내가 들었던 전화야.",
            "나|그 목소리… 처음부터 내 목소리였어.");

        // 화면 속 '나' 가 카메라를 올려다본다
        yield return SwapMonitor(monitor, "cam_look");
        SoundManager.Play("heartbeat", 0.9f);
        yield return dlg.Auto("화면 속 그 사람이 카메라를 똑바로 올려다본다.", 1.2f);
        SoundManager.Play("heartbeat", 1f);
        yield return dlg.Auto("…나와 똑같은 얼굴로.", 1.4f);
        dlg.Hide();

        // 모든 화면이 CAM 00 으로
        SoundManager.StopAmb();
        SoundManager.Play("static", 1f);
        StartCoroutine(glitch.Burst(0.8f, 0.8f));
        for (int i = 0; i < 6; i++)
        {
            bg.color = i % 2 == 0 ? new Color(0.5f, 1f, 0.6f) : Color.white;
            yield return new WaitForSeconds(0.07f);
        }
        bg.color = Color.white;
        monitor.sprite = S("cam_self");
        yield return dlg.Auto("나|…CAM 00?", 1.0f);
        yield return dlg.Auto("화면에 비친 건, 모니터 앞에 서 있는 누군가의 뒷모습.", 1.4f);
        SoundManager.Play("heartbeat", 1f);
        yield return dlg.Auto("나|저거… 지금의 나잖아.", 1.4f);
        dlg.Hide();
        yield return new WaitForSeconds(0.5f);

        // 방송
        SoundManager.Play("chime", 0.9f);
        yield return new WaitForSeconds(0.9f);
        yield return dlg.Sequence(
            "방송|관찰자 404-07. STAGE 3 완료.",
            "방송|기억 초기화 후, 다음 회차를 준비합니다.",
            "나|…초기화? 잠깐, 그럼 나도—");
        SoundManager.Play("whisper", 0.9f);
        StartCoroutine(glitch.Burst(0.6f, 0.6f));
        yield return dlg.Sequence("???|이번에는 얼마나 걸렸어?");

        // 암전
        SoundManager.Play("hangup", 0.7f);
        StartCoroutine(glitch.Burst(0.3f, 0.6f));
        yield return UIFx.Scale(monitor.transform, new Vector3(1f, 0.02f, 1f), 0.12f);
        monitor.gameObject.SetActive(false);
        flash.color = new Color(0, 0, 0, 0);
        yield return UIFx.FadeGraphic(flash, 1f, 0.3f);
        yield return new WaitForSeconds(1.0f);
        GameState.EndRun();
        SceneLoader.I.Load("Chapter03_Clear", 1.2f);
    }

    // =========================================================
    //  힌트 (30초 동안 진행이 없으면 전구가 깜빡임)
    // =========================================================
    static readonly string[][] Hints =
    {
        new[] { "", "연구실 벽의 가운 뒤에 무언가 걸려 있다. 가운을 옆으로 밀어 보자.", "사진 아래 날짜 APR. 14 → PC 비밀번호는 0414." },
        new[] { "PC 메일: 출입 카드는 '본인 파일' 서랍에 있다.", "UV 손전등을 선택하고, 보관함을 길게 비춰 보자.", "손자국이 잔뜩 묻은 404-07 서랍을 열자." },
        new[] { "관찰실 문 옆의 장치를 살펴보자.", "아이템 칸에서 출입 카드를 선택하자.", "카드를 선택한 채로 카드 리더기를 탭하자." },
        new[] { "지문 스캐너에 불이 들어왔다.", "스캐너를 꾹 누르고 있어 보자.", "스캐너를 2초 동안 누르고 있으면 된다." },
        new[] { "관찰실 문이 열렸다.", "관찰실 문을 탭하자.", "관찰실 문을 탭하자." },
    };

    int CurrentStage()
    {
        if (!loggedIn) return 0;
        if (!cardTaken) return 1;
        if (!cardOk) return 2;
        if (!scanned) return 3;
        return 4;
    }

    void Update()
    {
        if (busy || dlg.IsBlocking) return;
        idle += Time.deltaTime;
        var icon = Get<Image>("HintIcon");
        float a = idle > 30f ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 3f)) : 0.55f;
        icon.color = new Color(1, 1, 1, a);
    }

    void OnHint()
    {
        if (busy || dlg.IsBlocking) return;
        SoundManager.Play("click", 0.6f);
        int st = CurrentStage();
        if (st != hintStage) { hintStage = st; hintLevel = 0; }
        string h = Hints[st][Mathf.Min(hintLevel, 2)];
        if (h == "")   // 1단계: 메모를 봤는지에 따라
            h = stickySeen ? "메모: \"비밀번호는 내 생일. 사진에 적어 뒀잖아.\"" : "연구실 PC 는 비밀번호가 필요하다. 모니터 옆 노란 메모를 읽어 보자.";
        hintLevel++;
        idle = 0f;
        Run(dlg.Sequence("힌트 " + Mathf.Min(hintLevel, 3) + "/3|" + h));
    }
}
