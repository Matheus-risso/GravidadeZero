using UnityEngine;

/// <summary>
/// Muda as cores do cenário conforme a pontuação (parecido com o dia/noite do jogo do dinossauro).
/// A troca de paleta confunde o jogador e aumenta a dificuldade nas partidas longas.
/// </summary>
public class CicloDeCores : MonoBehaviour
{
    [System.Serializable]
    public struct Paleta
    {
        public string nome;
        public Color fundo;
        public Color chao;
        public Color marcas;
        public Color obstaculo;
        public Color estrelas;
        public Color jogador;
    }

    public static CicloDeCores Instance { get; private set; }

    [Header("Quando as cores mudam")]
    [Tooltip("Pontuação em que o cenário começa a trocar de cor.")]
    public int pontuacaoInicial = 300;
    [Tooltip("A cada quantos pontos troca para a próxima paleta.")]
    public int intervaloPontos = 250;
    public float duracaoTransicao = 1.2f;

    [Header("Referências")]
    public Camera cameraPrincipal;
    public SpriteRenderer chao;
    public SpriteRenderer teto;
    public Fundo fundo;
    public PlayerController jogador;

    [Header("Paletas (a primeira é a padrão)")]
    public Paleta[] paletas = PaletasPadrao();

    public Color CorObstaculo => atual.obstaculo;

    Paleta atual, origem, destino;
    int indiceAtual;
    float progresso = 1f;

    void Awake()
    {
        Instance = this;
        if (paletas == null || paletas.Length == 0) paletas = PaletasPadrao();
        atual = origem = destino = paletas[0];
    }

    void Start() => Aplicar();

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.Estado == EstadoJogo.Jogando)
        {
            int alvo = IndiceParaPontuacao(gm.Pontos);
            if (alvo != indiceAtual)
            {
                indiceAtual = alvo;
                origem = atual;
                destino = paletas[alvo];
                progresso = 0f;
                SoundManager.Instance?.TocarMudancaCor();
            }
        }

        if (progresso < 1f)
        {
            progresso = Mathf.Min(1f, progresso + Time.deltaTime / duracaoTransicao);
            atual = Misturar(origem, destino, Mathf.SmoothStep(0f, 1f, progresso));
            Aplicar();
        }
    }

    int IndiceParaPontuacao(int pontos)
    {
        if (pontos < pontuacaoInicial) return 0;
        int etapa = (pontos - pontuacaoInicial) / intervaloPontos;
        return (1 + etapa) % paletas.Length; // 1, 2, 3, ..., 0, 1, ...
    }

    void Aplicar()
    {
        if (cameraPrincipal != null) cameraPrincipal.backgroundColor = atual.fundo;
        if (chao != null) chao.color = atual.chao;
        if (teto != null) teto.color = atual.chao;
        if (fundo != null) fundo.DefinirCores(atual.estrelas, atual.marcas);
        if (jogador != null) jogador.DefinirCores(atual.jogador, atual.fundo);
    }

    static Paleta Misturar(Paleta a, Paleta b, float t) => new Paleta
    {
        nome = b.nome,
        fundo = Color.Lerp(a.fundo, b.fundo, t),
        chao = Color.Lerp(a.chao, b.chao, t),
        marcas = Color.Lerp(a.marcas, b.marcas, t),
        obstaculo = Color.Lerp(a.obstaculo, b.obstaculo, t),
        estrelas = Color.Lerp(a.estrelas, b.estrelas, t),
        jogador = Color.Lerp(a.jogador, b.jogador, t),
    };

    public static Paleta[] PaletasPadrao() => new[]
    {
        new Paleta { nome = "Noite neon",
            fundo = new Color(0.04f, 0.03f, 0.12f), chao = new Color(0.45f, 0.25f, 0.85f),
            marcas = new Color(0.6f, 0.35f, 1f, 0.8f), obstaculo = new Color(1f, 0.2f, 0.7f),
            estrelas = Color.white, jogador = new Color(0.2f, 0.95f, 1f) },
        new Paleta { nome = "Dia invertido",
            fundo = new Color(0.9f, 0.88f, 0.82f), chao = new Color(0.3f, 0.28f, 0.3f),
            marcas = new Color(0.12f, 0.12f, 0.12f, 0.8f), obstaculo = new Color(0.18f, 0.18f, 0.22f),
            estrelas = new Color(0.3f, 0.3f, 0.3f, 0.6f), jogador = new Color(0.1f, 0.45f, 0.9f) },
        new Paleta { nome = "Alerta vermelho",
            fundo = new Color(0.22f, 0.02f, 0.04f), chao = new Color(0.9f, 0.3f, 0.1f),
            marcas = new Color(1f, 0.6f, 0.2f, 0.8f), obstaculo = new Color(1f, 0.6f, 0.1f),
            estrelas = new Color(1f, 0.6f, 0.5f), jogador = Color.white },
        new Paleta { nome = "Matrix",
            fundo = new Color(0f, 0.07f, 0.03f), chao = new Color(0.1f, 0.5f, 0.2f),
            marcas = new Color(0.3f, 1f, 0.4f, 0.8f), obstaculo = new Color(0.45f, 1f, 0.35f),
            estrelas = new Color(0.4f, 1f, 0.5f), jogador = new Color(0.85f, 1f, 0.85f) },
        new Paleta { nome = "Pôr do sol",
            fundo = new Color(0.95f, 0.6f, 0.35f), chao = new Color(0.45f, 0.15f, 0.35f),
            marcas = new Color(1f, 0.85f, 0.5f, 0.8f), obstaculo = new Color(0.35f, 0.05f, 0.3f),
            estrelas = new Color(1f, 1f, 0.8f, 0.7f), jogador = new Color(0.1f, 0.2f, 0.5f) },
    };
}
