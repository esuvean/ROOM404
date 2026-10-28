// =============================================================
//  FINAL CHAPTER - WAKE UP (마지막 챕터) 전체 퍼즐 진행 + 엔딩 선택
//
//  화면: 초기화실 → 서버실 → 엘리베이터 홀 (끝에서 멈춤)
//
//  진행
//   기억 초기화 99% 에서 깨어남 → 칸막이 커튼을 스와이프 → 다음 회차의 나(404-10)
//   → UV 로 벽을 홀드: "THE CLOCK LIES / 04:04 +1 / -07" (Chapter 1 의 그 글씨는 예전의 나)
//   → 서버실 MAIN CLOCK 04:04 [다음 시각: 존재하지 않음] / 평가표 "관제실 코드 = 아직 오지 않은 시각"
//   → 엘리베이터 코드 0405 (0404 는 함정) → 관제실
//   → 의자는 비어 있고, 모니터에는 '지금 이 게임을 보고 있는 당신' 의 화면
//   → [계속 관찰한다] = ENDING A  /  [전원을 끈다] 3초 홀드 = ENDING B (04:05)
// =============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chapter04Manager : MonoBehaviour
{
    public static Chapter04Manager I;

    [Header("그림 (자동 연결됨)")]
    public List<Sprite> sprites = new List<Sprite>();

    RoomSwipeController room;
    DialogueManager dlg;
    InventoryManager inv;
    ZoomView zoom;
    ClueNotebook notes;
    GlitchEffect glitch;
    KeypadController keypad;
    HoldReveal wallUV;
    HoldLever shutdown;
    SwipeMover partition;

    readonly Dictionary<string, Transform> map = new Dictionary<string, Transform>();

    bool busy;
    bool curtainOpen, sleeperSeen, uvRevealed, clockSeen, printSeen, cardTried, codeOk;
    bool choosing, choseA, choseB;
    int curtainTaps, uvShortTaps, trapCount, doorTaps, clockTaps;

    Coroutine cctvCo;
    float idle;
    int hintStage = -1, hintLevel;

    // =========================================================
    void Awake()
    {
        I = this;
        TapRouter.Handler = OnTap;
        var canvas = FindFirstObjectByType<Canvas>();
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) map[t.name] = t;

        room = Get<RoomSwipeController>("FinalView");
        dlg = GetComponent<DialogueManager>();
        inv = GetComponent<InventoryManager>();
        zoom = GetComponent<ZoomView>();
        notes = GetComponent<ClueNotebook>();
        glitch = GetComponent<GlitchEffect>();
        keypad = GetComponent<KeypadController>();
        wallUV = Get<HoldReveal>("WallHit");
        shutdown = Get<HoldLever>("ShutdownButton");
        partition = Get<SwipeMover>("Partition");

        room.onWallChanged = OnWallChanged;
        room.onBlocked = d => Say(d < 0 ? "초기화실 뒤쪽은 막혀 있다." : "엘리베이터뿐이다.");
        partition.onDone = () => StartCoroutine(OnCurtainOpened());
        wallUV.canHold = () => inv.IsSelected("UV");
        wallUV.onRevealed = () => StartCoroutine(OnUVRevealed());
        keypad.onUnlocked = OnCodeOk;
        keypad.onTrap = OnKeypadTrap;
        shutdown.canHold = () => choosing;
        shutdown.onDone = () => { if (choosing) { choosing = false; choseB = true; } };
        shutdown.onRelease = () => Say("손을 떼자 게이지가 다시 내려갔다.");
        Get<Button>("ContinueButton").onClick.AddListener(() => { if (choosing) { choosing = false; choseA = true; SoundManager.Play("click"); } });

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

    bool retry;

    void Start()
    {
        // 엔딩 화면에서 [다른 결말 보기] 를 눌렀으면 관제실 선택 장면부터
        if (GameState.retryChoice)
        {
            GameState.retryChoice = false;
            retry = true;
            SoundManager.StopLoop();
            inv.Add("UV"); inv.Add("CARD");
            StartCoroutine(RetryFinale());
            return;
        }
        GameState.StartRun();
        SoundManager.StopLoop();
        SoundManager.Amb("amb_facility", 0.5f);
        inv.Add("UV");
        inv.Add("CARD");
        cctvCo = StartCoroutine(CctvBlink());
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        busy = true;
        var intro = Get<CanvasGroup>("IntroPanel");
        var t1 = Get<Text>("IntroText");
        var t2 = Get<Text>("IntroSub");
        intro.gameObject.SetActive(true);
        intro.alpha = 1f;
        SetAlpha(t1, 0f); SetAlpha(t2, 0f);
        yield return new WaitForSeconds(0.6f);
        SoundManager.Play("heartbeat", 0.7f);
        yield return UIFx.FadeGraphic(t1, 1f, 0.8f);
        yield return UIFx.FadeGraphic(t2, 1f, 0.6f);
        yield return new WaitForSeconds(1.4f);
        StartCoroutine(UIFx.FadeGraphic(t2, 0f, 0.6f));
        yield return UIFx.FadeGraphic(t1, 0f, 0.6f);

        // 초기화 99% 에서 오류
        SoundManager.Play("error", 0.7f);
        SoundManager.Play("static", 0.6f);
        StartCoroutine(glitch.Burst(0.6f, 0.7f));
        yield return UIFx.Fade(intro, 0f, 1.4f);
        intro.gameObject.SetActive(false);

        yield return dlg.Sequence(
            "방송|MEMORY RESET 99%… ERROR.",
            "방송|SUBJECT AWAKE.",
            "나|…머리가 깨질 것 같아.",
            "나|초기화… 하다가 멈춘 거야?",
            "나|기억나. ROOM 404, 끝없는 복도, 관찰실. 전부.",
            "나|이번엔… 잊어버리기 전에 끝내야 해.");

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
        for (int i = 0; i < 3; i++) s += (i == index ? "■" : "□") + (i < 2 ? " " : "");
        Get<Text>("WallDots").text = s;
        string[] names = { "RESET ROOM", "SERVER ROOM", "ELEVATOR" };
        Get<Text>("RoomTitle").text = names[index];
        GO("ArrowLeft").SetActive(index > 0);
        GO("ArrowRight").SetActive(index < 2);
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
    public void OnTap(string id)
    {
        if (id == "shutdown_tap") { if (choosing) Say("꾹 누르고 있어야 전원이 꺼질 것 같다."); return; }
        if (busy || dlg.IsBlocking) return;
        idle = 0f;

        switch (id)
        {
            // ---------- 초기화실 ----------
            case "mychair":
                SoundManager.Play("tap", 0.6f);
                Say("내가 누워 있던 의자다. 헤드셋에 아직 온기가 남아 있다.");
                break;

            case "monitor":
                notes.Add("reset", "초기화 모니터 — 대상 404-07 · 회차 10 · 99% 에서 오류 (내가 깨어남)");
                Run(zoom.Show(S("zoom_note"), "MEMORY RESET\n\n대상  404-07\n회차  10\n진행  99%\n\nERROR:\nSUBJECT AWAKE",
                    "의자 옆 모니터다.",
                    "나|회차 10… 이걸 열 번이나 했다고?"));
                break;

            case "clock":
                clockTaps++;
                SoundManager.Play("tap", 0.6f);
                Say(clockTaps == 1 ? "벽시계. 04시 04분. …초침이 움직이지 않는다." : "나|이 건물의 시계는 전부 04:04 야.");
                break;

            case "partition":
                if (curtainOpen) break;
                curtainTaps++;
                SoundManager.Play("tap", 0.6f);
                Say(curtainTaps == 1 ? "칸막이 커튼이다. 뒤에서 누군가 숨 쉬는 소리가 들린다." : "커튼을 옆으로 걷어 볼까?");
                break;

            case "sleeper":
                if (!curtainOpen) break;
                Run(Sleeper());
                break;

            case "wall":
                if (inv.IsSelected("UV"))
                {
                    if (uvRevealed) { Say("벽에 보랏빛 글씨가 남아 있다."); break; }
                    uvShortTaps++;
                    Say(uvShortTaps >= 2 ? "특별한 건 보이지 않는다. …조금 더 오래 비춰볼까?" : "특별한 건 보이지 않는다.");
                    break;
                }
                if (inv.Selected != null) { Say("여기엔 " + inv.Selected + "를 쓸 수 없을 것 같다."); break; }
                Say(uvRevealed ? "나|THE CLOCK LIES… 그 방 거울에 쓰여 있던 것도, 내 글씨였어." : "아무것도 없는 벽이다. 손톱으로 긁은 자국이 희미하게 있다.");
                break;

            // ---------- 서버실 ----------
            case "rack":
                SoundManager.Play("buzz", 0.3f);
                Say("서버마다 'ROOM 404' 라벨이 붙어 있다. 저 안에서 몇 명의 내가 돌아가고 있을까.");
                break;

            case "mainclock":
                clockSeen = true;
                notes.Add("mainclock", "서버실 MAIN CLOCK — 04:04 [LOCKED] · 다음 시각: 존재하지 않음");
                Run(zoom.Show(S("zoom_note"), "MAIN CLOCK\n\n04:04  [LOCKED]\n\n모든 방 동기화 중\n\n다음 시각:\n존재하지 않음",
                    "서버 화면에 큰 시계가 떠 있다.",
                    "나|이 시계 하나가… 모든 방의 시간을 04:04 에 묶어 두고 있어.",
                    "나|'다음 시각은 존재하지 않음'…?"));
                break;

            case "printer":
                printSeen = true;
                notes.Add("print", "평가표 — 판정: 재관찰 / ※ 관제실 코드 = 이 건물에 아직 오지 않은 시각");
                Run(zoom.Show(S("zoom_note"), "관찰자 평가 404-07\n\nSTAGE 3  " + Rec("Chapter03_Observation") + "\n판정  재관찰\n\n※ 관제실 코드 =\n이 건물에 아직\n오지 않은 시각",
                    "프린터에서 종이 한 장이 나와 있다.",
                    "나|재관찰… 나를 또 처음부터 돌리겠다는 거야.",
                    "나|아직 오지 않은 시각…"));
                break;

            case "cctv":
                Say("빨간 불이 깜빡인다. …저 너머에서도 누군가 보고 있겠지.");
                break;

            // ---------- 엘리베이터 홀 ----------
            case "elevator":
                if (codeOk) { StartCoroutine(Finale()); break; }
                doorTaps++;
                SoundManager.Play("tap", 0.8f);
                Say(doorTaps == 1 ? "엘리베이터. 호출 버튼이 없다." : "옆 패널에 관제실 코드를 넣어야 할 것 같다.");
                break;

            case "panel":
                if (codeOk) { Say("코드는 이미 확인되었다."); break; }
                keypad.Open();
                break;

            case "reader":
                if (inv.IsSelected("CARD") || inv.Has("CARD")) { Run(CardDenied()); break; }
                Say("카드 리더기다.");
                break;

            case "guide":
                notes.Add("guide", "층 안내 — 1F 관제실(CONTROL) / B4 관찰동");
                Say("층 안내: 1F 관제실 · B1~B3 (표시 없음) · B4 관찰동");
                break;

            case "display":
                Say(codeOk ? "층 표시등이 깜빡인다. 엘리베이터가 도착했다." : "층 표시등: B4");
                break;
        }
    }

    // ---------------- 초기화실 ----------------
    IEnumerator OnCurtainOpened()
    {
        curtainOpen = true;
        idle = 0f;
        GO("SleeperHit").SetActive(true);
        yield return new WaitForSeconds(0.2f);
        Say("커튼 뒤에… 의자가 하나 더 있다. 누군가 누워 있다.");
    }

    IEnumerator Sleeper()
    {
        if (sleeperSeen) { Say("나|…깨우지 말자. 깨어나면 그 방에서 다시 시작할 테니까."); yield break; }
        sleeperSeen = true;
        busy = true;
        SoundManager.Play("heartbeat", 0.7f);
        yield return dlg.Sequence(
            "헤드셋을 쓴 누군가가 잠들어 있다.",
            "손목 밴드: 404-10 · ROOM 404 투입 대기 · 04:04",
            "얼굴을 들여다본다.",
            "나|…나야.",
            "나|다음 회차의 나. 깨어나면 그 방에서 '…여기가 어디지?' 하겠지.",
            "나|내가 그랬던 것처럼.");
        notes.Add("sleeper", "커튼 뒤 — 잠든 404-10 (다음 회차의 나). 04:04 에 ROOM 404 투입 대기.");
        busy = false;
    }

    IEnumerator OnUVRevealed()
    {
        uvRevealed = true;
        busy = true;
        SoundManager.Play("stinger", 0.6f);
        yield return new WaitForSeconds(0.5f);
        yield return dlg.Sequence(
            "…벽에 보랏빛 글씨가 떠올랐다.",
            "THE CLOCK LIES.",
            "그 아래: 04:04 +1",
            "맨 아래 작은 서명: -07",
            "나|…이 글씨. ROOM 404 거울에 있던 것과 똑같아.",
            "나|그때 그 글씨를 남긴 건… 예전의 나였어.",
            "나|04:04 에 1을 더하라고?");
        notes.Add("uvmsg", "UV 벽 — \"THE CLOCK LIES / 04:04 +1 / -07\" (예전의 내가 남긴 글씨)");
        inv.Deselect();
        busy = false;
    }

    // ---------------- 엘리베이터 ----------------
    IEnumerator CardDenied()
    {
        busy = true;
        cardTried = true;
        inv.Deselect();
        SoundManager.Play("beep", 1f);
        yield return new WaitForSeconds(0.2f);
        SoundManager.Play("error", 0.8f);
        yield return dlg.Sequence(
            "삑— ACCESS DENIED.",
            "OBSERVER 404-07 : 권한 만료 (초기화 대상자)",
            "나|…내 카드는 이제 안 된다는 거네.",
            "나|옆 패널에 코드를 직접 넣는 수밖에 없어.");
        notes.Add("denied", "엘리베이터 카드 리더 — 404-07 권한 만료. 코드를 직접 넣어야 한다.");
        busy = false;
    }

    void OnKeypadTrap()
    {
        trapCount++;
        notes.Add("trap4", "관제실 코드에 0404 — 반복은 이미 충분하다.");
        Run(dlg.Sequence("LOOP DETECTED", trapCount == 1 ? "나|04:04 는… 이제 지긋지긋해." : "나|그 시간은 이미 충분히 반복했어."));
    }

    void OnCodeOk()
    {
        codeOk = true;
        idle = 0f;
        SoundManager.Play("chime", 0.9f);
        Get<Text>("FloorText").text = "1F";
        notes.Add("code", "관제실 코드 0405 — 처음으로 '다음 시각' 을 입력했다.");
        Run(dlg.Sequence("띵— 엘리베이터가 도착했다.", "나|04:05. …한 번도 오지 않았던 시간."));
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
    //  관제실 — 마지막 반전 + 결말 선택
    // =========================================================
    IEnumerator Finale()
    {
        busy = true;
        room.locked = true;
        dlg.Hide();
        if (cctvCo != null) StopCoroutine(cctvCo);

        SoundManager.Play("door_open", 0.8f);
        GO("ElevOpen").SetActive(true);
        yield return new WaitForSeconds(0.8f);

        // 엘리베이터 안
        var cut = Get<CanvasGroup>("CutscenePanel");
        var elev = Get<Image>("ElevInside");
        var ctrl = Get<Image>("ControlRoom");
        var floor = Get<Text>("ElevFloor");
        var flash = Get<Image>("Flash");
        var monitor = Get<Image>("Monitor");
        var chair = Get<Image>("Chair");
        var clock = Get<Text>("BigClock");
        cut.gameObject.SetActive(true);
        cut.alpha = 0f;
        ctrl.gameObject.SetActive(false); monitor.gameObject.SetActive(false); chair.gameObject.SetActive(false);
        clock.gameObject.SetActive(false);
        GO("ChoiceGroup").SetActive(false);
        elev.gameObject.SetActive(true); floor.gameObject.SetActive(true);
        floor.text = "B4";
        flash.color = new Color(0, 0, 0, 0);
        yield return UIFx.Fade(cut, 1f, 0.6f);
        SoundManager.Play("door_slam", 0.5f);
        SoundManager.Amb("amb_corridor", 0.4f);
        yield return dlg.Auto("문이 닫히고, 엘리베이터가 올라가기 시작한다.", 1.0f);
        dlg.Hide();
        string[] floors = { "B3", "B2", "B1", "404", "1F" };
        foreach (var f in floors)
        {
            yield return new WaitForSeconds(1.0f);
            if (f == "404")
            {
                SoundManager.Play("static", 0.8f);
                StartCoroutine(glitch.Burst(0.5f, 0.7f));
                floor.text = "404";
                yield return new WaitForSeconds(0.6f);
                continue;
            }
            SoundManager.Play("blip", 1f);
            floor.text = f;
        }
        SoundManager.Play("chime", 0.9f);
        yield return new WaitForSeconds(0.8f);

        // 관제실
        flash.color = Color.black;
        elev.gameObject.SetActive(false); floor.gameObject.SetActive(false);
        ctrl.gameObject.SetActive(true);
        chair.gameObject.SetActive(true);
        chair.sprite = S("obj_chair_occ");
        SoundManager.Amb("amb_room", 0.35f);
        yield return UIFx.FadeGraphic(flash, 0f, 1.4f);
        yield return dlg.Auto("1F · 관제실.", 0.9f);
        yield return dlg.Auto("벽을 가득 채운 수백 개의 화면. 전부 ROOM 404. 전부… 나.", 1.6f);
        SoundManager.Play("heartbeat", 0.8f);
        yield return dlg.Auto("가운데 의자에 누군가 등을 돌리고 앉아 있다.", 1.2f);
        dlg.Hide();
        yield return dlg.Sequence(
            "나|…당신이야?",
            "나|우리를 이 방에 가둬 두고, 계속 지켜보던 사람.",
            "대답이 없다. 천천히 다가가 의자를 돌린다.");

        SoundManager.Play("mirror_slide", 0.6f);
        StartCoroutine(UIFx.Shake((RectTransform)chair.transform, 0.3f, 6f));
        yield return new WaitForSeconds(0.3f);
        chair.sprite = S("obj_chair_empty");
        SoundManager.Play("stinger", 0.7f);
        yield return dlg.Sequence("…의자는 비어 있었다.", "걸쳐져 있던 흰 가운만 바닥으로 흘러내렸다.");

        // 책상 위 모니터 — 지금 이 게임을 보는 '당신'
        SoundManager.Play("static", 0.6f);
        monitor.gameObject.SetActive(true);
        monitor.transform.localScale = new Vector3(1f, 0.05f, 1f);
        yield return UIFx.Scale(monitor.transform, Vector3.one, 0.2f);
        yield return dlg.Sequence(
            "책상 위 모니터 하나만 켜져 있다.",
            "CAM ?? · OBSERVER: YOU",
            "나|이건… 화면이야. 누군가 손에 들고 보는.",
            "나|위에는 단서 노트, 아래에는 아이템 칸. 힌트 전구까지.",
            "나|…처음부터, 나를 지켜보던 건.",
            "나|지금 이걸 보고 있는 당신이었구나.");
        SoundManager.Play("heartbeat", 1f);
        yield return new WaitForSeconds(0.6f);
        yield return dlg.Sequence(
            "나|나한테 문을 열게 하고, 숫자를 찾게 하고.",
            "나|이번에는 얼마나 걸렸냐고 묻던 것도.",
            "나|…부탁이 있어.");
        SoundManager.Play("chime", 0.8f);
        yield return new WaitForSeconds(0.6f);
        yield return dlg.Sequence("방송|관찰자님.", "방송|관찰을 계속하시겠습니까?");
        yield return Choice(monitor, flash, clock, ctrl);
    }

    // 관제실 선택 장면부터 다시 (엔딩 화면 → 다른 결말 보기)
    IEnumerator RetryFinale()
    {
        busy = true;
        room.locked = true;
        var cut = Get<CanvasGroup>("CutscenePanel");
        var ctrl = Get<Image>("ControlRoom");
        var flash = Get<Image>("Flash");
        var monitor = Get<Image>("Monitor");
        var chair = Get<Image>("Chair");
        var clock = Get<Text>("BigClock");
        GO("IntroPanel").SetActive(false);
        cut.gameObject.SetActive(true);
        cut.alpha = 1f;
        GO("ElevInside").SetActive(false); GO("ElevFloor").SetActive(false);
        GO("ChoiceGroup").SetActive(false);
        clock.gameObject.SetActive(false);
        ctrl.gameObject.SetActive(true);
        chair.gameObject.SetActive(true); chair.sprite = S("obj_chair_empty");
        monitor.gameObject.SetActive(true); monitor.sprite = S("ctrl_screen");
        flash.color = Color.black;
        SoundManager.Amb("amb_room", 0.35f);
        SoundManager.Play("static", 0.6f);
        StartCoroutine(glitch.Burst(0.5f, 0.6f));
        yield return UIFx.FadeGraphic(flash, 0f, 1.2f);
        yield return dlg.Sequence("…다시, 관제실.", "모니터 속에서 당신의 화면이 나를 비추고 있다.");
        SoundManager.Play("chime", 0.8f);
        yield return new WaitForSeconds(0.5f);
        yield return dlg.Sequence("방송|관찰자님.", "방송|이번에는… 어떻게 하시겠습니까?");
        yield return Choice(monitor, flash, clock, ctrl);
    }

    IEnumerator Choice(Image monitor, Image flash, Text clock, Image ctrl)
    {
        // 결말 선택
        var choice = GO("ChoiceGroup");
        choice.SetActive(true);
        var cg = choice.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        yield return UIFx.Fade(cg, 1f, 0.6f);
        choosing = true; choseA = choseB = false;
        while (!choseA && !choseB) yield return null;
        choosing = false;
        SoundManager.StopLoop();
        yield return UIFx.Fade(cg, 0f, 0.3f);
        choice.SetActive(false);

        if (choseA) yield return EndingA(monitor, flash);
        else yield return EndingB(monitor, flash, clock, ctrl);
    }

    IEnumerator EndingA(Image monitor, Image flash)
    {
        PlayerPrefs.SetInt("0404_endingA", 1); PlayerPrefs.Save();
        yield return dlg.Sequence("방송|관찰을 계속합니다.", "나|…그래.", "나|그럼, 다음 회차에서 봐.");
        SoundManager.Play("static", 0.8f);
        StartCoroutine(glitch.Burst(0.5f, 0.6f));
        monitor.sprite = S("cam_room404");
        yield return dlg.Auto("방송|SUBJECT 404-10, 투입.", 1.2f);
        SoundManager.Play("buzz", 0.6f);
        yield return dlg.Auto("화면 속 ROOM 404. 시계는 04:04.", 1.2f);
        yield return dlg.Auto("404-10|…여기가 어디지?", 1.6f);
        dlg.Hide();
        SoundManager.StopAmb();
        flash.color = new Color(0, 0, 0, 0);
        yield return UIFx.FadeGraphic(flash, 1f, 0.8f);
        yield return new WaitForSeconds(0.8f);
        if (!retry) GameState.EndRun();
        GameState.fromRetry = retry;
        GameState.ending = "A";
        SceneLoader.I.Load("Ending", 1.0f);
    }

    IEnumerator EndingB(Image monitor, Image flash, Text clock, Image ctrl)
    {
        PlayerPrefs.SetInt("0404_endingB", 1); PlayerPrefs.Save();
        SoundManager.Play("power_on", 0.9f);
        yield return dlg.Auto("철컥—", 0.6f);
        dlg.Hide();
        // 화면이 하나씩 꺼진다
        SoundManager.StopAmb();
        var dark = Get<Image>("CutDark");
        for (int i = 1; i <= 6; i++)
        {
            SoundManager.Play("switch", 0.6f);
            SetAlpha(dark, i / 7f);
            yield return new WaitForSeconds(0.35f);
        }
        SoundManager.Play("hangup", 0.7f);
        monitor.gameObject.SetActive(false);
        SetAlpha(dark, 0.95f);
        yield return new WaitForSeconds(1.0f);

        // 처음으로 시간이 움직인다
        clock.gameObject.SetActive(true);
        clock.text = "04:04";
        clock.color = new Color(1f, 0.23f, 0.19f, 1f);
        SoundManager.Play("blip", 1f);
        yield return new WaitForSeconds(1.6f);
        SoundManager.Play("heartbeat", 0.9f);
        yield return new WaitForSeconds(1.0f);
        SoundManager.Play("success", 1f);
        clock.text = "04:05";
        clock.color = new Color(0.29f, 0.88f, 0.48f, 1f);
        StartCoroutine(UIFx.Punch(clock.transform, 0.3f, 0.4f));
        yield return new WaitForSeconds(1.0f);
        yield return dlg.Sequence(
            "나|…시간이, 움직였어.",
            "멀리서 문이 열리는 소리가 들린다. 바람이 분다.",
            "나|고마워.",
            "나|이제… 그만 봐도 돼.");
        SoundManager.Play("door_open", 0.7f);
        flash.color = new Color(1, 1, 1, 0);
        yield return UIFx.FadeGraphic(flash, 1f, 1.6f);
        yield return new WaitForSeconds(0.6f);
        if (!retry) GameState.EndRun();
        GameState.fromRetry = retry;
        GameState.ending = "B";
        SceneLoader.I.Load("Ending", 1.0f);
    }

    // =========================================================
    //  힌트
    // =========================================================
    static readonly string[][] Hints =
    {
        new[] { "엘리베이터는 '관제실 코드' 가 필요하다.", "서버실 MAIN CLOCK 과 프린터 종이, 초기화실 벽(UV 홀드)을 확인하자.", "04:04 다음 시각 → 코드는 0405." },
        new[] { "엘리베이터가 도착했다.", "엘리베이터를 탭하자.", "엘리베이터를 탭하자." },
    };

    int CurrentStage() => codeOk ? 1 : 0;

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
        hintLevel++;
        idle = 0f;
        Run(dlg.Sequence("힌트 " + Mathf.Min(hintLevel, 3) + "/3|" + h));
    }
}
