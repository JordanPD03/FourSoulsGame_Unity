using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adjunta este componente a un GameObject con Collider/Collider2D (world) o a un UI con GraphicRaycaster
/// para cerrar la preview al hacer click en un área vacía.
/// </summary>
public class PreviewDismissOnEmptyClick : MonoBehaviour, IPointerClickHandler
{
    // Para UI (EventSystem)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }
    }

    // Para world-space (collider)
    private void OnMouseDown()
    {
        if (CardPreviewUI.Instance != null)
        {
            CardPreviewUI.Instance.HidePreview();
        }
    }
}
