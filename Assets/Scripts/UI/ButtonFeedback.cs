using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LumaReef.UI
{
    public sealed class ButtonFeedback : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        Button button;
        Vector3 rest;
        float target=1,current=1;
        void Awake(){button=GetComponent<Button>();rest=transform.localScale;}
        void OnDisable(){target=current=1;transform.localScale=rest;}
        void Update(){if(Mathf.Abs(current-target)<.001f)return;current=Mathf.Lerp(current,target,1-Mathf.Exp(-24*Time.unscaledDeltaTime));transform.localScale=rest*current;}
        public void OnPointerDown(PointerEventData e){if(button.interactable)target=.93f;}
        public void OnPointerUp(PointerEventData e){target=1;}
        public void OnPointerEnter(PointerEventData e){if(button.interactable&&e.pointerId<0)target=1.035f;}
        public void OnPointerExit(PointerEventData e){target=1;}
    }
}
