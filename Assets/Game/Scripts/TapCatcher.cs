using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 화면(또는 UI)을 탭하면 onTap을 호출
public class TapCatcher : MonoBehaviour, IPointerClickHandler
{
    public Action onTap;
    public void OnPointerClick(PointerEventData e) { onTap?.Invoke(); }
}
