using UnityEngine;

/// <summary>
/// Partícula individual: anda, desacelera, some aos poucos e se destrói.
/// </summary>
public class Particula : MonoBehaviour
{
    Vector2 velocidade;
    float vida;
    float arrasto;
    float tempo;
    SpriteRenderer sr;
    Color corInicial;
    Vector3 escalaInicial;

    public void Iniciar(Vector2 velocidade, float vida, float arrasto)
    {
        this.velocidade = velocidade;
        this.vida = vida;
        this.arrasto = arrasto;
        sr = GetComponent<SpriteRenderer>();
        corInicial = sr.color;
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        tempo += Time.deltaTime;
        float progresso = tempo / vida;
        if (progresso >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        velocidade *= Mathf.Max(0f, 1f - arrasto * Time.deltaTime);
        transform.position += (Vector3)(velocidade * Time.deltaTime);

        Color c = corInicial;
        c.a *= 1f - progresso;
        sr.color = c;
        transform.localScale = escalaInicial * (1f - progresso * 0.7f);
    }
}
