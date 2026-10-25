// 아이템 획득 및 선택 (화면 하단 인벤토리)
//  - 아이템 칸(클릭 버튼)을 누르면 선택 / 다시 누르면 선택 해제
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager I;

    public Button[] slots;
    public Image[] icons;
    public Text label;
    public Sprite slotSprite, slotSelectedSprite;
    public Sprite keyIcon, uvIcon;
    public List<Sprite> extraIcons = new List<Sprite>();   // icon_chalk 같은 추가 아이콘

    readonly List<string> items = new List<string>();
    public string Selected { get; private set; }

    void Awake()
    {
        I = this;
        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            slots[i].onClick.AddListener(() => OnSlot(idx));
        }
        Refresh();
    }

    public void Add(string id)
    {
        if (items.Contains(id)) return;
        items.Add(id);
        Refresh();
        int i = items.IndexOf(id);
        StartCoroutine(UIFx.Punch(slots[i].transform, 0.25f, 0.3f));
    }

    public void Remove(string id)
    {
        items.Remove(id);
        if (Selected == id) Selected = null;
        Refresh();
    }

    public bool Has(string id) => items.Contains(id);
    public bool IsSelected(string id) => Selected == id;
    public void Deselect() { Selected = null; Refresh(); }

    void OnSlot(int i)
    {
        if (i >= items.Count) return;
        SoundManager.Play("click", 0.6f);
        Selected = Selected == items[i] ? null : items[i];
        Refresh();
    }

    Sprite IconFor(string id)
    {
        if (id == "KEY") return keyIcon;
        if (id == "UV") return uvIcon;
        var s = extraIcons.Find(x => x != null && x.name == "icon_" + id.ToLower());
        return s != null ? s : uvIcon;
    }

    void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = i < items.Count;
            icons[i].gameObject.SetActive(has);
            if (has) icons[i].sprite = IconFor(items[i]);
            var img = slots[i].GetComponent<Image>();
            img.sprite = has && items[i] == Selected ? slotSelectedSprite : slotSprite;
        }
        label.text = Selected == null ? "ITEM" : "ITEM   [ " + Selected + " - SELECTED ]";
    }
}
