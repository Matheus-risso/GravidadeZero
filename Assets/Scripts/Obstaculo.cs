using UnityEngine;

/// <summary>
/// Bloco que causa dano ao jogador. O tamanho é definido pelo Spawner.
/// Pode ser quebrado quando o jogador está com escudo.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Obstaculo : MonoBehaviour
{
    [Tooltip("Margem do colisor em relação ao visual (deixa a colisão um pouco mais justa).")]
    public float margem = 0.1f;

    bool quebrado;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        AtualizarCor();
    }

    void Update() => AtualizarCor();

    // Segue a cor atual do ciclo de cores do cenário
    void AtualizarCor()
    {
        if (CicloDeCores.Instance != null) sr.color = CicloDeCores.Instance.CorObstaculo;
    }

    public void Configurar(float largura, float altura)
    {
        GetComponent<SpriteRenderer>().size = new Vector2(largura, altura);
        GetComponent<BoxCollider2D>().size = new Vector2(
            Mathf.Max(0.1f, largura - margem),
            Mathf.Max(0.1f, altura - margem));
    }

    public void Quebrar()
    {
        if (quebrado) return;
        quebrado = true;
        Efeitos.Instance?.Explosao(transform.position, GetComponent<SpriteRenderer>().color, 20, 8f, 0.25f);
        Destroy(gameObject);
    }
}
