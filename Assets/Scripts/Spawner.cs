using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gera os padrões de obstáculos e moedas. Cada padrão sempre tem uma passagem possível.
/// </summary>
public class Spawner : MonoBehaviour
{
    public enum Padrao { AltoDoChao, AltoDoTeto, MeioFlutuante, DuploBaixo, ZigueZague }

    [Header("Prefabs")]
    public Obstaculo prefabObstaculo;
    public Moeda prefabMoeda;
    public ZonaGravidadeZero prefabZona;
    public PowerUp prefabVida;
    public PowerUp prefabEscudo;

    [Header("Área de jogo")]
    public float yChao = -4f;
    public float yTeto = 4f;
    public float xSurgimento = 13f;
    public float xRemocao = -14f;

    [Header("Ritmo")]
    [Tooltip("Segundos entre um padrão e outro no começo do jogo.")]
    public float tempoEntrePadroesInicial = 1.7f;
    [Tooltip("Segundos entre um padrão e outro na velocidade máxima.")]
    public float tempoEntrePadroesMinimo = 1.05f;
    [Range(0f, 1f)] public float chanceMoedas = 0.55f;

    [Header("Itens especiais")]
    [Range(0f, 1f)] public float chanceVida = 0.08f;
    [Range(0f, 1f)] public float chanceEscudo = 0.08f;
    [Tooltip("Distância mínima para os itens especiais começarem a aparecer.")]
    public float distanciaMinimaItens = 40f;

    [Header("Desbloqueio de padrões (distância percorrida)")]
    public float distanciaDuploBaixo = 100f;
    public float distanciaZigueZague = 250f;

