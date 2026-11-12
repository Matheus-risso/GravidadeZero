using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Estados possíveis do jogo (máquina de estados principal).
/// </summary>
public enum EstadoJogo { Menu, Jogando, Pausado, GameOver }

/// <summary>
/// Controla o fluxo do jogo: estados, velocidade do mundo, pontuação, vidas e recorde.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Quando o jogador escolhe "Jogar novamente", a cena é recarregada e o jogo começa direto, sem passar pelo menu.
    static bool iniciarDireto;

    [Header("Velocidade do mundo")]
    public float velocidadeInicial = 6f;
    [Tooltip("Quanto a velocidade aumenta por segundo.")]
    public float aceleracao = 0.12f;
    public float velocidadeMaxima = 14f;

    [Header("Regras")]
    public int vidasIniciais = 3;
    public int vidasMaximas = 3;
    public int pontosPorMoeda = 25;
    [Tooltip("Pontos ganhos ao pegar um coração com as vidas já no máximo.")]
    public int pontosVidaExtra = 50;

    [Header("Referências")]
    public PlayerController jogador;
    public UIManager ui;
    public CameraShake cameraShake;

    public EstadoJogo Estado { get; private set; } = EstadoJogo.Menu;
    public float Velocidade { get; private set; }
    public float TempoDeJogo { get; private set; }
    public float Distancia { get; private set; }
    public int Vidas { get; private set; }
    public int Recorde { get; private set; }
    public int Moedas => moedas;
    public int Pontos => Mathf.FloorToInt(Distancia) + moedas * pontosPorMoeda + pontosBonus;

    int moedas;
    int pontosBonus;
    float tempoNoGameOver;
    const string ChaveRecorde = "GravidadeZero_Recorde";

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Recorde = PlayerPrefs.GetInt(ChaveRecorde, 0);
        Vidas = vidasIniciais;
    }

    void Start()
    {
        if (iniciarDireto)
        {
            iniciarDireto = false;
            Jogar();
        }
        else
        {
            EntrarNoMenu();
        }
    }

    void Update()
    {
        switch (Estado)
        {
            case EstadoJogo.Menu:
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    Jogar();
                break;

            case EstadoJogo.Jogando:
                TempoDeJogo += Time.deltaTime;
                Velocidade = Mathf.Min(Velocidade + aceleracao * Time.deltaTime, velocidadeMaxima);
                Distancia += Velocidade * Time.deltaTime;
                ui.AtualizarHUD(Pontos, Mathf.Max(Recorde, Pontos), Vidas);

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                    Pausar();
                break;

            case EstadoJogo.Pausado:
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                    Continuar();
                break;

            case EstadoJogo.GameOver:
                tempoNoGameOver += Time.unscaledDeltaTime;
                if (tempoNoGameOver > 1.2f &&
                    (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R)))
                    JogarNovamente();
                break;
        }
    }

    // ---------- Transições de estado ----------

    void EntrarNoMenu()
    {
        Estado = EstadoJogo.Menu;
        Velocidade = 0f;
        ui.MostrarMenu(Recorde);
        SoundManager.Instance?.TocarMusicaMenu();
    }

    public void Jogar()
    {
        if (Estado == EstadoJogo.Jogando) return;
        Estado = EstadoJogo.Jogando;
        Velocidade = velocidadeInicial;
        TempoDeJogo = 0f;
        ui.MostrarJogo();
        ui.AtualizarHUD(Pontos, Recorde, Vidas);
        SoundManager.Instance?.TocarClique();
        SoundManager.Instance?.TocarMusicaJogo();
    }

    public void Pausar()
    {
        if (Estado != EstadoJogo.Jogando) return;
        Estado = EstadoJogo.Pausado;
        Time.timeScale = 0f;
        ui.MostrarPausa(true);
        SoundManager.Instance?.DefinirPausa(true);
    }

    public void Continuar()
    {
        if (Estado != EstadoJogo.Pausado) return;
        Estado = EstadoJogo.Jogando;
        Time.timeScale = 1f;
        ui.MostrarPausa(false);
        SoundManager.Instance?.DefinirPausa(false);
        SoundManager.Instance?.TocarClique();
    }

    void FimDeJogo()
    {
        Estado = EstadoJogo.GameOver;
        Velocidade = 0f;
        tempoNoGameOver = 0f;
        Time.timeScale = 1f;

        jogador.Morrer();
        cameraShake.Tremer(0.6f, 0.5f);
        SoundManager.Instance?.TocarGameOver();

        int pontosFinais = Pontos;
        bool novoRecorde = pontosFinais > Recorde;
        if (novoRecorde)
        {
            Recorde = pontosFinais;
            PlayerPrefs.SetInt(ChaveRecorde, Recorde);
            PlayerPrefs.Save();
        }
        StartCoroutine(MostrarGameOverDepois(pontosFinais, novoRecorde));
    }

    IEnumerator MostrarGameOverDepois(int pontosFinais, bool novoRecorde)
    {
        yield return new WaitForSecondsRealtime(1f);
        ui.MostrarGameOver(pontosFinais, Recorde, novoRecorde, moedas, Mathf.FloorToInt(Distancia));
        SoundManager.Instance?.TocarMusicaMenu();
    }

    // ---------- Eventos do jogo ----------

    public void PerderVida()
    {
        if (Estado != EstadoJogo.Jogando) return;

        Vidas--;
        ui.AtualizarHUD(Pontos, Mathf.Max(Recorde, Pontos), Vidas);
        ui.PulsarVidas();
        cameraShake.Tremer(0.35f, 0.3f);
        SoundManager.Instance?.TocarDano();

        if (Vidas <= 0)
            FimDeJogo();
        else
            StartCoroutine(PararTempo(0.08f)); // "hit stop": congela por um instante para dar peso ao impacto
    }

    public void AdicionarMoeda()
    {
        if (Estado != EstadoJogo.Jogando) return;
        moedas++;
        ui.PulsarPontos();
    }

    public void GanharVida()
    {
        if (Estado != EstadoJogo.Jogando) return;

        if (Vidas < vidasMaximas)
            Vidas++;
        else
            pontosBonus += pontosVidaExtra; // vidas cheias: vira pontos

        ui.AtualizarHUD(Pontos, Mathf.Max(Recorde, Pontos), Vidas);
        ui.PulsarVidas();
        SoundManager.Instance?.TocarVida();
    }

    IEnumerator PararTempo(float duracao)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duracao);
        if (Estado == EstadoJogo.Jogando) Time.timeScale = 1f;
    }

    // ---------- Botões da interface ----------

    public void JogarNovamente()
    {
        iniciarDireto = true;
        RecarregarCena();
    }

    public void VoltarAoMenu()
    {
        iniciarDireto = false;
        RecarregarCena();
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RecarregarCena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
