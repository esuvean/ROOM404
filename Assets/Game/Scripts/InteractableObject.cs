// 오브젝트 탭 조사
//  - 이 컴포넌트가 붙은 그림을 탭하면 지금 챕터 관리자에게 id를 알려줍니다.
//  - 손가락이 많이 움직였으면(스와이프) 탭으로 치지 않습니다.
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableObject : MonoBehaviour, IPointerClickHandler
{
    public string id;

    public void OnPointerClick(PointerEventData e)
    {
        if ((e.pressPosition - e.position).magnitude > Screen.height * 0.03f) return;
        TapRouter.Send(id);
    }
}