    float distanciaAteProximo = 6f;
    Padrao ultimoPadrao = Padrao.AltoDoChao;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Estado != EstadoJogo.Jogando) return;

        distanciaAteProximo -= gm.Velocidade * Time.deltaTime;
        if (distanciaAteProximo > 0f) return;

        float largura = CriarPadrao(SortearPadrao(gm.Distancia), gm.Velocidade);

        // O espaço até o próximo padrão é medido em TEMPO: assim, mesmo rápido, sempre dá para reagir.
        float dificuldade = Mathf.InverseLerp(gm.velocidadeInicial, gm.velocidadeMaxima, gm.Velocidade);
        float tempo = Mathf.Lerp(tempoEntrePadroesInicial, tempoEntrePadroesMinimo, dificuldade);
        distanciaAteProximo = largura + tempo * gm.Velocidade;
    }

    Padrao SortearPadrao(float distancia)
    {
        var opcoes = new List<(Padrao padrao, float peso)>
        {
            (Padrao.AltoDoChao, 3f),
            (Padrao.AltoDoTeto, 3f),
            (Padrao.MeioFlutuante, 2f),
        };
        if (distancia >= distanciaDuploBaixo) opcoes.Add((Padrao.DuploBaixo, 2f));
        if (distancia >= distanciaZigueZague) opcoes.Add((Padrao.ZigueZague, 1.5f));

        // Evita repetir padrões especiais seguidos
        if (ultimoPadrao == Padrao.DuploBaixo || ultimoPadrao == Padrao.ZigueZague)
            opcoes.RemoveAll(o => o.padrao == ultimoPadrao);

        float total = 0f;
        foreach (var o in opcoes) total += o.peso;
        float sorteio = Random.value * total;
        foreach (var o in opcoes)
        {
            sorteio -= o.peso;
            if (sorteio <= 0f) { ultimoPadrao = o.padrao; return o.padrao; }
        }
        ultimoPadrao = opcoes[0].padrao;
        return ultimoPadrao;
    }

    /// <summary>Cria o padrão e devolve a largura total dele.</summary>
    float CriarPadrao(Padrao padrao, float velocidade)
    {
        var raiz = new GameObject("Padrao_" + padrao);
        raiz.transform.position = new Vector3(xSurgimento, 0f, 0f);
        var corpo = raiz.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        var movimento = raiz.AddComponent<MovimentoMundo>();
        movimento.xRemocao = xRemocao;

        float largura;
        float meio = (yChao + yTeto) / 2f;

        switch (padrao)
        {
            case Padrao.AltoDoChao:      // obstáculo do chão até passar do meio -> fique no teto
                largura = 1.2f;
                Bloco(raiz.transform, 0f, yChao, meio + 0.8f, largura);
                Recompensa(raiz.transform, -1f, yTeto - 0.5f, 3, 1.1f);
                break;

            case Padrao.AltoDoTeto:      // obstáculo do teto até passar do meio -> fique no chão
                largura = 1.2f;
                Bloco(raiz.transform, 0f, meio - 0.8f, yTeto, largura);
                Recompensa(raiz.transform, -1f, yChao + 0.5f, 3, 1.1f);
                break;

            case Padrao.MeioFlutuante:   // obstáculo no meio -> fique no chão ou no teto
                largura = 1.4f;
                Bloco(raiz.transform, 0f, meio - 1.3f, meio + 1.3f, largura);
                Recompensa(raiz.transform, -0.8f, Random.value < 0.5f ? yChao + 0.5f : yTeto - 0.5f, 3, 1.1f);
                break;

            case Padrao.DuploBaixo:
            {
                // Obstáculos em cima e embaixo com um CAMPO DE GRAVIDADE ZERO no meio:
                // basta inverter uma vez para entrar no campo, que segura o jogador no centro.
                // O campo começa antes dos blocos (sempre ~0,6 s de antecedência, em qualquer velocidade).
                float entrada = velocidade * 0.6f + 1f;
                float larguraBlocos = Random.Range(1.5f, 2.8f);
                largura = entrada + larguraBlocos + 0.6f;
                Bloco(raiz.transform, entrada, yChao, meio - 2f, larguraBlocos);
                Bloco(raiz.transform, entrada, meio + 2f, yTeto, larguraBlocos);

                var zona = Instantiate(prefabZona, raiz.transform);
                zona.transform.localPosition = new Vector3(largura / 2f, meio, 0f);
                zona.Configurar(largura, 4.6f);

                Recompensa(raiz.transform, entrada + 0.3f, meio, Mathf.Max(1, Mathf.FloorToInt(larguraBlocos)), 1f);
                break;
            }

            default:                     // ZigueZague: alto do chão e logo depois alto do teto
                float espaco = velocidade * 0.95f + 1.2f;
                largura = espaco + 1.2f;
                Bloco(raiz.transform, 0f, yChao, meio + 0.8f, 1.2f);
                Bloco(raiz.transform, espaco, meio - 0.8f, yTeto, 1.2f);
                if (Random.value < chanceMoedas) Instantiate(prefabMoeda, raiz.transform).transform.localPosition =
                    new Vector3(espaco / 2f + 0.6f, meio, 0f);
                break;
        }

        movimento.largura = largura;
        return largura;
    }

    void Bloco(Transform pai, float xLocal, float yBase, float yTopo, float largura)
    {
        var obstaculo = Instantiate(prefabObstaculo, pai);
        obstaculo.transform.localPosition = new Vector3(xLocal + largura / 2f, (yBase + yTopo) / 2f, 0f);
        obstaculo.Configurar(largura, yTopo - yBase);
    }

    /// <summary>
    /// Coloca uma recompensa no caminho seguro do padrão: às vezes um item especial, às vezes moedas.
    /// </summary>
    void Recompensa(Transform pai, float xInicial, float y, int quantidade, float espacamento)
    {
        var gm = GameManager.Instance;
        float xCentro = xInicial + (quantidade - 1) * espacamento / 2f;

        if (gm.Distancia >= distanciaMinimaItens)
        {
            float chanceVidaAtual = gm.Vidas < gm.vidasMaximas ? chanceVida : 0f;
            float sorteio = Random.value;
            if (sorteio < chanceVidaAtual)
            {
                Item(prefabVida, pai, xCentro, y);
                return;
            }
            float chanceEscudoAtual = gm.jogador.ComEscudo ? 0f : chanceEscudo;
            if (sorteio < chanceVidaAtual + chanceEscudoAtual)
            {
                Item(prefabEscudo, pai, xCentro, y);
                return;
            }
        }

        if (Random.value < chanceMoedas)
            LinhaDeMoedas(pai, xInicial, y, quantidade, espacamento);
    }

    void Item(PowerUp prefab, Transform pai, float x, float y)
    {
        var item = Instantiate(prefab, pai);
        item.transform.localPosition = new Vector3(x, y, 0f);
    }

    void LinhaDeMoedas(Transform pai, float xInicial, float y, int quantidade, float espacamento)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var moeda = Instantiate(prefabMoeda, pai);
            moeda.transform.localPosition = new Vector3(xInicial + i * espacamento, y, 0f);
        }
    }
}
