using UnityEngine;

/// <summary>
/// Controla o jogador: entrada (inverter gravidade), física, colisões, escudo e feedback visual.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public enum EstadoJogador { NoChao, NoTeto, NoAr, Flutuando, Morto }

    [Header("Física")]
    public float escalaGravidade = 4f;
    [Tooltip("Limita a velocidade vertical para o jogador não atravessar a tela rápido demais.")]
    public float velocidadeVerticalMaxima = 14f;
    [Tooltip("Tempo mínimo entre duas inversões (evita tremedeira ao apertar muitas vezes).")]
    public float intervaloMinimoInversao = 0.15f;
    [Tooltip("Força com que o campo de gravidade zero puxa o jogador para o centro.")]
    public float forcaCampo = 5f;

    [Header("Dano e escudo")]
    public float tempoInvencivel = 1.5f;
    [Tooltip("Duração máxima do escudo (ele também acaba ao absorver 1 batida).")]
    public float duracaoEscudo = 10f;
    [Tooltip("Invencibilidade curta depois que o escudo quebra (evita levar dano do bloco vizinho).")]
    public float tempoProtecaoAposEscudo = 0.6f;

    [Header("Visual")]
    public Transform visual;
    public SpriteRenderer bolhaEscudo;
    public Color cor = Color.cyan;
    public Color corCampo = new Color(0.4f, 0.7f, 1f);

    public EstadoJogador Estado { get; private set; } = EstadoJogador.NoChao;
    public bool Invencivel => Time.time < invencivelAte;
    // O escudo dura até 10 s OU até absorver 1 batida (o que acontecer primeiro)
    public bool ComEscudo => Time.time < escudoAte;
    public float TempoRestanteEscudo => Mathf.Max(0f, escudoAte - Time.time);
    float escudoAte = -10f;

    Rigidbody2D rb;
    SpriteRenderer[] renderizadores;
    Vector3 escalaVisual;
    int sentidoGravidade = 1;        // 1 = puxa para baixo, -1 = puxa para cima
    float escalaYAtual = 1f;         // usado para "virar" o personagem suavemente
    Vector2 achatamento = Vector2.one;
    float ultimaInversao = -10f;
    float invencivelAte;
    float proximoRastro;
    float proximaInversaoDemo;

    // Campo de gravidade zero
    float ultimoContatoCampo = -10f;
    float yCentroCampo;
    bool estavaNoCampo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.gravityScale = escalaGravidade * sentidoGravidade;
        renderizadores = visual.GetComponentsInChildren<SpriteRenderer>();
        escalaVisual = visual.localScale;
        if (bolhaEscudo != null) bolhaEscudo.enabled = false;
        proximaInversaoDemo = Time.time + 1.5f;
    }

    void Update()
    {
        if (Estado == EstadoJogador.Morto) return;

        var gm = GameManager.Instance;
        if (gm == null) return;
        bool jogando = gm.Estado == EstadoJogo.Jogando;

        if (gm.Estado == EstadoJogo.Menu)
        {
            // Demonstração no menu: o personagem fica trocando de lado sozinho
            if (Time.time >= proximaInversaoDemo)
            {
                proximaInversaoDemo = Time.time + Random.Range(1.2f, 2.2f);
                InverterGravidade(false);
            }
        }
        else if (jogando && gm.TempoDeJogo > 0.1f && ApertouInverter() &&
                 Time.time - ultimaInversao >= intervaloMinimoInversao)
        {
            // Ignora os primeiros instantes para o clique/tecla de "Jogar" não inverter junto
            InverterGravidade(true);
        }

        // Rastro deixado para trás (sensação de velocidade)
        if (jogando && Time.time >= proximoRastro)
        {
            proximoRastro = Time.time + 0.035f;
            Efeitos.Instance?.Rastro(transform.position, cor, 0.55f, gm.Velocidade);
        }

        if (jogando)
            gm.ui.AtualizarEscudo(TempoRestanteEscudo);

        AtualizarVisual();
    }

    void FixedUpdate()
    {
        if (Estado == EstadoJogador.Morto) return;

        Vector2 v = rb.velocity;
        bool noCampo = Time.fixedTime - ultimoContatoCampo < 0.1f;

        if (noCampo)
        {
            // Gravidade zero: desliga a gravidade e puxa suavemente para o centro do campo
            rb.gravityScale = 0f;
            float velocidadeAlvo = (yCentroCampo - rb.position.y) * forcaCampo;
            v.y = Mathf.Lerp(v.y, velocidadeAlvo, 8f * Time.fixedDeltaTime);
            Estado = EstadoJogador.Flutuando;
            if (!estavaNoCampo) EntrouNoCampo();
        }
        else
        {
            rb.gravityScale = escalaGravidade * sentidoGravidade;
            if (estavaNoCampo) Estado = EstadoJogador.NoAr;
        }
        estavaNoCampo = noCampo;

        v.y = Mathf.Clamp(v.y, -velocidadeVerticalMaxima, velocidadeVerticalMaxima);
        rb.velocity = v;
    }

    static bool ApertouInverter()
    {
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetMouseButtonDown(0);
    }

    void InverterGravidade(bool comSom)
    {
        sentidoGravidade *= -1;
        if (!estavaNoCampo) rb.gravityScale = escalaGravidade * sentidoGravidade;
        ultimaInversao = Time.time;
        if (!estavaNoCampo) Estado = EstadoJogador.NoAr;

        achatamento = new Vector2(0.7f, 1.3f);
        if (comSom) SoundManager.Instance?.TocarInversao();
        Efeitos.Instance?.Explosao(transform.position, cor, 5, 3f, 0.12f);
    }

    void EntrouNoCampo()
    {
        achatamento = new Vector2(1.25f, 0.8f);
        SoundManager.Instance?.TocarCampo();
        Efeitos.Instance?.Explosao(transform.position, corCampo, 10, 4f, 0.14f);
    }

    public void AtivarEscudo()
    {
        escudoAte = Time.time + duracaoEscudo;
        SoundManager.Instance?.TocarEscudo();
    }

    /// <summary>Muda as cores do personagem (usado pelo ciclo de cores do cenário).</summary>
    public void DefinirCores(Color corpo, Color olhos)
    {
        cor = corpo;
        if (renderizadores == null || renderizadores.Length == 0) return;
        renderizadores[0].color = corpo;
        for (int i = 1; i < renderizadores.Length; i++) renderizadores[i].color = olhos;
    }

    void AtualizarVisual()
    {
        // Vira o personagem de cabeça para baixo de forma suave
        escalaYAtual = Mathf.MoveTowards(escalaYAtual, sentidoGravidade, Time.deltaTime * 10f);
        achatamento = Vector2.Lerp(achatamento, Vector2.one, Time.deltaTime * 12f);
        visual.localScale = new Vector3(escalaVisual.x * achatamento.x,
                                        escalaVisual.y * achatamento.y * escalaYAtual,
                                        escalaVisual.z);

        // Pisca enquanto está invencível depois de levar dano
        bool visivel = !Invencivel || Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
        foreach (var r in renderizadores) r.enabled = visivel;

        // Bolha do escudo
        if (bolhaEscudo != null)
        {
            // Pisca nos últimos 2 segundos para avisar que vai acabar
            float restante = TempoRestanteEscudo;
            bolhaEscudo.enabled = restante > 0f && (restante > 2f || Mathf.Repeat(Time.time * 8f, 1f) > 0.4f);
            bolhaEscudo.transform.localScale = Vector3.one * (1.5f + Mathf.Sin(Time.time * 8f) * 0.06f);
            bolhaEscudo.transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        }
    }

    // ---------- Colisões ----------

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (Estado == EstadoJogador.Morto) return;
        DefinirSuperficie(colisao);
        achatamento = new Vector2(1.3f, 0.7f);
        Efeitos.Instance?.Explosao(colisao.GetContact(0).point, cor, 4, 2f, 0.1f);
    }

    void OnCollisionStay2D(Collision2D colisao)
    {
        if (Estado == EstadoJogador.Morto) return;
        DefinirSuperficie(colisao);
    }

    void OnCollisionExit2D(Collision2D colisao)
    {
        if (Estado == EstadoJogador.Morto) return;
        Estado = EstadoJogador.NoAr;
    }

    void DefinirSuperficie(Collision2D colisao)
    {
        Estado = colisao.transform.position.y < transform.position.y
            ? EstadoJogador.NoChao
            : EstadoJogador.NoTeto;
    }

    void OnTriggerEnter2D(Collider2D outro) => VerificarGatilho(outro);
    void OnTriggerStay2D(Collider2D outro) => VerificarGatilho(outro);

    void VerificarGatilho(Collider2D outro)
    {
        if (Estado == EstadoJogador.Morto) return;

        var campo = outro.GetComponent<ZonaGravidadeZero>();
        if (campo != null)
        {
            ultimoContatoCampo = Time.fixedTime;
            yCentroCampo = campo.transform.position.y;
            return;
        }

        var moeda = outro.GetComponent<Moeda>();
        if (moeda != null)
        {
            moeda.Coletar();
            return;
        }

        var powerUp = outro.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.Coletar(this);
            return;
        }

        var obstaculo = outro.GetComponent<Obstaculo>();
        if (obstaculo == null) return;

        if (Invencivel) return;

        if (ComEscudo)
        {
            // O escudo absorve a batida: quebra o obstáculo e se desfaz
            escudoAte = -10f;
            invencivelAte = Time.time + tempoProtecaoAposEscudo;
            obstaculo.Quebrar();
            if (bolhaEscudo != null)
                Efeitos.Instance?.Explosao(transform.position, bolhaEscudo.color, 18, 7f, 0.16f);
            GameManager.Instance?.cameraShake.Tremer(0.2f, 0.2f);
            SoundManager.Instance?.TocarQuebra();
        }
        else
        {
            LevarDano();
        }
    }

    void LevarDano()
    {
        invencivelAte = Time.time + tempoInvencivel;
        Efeitos.Instance?.Explosao(transform.position, Color.white, 10, 6f, 0.15f);
        GameManager.Instance.PerderVida();
    }

    public void Morrer()
    {
        Estado = EstadoJogador.Morto;
        rb.simulated = false;
        foreach (var r in renderizadores) r.enabled = false;
        if (bolhaEscudo != null) bolhaEscudo.enabled = false;
        Efeitos.Instance?.Explosao(transform.position, cor, 30, 9f, 0.22f);
        Efeitos.Instance?.Explosao(transform.position, Color.white, 12, 5f, 0.15f);
    }
}
