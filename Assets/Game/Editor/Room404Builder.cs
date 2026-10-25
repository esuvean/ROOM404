// =============================================================
//  메뉴 [04:04 > 씬 전체 자동 생성] 을 누르면
//  Title / Chapter01_Room404 / Chapter01_Clear 세 개의 씬을
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
        string clear = BuildClear();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(title, true),
            new EditorBuildSettingsScene(room, true),
            new EditorBuildSettingsScene(clear, true),
        };

        PlayerSettings.productName = "04:04";
        PlayerSettings.defaultWebScreenWidth = 480;
        PlayerSettings.defaultWebScreenHeight = 854;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.WebGL.decompressionFallback = true;

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(title);
        Debug.Log("<color=#49e07a>[04:04] 씬 3개 생성 완료! Title 씬에서 ▶ Play 를 눌러보세요.</color>");
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
        ts.tapText = tap; ts.catcher = catcher; ts.bg = bg;
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
    //  3) Chapter01_Clear  (결과 화면 → 다음 챕터 → 추후 제작)
    // =========================================================
    static string BuildClear()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Base(out var canvas);
        var cv = canvas.transform;
        var gm = new GameObject("GameManager");
        var cs = gm.AddComponent<ClearScreen>();

        var frame = FR(UI("ClearFrame", cv), 0, 0, FW, FH);
        Img(frame, Spr("ui_white"), Color.black, false);
        frame.gameObject.AddComponent<RectMask2D>();

        // 결과
        var clearRt = Stretch(UI("ClearGroup", frame));
        cs.clearGroup = clearRt.gameObject.AddComponent<CanvasGroup>();
        cs.chapterText = Txt(TL(UI("ChapterText", clearRt), 0, 300, FW, 40), "CHAPTER 1", 24, Dim, TextAnchor.MiddleCenter, true);
        cs.clearText = Txt(TL(UI("ClearText", clearRt), 0, 340, FW, 90), "CLEAR", 72, Cream, TextAnchor.MiddleCenter, true);
        cs.timeCodeText = Txt(TL(UI("TimeCode", clearRt), 0, 440, FW, 30), "04:04", 24, Red, TextAnchor.MiddleCenter, true);
        cs.recordText = Txt(TL(UI("Record", clearRt), 0, 490, FW, 24), "", 12, Dim, TextAnchor.MiddleCenter);
        var nextRt = TL(UI("NextButton", clearRt), 120, 660, 240, 56);
        cs.nextButton = Btn(nextRt, Spr("ui_white"), new Color(1, 1, 1, 0.06f));
        nextRt.gameObject.AddComponent<CanvasGroup>();
        Txt(Stretch(UI("Label", nextRt)), "다음 챕터로  ▶", 24, Cream, TextAnchor.MiddleCenter, true);

        // 복도를 걸어감
        var walkRt = Stretch(UI("WalkGroup", frame));
        cs.walkGroup = walkRt.gameObject.AddComponent<CanvasGroup>();
        cs.corridor = Img(Center(UI("Corridor", walkRt), 0, 0, 600, 860), Spr("corridor_persp"), Color.white, false);
        Img(Stretch(UI("WalkVignette", walkRt)), Spr("fx_vignette"), Color.white, false);
        cs.walkTitle = Txt(TL(UI("WalkTitle", walkRt), 0, 640, FW, 40), "CHAPTER 2", 24, Cream, TextAnchor.MiddleCenter, true);
        cs.walkTitle.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
        var walkSub = Txt(TL(UI("WalkSub", walkRt), 0, 680, FW, 30), "복도", 24, Dim, TextAnchor.MiddleCenter, true);
        walkSub.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);

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
        Txt(TL(UI("SoonSmall", soonRt), 0, 300, FW, 24), "CHAPTER 2 · 복도", 12, Dim, TextAnchor.MiddleCenter, true);
        cs.soonTitle = Txt(TL(UI("SoonTitle", soonRt), 0, 330, FW, 80), "추후 제작", 48, Cream, TextAnchor.MiddleCenter, true);
        cs.soonSub = Txt(TL(UI("SoonSub", soonRt), 0, 420, FW, 60), "다음 챕터는 아직 준비 중입니다.", 24, Dim, TextAnchor.MiddleCenter);
        var backRt = TL(UI("BackButton", soonRt), 120, 620, 240, 56);
        cs.backButton = Btn(backRt, Spr("ui_white"), Hex("2a2420"));
        Txt(Stretch(UI("Label", backRt)), "◀  돌아가기", 24, Cream, TextAnchor.MiddleCenter, true);

        var fade = FadePanel(cv);
        var sl = gm.AddComponent<SceneLoader>();
        sl.fade = fade; sl.fadeInOnStart = true; sl.fadeInTime = 0.6f;

        return Save("Chapter01_Clear");
    }
}
