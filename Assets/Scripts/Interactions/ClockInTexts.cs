using UnityEngine;

namespace ProductsPlease.Interactions
{
    public class ClockInTexts : MonoBehaviour
    {
        public string message1 = "Clock in  (E)";
        public string message2 = "";
        [SerializeField] private InteractableComponent interactableComponent;

        public void UpdateTooltipMessage()
        {
            interactableComponent.message = interactableComponent.message == message1 ? message2 : message1;
        }

        public void DisableAfterUse(InteractableComponent component)
        {
            component.canBeUsed = false;
        }
    }
}