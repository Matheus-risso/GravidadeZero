using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Faz um elemento de interface piscar suavemente (ex.: "Pressione ESPAÇO").
/// </summary>
[RequireComponent(typeof(Graphic))]
public class Piscar : MonoBehaviour
{
    public float velocidade = 3f;
    public float alfaMinimo = 0.25f;

    Graphic grafico;
    Color corBase;

    void Awake()
    {
        grafico = GetComponent<Graphic>();
        corBase = grafico.color;
    }

    void Update()
    {
        Color c = corBase;
        c.a = corBase.a * Mathf.Lerp(alfaMinimo, 1f, Mathf.Abs(Mathf.Sin(Time.unscaledTime * velocidade)));
        grafico.color = c;
    }
}
