using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NHNHackathon.AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class UIButtonHoverSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private Selectable selectable;

        public void OnPointerEnter(PointerEventData eventData)
        {
            selectable ??= GetComponent<Selectable>();
            if (selectable == null || selectable.IsInteractable())
                UISfxPlayer.Instance?.PlayHover();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            selectable ??= GetComponent<Selectable>();
            if (selectable == null || selectable.IsInteractable())
                UISfxPlayer.Instance?.PlayClick();
        }

        private void OnValidate() => selectable ??= GetComponent<Selectable>();
    }
}
