// =============================================================
//  Chapter 2 - THE LOOP (반복되는 복도) 전체 퍼즐 진행 관리
//
//  화면: 복도 입구 → 객실 복도 → 배전함 구역 → 복도 끝 (끝에서 멈춤)
//
//  진행
//   카트 스와이프 → 분필·점검표 → (분필로 벽에 X)
//   → 비상구를 열려고 하면 복도 입구로 되돌아옴 (X 는 그대로!)
//   → UV 손전등으로 X 벽을 홀드하면 수많은 옛 표시 + "왼쪽부터 세어."
//   → 세 문 노크 3 · 1 · 2 → 배전함 덮개 위로 스와이프 → 코드 312
//   → 레버 2초 홀드 → 전원 복구 / SUBJECT 404 DETECTED
//   → 비상구 → 하얀 시설 복도 → CCTV 속 여러 명의 나 → CHAPTER 2 CLEAR
// =============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chapter02Manager : MonoBehaviour
{
    public static Chapter02Manager I;

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
    HoldLever lever;
    SwipeMover cart, flap;

    readonly Dictionary<string, Transform> map = new Dictionary<string, Transform>();

    // ---- 진행 상태 ----
    bool busy;
    bool cartMoved, chalkTaken, xDrawn, uvRevealed;
    bool flapOpen, boxUnlocked, powerOn, whisperDone;
    readonly bool[] knocked = new bool[3];
    int loops, exitTaps, cartTaps, flapTaps, wallTaps, leverTaps, doorTaps404, uvShortTaps, trapCount;

    Coroutine cctvCo;

    // 힌트
    float idle;
    int hintStage = -1, hintLevel;

    static readonly int[] KnockCount = { 3, 1, 2 };   // 왼쪽 · 가운데 · 오른쪽

    // =========================================================
    void Awake()
    {
        I = this;
        TapRouter.Handler = OnTap;
        var canvas = FindFirstObjectByType<Canvas>();
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true)) map[t.name] = t;

        room = Get<RoomSwipeController>("CorridorView");
        dlg = GetComponent<DialogueManager>();
        inv = GetComponent<InventoryManager>();
        zoom = GetComponent<ZoomView>();
        notes = GetComponent<ClueNotebook>();
        glitch = GetComponent<GlitchEffect>();
        keypad = GetComponent<KeypadController>();
        wallUV = Get<HoldReveal>("WallHit");
        lever = Get<HoldLever>("LeverHit");
        cart = Get<SwipeMover>("Cart");
        flap = Get<SwipeMover>("PowerFlap");

        room.onWallChanged = OnWallChanged;
        room.onBlocked = d => Say(d < 0 ? "나|뒤쪽은 막혀 있다. …ROOM 404 뿐이야." : "더 갈 곳이 없다.");
        cart.onDone = () => StartCoroutine(OnCartMoved());
        flap.onDone = () => StartCoroutine(OnFlapOpened());
        wallUV.canHold = () => inv.IsSelected("UV");
        wallUV.onRevealed = () => StartCoroutine(OnUVRevealed());
        keypad.onUnlocked = OnBoxUnlocked;
        keypad.onTrap = OnKeypadTrap;
        lever.canHold = () => boxUnlocked && !powerOn;
        lever.onDone = () => StartCoroutine(PowerOn());
        lever.onRelease = () => Say("손을 떼자 레버가 다시 튕겨 올라갔다.");

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
        SoundManager.Amb("amb_corridor", 0.55f);
        inv.Add("UV");   // Chapter 1 에서 얻은 UV 손전등은 그대로 가지고 있다
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
        SoundManager.Play("buzz", 0.8f);
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
            "나|…일단 여기서 나가야 해.",
            "나|문 번호가 다 똑같으니까… 길을 잃기 쉽겠다.",
            "주머니 속에 UV 손전등이 그대로 있다.");

        // FIND THE EXIT
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

    // =========================================================
    //  탭 처리
    // =========================================================
    public void OnTap(string id)
    {
        if (busy || dlg.IsBlocking) return;
        idle = 0f;

        switch (id)
        {
            // ---------- 화면 1 : 복도 입구 ----------
            case "room404":
                doorTaps404++;
                SoundManager.Play("tap", 0.8f);
                if (loops > 0) Say("나|…또 이 문 앞이야.");
                else if (doorTaps404 == 1) Say("방금 나온 ROOM 404 다.");
                else Say("나|다시 들어가고 싶진 않아.");
                break;

            case "evac":
                notes.Add("evac2", "복도 대피도 — 초록색 EXIT 화살표가 ROOM 404 를 가리킨다.");
                Run(zoom.Show(S("zoom_evac_exit"), null,
                    "복도 벽의 비상 대피도. 여기도 방 번호가 전부 404 다.",
                    "그런데 초록색 EXIT 화살표가… 이 방, ROOM 404 를 가리키고 있다.",
                    "나|출구가 저 방이라고?"));
                break;

            case "light":
                SoundManager.Play("buzz", 0.4f);
                Say("형광등이 지직거린다.");
                break;

            case "cart":
                if (cartMoved) { Say("카트 위에는 더러운 수건뿐이다."); break; }
                cartTaps++;
                SoundManager.Play("tap", 0.6f);
                if (cartTaps == 1) Say("낡은 청소 카트다. 바퀴가 달려 있다.");
                else Say("낡은 청소 카트다. …옆으로 밀 수 있을 것 같다.");
                break;

            case "chalk":
                if (chalkTaken) break;
                chalkTaken = true;
                GO("Chalk").SetActive(false);
                inv.Add("CHALK");
                SoundManager.Play("pickup");
                Say("분필을 얻었다.");
                break;

            case "checklist":
                notes.Add("checklist", "청소 점검표 — \"모든 문이 같아 보인다면, 문이 대답하는 횟수를 기억할 것.\" (아래는 찢겨 나감)");
                Run(zoom.Show(S("zoom_paper"), "객실 점검표\n\n모든 문이 같아 보인다면,\n문이 대답하는 횟수를\n기억할 것.",
                    "청소 직원의 점검표다. 아래쪽은 찢겨 나갔다."));
                break;

            case "wall":
                TapWall();
                break;

            // ---------- 화면 2 : 객실 복도 ----------
            case "door_l": Run(Knock(0)); break;
            case "door_m": Run(Knock(1)); break;
            case "door_r": Run(Knock(2)); break;

            case "sign":
                Say("EXIT → 화살표가 복도 끝을 가리킨다.");
                break;

            // ---------- 화면 3 : 배전함 ----------
            case "flap":
                flapTaps++;
                SoundManager.Play("tap", 0.6f);
                if (flapTaps == 1) Say("배전함 앞에 먼지 덮개가 내려와 있다.");
                else Say("먼지 덮개가 내려와 있다. …위로 밀어 올릴 수 있을 것 같다.");
                break;

            case "box":
                if (!flapOpen) { Say("배전함 앞에 먼지 덮개가 내려와 있다."); break; }
                if (!boxUnlocked) { keypad.Open(); break; }
                if (!powerOn) Say("안에 큰 레버가 있다.");
                else Say("배전함에 전원이 들어와 있다.");
                break;

            case "lever":
                if (powerOn) { Say("레버는 내려가 있다."); break; }
                leverTaps++;
                if (leverTaps == 1) Say("레버가 계속 원래 자리로 돌아간다.");
                else Say("레버가 계속 원래 자리로 돌아간다. …꾹 누르고 있어야 할 것 같다.");
                break;

            case "clock":
                notes.Add("clock2", "복도 벽시계 — 여기도 04:04 에서 멈춰 있다.");
                Say("이 시계도 04시 04분에서 멈춰 있다.");
                break;

            case "sticker":
                Say("고압 주의. 관계자 외 조작 금지.");
                break;

            // ---------- 화면 4 : 복도 끝 ----------
            case "exit":
                if (powerOn) { StartCoroutine(Escape()); break; }
                exitTaps++;
                SoundManager.Play("tap", 0.8f);
                if (exitTaps == 1) Say("잠겨 있다. 문 옆 표시등이 빨갛게 켜져 있다.");
                else StartCoroutine(Loop());
                break;

            case "lamp":
                Say(powerOn ? "표시등이 초록색이다. UNLOCKED." : "잠금 표시등이 빨갛게 켜져 있다.");
                break;

            case "cctv":
                Say(powerOn ? "빨간 불이 깜빡인다. …누군가 보고 있다." : "꺼진 CCTV 카메라다.");
                break;
        }
    }

    // ---------------- 빈 벽 (분필 X / UV) ----------------
    void TapWall()
    {
        if (inv.IsSelected("CHALK"))
        {
            if (xDrawn) { Say("이미 X 가 그려져 있다."); return; }
            Run(DrawX());
            return;
        }
        if (inv.IsSelected("UV"))
        {
            if (uvRevealed) { Say("벽 가득 보랏빛 표시가 남아 있다."); return; }
            uvShortTaps++;
            Say(uvShortTaps >= 2 ? "특별한 건 보이지 않는다. …조금 더 오래 비춰볼까?" : "특별한 건 보이지 않는다.");
            return;
        }
        if (inv.Selected != null) { Say("여기엔 " + inv.Selected + "를 쓸 수 없을 것 같다."); return; }
        if (uvRevealed) { Say("나|…이 벽에, 몇 번이나 표시를 한 거야."); return; }
        if (xDrawn) { Say("내가 그린 X 표시가 남아 있다."); return; }
        wallTaps++;
        if (wallTaps == 1) Say("아무것도 없는 벽이다. 이 부분만 벽지가 조금 밝다.");
        else if (chalkTaken) Say("아무것도 없는 벽이다. …표시를 해 둘 수 있을 것 같은데.");
        else Say("아무것도 없는 벽이다.");
    }

    IEnumerator DrawX()
    {
        busy = true;
        xDrawn = true;
        inv.Deselect();
        SoundManager.Play("chalk", 0.9f);
        var x = Get<Image>("ChalkX");
        x.gameObject.SetActive(true);
        SetAlpha(x, 0f);
        yield return UIFx.FadeGraphic(x, 1f, 0.5f);
        notes.Add("x", "복도 입구 벽 — 내가 분필로 X 를 그려 두었다.");
        busy = false;
        Say("나|헷갈리니까 표시해 두자.");
    }

    IEnumerator OnCartMoved()
    {
        cartMoved = true;
        idle = 0f;
        yield return new WaitForSeconds(0.2f);
        Say("카트 아래에 무언가 떨어져 있다.");
    }

    IEnumerator OnUVRevealed()
    {
        uvRevealed = true;
        busy = true;
        SoundManager.Play("stinger", 0.7f);
        yield return new WaitForSeconds(0.5f);
        var lines = new List<string>
        {
            "…벽 가득, 보랏빛 X 표시가 떠올랐다.",
            xDrawn ? "내가 그린 것과 똑같은 X. 수십 개." : "누군가 그린 X. 수십 개.",
            "그 아래에는 셀 수 없이 많은 줄이 그어져 있다.",
            loops > 0 ? "나|…나보다 먼저, 누군가 여기서 몇 번이나 돌아온 거야?" : "나|누가… 이렇게 많이 표시를 한 거야?",
            "구석에 작은 글씨가 있다. \"왼쪽부터 세어.\""
        };
        yield return dlg.Sequence(lines.ToArray());
        notes.Add("uvwall", "UV 벽 — 수많은 X 와 줄. 구석에 \"왼쪽부터 세어.\"");
        inv.Deselect();
        busy = false;
    }

    // ---------------- 노크 퍼즐 ----------------
    IEnumerator Knock(int i)
    {
        busy = true;
        bool allBefore = knocked[0] && knocked[1] && knocked[2];
        var door = (RectTransform)map["Door_" + "LMR"[i]];
        var cap = Get<Text>("KnockText_" + "LMR"[i]);
        cap.gameObject.SetActive(true);
        SetAlpha(cap, 1f);
        cap.text = "";

        // 가운데 문: 세 문을 다 두드린 뒤 한 번 더 → 딱 한 번 속삭임
        bool whisper = i == 1 && allBefore && !whisperDone;

        int n = KnockCount[i];
        yield return new WaitForSeconds(0.25f);
        for (int k = 0; k < n; k++)
        {
            SoundManager.Play("knock", 1f);
            cap.text += (k == 0 ? "" : " / ") + "똑";
            StartCoroutine(UIFx.Shake(door, 0.18f, 5f));
            yield return new WaitForSeconds(0.45f);
        }

        if (whisper)
        {
            whisperDone = true;
            SoundManager.StopAmb();
            yield return new WaitForSeconds(1.2f);
            SoundManager.Play("whisper", 0.9f);
            yield return dlg.Sequence("???|…거기 있지?");
            SoundManager.Amb("amb_corridor", 0.55f);
            yield return dlg.Sequence("나|…누구야?", "대답이 없다.");
        }

        yield return new WaitForSeconds(0.4f);
        StartCoroutine(UIFx.FadeGraphic(cap, 0f, 0.8f));

        bool first = !knocked[i];
        knocked[i] = true;
        busy = false;

        if (!whisper && first)
        {
            if (i == 0) Say("나|문은 열리지 않는데… 안에서 대답한다. 누가 있나?");
            else if (i == 1) Say("나|여긴 한 번이네.");
            else Say("나|문마다 소리가 달라…");
        }
        if (knocked[0] && knocked[1] && knocked[2] && !allBefore)
        {
            notes.Add("knock", "노크 — 왼쪽 문 3번 · 가운데 문 1번 · 오른쪽 문 2번");
            yield return new WaitForSeconds(0.1f);
            Say("나|문마다 대답하는 횟수가 달라. …뭔가 숫자 같은데.");
        }
    }

    // ---------------- 배전함 ----------------
    IEnumerator OnFlapOpened()
    {
        flapOpen = true;
        idle = 0f;
        var f = Get<Image>("PowerFlap");
        f.raycastTarget = false;
        yield return UIFx.FadeGraphic(f, 0f, 0.3f);
        f.gameObject.SetActive(false);
        notes.Add("box", "배전함 — 세 자리 관리 코드가 필요하다.");
        Say("덮개 뒤에 숫자 입력 장치가 드러났다. 세 자리 관리 코드가 필요한 것 같다.");
    }

    void OnKeypadTrap()
    {
        trapCount++;
        notes.Add("trap2", "관리 코드에 404 — ROOM NUMBER IS NOT A KEY. (방 번호는 열쇠가 아니다)");
        Run(dlg.Sequence("ROOM NUMBER IS NOT A KEY", trapCount == 1 ? "…방 번호는 열쇠가 아니다." : "나|404 는 아니라니까."));
    }

    void OnBoxUnlocked()
    {
        boxUnlocked = true;
        idle = 0f;
        SoundManager.Play("unlock");
        Get<Image>("PowerBox").sprite = S("obj_powerbox_open");
        GO("LeverHit").SetActive(true);
        notes.Add("box_open", "배전함 — 312 로 열림. 안에 비상 전원 레버. (HOLD TO RESTORE POWER)");
        Run(dlg.Sequence("철컥— 배전함이 열렸다.", "안에 큰 레버가 있다. 옆에 작은 글씨: HOLD TO RESTORE POWER"));
    }

    IEnumerator PowerOn()
    {
        busy = true;
        powerOn = true;
        room.locked = true;
        var leverImg = Get<Image>("Lever");
        leverImg.sprite = S("obj_lever_down");
        lever.ResetHandle();
        SoundManager.Play("power_on", 1f);
        yield return dlg.Auto("철컥!", 0.3f);

        // 복도 전체의 조명이 꺼졌다 켜진다
        var dark = Get<Image>("DarkOverlay");
        float[] seq = { 0.9f, 0.08f, 0.1f, 0.05f, 0.95f, 0.06f };
        for (int i = 0; i < seq.Length; i += 2) { SetAlpha(dark, seq[i]); yield return new WaitForSeconds(0.07f + seq[i + 1]); }
        SetAlpha(dark, 0.98f);
        SoundManager.StopAmb();
        dlg.Hide();
        yield return new WaitForSeconds(0.6f);
        SoundManager.Play("buzz", 0.7f);
        SetAlpha(dark, 0f);
        SoundManager.Amb("amb_corridor", 0.55f);

        // 비상구 잠금 해제 표시 + CCTV 켜짐
        Get<Image>("LockLamp").sprite = S("obj_lamp_green");
        Get<Image>("CCTV").sprite = S("obj_cctv_on");
        cctvCo = StartCoroutine(CctvBlink());

        yield return dlg.Sequence("나|전기가 들어왔어.");
        SoundManager.Play("chime", 0.8f);
        yield return new WaitForSeconds(1.2f);
        yield return dlg.Sequence("방송|EMERGENCY EXIT UNLOCKED.", "방송|비상구 잠금이 해제되었습니다.");
        SoundManager.Play("static", 0.7f);
        StartCoroutine(glitch.Burst(0.6f, 0.5f));
        yield return new WaitForSeconds(0.7f);
        yield return dlg.Sequence("방송|…SUBJECT 404 DETECTED.", "나|…대상자?");
        notes.Add("power", "전원 복구 — 안내 방송 \"SUBJECT 404 DETECTED.\" 비상구 잠금 해제.");
        room.locked = false;
        busy = false;
        idle = 0f;
        Say("나|…비상구로 가 보자.");
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

    // ---------------- 반복 (비상구 → 복도 입구) ----------------
    IEnumerator Loop()
    {
        busy = true;
        room.locked = true;
        dlg.Hide();
        var dark = Get<Image>("DarkOverlay");
        var title = Get<Text>("RoomTitle");

        // 손잡이가 덜컥 → 형광등 깜빡 → 암전
        StartCoroutine(UIFx.Shake((RectTransform)map["ExitDoor"], 0.3f, 4f));
        SoundManager.Play("tap", 1f);
        yield return new WaitForSeconds(0.35f);
        SoundManager.Play("loop", 0.9f);
        SoundManager.Play("buzz", 0.8f);
        for (int i = 0; i < 4; i++) { SetAlpha(dark, i % 2 == 0 ? 0.85f : 0.1f); yield return new WaitForSeconds(0.08f); }
        if (loops >= 1) StartCoroutine(glitch.Burst(0.6f, 0.4f + 0.1f * loops));
        title.text = "C0RR1D0R";
        SetAlpha(dark, 1f);
        SoundManager.StopAmb();
        yield return new WaitForSeconds(loops >= 2 ? 0.6f : 1.0f);

        room.JumpTo(0);
        loops++;
        exitTaps = 1;   // 다음부터는 탭하면 바로 반복
        title.text = "CORRIDOR";
        SoundManager.Amb("amb_corridor", 0.55f);
        yield return UIFx.FadeGraphic(dark, 0f, 0.6f);
        room.locked = false;

        if (loops == 1)
        {
            notes.Add("loop", "비상구 — 열려고 하면 복도 입구로 되돌아온다.");
            if (xDrawn)
                yield return dlg.Sequence("나|어…?", "나|나 분명 끝까지 갔는데?",
                    "…벽에 내가 그린 X 가 그대로 있다.",
                    "나|이거… 내가 그린 거잖아.", "나|앞으로 걸었는데, 왜 다시 여기야?");
            else
                yield return dlg.Sequence("나|어…?", "나|나 분명 끝까지 갔는데?",
                    cartMoved ? "나|저 카트… 아까 내가 밀어 둔 그 자리 그대로야." : "나|저 카트… 아까 그 자리 그대로야.",
                    "나|…같은 곳인지 확인하려면, 표시라도 해 둬야겠어.");
        }
        else if (loops == 2)
        {
            notes.Add("loop2", "반복 — 전기가 들어와야 비상구가 열리는 것 같다.");
            yield return dlg.Sequence("나|길이 반복되는 게 아니야…", "나|이 공간이 나를 처음으로 돌려보내고 있어.",
                "나|…비상구에 불이 안 들어와 있었어. 전기가 필요한 건가?");
        }
        else
        {
            Say("…또 입구다.");
        }
        busy = false;
        idle = 0f;
    }

    // ---------------- 탈출 + 마지막 반전 ----------------
    IEnumerator Escape()
    {
        busy = true;
        room.locked = true;
        GO("InputBlocker").SetActive(true);
        dlg.Hide();
        if (cctvCo != null) StopCoroutine(cctvCo);

        SoundManager.Play("unlock");
        yield return dlg.Auto("철컥.", 0.5f);
        SoundManager.Play("door_open");
        Get<Image>("ExitDoor").sprite = S("obj_exit_open");
        yield return dlg.Auto("나|드디어 나갈 수 있겠네.", 1.0f);
        dlg.Hide();

        // 문 너머: 하얀 시설 복도
        var cut = Get<CanvasGroup>("CutscenePanel");
        var hall = Get<Image>("FacilityHall");
        var flash = Get<Image>("Flash");
        var monitor = Get<Image>("Monitor");
        cut.gameObject.SetActive(true);
        cut.alpha = 0f;
        monitor.gameObject.SetActive(false);
        hall.transform.localScale = Vector3.one;
        flash.color = new Color(1, 1, 1, 1);
        SoundManager.Amb("amb_facility", 0.6f);
        yield return UIFx.Fade(cut, 1f, 0.6f);
        yield return UIFx.FadeGraphic(flash, 0f, 1.4f);

        yield return dlg.Auto("문 너머는… 바깥이 아니었다.", 1.0f);
        yield return dlg.Auto("새하얀 복도. 벽에는 RESTRICTED AREA.", 1.2f);
        yield return dlg.Auto("나|여긴… 모텔이 아니잖아.", 1.2f);
        dlg.Hide();

        StartCoroutine(UIFx.Scale(hall.transform, Vector3.one * 1.06f, 1.6f));
        for (int i = 0; i < 3; i++) { SoundManager.Play("step", 0.8f); yield return new WaitForSeconds(0.5f); }

        // 뒤에서 비상구가 닫힌다
        SoundManager.Play("door_slam");
        flash.color = new Color(0, 0, 0, 0.5f);
        StartCoroutine(UIFx.FadeGraphic(flash, 0f, 0.4f));
        StartCoroutine(UIFx.Shake((RectTransform)cut.transform, 0.5f, 14f));
        yield return dlg.Auto("쾅!", 0.8f);
        dlg.Hide();
        yield return new WaitForSeconds(0.6f);

        // 벽의 작은 CCTV 모니터가 켜진다
        SoundManager.Play("static", 0.7f);
        monitor.gameObject.SetActive(true);
        monitor.transform.localScale = new Vector3(1f, 0.05f, 1f);
        yield return UIFx.Scale(monitor.transform, Vector3.one, 0.25f);
        SoundManager.Play("buzz", 0.4f);
        yield return dlg.Auto("벽의 작은 모니터가 켜졌다. 방금 지나온 모텔 복도다.", 1.4f);
        yield return dlg.Auto("그런데…", 1.0f);
        SoundManager.Play("heartbeat", 0.8f);
        yield return dlg.Auto("나|잠깐…", 0.9f);
        SoundManager.Play("heartbeat", 0.9f);
        yield return dlg.Auto("나|저 사람들… 왜 다 나랑 똑같이 생겼어?", 1.8f);
        dlg.Hide();

        // 모니터가 툭 꺼진다 → 암전
        SoundManager.Play("hangup", 0.7f);
        StartCoroutine(glitch.Burst(0.3f, 0.6f));
        yield return UIFx.Scale(monitor.transform, new Vector3(1f, 0.02f, 1f), 0.12f);
        monitor.gameObject.SetActive(false);
        SoundManager.StopAmb();
        flash.color = new Color(0, 0, 0, 0);
        yield return UIFx.FadeGraphic(flash, 1f, 0.3f);
        yield return new WaitForSeconds(1.0f);
        GameState.EndRun();
        SceneLoader.I.Load("Chapter02_Clear", 1.2f);
    }

    // =========================================================
    //  힌트 (30초 동안 진행이 없으면 전구가 깜빡임)
    // =========================================================
    static readonly string[][] Hints =
    {
        new[] { "모든 문은 같아 보이지만, 소리는 다르다.", "객실 복도의 문을 왼쪽부터 하나씩 두드려 보자.", "각 문에서 들리는 노크 횟수를 숫자로 바꾸면 된다." },
        new[] { "배전함 앞의 덮개가 위로 열릴 것 같다.", "덮개를 위쪽으로 밀어 보자.", "덮개를 위로 스와이프하자." },
        new[] { "", "UV 손전등을 선택하고, 복도 입구의 빈 벽을 길게 비춰 보자.", "노크 횟수를 왼쪽 문부터 순서대로 적으면 → 관리 코드 312." },
        new[] { "레버는 손을 떼면 다시 올라간다.", "레버를 꾹 누르고 있어 보자.", "레버를 2초 동안 누르고 있으면 전원이 돌아온다." },
        new[] { "전기가 들어왔다.", "복도 끝 비상구로 가 보자.", "비상구를 탭하자." },
    };

    int CurrentStage()
    {
        if (!(knocked[0] && knocked[1] && knocked[2])) return 0;
        if (!flapOpen) return 1;
        if (!boxUnlocked) return 2;
        if (!powerOn) return 3;
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
        if (h == "")   // 관리 코드 1단계: 점검표를 봤는지에 따라
            h = cartMoved ? "청소 점검표: \"문이 대답하는 횟수를 기억할 것.\"" : "복도 입구의 청소 카트를 옆으로 밀어 보자. 아래에 무언가 있다.";
        hintLevel++;
        idle = 0f;
        Run(dlg.Sequence("힌트 " + Mathf.Min(hintLevel, 3) + "/3|" + h));
    }
}
