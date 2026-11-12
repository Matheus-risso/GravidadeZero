using UnityEngine;

/// <summary>
/// Fundo com estrelas em paralaxe e marcas no chão/teto que se movem com o mundo.
/// </summary>
public class Fundo : MonoBehaviour
{
    public Sprite sprite;
    public int quantidadeEstrelas = 70;
    public Color corMarcas = new Color(0.6f, 0.35f, 1f, 0.8f);
    public float yChao = -4.5f;
    public float yTeto = 4.5f;
    [Tooltip("Velocidade das estrelas quando o jogo não está rodando (menu).")]
    public float velocidadeNoMenu = 2f;

    const float LarguraEstrelas = 22f;
    const float LarguraMarcas = 40f;
    const float EspacoMarcas = 2f;

    Transform[] estrelas;
    float[] fatores;
    float[] brilhos;
    Transform[] marcas;
    Color corEstrelas = Color.white;

    /// <summary>Troca as cores das estrelas e das marcas do chão/teto (usado pelo ciclo de cores).</summary>
    public void DefinirCores(Color novaCorEstrelas, Color novaCorMarcas)
    {
        corEstrelas = novaCorEstrelas;
        corMarcas = novaCorMarcas;
        AplicarCores();
    }

    void AplicarCores()
    {
        if (estrelas == null) return;
        for (int i = 0; i < estrelas.Length; i++)
        {
            Color c = corEstrelas;
            c.a *= brilhos[i];
            estrelas[i].GetComponent<SpriteRenderer>().color = c;
        }
        foreach (var marca in marcas) marca.GetComponent<SpriteRenderer>().color = corMarcas;
    }

    void Start()
    {
        estrelas = new Transform[quantidadeEstrelas];
        fatores = new float[quantidadeEstrelas];
        brilhos = new float[quantidadeEstrelas];
        for (int i = 0; i < quantidadeEstrelas; i++)
        {
            float profundidade = Random.value; // 0 = longe, 1 = perto
            brilhos[i] = Mathf.Lerp(0.15f, 0.8f, profundidade);
            var estrela = CriarQuadrado("Estrela", Color.white, -10);
            estrela.position = new Vector3(Random.Range(-LarguraEstrelas / 2f, LarguraEstrelas / 2f), Random.Range(-4f, 4f), 0f);
            estrela.localScale = Vector3.one * Mathf.Lerp(0.04f, 0.12f, profundidade);
            estrelas[i] = estrela;
            fatores[i] = Mathf.Lerp(0.05f, 0.35f, profundidade);
        }

        int marcasPorLinha = Mathf.RoundToInt(LarguraMarcas / EspacoMarcas);
        marcas = new Transform[marcasPorLinha * 2];
        for (int i = 0; i < marcasPorLinha; i++)
        {
            float x = -LarguraMarcas / 2f + i * EspacoMarcas;
            marcas[i * 2] = CriarMarca(x, yChao);
            marcas[i * 2 + 1] = CriarMarca(x, yTeto);
        }
        AplicarCores();
    }

    Transform CriarMarca(float x, float y)
    {
        var marca = CriarQuadrado("Marca", corMarcas, 1);
        marca.position = new Vector3(x, y, 0f);
        marca.localScale = new Vector3(0.7f, 0.12f, 1f);
        return marca;
    }

    Transform CriarQuadrado(string nome, Color cor, int ordem)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = ordem;
        return go.transform;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        float velocidadeMundo = gm != null ? gm.Velocidade : 0f;
        bool parado = gm == null || gm.Estado == EstadoJogo.Menu;
        float velocidadeEstrelas = parado ? velocidadeNoMenu : velocidadeMundo;

        for (int i = 0; i < estrelas.Length; i++)
            Mover(estrelas[i], velocidadeEstrelas * fatores[i] * Time.deltaTime, LarguraEstrelas);

        foreach (var marca in marcas)
            Mover(marca, velocidadeMundo * Time.deltaTime, LarguraMarcas);
    }

    static void Mover(Transform t, float deslocamento, float largura)
    {
        Vector3 p = t.position;
        p.x -= deslocamento;
        if (p.x < -largura / 2f) p.x += largura;
        t.position = p;
    }
}
