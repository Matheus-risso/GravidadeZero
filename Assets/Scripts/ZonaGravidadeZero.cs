using UnityEngine;

/// <summary>
/// Campo de gravidade zero: enquanto o jogador está dentro, a gravidade é desligada
/// e ele é puxado suavemente para o centro do campo (atravessa o meio flutuando).
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ZonaGravidadeZero : MonoBehaviour
{
    SpriteRenderer sr;
    Color corBase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        corBase = sr.color;
    }

    public void Configurar(float largura, float altura)
    {
        sr.size = new Vector2(largura, altura);
        GetComponent<BoxCollider2D>().size = new Vector2(largura, altura);
    }

    void Update()
    {
        // Pulsa suavemente para chamar atenção
        Color c = corBase;
        c.a = corBase.a * (0.65f + 0.35f * Mathf.Sin(Time.time * 5f));
        sr.color = c;
    }
}
