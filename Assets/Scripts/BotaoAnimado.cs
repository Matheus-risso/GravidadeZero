using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Feedback dos botões: cresce um pouco e faz um som quando o mouse passa por cima.
/// </summary>
public class BotaoAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float escalaHover = 1.07f;

    float escalaAlvo = 1f;

    public void OnPointerEnter(PointerEventData dados)
    {
        escalaAlvo = escalaHover;
        SoundManager.Instance?.TocarHover();
    }

    public void OnPointerExit(PointerEventData dados) => escalaAlvo = 1f;

    void OnDisable()
    {
        escalaAlvo = 1f;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        float escala = Mathf.Lerp(transform.localScale.x, escalaAlvo, Time.unscaledDeltaTime * 14f);
        transform.localScale = Vector3.one * escala;
    }
}
