using UnityEngine;

/// <summary>
/// Moeda coletável: gira (animação) e dá pontos ao ser coletada.
/// </summary>
public class Moeda : MonoBehaviour
{
    bool coletada;
    Vector3 escalaBase;
    float fase;
    Color cor = Color.yellow;

    void Awake()
    {
        escalaBase = transform.localScale;
        fase = Random.value * 10f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) cor = sr.color;
    }

    void Update()
    {
        // Simula a moeda girando achatando a largura
        float giro = Mathf.Abs(Mathf.Cos((Time.time + fase) * 4f));
        transform.localScale = new Vector3(escalaBase.x * Mathf.Lerp(0.2f, 1f, giro), escalaBase.y, escalaBase.z);
    }

    public void Coletar()
    {
        if (coletada) return;
        coletada = true;

        GameManager.Instance?.AdicionarMoeda();
        SoundManager.Instance?.TocarMoeda();
        Efeitos.Instance?.Explosao(transform.position, cor, 8, 4f, 0.14f);
        Destroy(gameObject);
    }
}
