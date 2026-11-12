using UnityEngine;

/// <summary>
/// Item coletável especial: vida extra ou escudo temporário.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public enum Tipo { Vida, Escudo }

    public Tipo tipo;

    bool coletado;
    Vector3 posicaoBase;
    Vector3 escalaBase;
    float fase;

    void Start()
    {
        posicaoBase = transform.localPosition;
        escalaBase = transform.localScale;
        fase = Random.value * 6f;
    }

    void Update()
    {
        // Flutua e pulsa para se destacar das moedas
        float t = Time.time + fase;
        transform.localPosition = posicaoBase + Vector3.up * Mathf.Sin(t * 3f) * 0.15f;
        transform.localScale = escalaBase * (1f + Mathf.Sin(t * 6f) * 0.08f);
    }

    public void Coletar(PlayerController jogador)
    {
        if (coletado) return;
        coletado = true;

        if (tipo == Tipo.Vida)
            GameManager.Instance?.GanharVida();
        else
            jogador.AtivarEscudo();

        var sr = GetComponent<SpriteRenderer>();
        Efeitos.Instance?.Explosao(transform.position, sr != null ? sr.color : Color.white, 16, 6f, 0.18f);
        Destroy(gameObject);
    }
}
