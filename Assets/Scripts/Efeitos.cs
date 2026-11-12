using UnityEngine;

/// <summary>
/// Cria partículas simples (explosões e rastro) usando sprites.
/// </summary>
public class Efeitos : MonoBehaviour
{
    public static Efeitos Instance { get; private set; }

    public Sprite sprite;

    void Awake()
    {
        Instance = this;
    }

    public void Explosao(Vector3 posicao, Color cor, int quantidade, float forca, float tamanho)
    {
        for (int i = 0; i < quantidade; i++)
        {
            Vector2 direcao = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * forca;
            Criar(posicao, cor, tamanho * Random.Range(0.6f, 1.3f), direcao, Random.Range(0.3f, 0.6f), 3f);
        }
    }

    public void Rastro(Vector3 posicao, Color cor, float tamanho, float velocidadeMundo)
    {
        cor.a *= 0.35f;
        Criar(posicao, cor, tamanho, Vector2.left * velocidadeMundo, 0.25f, 0f);
    }

    void Criar(Vector3 posicao, Color cor, float tamanho, Vector2 velocidade, float vida, float arrasto)
    {
        var go = new GameObject("Particula");
        go.transform.position = new Vector3(posicao.x, posicao.y, 0f);
        go.transform.localScale = Vector3.one * tamanho;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = 4;

        go.AddComponent<Particula>().Iniciar(velocidade, vida, arrasto);
    }
}
