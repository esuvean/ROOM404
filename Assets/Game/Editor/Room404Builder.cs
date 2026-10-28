// =============================================================
//  메뉴 [04:04 > 씬 전체 자동 생성] 을 누르면
//  Title / Chapter01_Room404 / Chapter01_Clear
//  / Chapter02_Corridor / Chapter02_Clear
//  / Chapter03_Observation / Chapter03_Clear 씬을
//  기획서(34번 Unity 씬 구성) 대로 자동으로 만들어 줍니다.
//  (그림, 버튼, 스크립트 연결까지 전부)
// =============================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Room404Builder
{
    const string ART = "Assets/Game/Art";
    const string SCENES = "Assets/Game/Scenes";
    const float FW = 480f, FH = 854f;   // 화면 크기 (세로형)
    const float PX = 4f;                // 도트 1칸 = 4픽셀

    static Font font, fontBold;

    static readonly Color Cream = Hex("cfc6ad");
    static readonly Color Dim = Hex("8a8170");
    static readonly Color Bar = Hex("0e0c0b");
    static readonly Color Red = Hex("ff3b30");
    static readonly Color Green = Hex("49e07a");

    // ---------------------------------------------------------
    [MenuItem("04:04/씬 전체 자동 생성", priority = 1)]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        FixImports();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Fonts/Galmuri11.ttf");
        fontBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/Game/Fonts/Galmuri11-Bold.ttf");
        if (font == null) { Debug.LogError("폰트를 찾을 수 없습니다: Assets/Game/Fonts/Galmuri11.ttf"); return; }
        if (fontBold == null) fontBold = font;
        Directory.CreateDirectory(SCENES);

        string title = BuildTitle();
        string room = BuildRoom();
        string clear = BuildClear("Chapter01_Clear", "CHAPTER 1", "04:04", null, "Chapter02_Corridor",
                                  "corridor_persp", "CHAPTER 2", "THE LOOP", "");
        string corridor = BuildCorridor();
        string clear2 = BuildClear("Chapter02_Clear", "CHAPTER 2", "YOU HAVE BEEN HERE BEFORE.", "당신은 이전에도 이곳에 있었습니다.", "Chapter03_Observation",
                                   "Ch2/facility_hall", "CHAPTER 3", "OBSERVATION", "");
        string obs = BuildObservation();
        string clear3 = BuildClear("Chapter03_Clear", "CHAPTER 3", "WHO IS WATCHING?", "지금 이 화면을 보고 있는 건, 누구입니까?", "Chapter04_WakeUp",
                                   "Ch3/bg_hall", "FINAL CHAPTER", "WAKE UP", "");
        string wake = BuildWakeUp();
        string ending = BuildEnding();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(title, true),
            new EditorBuildSettingsScene(room, true),
            new EditorBuildSettingsScene(clear, true),
            new EditorBuildSettingsScene(corridor, true),
            new EditorBuildSettingsScene(clear2, true),
            new EditorBuildSettingsScene(obs, true),
            new EditorBuildSettingsScene(clear3, true),
            new EditorBuildSettingsScene(wake, true),
            new EditorBuildSettingsScene(ending, true),
        };

        PlayerSettings.productName = "04:04";
        PlayerSettings.defaultWebScreenWidth = 480;
        PlayerSettings.defaultWebScreenHeight = 854;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.WebGL.decompressionFallback = true;

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(title);
        Debug.Log("<color=#49e07a>[04:04] 씬 9개 생성 완료! Title 씬에서 ▶ Play 를 눌러보세요.</color>");
    }

    [MenuItem("04:04/Chapter 2 바로 테스트 (씬 열기)", priority = 2)]
    static void OpenCh2()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene($"{SCENES}/Chapter02_Corridor.unity");
    }

    [MenuItem("04:04/Chapter 3 바로 테스트 (씬 열기)", priority = 3)]
    static void OpenCh3()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene($"{SCENES}/Chapter03_Observation.unity");
    }

    [MenuItem("04:04/Chapter 4 바로 테스트 (씬 열기)", priority = 4)]
    static void OpenCh4()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene($"{SCENES}/Chapter04_WakeUp.unity");
    }

    [MenuItem("04:04/엔딩 기록 지우기 (타이틀 04:04 로 되돌리기)", priority = 21)]
    static void ResetEndings()
    {
        PlayerPrefs.DeleteKey("0404_endingA"); PlayerPrefs.DeleteKey("0404_endingB"); PlayerPrefs.Save();
        Debug.Log("[04:04] 엔딩 기록을 지웠습니다.");
    }

    // 웹(WebGL) 빌드 → 프로젝트 폴더/docs  (GitHub Pages 가 이 docs 폴더를 그대로 보여줌)
    [MenuItem("04:04/웹 빌드 (docs 폴더)", priority = 10)]
    public static void BuildWeb()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;   // 서버 설정 없이도 열리게
        PlayerSettings.WebGL.decompressionFallback = false;
        var scenes = new List<string>();
        foreach (var sc in EditorBuildSettings.scenes) if (sc.enabled) scenes.Add(sc.path);
        var report = BuildPipeline.BuildPlayer(scenes.ToArray(), "docs", BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            PatchWebIndex("docs/index.html");
            System.IO.File.WriteAllText("docs/.nojekyll", "");
            Debug.Log("<color=#49e07a>[04:04] 웹 빌드 완료! → docs 폴더 (GitHub 에 올리면 사이트에 반영)</color>");
        }
        else
            Debug.LogError("[04:04] 웹 빌드 실패: " + report.summary.result);
    }

    // 아이폰 Safari 는 가끔 효과음 하나를 못 읽고 "EncodingError: Decoding failed" 팝업을 띄움.
    // 그 소리 하나만 조용히 건너뛰고 게임은 계속 되도록 index.html 에 처리기를 넣는다.
    static void PatchWebIndex(string path)
    {
        if (!System.IO.File.Exists(path)) return;
        string html = System.IO.File.ReadAllText(path);
        if (html.Contains("ROOM404_AUDIO_FIX")) return;
        const string marker = "showBanner: unityShowBanner,";
        const string handler = marker + @"
        // ROOM404_AUDIO_FIX: 소리 해독 실패는 팝업 없이 무시
        errorHandler: function(err, url, line) {
          var m = String(err);
          if (m.indexOf('EncodingError') >= 0 || m.indexOf('Decoding failed') >= 0 || m.indexOf('decodeAudioData') >= 0) {
            console.warn('[04:04] 오디오 해독 실패(무시함): ' + m);
            return true;
          }
          return false;
        },";
        if (!html.Contains(marker)) { Debug.LogWarning("[04:04] index.html 패치 위치를 못 찾음"); return; }
        html = html.Replace(marker, handler);
        // 탭 제목도 게임 이름으로
        html = html.Replace("<title>Unity Web Player | 0404</title>", "<title>04:04</title>");
        html = html.Replace("<title>Unity Web Player | 04:04</title>", "<title>04:04</title>");
        System.IO.File.WriteAllText(path, html);
    }

    [MenuItem("04:04/게임 화면 세로(480x854)로 보기 도움말", priority = 20)]
    static void Help()
    {
        EditorUtility.DisplayDialog("Game 화면 크기",
            "Game 창 위쪽의 해상도 메뉴(예: Full HD) → 맨 아래 + 버튼 →\nWidth 480, Height 854 로 추가하면 휴대폰처럼 세로로 보입니다.", "확인");
    }

    // 그림 가져오기 설정 확인 (도트 그림이 흐려지지 않게)
    static void FixImports()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ART }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti != null && PixelArtImportSettings.Apply(ti, p)) ti.SaveAndReimport();
        }
    }

    // =========================================================
    //  공용 도우미
    // =========================================================
    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
    static Sprite Spr(string n)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART}/{n}.png");
        if (s == null) Debug.LogWarning("그림 없음: " + n);
        return s;
    }

    static RectTransform UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // 부모의 왼쪽 위 기준 좌표 (픽셀)
    static RectTransform TL(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    // Canvas 바로 아래: 480x854 화면 기준 좌표 (화면 가운데 정렬)
    static RectTransform FR(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x - FW / 2f, FH / 2f - y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    static RectTransform Center(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(RectTransform rt, Sprite s, Color c, bool raycast)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = raycast;
        return img;
    }

    // 도트 좌표(1칸=4px)로 그림 배치
    static Image Art(string name, Transform parent, string sprite, float ax, float ay, string id = null)
    {
        var s = Spr(sprite);
        var rt = TL(UI(name, parent), ax * PX, ay * PX, s.rect.width * PX, s.rect.height * PX);
        var img = Img(rt, s, Color.white, id != null);
        if (id != null) rt.gameObject.AddComponent<InteractableObject>().id = id;
        return img;
    }

    // 눈에 안 보이는 터치 영역
    static Image Hit(string name, Transform parent, float ax, float ay, float aw, float ah, string id)
    {
        var rt = TL(UI(name, parent), ax * PX, ay * PX, aw * PX, ah * PX);
        var img = Img(rt, null, new Color(0, 0, 0, 0), true);
        rt.gameObject.AddComponent<InteractableObject>().id = id;
        return img;
    }

    static Text Txt(RectTransform rt, string text, int size, Color c, TextAnchor align, bool bold = false)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = bold ? fontBold : font;
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.15f;
        t.raycastTarget = false;
        t.supportRichText = false;
        return t;
    }

    static Button Btn(RectTransform rt, Sprite s, Color c)
    {
        var img = Img(rt, s, c, true);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.highlightedColor = new Color(1, 1, 1, 1);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1);
        cb.selectedColor = Color.white;
        b.colors = cb;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        return b;
    }

    static Button TextButton(string name, Transform parent, string label, int size, Color bg, Color fg)
    {
        var rt = UI(name, parent);
        var b = Btn(rt, Spr("ui_white"), bg);
        Txt(Stretch(UI("Label", rt)), label, size, fg, TextAnchor.MiddleCenter, true);
        return b;
    }

    static void Base(out Canvas canvas)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();

        var cgo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.layer = 5;
        canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(FW, FH);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var es = new GameObject("EventSystem", typeof(EventSystem));
        var mod = es.AddComponent<InputSystemUIInputModule>();
        mod.AssignDefaultActions();
    }

    static CanvasGroup FadePanel(Transform canvas)
    {
        var rt = Stretch(UI("FadePanel", canvas));
        Img(rt, Spr("ui_white"), Color.black, true);
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    static string Save(string name)
    {
        string path = $"{SCENES}/{name}.unity";
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    // =========================================================
    //  1) Title
    // =========================================================
    static string BuildTitle()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;

        var frame = FR(UI("TitleFrame", cv), 0, 0, FW, FH);
        frame.gameObject.AddComponent<RectMask2D>();
        Img(frame, Spr("ui_white"), Color.black, false);
        var bg = Img(Center(UI("Background", frame), 0, 0, 600, 860), Spr("corridor_persp"), Color.white, false);
        Img(Stretch(UI("Vignette", frame)), Spr("fx_vignette"), Color.white, false);

        var logo = Txt(TL(UI("Logo", frame), 0, 300, FW, 90), "04:04", 72, Cream, TextAnchor.MiddleCenter, true);
        logo.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
        var sub = Txt(TL(UI("SubTitle", frame), 0, 392, FW, 40), "ROOM 404", 24, Dim, TextAnchor.MiddleCenter, true);
        var tap = Txt(TL(UI("TapToStart", frame), 0, 690, FW, 40), "TAP TO START", 24, Cream, TextAnchor.MiddleCenter, true);
        Txt(TL(UI("Credit", frame), 0, 818, FW, 24), "Font: Galmuri (OFL)", 12, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter);

        var catcherRt = Stretch(UI("TapArea", cv));
        Img(catcherRt, null, new Color(0, 0, 0, 0), true);
        var catcher = catcherRt.gameObject.AddComponent<TapCatcher>();

        var fade = FadePanel(cv);

        var gm = new GameObject("GameManager");
        var ts = gm.AddComponent<TitleScreen>();
        ts.tapText = tap; ts.catcher = catcher; ts.bg = bg; ts.logo = logo;
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 1.5f;

        return Save("Title");
    }

    // =========================================================
    //  2) Chapter01_Room404
    // =========================================================
    static string BuildRoom()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");

        // ---------------- RoomView ----------------
        var roomView = FR(UI("RoomView", cv), 0, 46, FW, 688);
        Img(roomView, Spr("ui_white"), Color.black, true);
        roomView.gameObject.AddComponent<RectMask2D>();
        var swipe = roomView.gameObject.AddComponent<RoomSwipeController>();

        RectTransform Wall(string name, string sprite)
        {
            var w = TL(UI(name, roomView), 0, 0, FW, 688);
            Img(w, Spr(sprite), Color.white, true);
            return w;
        }

        // 화면 1 — 침대
        var wBed = Wall("Wall_Bed", "wall_bed");
        Hit("BedHit", wBed, 2, 96, 80, 50, "bed");
        Hit("LampHit", wBed, 84, 78, 16, 22, "lamp");
        Art("Photo", wBed, "obj_photo", 24, 26, "photo");
        Art("Clock", wBed, "obj_clock", 97, 90, "clock");

        // 화면 2 — 창문
        var wWin = Wall("Wall_Window", "wall_window");
        Hit("WindowHit", wWin, 24, 22, 72, 84, "window");
        Hit("RadiatorHit", wWin, 34, 112, 52, 10, "radiator");
        Art("Scratch", wWin, "obj_scratch", 40, 56, "scratch");
        Art("Key", wWin, "obj_key", 68, 101, "key");
        var curtainsRt = TL(UI("Curtains", wWin), 14 * PX, 14 * PX, 92 * PX, 102 * PX);
        Img(curtainsRt, null, new Color(0, 0, 0, 0), true);
        var curL = Art("Curtain_L", curtainsRt, "obj_curtain_l", 0, 0);
        var curR = Art("Curtain_R", curtainsRt, "obj_curtain_r", 42, 0);
        var curtain = curtainsRt.gameObject.AddComponent<CurtainController>();
        curtain.left = curL.rectTransform; curtain.right = curR.rectTransform; curtain.openOffset = 36 * PX;

        // 화면 4 — 출입문
        var wDoor = Wall("Wall_Door", "wall_door");
        Art("Door", wDoor, "obj_door_closed", 36, 22, "door");
        Hit("EvacHit", wDoor, 5, 45, 24, 22, "evac");
        Hit("SwitchPlateHit", wDoor, 98, 88, 10, 12, "switchplate");
        var kScreen = TL(UI("KeypadScreen", wDoor), 95 * PX, 59 * PX, 12 * PX, 5 * PX);
        Img(kScreen, Spr("ui_white"), new Color(Green.r, Green.g, Green.b, 0.55f), false);
        kScreen.gameObject.SetActive(false);
        Hit("KeypadHit", wDoor, 90, 54, 22, 30, "keypad");

        // 화면 3 — 거울 / 책상
        var wMir = Wall("Wall_Mirror", "wall_mirror");
        Hit("DeskHit", wMir, 10, 100, 100, 42, "desk");
        Art("Switch", wMir, "obj_switch_off", 55, 56, "switch");
        var mirrorRt = TL(UI("Mirror", wMir), 36 * PX, 18 * PX, 48 * PX, 76 * PX);
        var mirrorCtrl = mirrorRt.gameObject.AddComponent<MirrorController>();
        mirrorCtrl.mirror = mirrorRt; mirrorCtrl.slideDistance = 40 * PX;
        var glass = Art("MirrorGlass", mirrorRt, "obj_mirror", 0, 0);
        glass.raycastTarget = true;
        var glow = Img(TL(UI("UVGlow", mirrorRt), -10 * PX, 6 * PX, 68 * PX, 68 * PX), Spr("fx_uvglow"), new Color(1, 1, 1, 0), false);
        var uvText = Art("UVText", mirrorRt, "obj_uv_text", 0, 0);
        var uvGroup = uvText.gameObject.AddComponent<CanvasGroup>();
        uvGroup.alpha = 0; uvGroup.blocksRaycasts = false; uvGroup.interactable = false;
        var hold = glass.gameObject.AddComponent<HoldReveal>();
        hold.revealGroup = uvGroup; hold.glow = glow; hold.holdTime = 1.5f;
        Art("Note", mirrorRt, "obj_note", 33, 8, "note");
        var hand = Art("Handprint", mirrorRt, "obj_handprint", 36, 60, "handprint");
        hand.gameObject.SetActive(false);
        Art("DrawerClosed", wMir, "obj_drawer_closed", 40, 110, "drawer");
        var drawerOpen = Art("DrawerOpen", wMir, "obj_drawer_open", 36, 108);
        Art("UVItem", drawerOpen.transform, "obj_uv_item", 6, 6, "uvitem");
        Art("MemoItem", drawerOpen.transform, "obj_memo_item", 28, 5, "memo");
        drawerOpen.gameObject.SetActive(false);
        Art("Phone", wMir, "obj_phone", 86, 86, "phone");

        // 순서: 침대 → 창문 → 출입문 → 거울/책상
        swipe.walls = new[] { wBed, wWin, wDoor, wMir };

        // 방 위에 덮는 효과들
        Img(Stretch(UI("DarkOverlay", roomView)), Spr("ui_white"), new Color(0, 0, 0, 0), false);
        Img(Stretch(UI("Vignette", roomView)), Spr("fx_vignette"), Color.white, false);
        // 화살표는 오브젝트(시계, 전화기 등)와 겹치지 않는 높이에 작게 배치
        var arrowL = Btn(TL(UI("ArrowLeft", roomView), 0, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowL.transform), 0, 0, 21, 39), Spr("icon_arrow_l"), Color.white, false);
        var arrowR = Btn(TL(UI("ArrowRight", roomView), 440, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowR.transform), 0, 0, 21, 39), Spr("icon_arrow_r"), Color.white, false);

        // ---------------- TopBar ----------------
        var top = FR(UI("TopBar", cv), 0, 0, FW, 46);
        Img(top, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", top), 0, 44, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var gear = Btn(TL(UI("GearButton", top), 4, 1, 44, 44), null, new Color(0, 0, 0, 0));
        Img(Center(UI("GearIcon", gear.transform), 0, 0, 24, 24), Spr("icon_gear"), Color.white, false);
        Txt(TL(UI("RoomTitle", top), 0, 4, FW, 26), "ROOM 404", 24, Cream, TextAnchor.MiddleCenter, true);
        Txt(TL(UI("WallDots", top), 0, 30, FW, 14), "■ □ □ □", 12, Dim, TextAnchor.MiddleCenter);
        var noteBtn = Btn(TL(UI("NoteButton", top), 432, 1, 44, 44), null, new Color(0, 0, 0, 0));
        var noteIcon = Img(Center(UI("NoteIcon", noteBtn.transform), 0, 0, 24, 24), Spr("icon_note"), Color.white, false);
        var newDot = Img(TL(UI("NewDot", noteBtn.transform), 30, 6, 8, 8), Spr("ui_white"), Red, false);

        // ---------------- InventoryPanel ----------------
        var invRt = FR(UI("InventoryPanel", cv), 0, 734, FW, 120);
        Img(invRt, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", invRt), 0, 0, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var invLabel = Txt(TL(UI("ItemLabel", invRt), 44, 8, 400, 16), "ITEM", 12, Dim, TextAnchor.MiddleLeft, true);
        var slots = new Button[4];
        var icons = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var srt = TL(UI(i == 0 ? "KeyButton" : i == 1 ? "UVButton" : "Slot" + (i + 1), invRt), 44 + i * 104, 30, 80, 80);
            slots[i] = Btn(srt, Spr("ui_slot"), Color.white);
            slots[i].transition = Selectable.Transition.None;
            icons[i] = Img(Center(UI("Icon", srt), 0, 0, 64, 64), Spr("icon_key"), Color.white, false);
            icons[i].gameObject.SetActive(false);
        }
        var inv = gm.AddComponent<InventoryManager>();
        inv.slots = slots; inv.icons = icons; inv.label = invLabel;
        inv.slotSprite = Spr("ui_slot"); inv.slotSelectedSprite = Spr("ui_slot_sel");
        inv.keyIcon = Spr("icon_key"); inv.uvIcon = Spr("icon_uv");

        // ---------------- ZoomPanel (확대 화면) ----------------
        var zoomRt = Stretch(UI("ZoomPanel", cv));
        Img(zoomRt, Spr("ui_white"), new Color(0, 0, 0, 0.88f), true);
        var zoomCatcher = zoomRt.gameObject.AddComponent<TapCatcher>();
        var zoomImg = Img(Center(UI("ZoomImage", zoomRt), 0, 70, 300, 300), Spr("zoom_clock"), Color.white, false);
        var paper = Txt(Center(UI("PaperText", zoomImg.transform), 0, 0, 280, 200), "", 24, Hex("2a2420"), TextAnchor.MiddleCenter, true);
        var zoom = gm.AddComponent<ZoomView>();
        zoom.panel = zoomRt.gameObject; zoom.image = zoomImg; zoom.paperText = paper; zoom.catcher = zoomCatcher;

        // ---------------- KeypadPanel (번호키) ----------------
        var kpRt = Stretch(UI("KeypadPanel", cv));
        Img(kpRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var kpCatcher = kpRt.gameObject.AddComponent<TapCatcher>();
        var body = Center(UI("KeypadBody", kpRt), 0, 30, 320, 480);
        Img(body, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("KeypadLabel", body), 0, 14, 320, 16), "ROOM 404", 12, Dim, TextAnchor.MiddleCenter, true);
        Img(TL(UI("DisplayBg", body), 28, 36, 264, 64), Spr("ui_white"), Hex("0c1310"), false);
        var display = Txt(TL(UI("Display", body), 28, 36, 264, 64), "____", 48, Green, TextAnchor.MiddleCenter, true);
        var digits = new Button[10];
        string[] order = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "" };
        Button back = null;
        for (int i = 0; i < 12; i++)
        {
            if (order[i] == "") continue;
            int col = i % 3, row = i / 3;
            var brt = TL(UI("Key_" + (order[i] == "<" ? "Back" : order[i]), body), 40 + col * 88, 116 + row * 64, 64, 52);
            var b = Btn(brt, Spr("ui_keybtn"), Color.white);
            Txt(Stretch(UI("Label", brt)), order[i] == "<" ? "←" : order[i], 24, Cream, TextAnchor.MiddleCenter, true);
            if (order[i] == "<") back = b; else digits[int.Parse(order[i])] = b;
        }
        var enterRt = TL(UI("Key_Enter", body), 40, 376, 240, 56);
        var enter = Btn(enterRt, Spr("ui_keybtn"), Hex("9fd9b0"));
        Txt(Stretch(UI("Label", enterRt)), "ENTER", 24, Cream, TextAnchor.MiddleCenter, true);
        var closeRt = Center(UI("Key_Close", kpRt), 0, -250, 160, 44);
        var close = Btn(closeRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", closeRt)), "닫기", 24, Cream, TextAnchor.MiddleCenter, true);
        var keypad = gm.AddComponent<KeypadController>();
        keypad.panel = kpRt.gameObject; keypad.body = body; keypad.display = display;
        keypad.digits = digits; keypad.backButton = back; keypad.enterButton = enter; keypad.closeButton = close;
        keypad.dimCatcher = kpCatcher;

        // ---------------- CluePanel (단서 노트) ----------------
        var clueRt = Stretch(UI("CluePanel", cv));
        Img(clueRt, Spr("ui_white"), new Color(0, 0, 0, 0.8f), true);
        var clueCatcher = clueRt.gameObject.AddComponent<TapCatcher>();
        var book = Center(UI("Notebook", clueRt), 0, 30, 440, 680);
        Img(book, Spr("ui_white"), Hex("cfc6ad"), true);
        Img(TL(UI("BookEdge", book), 0, 0, 440, 56), Spr("ui_white"), Hex("5a2a26"), false);
        Txt(TL(UI("BookTitle", book), 0, 0, 440, 56), "단서 노트", 24, Hex("f0e6d0"), TextAnchor.MiddleCenter, true);
        var clueText = Txt(TL(UI("ClueList", book), 20, 72, 400, 596), "", 24, Hex("2a2420"), TextAnchor.UpperLeft);
        clueText.lineSpacing = 1.0f;
        var clueClose = TextButton("ClueClose", clueRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)clueClose.transform, 0, -340, 160, 40);
        var notes = gm.AddComponent<ClueNotebook>();
        notes.panel = clueRt.gameObject; notes.listText = clueText; notes.openButton = noteBtn; notes.closeButton = clueClose;
        notes.dimCatcher = clueCatcher; notes.icon = noteIcon.rectTransform; notes.newDot = newDot;

        // ---------------- CutscenePanel (복도 연출) ----------------
        var cutRt = FR(UI("CutscenePanel", cv), 0, 0, FW, FH);
        Img(cutRt, Spr("ui_white"), Color.black, true);
        cutRt.gameObject.AddComponent<RectMask2D>();
        var cutCg = cutRt.gameObject.AddComponent<CanvasGroup>();
        Img(Center(UI("CorridorPersp", cutRt), 0, 0, 600, 860), Spr("corridor_persp"), Color.white, false);
        var side = Img(TL(UI("CorridorSide", cutRt), 0, 83, 1600, 688), Spr("corridor_side"), Color.white, false);
        side.gameObject.SetActive(false);
        Img(Stretch(UI("CutVignette", cutRt)), Spr("fx_vignette"), Color.white, false);
        Img(Stretch(UI("Flash", cutRt)), Spr("ui_white"), new Color(1, 1, 1, 0), false);
        cutRt.gameObject.SetActive(false);

        // ---------------- DialoguePanel (대사) ----------------
        var blockerRt = Stretch(UI("DialogueBlocker", cv));
        Img(blockerRt, null, new Color(0, 0, 0, 0), true);
        blockerRt.gameObject.AddComponent<TapCatcher>();
        var dlgRt = FR(UI("DialoguePanel", cv), 16, 586, 448, 136);
        var dlgCg = dlgRt.gameObject.AddComponent<CanvasGroup>();
        dlgCg.blocksRaycasts = false;
        Img(dlgRt, Spr("ui_white"), Hex("6b6152"), false);
        Img(TL(UI("Inner", dlgRt), 2, 2, 444, 132), Spr("ui_white"), new Color(0.06f, 0.05f, 0.045f, 0.94f), false);
        var speaker = Txt(TL(UI("Speaker", dlgRt), 18, 10, 300, 16), "", 12, Hex("e3c27a"), TextAnchor.MiddleLeft, true);
        var bodyT = Txt(TL(UI("Body", dlgRt), 18, 32, 412, 96), "", 24, Cream, TextAnchor.UpperLeft);
        var next = Txt(TL(UI("NextMark", dlgRt), 408, 108, 24, 20), "▼", 12, Cream, TextAnchor.MiddleCenter);
        var dlg = gm.AddComponent<DialogueManager>();
        dlg.panel = dlgCg; dlg.speakerText = speaker; dlg.bodyText = bodyT; dlg.nextMark = next; dlg.blocker = blockerRt.gameObject;

        // ---------------- 노이즈 / 입력 차단 / 시작 연출 / 페이드 ----------------
        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        var glitch = gm.AddComponent<GlitchEffect>();
        glitch.noise = noise;
        keypad.glitch = glitch;

        var blockRt = Stretch(UI("InputBlocker", cv));
        Img(blockRt, null, new Color(0, 0, 0, 0), true);
        blockRt.gameObject.SetActive(false);

        var introRt = Stretch(UI("IntroPanel", cv));
        Img(introRt, Spr("ui_white"), Color.black, true);
        introRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Center(UI("IntroText", introRt), 0, 0, FW, 100), "04:04", 72, Red, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.3f;

        // ---------------- 퍼즐 관리자 ----------------
        var mgr = gm.AddComponent<Chapter01Manager>();
        string[] needed = { "obj_switch_on", "obj_switch_off", "obj_phone", "obj_phone_ring", "obj_door_open",
                            "zoom_clock", "zoom_photo", "zoom_scratch", "zoom_evac", "zoom_paper" };
        mgr.sprites = new List<Sprite>();
        foreach (var n in needed) mgr.sprites.Add(Spr(n));

        return Save("Chapter01_Room404");
    }

    // =========================================================
    //  3) ChapterXX_Clear  (결과 화면 → 다음 챕터 / 추후 제작)
    // =========================================================
    static string BuildClear(string sceneName, string chapterLabel, string timeCode, string extra, string nextScene,
                             string walkSprite, string walkTitle, string walkSubLabel, string soonLabel)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");
        var cs = gm.AddComponent<ClearScreen>();
        cs.nextScene = nextScene;

        var frame = FR(UI("ClearFrame", cv), 0, 0, FW, FH);
        Img(frame, Spr("ui_white"), Color.black, false);
        frame.gameObject.AddComponent<RectMask2D>();

        // 결과
        var clearRt = Stretch(UI("ClearGroup", frame));
        cs.clearGroup = clearRt.gameObject.AddComponent<CanvasGroup>();
        cs.chapterText = Txt(TL(UI("ChapterText", clearRt), 0, 300, FW, 40), chapterLabel, 24, Dim, TextAnchor.MiddleCenter, true);
        cs.clearText = Txt(TL(UI("ClearText", clearRt), 0, 340, FW, 90), "CLEAR", 72, Cream, TextAnchor.MiddleCenter, true);
        cs.timeCodeText = Txt(TL(UI("TimeCode", clearRt), 0, 440, FW, 30), timeCode, 24, Red, TextAnchor.MiddleCenter, true);
        float recY = 490;
        if (!string.IsNullOrEmpty(extra))
        {
            cs.extraText = Txt(TL(UI("Extra", clearRt), 0, 476, FW, 24), extra, 12, Cream, TextAnchor.MiddleCenter);
            recY = 520;
        }
        cs.recordText = Txt(TL(UI("Record", clearRt), 0, recY, FW, 24), "", 12, Dim, TextAnchor.MiddleCenter);
        var nextRt = TL(UI("NextButton", clearRt), 120, 660, 240, 56);
        cs.nextButton = Btn(nextRt, Spr("ui_white"), new Color(1, 1, 1, 0.06f));
        nextRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Stretch(UI("Label", nextRt)), "다음 챕터로  ▶", 24, Cream, TextAnchor.MiddleCenter, true);

        // 복도를 걸어감
        var walkRt = Stretch(UI("WalkGroup", frame));
        cs.walkGroup = walkRt.gameObject.AddComponent<CanvasGroup>();
        cs.corridor = Img(Center(UI("Corridor", walkRt), 0, 0, 600, 860), Spr(walkSprite), Color.white, false);
        Img(Stretch(UI("WalkVignette", walkRt)), Spr("fx_vignette"), Color.white, false);
        cs.walkTitle = Txt(TL(UI("WalkTitle", walkRt), 0, 640, FW, 40), walkTitle, 24, Cream, TextAnchor.MiddleCenter, true);
        cs.walkTitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        var walkSub = Txt(TL(UI("WalkSub", walkRt), 0, 680, FW, 30), walkSubLabel, 24, Dim, TextAnchor.MiddleCenter, true);
        walkSub.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        if (walkSprite.Contains("facility") || walkSprite.Contains("Ch3/"))   // 하얀 복도 위에서는 글자를 어둡게
        {
            cs.walkTitle.color = Hex("2a2420"); walkSub.color = Hex("5a2a26");
            cs.walkTitle.GetComponent<Shadow>().effectColor = new Color(1, 1, 1, 0.6f);
            walkSub.GetComponent<Shadow>().effectColor = new Color(1, 1, 1, 0.6f);
        }

        // 노이즈
        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        cs.glitch = gm.AddComponent<GlitchEffect>();
        cs.glitch.noise = noise;

        // 추후 제작
        var soonRt = FR(UI("ComingSoonGroup", cv), 0, 0, FW, FH);
        Img(soonRt, Spr("ui_white"), Color.black, true);
        cs.soonGroup = soonRt.gameObject.AddComponent<CanvasGroup>();
        Txt(TL(UI("SoonSmall", soonRt), 0, 300, FW, 24), soonLabel, 12, Dim, TextAnchor.MiddleCenter, true);
        cs.soonTitle = Txt(TL(UI("SoonTitle", soonRt), 0, 330, FW, 80), "추후 제작", 48, Cream, TextAnchor.MiddleCenter, true);
        cs.soonSub = Txt(TL(UI("SoonSub", soonRt), 0, 420, FW, 60), "다음 챕터는 아직 준비 중입니다.", 24, Dim, TextAnchor.MiddleCenter);
        var backRt = TL(UI("BackButton", soonRt), 120, 620, 240, 56);
        cs.backButton = Btn(backRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", backRt)), "◀  돌아가기", 24, Cream, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.6f;

        return Save(sceneName);
    }

    // =========================================================
    //  4) Chapter02_Corridor  (THE LOOP — 반복되는 복도)
    //     복도 입구 → 객실 복도 → 배전함 구역 → 복도 끝
    // =========================================================
    static Image Ch2Art(string name, Transform parent, string sprite, float ax, float ay, bool raycast)
    {
        var img = Art(name, parent, "Ch2/" + sprite, ax, ay);
        img.raycastTarget = raycast;
        return img;
    }

    static string BuildCorridor()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");

        // ---------------- CorridorView ----------------
        var view = FR(UI("CorridorView", cv), 0, 46, FW, 688);
        Img(view, Spr("ui_white"), Color.black, true);
        view.gameObject.AddComponent<RectMask2D>();
        var swipe = view.gameObject.AddComponent<RoomSwipeController>();
        swipe.wrap = false;

        RectTransform View(string name, string sprite)
        {
            var w = TL(UI(name, view), 0, 0, FW, 688);
            Img(w, Spr("Ch2/" + sprite), Color.white, true);
            return w;
        }

        // 화면 1 — 복도 입구
        var vEnt = View("View_Entrance", "bg_entrance");
        Hit("Room404Hit", vEnt, 4, 40, 36, 92, "room404");
        Hit("EvacHit", vEnt, 45, 51, 22, 20, "evac");
        Hit("LightHit", vEnt, 44, 0, 32, 10, "light");
        var wallRt = TL(UI("WallHit", vEnt), 73 * PX, 29 * PX, 42 * PX, 68 * PX);
        Img(wallRt, null, new Color(0, 0, 0, 0), true);
        var wallGlow = Img(TL(UI("WallUVGlow", wallRt), -10 * PX, -4 * PX, 62 * PX, 76 * PX), Spr("fx_uvglow"), new Color(1, 1, 1, 0), false);
        var marks = Ch2Art("UVMarks", wallRt, "obj_uv_marks", 0, 0, false);
        var marksCg = marks.gameObject.AddComponent<CanvasGroup>();
        marksCg.alpha = 0; marksCg.blocksRaycasts = false; marksCg.interactable = false;
        var uvMsg = Txt(TL(UI("UVMessage", marks.transform), 4, 166, 160, 16), "왼쪽부터 세어.", 12, new Color(0.8f, 0.62f, 1f), TextAnchor.MiddleLeft, true);
        var chalkX = Ch2Art("ChalkX", wallRt, "obj_chalk_x", 9, 11, false);
        chalkX.gameObject.SetActive(false);
        var wallHold = wallRt.gameObject.AddComponent<HoldReveal>();
        wallHold.revealGroup = marksCg; wallHold.glow = wallGlow; wallHold.holdTime = 1.5f; wallHold.tapId = "wall";
        Ch2Art("Chalk", vEnt, "obj_chalk", 70, 126, true).gameObject.AddComponent<InteractableObject>().id = "chalk";
        Ch2Art("Checklist", vEnt, "obj_checklist", 92, 124, true).gameObject.AddComponent<InteractableObject>().id = "checklist";
        var cart = Ch2Art("Cart", vEnt, "obj_cart", 64, 104, true);
        var cartMover = cart.gameObject.AddComponent<SwipeMover>();
        cartMover.direction = Vector2.left; cartMover.distance = 40 * PX; cartMover.tapId = "cart"; cartMover.sound = "cart";

        // 화면 2 — 객실 복도 (문 세 개, 전부 404)
        var vDoors = View("View_Doors", "bg_doors");
        Hit("SignHit", vDoors, 44, 21, 32, 11, "sign");
        string[] lmr = { "L", "M", "R" };
        string[] ids = { "door_l", "door_m", "door_r" };
        float[] dx = { 6, 44, 82 };
        for (int i = 0; i < 3; i++)
        {
            var d = Ch2Art("Door_" + lmr[i], vDoors, "obj_door404", dx[i], 42, true);
            d.gameObject.AddComponent<InteractableObject>().id = ids[i];
            var kt = Txt(TL(UI("KnockText_" + lmr[i], d.transform), -32, -44, 32 * PX + 64, 36), "", 24, Cream, TextAnchor.MiddleCenter, true);
            kt.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
            kt.gameObject.SetActive(false);
        }
        var wireDrop = Ch2Art("WireDrop", vDoors, "obj_wire", 52, 126, true);
        wireDrop.gameObject.AddComponent<InteractableObject>().id = "wire";
        wireDrop.gameObject.SetActive(false);

        // 화면 3 — 배전함 구역
        var vPow = View("View_Power", "bg_power");
        Hit("ClockHit", vPow, 92, 22, 22, 22, "clock");
        Hit("StickerHit", vPow, 8, 50, 16, 14, "sticker");
        var box = Ch2Art("PowerBox", vPow, "obj_powerbox", 32, 40, true);
        box.gameObject.AddComponent<InteractableObject>().id = "box";
        var flap = Ch2Art("PowerFlap", box.transform, "obj_power_flap", 13, 13, true);
        var flapMover = flap.gameObject.AddComponent<SwipeMover>();
        flapMover.direction = Vector2.up; flapMover.distance = 26 * PX; flapMover.tapId = "flap"; flapMover.sound = "curtain";
        // 게이지
        var gauge = TL(UI("Gauge", vPow), 40 * PX, 27 * PX, 40 * PX, 8 * PX);
        Img(gauge, Spr("Ch2/ui_gauge"), Color.white, false);
        var fill = Img(TL(UI("GaugeFill", gauge), 4, 4, 40 * PX - 8, 8 * PX - 8), Spr("ui_white"), Green, false);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0;
        var pct = Txt(TL(UI("GaugeText", gauge), -40, -26, 40 * PX + 80, 22), "POWER 0%", 12, Cream, TextAnchor.MiddleCenter, true);
        pct.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        gauge.gameObject.SetActive(false);
        // 레버 (배전함이 열리면 나타남)
        var leverHit = TL(UI("LeverHit", vPow), 44 * PX, 62 * PX, 32 * PX, 48 * PX);
        Img(leverHit, null, new Color(0, 0, 0, 0), true);
        var leverImg = Ch2Art("Lever", leverHit, "obj_lever_up", 10, 4, false);
        var hl = leverHit.gameObject.AddComponent<HoldLever>();
        hl.holdTime = 2f; hl.handle = leverImg.rectTransform; hl.travel = 14 * PX;
        hl.gaugeRoot = gauge.gameObject; hl.fill = fill; hl.percent = pct; hl.tapId = "lever";
        leverHit.gameObject.SetActive(false);

        // 화면 4 — 복도 끝 (비상구)
        var vExit = View("View_Exit", "bg_exit");
        Ch2Art("ExitDoor", vExit, "obj_exit_closed", 32, 38, true).gameObject.AddComponent<InteractableObject>().id = "exit";
        Ch2Art("ExitSign", vExit, "obj_exit_sign", 45, 26, true).gameObject.AddComponent<InteractableObject>().id = "sign";
        Ch2Art("LockLamp", vExit, "obj_lamp_red", 92, 80, true).gameObject.AddComponent<InteractableObject>().id = "lamp";
        Ch2Art("CCTV", vExit, "obj_cctv_off", 98, 12, true).gameObject.AddComponent<InteractableObject>().id = "cctv";

        swipe.walls = new[] { vEnt, vDoors, vPow, vExit };

        Img(Stretch(UI("DarkOverlay", view)), Spr("ui_white"), new Color(0, 0, 0, 0), false);
        Img(Stretch(UI("Vignette", view)), Spr("fx_vignette"), Color.white, false);
        var arrowL = Btn(TL(UI("ArrowLeft", view), 0, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowL.transform), 0, 0, 21, 39), Spr("icon_arrow_l"), Color.white, false);
        var arrowR = Btn(TL(UI("ArrowRight", view), 440, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowR.transform), 0, 0, 21, 39), Spr("icon_arrow_r"), Color.white, false);
        var find = Txt(TL(UI("FindExit", view), 0, 300, FW, 60), "FIND THE EXIT", 48, Green, TextAnchor.MiddleCenter, true);
        find.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        find.gameObject.SetActive(false);

        // ---------------- TopBar ----------------
        var top = FR(UI("TopBar", cv), 0, 0, FW, 46);
        Img(top, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", top), 0, 44, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var gear = Btn(TL(UI("GearButton", top), 4, 1, 44, 44), null, new Color(0, 0, 0, 0));
        Img(Center(UI("GearIcon", gear.transform), 0, 0, 24, 24), Spr("icon_gear"), Color.white, false);
        Txt(TL(UI("RoomTitle", top), 0, 4, FW, 26), "CORRIDOR", 24, Cream, TextAnchor.MiddleCenter, true);
        Txt(TL(UI("WallDots", top), 0, 30, FW, 14), "■ □ □ □", 12, Dim, TextAnchor.MiddleCenter);
        var noteBtn = Btn(TL(UI("NoteButton", top), 432, 1, 44, 44), null, new Color(0, 0, 0, 0));
        var noteIcon = Img(Center(UI("NoteIcon", noteBtn.transform), 0, 0, 24, 24), Spr("icon_note"), Color.white, false);
        var newDot = Img(TL(UI("NewDot", noteBtn.transform), 30, 6, 8, 8), Spr("ui_white"), Red, false);

        // ---------------- InventoryPanel (아이템 3칸 + 힌트) ----------------
        var invRt = FR(UI("InventoryPanel", cv), 0, 734, FW, 120);
        Img(invRt, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", invRt), 0, 0, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var invLabel = Txt(TL(UI("ItemLabel", invRt), 44, 8, 400, 16), "ITEM", 12, Dim, TextAnchor.MiddleLeft, true);
        var slots = new Button[3];
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var srt = TL(UI("Slot" + (i + 1), invRt), 44 + i * 104, 30, 80, 80);
            slots[i] = Btn(srt, Spr("ui_slot"), Color.white);
            slots[i].transition = Selectable.Transition.None;
            icons[i] = Img(Center(UI("Icon", srt), 0, 0, 64, 64), Spr("icon_uv"), Color.white, false);
            icons[i].gameObject.SetActive(false);
        }
        var hintRt = TL(UI("HintButton", invRt), 44 + 3 * 104, 30, 80, 80);
        var hintBtn = Btn(hintRt, Spr("ui_slot"), Hex("8a8170"));
        Img(Center(UI("HintIcon", hintRt), 0, -6, 48, 48), Spr("Ch2/icon_hint"), Color.white, false);
        Txt(TL(UI("HintLabel", hintRt), 0, 60, 80, 16), "HINT", 12, Dim, TextAnchor.MiddleCenter, true);
        var inv = gm.AddComponent<InventoryManager>();
        inv.slots = slots; inv.icons = icons; inv.label = invLabel;
        inv.slotSprite = Spr("ui_slot"); inv.slotSelectedSprite = Spr("ui_slot_sel");
        inv.keyIcon = Spr("icon_key"); inv.uvIcon = Spr("icon_uv");
        inv.extraIcons = new List<Sprite> { Spr("Ch2/icon_chalk"), Spr("Ch2/icon_wire") };

        // ---------------- ZoomPanel ----------------
        var zoomRt = Stretch(UI("ZoomPanel", cv));
        Img(zoomRt, Spr("ui_white"), new Color(0, 0, 0, 0.88f), true);
        var zoomCatcher = zoomRt.gameObject.AddComponent<TapCatcher>();
        var zoomImg = Img(Center(UI("ZoomImage", zoomRt), 0, 70, 300, 300), Spr("zoom_paper"), Color.white, false);
        var paper = Txt(Center(UI("PaperText", zoomImg.transform), 0, 0, 280, 200), "", 24, Hex("2a2420"), TextAnchor.MiddleCenter, true);
        var zoom = gm.AddComponent<ZoomView>();
        zoom.panel = zoomRt.gameObject; zoom.image = zoomImg; zoom.paperText = paper; zoom.catcher = zoomCatcher;

        // ---------------- KeypadPanel (배전함 관리 코드 3자리) ----------------
        var kpRt = Stretch(UI("KeypadPanel", cv));
        Img(kpRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var kpCatcher = kpRt.gameObject.AddComponent<TapCatcher>();
        var body = Center(UI("KeypadBody", kpRt), 0, 30, 320, 480);
        Img(body, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("KeypadLabel", body), 0, 14, 320, 16), "MAINTENANCE LOCK", 12, Dim, TextAnchor.MiddleCenter, true);
        Img(TL(UI("DisplayBg", body), 28, 36, 264, 64), Spr("ui_white"), Hex("0c1310"), false);
        var display = Txt(TL(UI("Display", body), 28, 36, 264, 64), "___", 48, Green, TextAnchor.MiddleCenter, true);
        var digits = new Button[10];
        string[] order = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "" };
        Button back = null;
        for (int i = 0; i < 12; i++)
        {
            if (order[i] == "") continue;
            int col = i % 3, row = i / 3;
            var brt = TL(UI("Key_" + (order[i] == "<" ? "Back" : order[i]), body), 40 + col * 88, 116 + row * 64, 64, 52);
            var b = Btn(brt, Spr("ui_keybtn"), Color.white);
            Txt(Stretch(UI("Label", brt)), order[i] == "<" ? "C" : order[i], 24, Cream, TextAnchor.MiddleCenter, true);
            if (order[i] == "<") back = b; else digits[int.Parse(order[i])] = b;
        }
        var enterRt = TL(UI("Key_Enter", body), 40, 376, 240, 56);
        var enter = Btn(enterRt, Spr("ui_keybtn"), Hex("9fd9b0"));
        Txt(Stretch(UI("Label", enterRt)), "ENTER", 24, Cream, TextAnchor.MiddleCenter, true);
        var closeRt = Center(UI("Key_Close", kpRt), 0, -250, 160, 44);
        var close = Btn(closeRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", closeRt)), "닫기", 24, Cream, TextAnchor.MiddleCenter, true);
        var keypad = gm.AddComponent<KeypadController>();
        keypad.panel = kpRt.gameObject; keypad.body = body; keypad.display = display;
        keypad.digits = digits; keypad.backButton = back; keypad.enterButton = enter; keypad.closeButton = close;
        keypad.dimCatcher = kpCatcher;
        keypad.answer = "312"; keypad.trap = "404"; keypad.length = 3; keypad.trapFlash = "404?";

        // ---------------- WiringPanel (배선 연결 미니게임) — 아래 인벤토리는 가리지 않음 ----------------
        var wpRt = FR(UI("WiringPanel", cv), 0, 0, FW, 734);
        Img(wpRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var wBody = Center(UI("WiringBody", wpRt), 0, 20, 360, 440);
        Img(wBody, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("WiringTitle", wBody), 0, 14, 360, 16), "MAINTENANCE · WIRING", 12, Dim, TextAnchor.MiddleCenter, true);
        var wStatus = Txt(TL(UI("WiringStatus", wBody), 0, 36, 360, 30), "", 24, Cream, TextAnchor.MiddleCenter, true);
        Color[] wc = { Hex("e0443a"), Hex("3f7ee0"), Hex("f0c838"), Hex("49c06a") };
        var wLines = new RectTransform[4];
        var wLeft = new RectTransform[4];
        var wRight = new RectTransform[4];
        var wRightImg = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var ln = UI("WireLine_" + i, wBody);
            ln.anchorMin = ln.anchorMax = new Vector2(0.5f, 0.5f);
            ln.pivot = new Vector2(0f, 0.5f);
            ln.sizeDelta = new Vector2(10, 14);
            Img(ln, Spr("ui_white"), wc[i], false);
            wLines[i] = ln;
        }
        for (int i = 0; i < 4; i++)
        {
            float y = 96 + i * 78;
            Img(TL(UI("WireSocketL_" + i, wBody), 20, y - 4, 72, 48), Spr("ui_white"), Hex("1a1f21"), false);
            wLeft[i] = TL(UI("WireL_" + i, wBody), 28, y, 56, 40);
            Img(wLeft[i], Spr("ui_white"), wc[i], true);
            Img(TL(UI("Plug", wLeft[i]), 40, 12, 16, 16), Spr("ui_white"), Hex("cfd3d1"), false);
            Img(TL(UI("WireSocketR_" + i, wBody), 268, y - 4, 72, 48), Spr("ui_white"), Hex("1a1f21"), false);
            wRight[i] = TL(UI("WireR_" + i, wBody), 276, y, 56, 40);
            wRightImg[i] = Img(wRight[i], Spr("ui_white"), Color.white, false);
        }
        var cut = TL(UI("CutMark", wBody), 92, 96, 40, 40);
        Img(cut, Spr("ui_white"), Hex("2a2420"), false);
        Txt(Stretch(UI("Label", cut)), "×", 24, Red, TextAnchor.MiddleCenter, true);
        var wClose = TextButton("WiringClose", wpRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)wClose.transform, 0, -268, 160, 44);
        var wp = gm.AddComponent<WiringPanel>();
        wp.panel = wpRt.gameObject; wp.body = wBody; wp.left = wLeft; wp.right = wRight; wp.rightImages = wRightImg;
        wp.lines = wLines; wp.cutMark = cut.gameObject; wp.status = wStatus; wp.closeButton = wClose; wp.colors = wc;

        // ---------------- CluePanel ----------------
        var clueRt = Stretch(UI("CluePanel", cv));
        Img(clueRt, Spr("ui_white"), new Color(0, 0, 0, 0.8f), true);
        var clueCatcher = clueRt.gameObject.AddComponent<TapCatcher>();
        var book = Center(UI("Notebook", clueRt), 0, 30, 440, 680);
        Img(book, Spr("ui_white"), Hex("cfc6ad"), true);
        Img(TL(UI("BookEdge", book), 0, 0, 440, 56), Spr("ui_white"), Hex("5a2a26"), false);
        Txt(TL(UI("BookTitle", book), 0, 0, 440, 56), "단서 노트", 24, Hex("f0e6d0"), TextAnchor.MiddleCenter, true);
        var clueText = Txt(TL(UI("ClueList", book), 20, 72, 400, 596), "", 24, Hex("2a2420"), TextAnchor.UpperLeft);
        clueText.lineSpacing = 1.0f;
        var clueClose = TextButton("ClueClose", clueRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)clueClose.transform, 0, -340, 160, 40);
        var notes = gm.AddComponent<ClueNotebook>();
        notes.panel = clueRt.gameObject; notes.listText = clueText; notes.openButton = noteBtn; notes.closeButton = clueClose;
        notes.dimCatcher = clueCatcher; notes.icon = noteIcon.rectTransform; notes.newDot = newDot;

        // ---------------- CutscenePanel (시설 복도 + CCTV) ----------------
        var cutRt = FR(UI("CutscenePanel", cv), 0, 0, FW, FH);
        Img(cutRt, Spr("ui_white"), Color.black, true);
        cutRt.gameObject.AddComponent<RectMask2D>();
        cutRt.gameObject.AddComponent<CanvasGroup>();
        Img(Center(UI("FacilityHall", cutRt), 0, 0, 600, 860), Spr("Ch2/facility_hall"), Color.white, false);
        Img(Center(UI("Monitor", cutRt), 0, 60, 112 * PX, 80 * PX), Spr("Ch2/cctv_clones"), Color.white, false);
        Img(Stretch(UI("CutVignette", cutRt)), Spr("fx_vignette"), new Color(1, 1, 1, 0.6f), false);
        Img(Stretch(UI("Flash", cutRt)), Spr("ui_white"), new Color(1, 1, 1, 0), false);
        cutRt.gameObject.SetActive(false);

        // ---------------- DialoguePanel ----------------
        var blockerRt = Stretch(UI("DialogueBlocker", cv));
        Img(blockerRt, null, new Color(0, 0, 0, 0), true);
        blockerRt.gameObject.AddComponent<TapCatcher>();
        var dlgRt = FR(UI("DialoguePanel", cv), 16, 586, 448, 136);
        var dlgCg = dlgRt.gameObject.AddComponent<CanvasGroup>();
        dlgCg.blocksRaycasts = false;
        Img(dlgRt, Spr("ui_white"), Hex("6b6152"), false);
        Img(TL(UI("Inner", dlgRt), 2, 2, 444, 132), Spr("ui_white"), new Color(0.06f, 0.05f, 0.045f, 0.94f), false);
        var speaker = Txt(TL(UI("Speaker", dlgRt), 18, 10, 300, 16), "", 12, Hex("e3c27a"), TextAnchor.MiddleLeft, true);
        var bodyT = Txt(TL(UI("Body", dlgRt), 18, 32, 412, 96), "", 24, Cream, TextAnchor.UpperLeft);
        var next = Txt(TL(UI("NextMark", dlgRt), 408, 108, 24, 20), "▼", 12, Cream, TextAnchor.MiddleCenter);
        var dlg = gm.AddComponent<DialogueManager>();
        dlg.panel = dlgCg; dlg.speakerText = speaker; dlg.bodyText = bodyT; dlg.nextMark = next; dlg.blocker = blockerRt.gameObject;

        // ---------------- 노이즈 / 입력 차단 / 시작 연출 / 페이드 ----------------
        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        var glitch = gm.AddComponent<GlitchEffect>();
        glitch.noise = noise;
        keypad.glitch = glitch;

        var blockRt = Stretch(UI("InputBlocker", cv));
        Img(blockRt, null, new Color(0, 0, 0, 0), true);
        blockRt.gameObject.SetActive(false);

        var introRt = Stretch(UI("IntroPanel", cv));
        Img(introRt, Spr("ui_white"), Color.black, true);
        introRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Center(UI("IntroText", introRt), 0, 20, FW, 70), "CHAPTER 2", 48, Cream, TextAnchor.MiddleCenter, true);
        Txt(Center(UI("IntroSub", introRt), 0, -36, FW, 40), "THE LOOP", 24, Red, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.3f;

        // ---------------- 퍼즐 관리자 ----------------
        var mgr = gm.AddComponent<Chapter02Manager>();
        string[] needed = { "Ch2/obj_powerbox_open", "Ch2/obj_lever_down", "Ch2/obj_lamp_green", "Ch2/obj_cctv_on", "Ch2/obj_cctv_off",
                            "Ch2/obj_exit_open", "Ch2/zoom_evac_exit", "zoom_paper" };
        mgr.sprites = new List<Sprite>();
        foreach (var n in needed) mgr.sprites.Add(Spr(n));

        return Save("Chapter02_Corridor");
    }

    // =========================================================
    //  5) Chapter03_Observation  (OBSERVATION — 관찰동)
    //     B4 복도 → 연구실 → 기록 보관실 → 관찰실 문
    // =========================================================
    static Image Ch3Art(string name, Transform parent, string sprite, float ax, float ay, bool raycast)
    {
        var img = Art(name, parent, sprite.Contains("/") ? sprite : "Ch3/" + sprite, ax, ay);
        img.raycastTarget = raycast;
        return img;
    }

    static string BuildObservation()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");

        // ---------------- LabView ----------------
        var view = FR(UI("LabView", cv), 0, 46, FW, 688);
        Img(view, Spr("ui_white"), Color.black, true);
        view.gameObject.AddComponent<RectMask2D>();
        var swipe = view.gameObject.AddComponent<RoomSwipeController>();
        swipe.wrap = false;

        RectTransform View(string name, string sprite)
        {
            var w = TL(UI(name, view), 0, 0, FW, 688);
            Img(w, Spr("Ch3/" + sprite), Color.white, true);
            return w;
        }

        // 화면 1 — B4 복도
        var vHall = View("View_Hall", "bg_hall");
        Hit("BackDoorHit", vHall, 4, 30, 34, 98, "backdoor");
        Hit("MirrorHit", vHall, 46, 38, 28, 48, "mirror");
        Hit("LogHit", vHall, 79, 52, 18, 27, "log");
        Hit("CoolerHit", vHall, 99, 64, 20, 64, "cooler");
        Hit("SignHit", vHall, 76, 19, 42, 12, "sign");

        // 화면 2 — 연구실
        var vOff = View("View_Office", "bg_office");
        Hit("BoardHit", vOff, 4, 22, 52, 40, "board");
        Hit("DeskHit", vOff, 4, 100, 80, 30, "desk");
        Hit("PCHit", vOff, 22, 66, 34, 34, "pc");
        Hit("StickyHit", vOff, 49, 63, 10, 10, "sticky");
        Hit("MugHit", vOff, 62, 88, 12, 11, "mug");
        Hit("PhotoHit", vOff, 86, 42, 26, 30, "photo");
        var coat = Ch3Art("Coat", vOff, "obj_coat", 84, 30, true);
        var coatMover = coat.gameObject.AddComponent<SwipeMover>();
        coatMover.direction = Vector2.right; coatMover.distance = 30 * PX; coatMover.tapId = "coat"; coatMover.sound = "curtain";

        // 화면 3 — 기록 보관실 (보관함 전체는 UV 홀드, 서랍은 짧게 탭)
        var vArc = View("View_Archive", "bg_archive");
        Hit("BoxHitL", vArc, 2, 110, 12, 22, "box");
        Hit("BoxHitR", vArc, 106, 102, 13, 30, "box");
        var cab = TL(UI("CabinetHit", vArc), 18 * PX, 34 * PX, 84 * PX, 96 * PX);
        Img(cab, null, new Color(0, 0, 0, 0), true);
        var cabGlow = Img(TL(UI("CabinetUVGlow", cab), -8 * PX, 0, 100 * PX, 96 * PX), Spr("fx_uvglow"), new Color(1, 1, 1, 0), false);
        string[] dids = { "d4", "d5", "d6", "d7", "d8", "d9" };
        for (int i = 0; i < 6; i++)
        {
            int c = i % 2, r = i / 2;
            var drt = TL(UI("Drawer_" + dids[i], cab), (4 + c * 40) * PX, (20 + r * 26) * PX, 36 * PX, 22 * PX);
            Img(drt, null, new Color(0, 0, 0, 0), true);
            drt.gameObject.AddComponent<TapTag>().id = dids[i];
            var op = Ch3Art("DrawerOpen_" + dids[i], drt, "obj_drawer_open", 0, 0, false);
            op.gameObject.SetActive(false);
        }
        var lamp = Ch3Art("CabinetLamp", cab, "Ch2/obj_lamp_red", 74, 3, true);
        lamp.gameObject.AddComponent<TapTag>().id = "lamp";
        var prints = Ch3Art("UVPrints", cab, "obj_uv_prints", 0, 0, false);
        var printsCg = prints.gameObject.AddComponent<CanvasGroup>();
        printsCg.alpha = 0; printsCg.blocksRaycasts = false; printsCg.interactable = false;
        var cabHold = cab.gameObject.AddComponent<HoldReveal>();
        cabHold.revealGroup = printsCg; cabHold.glow = cabGlow; cabHold.holdTime = 1.5f; cabHold.tapId = "cabinet";

        // 화면 4 — 관찰실 문
        var vDoor = View("View_ObsDoor", "bg_obsdoor");
        var doorOpen = Ch3Art("ObsDoorOpen", vDoor, "obj_obsdoor_open", 28, 34, false);
        doorOpen.gameObject.SetActive(false);
        Hit("DoorHit", vDoor, 26, 30, 68, 98, "door");
        Ch3Art("CCTV", vDoor, "Ch2/obj_cctv_off", 4, 18, true).gameObject.AddComponent<InteractableObject>().id = "cctv";
        Ch3Art("ReaderLamp", vDoor, "Ch2/obj_lamp_red", 99, 49, true).gameObject.AddComponent<InteractableObject>().id = "reader";
        Hit("ReaderHit", vDoor, 95, 61, 16, 25, "reader");
        var scanHit = TL(UI("ScannerHit", vDoor), 95 * PX, 87 * PX, 18 * PX, 22 * PX);
        Img(scanHit, null, new Color(0, 0, 0, 0), true);
        var scanGlow = Ch3Art("ScanGlow", scanHit, "obj_scan_glow", 1, 1, false);
        scanGlow.gameObject.SetActive(false);
        var gauge = TL(UI("Gauge", vDoor), 70 * PX, 114 * PX, 40 * PX, 8 * PX);
        Img(gauge, Spr("Ch2/ui_gauge"), Color.white, false);
        var fill = Img(TL(UI("GaugeFill", gauge), 4, 4, 40 * PX - 8, 8 * PX - 8), Spr("ui_white"), Hex("49e0c8"), false);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0;
        var pct = Txt(TL(UI("GaugeText", gauge), -40, -26, 40 * PX + 80, 22), "SCAN 0%", 12, Cream, TextAnchor.MiddleCenter, true);
        pct.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        gauge.gameObject.SetActive(false);
        var hl = scanHit.gameObject.AddComponent<HoldLever>();
        hl.holdTime = 2f; hl.handle = null; hl.travel = 0;
        hl.gaugeRoot = gauge.gameObject; hl.fill = fill; hl.percent = pct; hl.tapId = "scanner"; hl.label = "SCAN";

        swipe.walls = new[] { vHall, vOff, vArc, vDoor };

        Img(Stretch(UI("DarkOverlay", view)), Spr("ui_white"), new Color(0, 0, 0, 0), false);
        Img(Stretch(UI("Vignette", view)), Spr("fx_vignette"), new Color(1, 1, 1, 0.7f), false);
        var arrowL = Btn(TL(UI("ArrowLeft", view), 0, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowL.transform), 0, 0, 21, 39), Spr("icon_arrow_l"), Color.white, false);
        var arrowR = Btn(TL(UI("ArrowRight", view), 440, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowR.transform), 0, 0, 21, 39), Spr("icon_arrow_r"), Color.white, false);
        var find = Txt(TL(UI("FindExit", view), 0, 300, FW, 60), "FIND THE OBSERVER", 36, Red, TextAnchor.MiddleCenter, true);
        find.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        find.gameObject.SetActive(false);

        // ---------------- TopBar ----------------
        var top = FR(UI("TopBar", cv), 0, 0, FW, 46);
        Img(top, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", top), 0, 44, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var gear = Btn(TL(UI("GearButton", top), 4, 1, 44, 44), null, new Color(0, 0, 0, 0));
        Img(Center(UI("GearIcon", gear.transform), 0, 0, 24, 24), Spr("icon_gear"), Color.white, false);
        Txt(TL(UI("RoomTitle", top), 0, 4, FW, 26), "B4 · HALL", 24, Cream, TextAnchor.MiddleCenter, true);
        Txt(TL(UI("WallDots", top), 0, 30, FW, 14), "■ □ □ □", 12, Dim, TextAnchor.MiddleCenter);
        var noteBtn = Btn(TL(UI("NoteButton", top), 432, 1, 44, 44), null, new Color(0, 0, 0, 0));
        var noteIcon = Img(Center(UI("NoteIcon", noteBtn.transform), 0, 0, 24, 24), Spr("icon_note"), Color.white, false);
        var newDot = Img(TL(UI("NewDot", noteBtn.transform), 30, 6, 8, 8), Spr("ui_white"), Red, false);

        // ---------------- InventoryPanel (아이템 3칸 + 힌트) ----------------
        var invRt = FR(UI("InventoryPanel", cv), 0, 734, FW, 120);
        Img(invRt, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", invRt), 0, 0, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var invLabel = Txt(TL(UI("ItemLabel", invRt), 44, 8, 400, 16), "ITEM", 12, Dim, TextAnchor.MiddleLeft, true);
        var slots = new Button[3];
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var srt = TL(UI("Slot" + (i + 1), invRt), 44 + i * 104, 30, 80, 80);
            slots[i] = Btn(srt, Spr("ui_slot"), Color.white);
            slots[i].transition = Selectable.Transition.None;
            icons[i] = Img(Center(UI("Icon", srt), 0, 0, 64, 64), Spr("icon_uv"), Color.white, false);
            icons[i].gameObject.SetActive(false);
        }
        var hintRt = TL(UI("HintButton", invRt), 44 + 3 * 104, 30, 80, 80);
        Btn(hintRt, Spr("ui_slot"), Hex("8a8170"));
        Img(Center(UI("HintIcon", hintRt), 0, -6, 48, 48), Spr("Ch2/icon_hint"), Color.white, false);
        Txt(TL(UI("HintLabel", hintRt), 0, 60, 80, 16), "HINT", 12, Dim, TextAnchor.MiddleCenter, true);
        var inv = gm.AddComponent<InventoryManager>();
        inv.slots = slots; inv.icons = icons; inv.label = invLabel;
        inv.slotSprite = Spr("ui_slot"); inv.slotSelectedSprite = Spr("ui_slot_sel");
        inv.keyIcon = Spr("icon_key"); inv.uvIcon = Spr("icon_uv");
        inv.extraIcons = new List<Sprite> { Spr("Ch3/icon_card") };

        // ---------------- ZoomPanel ----------------
        var zoomRt = Stretch(UI("ZoomPanel", cv));
        Img(zoomRt, Spr("ui_white"), new Color(0, 0, 0, 0.88f), true);
        var zoomCatcher = zoomRt.gameObject.AddComponent<TapCatcher>();
        var zoomImg = Img(Center(UI("ZoomImage", zoomRt), 0, 70, 300, 300), Spr("zoom_paper"), Color.white, false);
        var paper = Txt(Center(UI("PaperText", zoomImg.transform), 0, -6, 280, 200), "", 24, Hex("2a2420"), TextAnchor.MiddleCenter, true);
        paper.lineSpacing = 1.0f;
        var zoom = gm.AddComponent<ZoomView>();
        zoom.panel = zoomRt.gameObject; zoom.image = zoomImg; zoom.paperText = paper; zoom.catcher = zoomCatcher;

        // ---------------- KeypadPanel (연구실 PC 로그인 4자리) ----------------
        var kpRt = Stretch(UI("KeypadPanel", cv));
        Img(kpRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var kpCatcher = kpRt.gameObject.AddComponent<TapCatcher>();
        var body = Center(UI("KeypadBody", kpRt), 0, 30, 320, 480);
        Img(body, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("KeypadLabel", body), 0, 14, 320, 16), "LOGIN · USER ████", 12, Dim, TextAnchor.MiddleCenter, true);
        Img(TL(UI("DisplayBg", body), 28, 36, 264, 64), Spr("ui_white"), Hex("0c1310"), false);
        var display = Txt(TL(UI("Display", body), 28, 36, 264, 64), "____", 48, Green, TextAnchor.MiddleCenter, true);
        var digits = new Button[10];
        string[] order = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "" };
        Button back = null;
        for (int i = 0; i < 12; i++)
        {
            if (order[i] == "") continue;
            int col = i % 3, row = i / 3;
            var brt = TL(UI("Key_" + (order[i] == "<" ? "Back" : order[i]), body), 40 + col * 88, 116 + row * 64, 64, 52);
            var b = Btn(brt, Spr("ui_keybtn"), Color.white);
            Txt(Stretch(UI("Label", brt)), order[i] == "<" ? "←" : order[i], 24, Cream, TextAnchor.MiddleCenter, true);
            if (order[i] == "<") back = b; else digits[int.Parse(order[i])] = b;
        }
        var enterRt = TL(UI("Key_Enter", body), 40, 376, 240, 56);
        var enter = Btn(enterRt, Spr("ui_keybtn"), Hex("9fd9b0"));
        Txt(Stretch(UI("Label", enterRt)), "ENTER", 24, Cream, TextAnchor.MiddleCenter, true);
        var closeRt = Center(UI("Key_Close", kpRt), 0, -250, 160, 44);
        var close = Btn(closeRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", closeRt)), "닫기", 24, Cream, TextAnchor.MiddleCenter, true);
        var keypad = gm.AddComponent<KeypadController>();
        keypad.panel = kpRt.gameObject; keypad.body = body; keypad.display = display;
        keypad.digits = digits; keypad.backButton = back; keypad.enterButton = enter; keypad.closeButton = close;
        keypad.dimCatcher = kpCatcher;
        keypad.answer = "0414"; keypad.trap = "0404"; keypad.length = 4; keypad.trapFlash = "DENY";

        // ---------------- SwipePanel (카드 긁기 미니게임) ----------------
        var spRt = FR(UI("SwipePanel", cv), 0, 0, FW, 734);
        Img(spRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var sBody = Center(UI("SwipeBody", spRt), 0, 20, 420, 330);
        Img(sBody, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("SwipeTitle", sBody), 0, 14, 420, 16), "ID CARD READER", 12, Dim, TextAnchor.MiddleCenter, true);
        Img(TL(UI("SwipeDisplayBg", sBody), 28, 36, 364, 52), Spr("ui_white"), Hex("0c1310"), false);
        var sDisplay = Txt(TL(UI("SwipeDisplay", sBody), 28, 36, 364, 52), "PLEASE SWIPE CARD", 24, Cream, TextAnchor.MiddleCenter, true);
        var sLamp = Img(TL(UI("SwipeLamp", sBody), 380, 12, 20, 16), Spr("ui_white"), new Color(0.3f, 0.3f, 0.3f), false);
        var track = Center(UI("SwipeTrack", sBody), 0, -40, 380, 130);
        Img(track, Spr("ui_white"), Hex("1a1f21"), false);
        Img(TL(UI("SwipeSlot", track), 0, 60, 380, 10), Spr("ui_white"), Color.black, false);
        Txt(TL(UI("SwipeArrow", track), 0, 104, 380, 20), "카드를 오른쪽 끝까지 긁으세요  →", 12, Dim, TextAnchor.MiddleCenter, true);
        var cardRt = Center(UI("SwipeCard", track), -110, 6, 140, 90);
        Img(cardRt, Spr("Ch3/zoom_card"), Color.white, true);
        var sClose = TextButton("SwipeClose", spRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)sClose.transform, 0, -230, 160, 44);
        var cs = cardRt.gameObject.AddComponent<CardSwipe>();
        cs.panel = spRt.gameObject; cs.body = sBody; cs.card = cardRt; cs.display = sDisplay; cs.lamp = sLamp; cs.closeButton = sClose;
        cs.startX = -110; cs.endX = 110;

        // ---------------- CluePanel ----------------
        var clueRt = Stretch(UI("CluePanel", cv));
        Img(clueRt, Spr("ui_white"), new Color(0, 0, 0, 0.8f), true);
        var clueCatcher = clueRt.gameObject.AddComponent<TapCatcher>();
        var book = Center(UI("Notebook", clueRt), 0, 30, 440, 680);
        Img(book, Spr("ui_white"), Hex("cfc6ad"), true);
        Img(TL(UI("BookEdge", book), 0, 0, 440, 56), Spr("ui_white"), Hex("5a2a26"), false);
        Txt(TL(UI("BookTitle", book), 0, 0, 440, 56), "단서 노트", 24, Hex("f0e6d0"), TextAnchor.MiddleCenter, true);
        var clueText = Txt(TL(UI("ClueList", book), 20, 72, 400, 596), "", 24, Hex("2a2420"), TextAnchor.UpperLeft);
        clueText.lineSpacing = 1.0f;
        var clueClose = TextButton("ClueClose", clueRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)clueClose.transform, 0, -340, 160, 40);
        var notes = gm.AddComponent<ClueNotebook>();
        notes.panel = clueRt.gameObject; notes.listText = clueText; notes.openButton = noteBtn; notes.closeButton = clueClose;
        notes.dimCatcher = clueCatcher; notes.icon = noteIcon.rectTransform; notes.newDot = newDot;

        // ---------------- CutscenePanel (관찰실) ----------------
        var cutRt = FR(UI("CutscenePanel", cv), 0, 0, FW, FH);
        Img(cutRt, Spr("ui_white"), Color.black, true);
        cutRt.gameObject.AddComponent<RectMask2D>();
        cutRt.gameObject.AddComponent<CanvasGroup>();
        Img(Center(UI("ObsRoom", cutRt), 0, 0, 600, 860), Spr("Ch3/bg_obsroom"), Color.white, false);
        var mon = Img(Center(UI("Monitor", cutRt), 0, 236, 112 * PX, 80 * PX), Spr("Ch3/cam_room404"), Color.white, false);
        mon.gameObject.SetActive(false);
        var ph = Img(Center(UI("ObsPhone", cutRt), 150, 44, 22 * 5, 14 * 5), Spr("obj_phone"), Color.white, true);
        ph.gameObject.AddComponent<InteractableObject>().id = "obsphone";
        ph.gameObject.SetActive(false);
        var phHint = Txt(Center(UI("PhoneHint", cutRt), 150, -8, 220, 30), "▲ 수화기를 탭하자", 12, Green, TextAnchor.MiddleCenter, true);
        phHint.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        phHint.gameObject.SetActive(false);
        Img(Stretch(UI("CutVignette", cutRt)), Spr("fx_vignette"), new Color(1, 1, 1, 0.8f), false);
        Img(Stretch(UI("Flash", cutRt)), Spr("ui_white"), new Color(1, 1, 1, 0), false);
        cutRt.gameObject.SetActive(false);

        // ---------------- DialoguePanel ----------------
        var blockerRt = Stretch(UI("DialogueBlocker", cv));
        Img(blockerRt, null, new Color(0, 0, 0, 0), true);
        blockerRt.gameObject.AddComponent<TapCatcher>();
        var dlgRt = FR(UI("DialoguePanel", cv), 16, 586, 448, 136);
        var dlgCg = dlgRt.gameObject.AddComponent<CanvasGroup>();
        dlgCg.blocksRaycasts = false;
        Img(dlgRt, Spr("ui_white"), Hex("6b6152"), false);
        Img(TL(UI("Inner", dlgRt), 2, 2, 444, 132), Spr("ui_white"), new Color(0.06f, 0.05f, 0.045f, 0.94f), false);
        var speaker = Txt(TL(UI("Speaker", dlgRt), 18, 10, 300, 16), "", 12, Hex("e3c27a"), TextAnchor.MiddleLeft, true);
        var bodyT = Txt(TL(UI("Body", dlgRt), 18, 32, 412, 96), "", 24, Cream, TextAnchor.UpperLeft);
        var next = Txt(TL(UI("NextMark", dlgRt), 408, 108, 24, 20), "▼", 12, Cream, TextAnchor.MiddleCenter);
        var dlg = gm.AddComponent<DialogueManager>();
        dlg.panel = dlgCg; dlg.speakerText = speaker; dlg.bodyText = bodyT; dlg.nextMark = next; dlg.blocker = blockerRt.gameObject;

        // ---------------- 노이즈 / 입력 차단 / 시작 연출 / 페이드 ----------------
        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        var glitch = gm.AddComponent<GlitchEffect>();
        glitch.noise = noise;
        keypad.glitch = glitch;

        var blockRt = Stretch(UI("InputBlocker", cv));
        Img(blockRt, null, new Color(0, 0, 0, 0), true);
        blockRt.gameObject.SetActive(false);

        var introRt = Stretch(UI("IntroPanel", cv));
        Img(introRt, Spr("ui_white"), Color.black, true);
        introRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Center(UI("IntroText", introRt), 0, 20, FW, 70), "CHAPTER 3", 48, Cream, TextAnchor.MiddleCenter, true);
        Txt(Center(UI("IntroSub", introRt), 0, -36, FW, 40), "OBSERVATION", 24, Red, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.3f;

        // ---------------- 퍼즐 관리자 ----------------
        var mgr = gm.AddComponent<Chapter03Manager>();
        string[] needed = { "Ch2/obj_lamp_green", "Ch2/obj_cctv_on", "Ch2/obj_cctv_off", "obj_phone", "obj_phone_ring", "zoom_paper",
                            "Ch3/zoom_mirror", "Ch3/zoom_photo3", "Ch3/zoom_note", "Ch3/zoom_pc", "Ch3/zoom_card",
                            "Ch3/cam_room404", "Ch3/cam_phone", "Ch3/cam_look", "Ch3/cam_self" };
        mgr.sprites = new List<Sprite>();
        foreach (var n in needed) mgr.sprites.Add(Spr(n));

        return Save("Chapter03_Observation");
    }

    // =========================================================
    //  6) Chapter04_WakeUp  (FINAL CHAPTER — 초기화실 → 서버실 → 엘리베이터)
    // =========================================================
    static Image Ch4Art(string name, Transform parent, string sprite, float ax, float ay, bool raycast)
    {
        var img = Art(name, parent, sprite.Contains("/") ? sprite : "Ch4/" + sprite, ax, ay);
        img.raycastTarget = raycast;
        return img;
    }

    static string BuildWakeUp()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");

        var view = FR(UI("FinalView", cv), 0, 46, FW, 688);
        Img(view, Spr("ui_white"), Color.black, true);
        view.gameObject.AddComponent<RectMask2D>();
        var swipe = view.gameObject.AddComponent<RoomSwipeController>();
        swipe.wrap = false;

        RectTransform View(string name, string sprite)
        {
            var w = TL(UI(name, view), 0, 0, FW, 688);
            Img(w, Spr("Ch4/" + sprite), Color.white, true);
            return w;
        }

        // 화면 1 — 초기화실
        var vReset = View("View_Reset", "bg_reset");
        Hit("MyChairHit", vReset, 6, 78, 40, 54, "mychair");
        Hit("MonitorHit", vReset, 6, 34, 28, 26, "monitor");
        Hit("ClockHit", vReset, 51, 19, 20, 16, "clock");
        var wallRt = TL(UI("WallHit", vReset), 46 * PX, 40 * PX, 30 * PX, 64 * PX);
        Img(wallRt, null, new Color(0, 0, 0, 0), true);
        var wallGlow = Img(TL(UI("WallUVGlow", wallRt), -10 * PX, -2 * PX, 50 * PX, 68 * PX), Spr("fx_uvglow"), new Color(1, 1, 1, 0), false);
        var msg = Ch4Art("UVMessage", wallRt, "obj_uv_msg", 0, 4, false);
        var msgCg = msg.gameObject.AddComponent<CanvasGroup>();
        msgCg.alpha = 0; msgCg.blocksRaycasts = false; msgCg.interactable = false;
        var wallHold = wallRt.gameObject.AddComponent<HoldReveal>();
        wallHold.revealGroup = msgCg; wallHold.glow = wallGlow; wallHold.holdTime = 1.5f; wallHold.tapId = "wall";
        var sleeper = Hit("SleeperHit", vReset, 78, 78, 42, 54, "sleeper");
        sleeper.gameObject.SetActive(false);
        var part = Ch4Art("Partition", vReset, "obj_partition", 74, 17, true);
        var partMover = part.gameObject.AddComponent<SwipeMover>();
        partMover.direction = Vector2.right; partMover.distance = 42 * PX; partMover.tapId = "partition"; partMover.sound = "curtain";

        // 화면 2 — 서버실
        var vServer = View("View_Server", "bg_server");
        Hit("RackHitL", vServer, 2, 22, 28, 108, "rack");
        Hit("RackHitM", vServer, 34, 22, 30, 108, "rack");
        Hit("RackHitR", vServer, 98, 22, 22, 108, "rack");
        Hit("MainClockHit", vServer, 36, 54, 26, 18, "mainclock");
        Hit("PrinterHit", vServer, 66, 84, 30, 46, "printer");
        Ch4Art("CCTV", vServer, "Ch2/obj_cctv_off", 72, 12, true).gameObject.AddComponent<InteractableObject>().id = "cctv";

        // 화면 3 — 엘리베이터 홀
        var vElev = View("View_Elevator", "bg_elevator");
        var elevOpen = Ch4Art("ElevOpen", vElev, "obj_elev_open", 32, 36, false);
        elevOpen.gameObject.SetActive(false);
        Hit("ElevatorHit", vElev, 30, 32, 56, 96, "elevator");
        Hit("DisplayHit", vElev, 46, 20, 24, 10, "display");
        Txt(TL(UI("FloorText", vElev), 46 * PX, 20 * PX, 24 * PX, 10 * PX), "B4", 24, Red, TextAnchor.MiddleCenter, true);
        Hit("PanelHit", vElev, 91, 55, 22, 50, "panel");
        Hit("ReaderHit", vElev, 9, 69, 16, 22, "reader");
        Hit("GuideHit", vElev, 3, 29, 26, 34, "guide");

        swipe.walls = new[] { vReset, vServer, vElev };

        Img(Stretch(UI("DarkOverlay", view)), Spr("ui_white"), new Color(0, 0, 0, 0), false);
        Img(Stretch(UI("Vignette", view)), Spr("fx_vignette"), new Color(1, 1, 1, 0.8f), false);
        var arrowL = Btn(TL(UI("ArrowLeft", view), 0, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowL.transform), 0, 0, 21, 39), Spr("icon_arrow_l"), Color.white, false);
        var arrowR = Btn(TL(UI("ArrowRight", view), 440, 246, 40, 72), null, new Color(0, 0, 0, 0));
        Img(Center(UI("Icon", arrowR.transform), 0, 0, 21, 39), Spr("icon_arrow_r"), Color.white, false);
        var find = Txt(TL(UI("FindExit", view), 0, 300, FW, 60), "END THE OBSERVATION", 30, Green, TextAnchor.MiddleCenter, true);
        find.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        find.gameObject.SetActive(false);

        // TopBar
        var top = FR(UI("TopBar", cv), 0, 0, FW, 46);
        Img(top, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", top), 0, 44, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var gear = Btn(TL(UI("GearButton", top), 4, 1, 44, 44), null, new Color(0, 0, 0, 0));
        Img(Center(UI("GearIcon", gear.transform), 0, 0, 24, 24), Spr("icon_gear"), Color.white, false);
        Txt(TL(UI("RoomTitle", top), 0, 4, FW, 26), "RESET ROOM", 24, Cream, TextAnchor.MiddleCenter, true);
        Txt(TL(UI("WallDots", top), 0, 30, FW, 14), "■ □ □", 12, Dim, TextAnchor.MiddleCenter);
        var noteBtn = Btn(TL(UI("NoteButton", top), 432, 1, 44, 44), null, new Color(0, 0, 0, 0));
        var noteIcon = Img(Center(UI("NoteIcon", noteBtn.transform), 0, 0, 24, 24), Spr("icon_note"), Color.white, false);
        var newDot = Img(TL(UI("NewDot", noteBtn.transform), 30, 6, 8, 8), Spr("ui_white"), Red, false);

        // Inventory
        var invRt = FR(UI("InventoryPanel", cv), 0, 734, FW, 120);
        Img(invRt, Spr("ui_white"), Bar, true);
        Img(TL(UI("Line", invRt), 0, 0, FW, 2), Spr("ui_white"), Hex("2a2420"), false);
        var invLabel = Txt(TL(UI("ItemLabel", invRt), 44, 8, 400, 16), "ITEM", 12, Dim, TextAnchor.MiddleLeft, true);
        var slots = new Button[3];
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var srt = TL(UI("Slot" + (i + 1), invRt), 44 + i * 104, 30, 80, 80);
            slots[i] = Btn(srt, Spr("ui_slot"), Color.white);
            slots[i].transition = Selectable.Transition.None;
            icons[i] = Img(Center(UI("Icon", srt), 0, 0, 64, 64), Spr("icon_uv"), Color.white, false);
            icons[i].gameObject.SetActive(false);
        }
        var hintRt = TL(UI("HintButton", invRt), 44 + 3 * 104, 30, 80, 80);
        Btn(hintRt, Spr("ui_slot"), Hex("8a8170"));
        Img(Center(UI("HintIcon", hintRt), 0, -6, 48, 48), Spr("Ch2/icon_hint"), Color.white, false);
        Txt(TL(UI("HintLabel", hintRt), 0, 60, 80, 16), "HINT", 12, Dim, TextAnchor.MiddleCenter, true);
        var inv = gm.AddComponent<InventoryManager>();
        inv.slots = slots; inv.icons = icons; inv.label = invLabel;
        inv.slotSprite = Spr("ui_slot"); inv.slotSelectedSprite = Spr("ui_slot_sel");
        inv.keyIcon = Spr("icon_key"); inv.uvIcon = Spr("icon_uv");
        inv.extraIcons = new List<Sprite> { Spr("Ch3/icon_card") };

        // Zoom
        var zoomRt = Stretch(UI("ZoomPanel", cv));
        Img(zoomRt, Spr("ui_white"), new Color(0, 0, 0, 0.88f), true);
        var zoomCatcher = zoomRt.gameObject.AddComponent<TapCatcher>();
        var zoomImg = Img(Center(UI("ZoomImage", zoomRt), 0, 70, 300, 300), Spr("zoom_paper"), Color.white, false);
        var paper = Txt(Center(UI("PaperText", zoomImg.transform), 0, -6, 280, 200), "", 24, Hex("2a2420"), TextAnchor.MiddleCenter, true);
        paper.lineSpacing = 1.0f;
        var zoom = gm.AddComponent<ZoomView>();
        zoom.panel = zoomRt.gameObject; zoom.image = zoomImg; zoom.paperText = paper; zoom.catcher = zoomCatcher;

        // Keypad (관제실 코드)
        var kpRt = Stretch(UI("KeypadPanel", cv));
        Img(kpRt, Spr("ui_white"), new Color(0, 0, 0, 0.82f), true);
        var kpCatcher = kpRt.gameObject.AddComponent<TapCatcher>();
        var body = Center(UI("KeypadBody", kpRt), 0, 30, 320, 480);
        Img(body, Spr("ui_keypad"), Color.white, true);
        Txt(TL(UI("KeypadLabel", body), 0, 14, 320, 16), "CONTROL ROOM · CODE", 12, Dim, TextAnchor.MiddleCenter, true);
        Img(TL(UI("DisplayBg", body), 28, 36, 264, 64), Spr("ui_white"), Hex("0c1310"), false);
        var display = Txt(TL(UI("Display", body), 28, 36, 264, 64), "____", 48, Green, TextAnchor.MiddleCenter, true);
        var digits = new Button[10];
        string[] order = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "" };
        Button back = null;
        for (int i = 0; i < 12; i++)
        {
            if (order[i] == "") continue;
            int col = i % 3, row = i / 3;
            var brt = TL(UI("Key_" + (order[i] == "<" ? "Back" : order[i]), body), 40 + col * 88, 116 + row * 64, 64, 52);
            var b = Btn(brt, Spr("ui_keybtn"), Color.white);
            Txt(Stretch(UI("Label", brt)), order[i] == "<" ? "←" : order[i], 24, Cream, TextAnchor.MiddleCenter, true);
            if (order[i] == "<") back = b; else digits[int.Parse(order[i])] = b;
        }
        var enterRt = TL(UI("Key_Enter", body), 40, 376, 240, 56);
        var enter = Btn(enterRt, Spr("ui_keybtn"), Hex("9fd9b0"));
        Txt(Stretch(UI("Label", enterRt)), "ENTER", 24, Cream, TextAnchor.MiddleCenter, true);
        var closeRt = Center(UI("Key_Close", kpRt), 0, -250, 160, 44);
        var close = Btn(closeRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", closeRt)), "닫기", 24, Cream, TextAnchor.MiddleCenter, true);
        var keypad = gm.AddComponent<KeypadController>();
        keypad.panel = kpRt.gameObject; keypad.body = body; keypad.display = display;
        keypad.digits = digits; keypad.backButton = back; keypad.enterButton = enter; keypad.closeButton = close;
        keypad.dimCatcher = kpCatcher;
        keypad.answer = "0405"; keypad.trap = "0404"; keypad.length = 4; keypad.trapFlash = "LOOP";

        // Clue notebook
        var clueRt = Stretch(UI("CluePanel", cv));
        Img(clueRt, Spr("ui_white"), new Color(0, 0, 0, 0.8f), true);
        var clueCatcher = clueRt.gameObject.AddComponent<TapCatcher>();
        var book = Center(UI("Notebook", clueRt), 0, 30, 440, 680);
        Img(book, Spr("ui_white"), Hex("cfc6ad"), true);
        Img(TL(UI("BookEdge", book), 0, 0, 440, 56), Spr("ui_white"), Hex("5a2a26"), false);
        Txt(TL(UI("BookTitle", book), 0, 0, 440, 56), "단서 노트", 24, Hex("f0e6d0"), TextAnchor.MiddleCenter, true);
        var clueText = Txt(TL(UI("ClueList", book), 20, 72, 400, 596), "", 24, Hex("2a2420"), TextAnchor.UpperLeft);
        clueText.lineSpacing = 1.0f;
        var clueClose = TextButton("ClueClose", clueRt, "닫기", 24, Hex("2a2420"), Cream);
        Center((RectTransform)clueClose.transform, 0, -340, 160, 40);
        var notes = gm.AddComponent<ClueNotebook>();
        notes.panel = clueRt.gameObject; notes.listText = clueText; notes.openButton = noteBtn; notes.closeButton = clueClose;
        notes.dimCatcher = clueCatcher; notes.icon = noteIcon.rectTransform; notes.newDot = newDot;

        // ---------------- CutscenePanel (엘리베이터 → 관제실 → 결말 선택) ----------------
        var cutRt = FR(UI("CutscenePanel", cv), 0, 0, FW, FH);
        Img(cutRt, Spr("ui_white"), Color.black, true);
        cutRt.gameObject.AddComponent<RectMask2D>();
        cutRt.gameObject.AddComponent<CanvasGroup>();
        Img(Center(UI("ElevInside", cutRt), 0, 0, 600, 860), Spr("Ch4/elev_inside"), Color.white, false);
        Txt(Center(UI("ElevFloor", cutRt), 0, 270, 200, 80), "B4", 48, Red, TextAnchor.MiddleCenter, true);
        Img(Center(UI("ControlRoom", cutRt), 0, 0, 600, 860), Spr("Ch4/bg_control"), Color.white, false);
        Img(Center(UI("Chair", cutRt), 0, -30, 32 * 5, 40 * 5), Spr("Ch4/obj_chair_occ"), Color.white, false);
        Img(Center(UI("Monitor", cutRt), 0, 190, 112 * PX, 80 * PX), Spr("Ch4/ctrl_screen"), Color.white, false);
        Img(Stretch(UI("CutDark", cutRt)), Spr("ui_white"), new Color(0, 0, 0, 0), false);
        var bigClock = Txt(Center(UI("BigClock", cutRt), 0, 120, FW, 100), "04:04", 72, Red, TextAnchor.MiddleCenter, true);
        bigClock.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

        var choiceRt = Stretch(UI("ChoiceGroup", cutRt));
        choiceRt.gameObject.AddComponent<CanvasGroup>();
        var cont = TextButton("ContinueButton", choiceRt, "계속 관찰한다", 24, Hex("2a2420"), Cream);
        Center((RectTransform)cont.transform, 0, -20, 320, 56);
        var sdRt = Center(UI("ShutdownButton", choiceRt), 0, -92, 320, 56);
        Img(sdRt, Spr("ui_white"), Hex("5a2a26"), true);
        Txt(Stretch(UI("Label", sdRt)), "전원을 끈다  (길게 누르기)", 24, Cream, TextAnchor.MiddleCenter, true);
        var sdGauge = Center(UI("ShutdownGauge", choiceRt), 0, -138, 40 * PX, 8 * PX);
        Img(sdGauge, Spr("Ch2/ui_gauge"), Color.white, false);
        var sdFill = Img(TL(UI("ShutdownFill", sdGauge), 4, 4, 40 * PX - 8, 8 * PX - 8), Spr("ui_white"), Green, false);
        sdFill.type = Image.Type.Filled; sdFill.fillMethod = Image.FillMethod.Horizontal; sdFill.fillOrigin = 0; sdFill.fillAmount = 0;
        var sdPct = Txt(TL(UI("ShutdownText", sdGauge), -40, 34, 40 * PX + 80, 22), "SHUTDOWN 0%", 12, Cream, TextAnchor.MiddleCenter, true);
        sdPct.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        var sdHold = sdRt.gameObject.AddComponent<HoldLever>();
        sdHold.holdTime = 3f; sdHold.handle = null; sdHold.travel = 0;
        sdHold.gaugeRoot = sdGauge.gameObject; sdHold.fill = sdFill; sdHold.percent = sdPct; sdHold.tapId = "shutdown_tap"; sdHold.label = "SHUTDOWN";
        choiceRt.gameObject.SetActive(false);

        Img(Stretch(UI("CutVignette", cutRt)), Spr("fx_vignette"), new Color(1, 1, 1, 0.8f), false);
        Img(Stretch(UI("Flash", cutRt)), Spr("ui_white"), new Color(1, 1, 1, 0), false);
        cutRt.gameObject.SetActive(false);

        // Dialogue
        var blockerRt = Stretch(UI("DialogueBlocker", cv));
        Img(blockerRt, null, new Color(0, 0, 0, 0), true);
        blockerRt.gameObject.AddComponent<TapCatcher>();
        var dlgRt = FR(UI("DialoguePanel", cv), 16, 586, 448, 136);
        var dlgCg = dlgRt.gameObject.AddComponent<CanvasGroup>();
        dlgCg.blocksRaycasts = false;
        Img(dlgRt, Spr("ui_white"), Hex("6b6152"), false);
        Img(TL(UI("Inner", dlgRt), 2, 2, 444, 132), Spr("ui_white"), new Color(0.06f, 0.05f, 0.045f, 0.94f), false);
        var speaker = Txt(TL(UI("Speaker", dlgRt), 18, 10, 300, 16), "", 12, Hex("e3c27a"), TextAnchor.MiddleLeft, true);
        var bodyT = Txt(TL(UI("Body", dlgRt), 18, 32, 412, 96), "", 24, Cream, TextAnchor.UpperLeft);
        var next = Txt(TL(UI("NextMark", dlgRt), 408, 108, 24, 20), "▼", 12, Cream, TextAnchor.MiddleCenter);
        var dlg = gm.AddComponent<DialogueManager>();
        dlg.panel = dlgCg; dlg.speakerText = speaker; dlg.bodyText = bodyT; dlg.nextMark = next; dlg.blocker = blockerRt.gameObject;

        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        var glitch = gm.AddComponent<GlitchEffect>();
        glitch.noise = noise;
        keypad.glitch = glitch;

        var blockRt = Stretch(UI("InputBlocker", cv));
        Img(blockRt, null, new Color(0, 0, 0, 0), true);
        blockRt.gameObject.SetActive(false);

        var introRt = Stretch(UI("IntroPanel", cv));
        Img(introRt, Spr("ui_white"), Color.black, true);
        introRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Center(UI("IntroText", introRt), 0, 20, FW, 70), "FINAL CHAPTER", 48, Cream, TextAnchor.MiddleCenter, true);
        Txt(Center(UI("IntroSub", introRt), 0, -36, FW, 40), "WAKE UP", 24, Red, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.3f;

        var mgr = gm.AddComponent<Chapter04Manager>();
        string[] needed = { "Ch2/obj_cctv_on", "Ch2/obj_cctv_off", "Ch3/zoom_note", "Ch3/cam_room404",
                            "Ch4/obj_chair_occ", "Ch4/obj_chair_empty", "Ch4/ctrl_screen" };
        mgr.sprites = new List<Sprite>();
        foreach (var n in needed) mgr.sprites.Add(Spr(n));

        return Save("Chapter04_WakeUp");
    }

    // =========================================================
    //  7) Ending  (ENDING A / ENDING B + 크레딧)
    // =========================================================
    static string BuildEnding()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");
        var es = gm.AddComponent<EndingScreen>();

        var frame = FR(UI("EndingFrame", cv), 0, 0, FW, FH);
        Img(frame, Spr("ui_white"), Color.black, false);
        frame.gameObject.AddComponent<RectMask2D>();
        es.background = Img(Center(UI("Background", frame), 0, 0, 600, 860), Spr("Ch4/end_sunrise"), Color.white, false);
        es.bgA = Spr("Ch4/bg_control"); es.bgB = Spr("Ch4/end_sunrise");
        Img(Stretch(UI("Vignette", frame)), Spr("fx_vignette"), Color.white, false);

        Shadow Sh(Text t) { var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.9f); s.effectDistance = new Vector2(2, -2); return s; }
        es.label = Txt(TL(UI("Label", frame), 0, 230, FW, 30), "ENDING", 24, Cream, TextAnchor.MiddleCenter, true); Sh(es.label);
        es.title = Txt(TL(UI("Title", frame), 0, 266, FW, 90), "04:05", 72, Cream, TextAnchor.MiddleCenter, true); Sh(es.title);
        es.body = Txt(TL(UI("Body", frame), 20, 380, FW - 40, 130), "", 24, Cream, TextAnchor.UpperCenter); Sh(es.body);
        es.record = Txt(TL(UI("Record", frame), 0, 530, FW, 24), "", 12, Cream, TextAnchor.MiddleCenter); Sh(es.record);
        es.other = Txt(TL(UI("Other", frame), 0, 400, FW, 50), "", 12, Cream, TextAnchor.MiddleCenter); Sh(es.other);

        es.credits = Txt(Center(UI("Credits", frame), 0, -900, FW - 40, 760),
            "04:04\n\n\nCHAPTER 1   ROOM 404\nCHAPTER 2   THE LOOP\nCHAPTER 3   OBSERVATION\nFINAL   WAKE UP\n\n\n만든 사람\n이수빈\n\n\n폰트\nGalmuri (OFL)\n\n\n그리고 끝까지 지켜봐 준\n당신에게.\n\n\nTHANK YOU FOR WATCHING.",
            24, Cream, TextAnchor.UpperCenter, true);
        Sh(es.credits);
        es.creditsRoll = es.credits.rectTransform;

        var backRt = TL(UI("BackButton", frame), 120, 680, 240, 56);
        es.backButton = Btn(backRt, Spr("ui_white"), Hex("2a2420"));
        backRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Stretch(UI("Label", backRt)), "◀  처음으로", 24, Cream, TextAnchor.MiddleCenter, true);
        var retryRt = TL(UI("RetryButton", frame), 100, 606, 280, 56);
        es.retryButton = Btn(retryRt, Spr("ui_white"), Hex("5a2a26"));
        retryRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Stretch(UI("Label", retryRt)), "▶  다른 결말 보기", 24, Cream, TextAnchor.MiddleCenter, true);

        var noiseRt = Stretch(UI("Noise", cv));
        var noise = noiseRt.gameObject.AddComponent<RawImage>();
        noise.texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ART}/fx_noise.png");
        noise.color = new Color(1, 1, 1, 0); noise.raycastTarget = false;
        es.glitch = gm.AddComponent<GlitchEffect>();
        es.glitch.noise = noise;

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 1.0f;

        return Save("Ending");
    }

}
