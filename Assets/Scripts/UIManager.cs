using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Interface: HUD (pontos, recorde, vidas, escudo) e painéis de menu, pausa e fim de jogo.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject hud;
    public GameObject painelMenu;
    public GameObject painelPausa;
    public GameObject painelGameOver;

    [Header("HUD")]
    public Text textoPontos;
    public Text textoRecordeHud;
    public Image[] iconesVida;
    public Color corVidaCheia = new Color(1f, 0.3f, 0.35f);
    public Color corVidaVazia = new Color(1f, 1f, 1f, 0.12f);
    public GameObject indicadorEscudo;
    public Text textoEscudo;

    [Header("Menu")]
    public Text textoRecordeMenu;

    [Header("Fim de jogo")]
    public Text textoPontosFinal;
    public Text textoRecordeFinal;
    public Text textoMoedasFinal;
    public Text textoDistanciaFinal;
    public GameObject textoNovoRecorde;

    float pulsoPontos = 1f;
    float pulsoVidas = 1f;

    void Update()
    {
        // Pequeno "pulo" de escala quando algo acontece (feedback ao jogador)
        pulsoPontos = Mathf.Lerp(pulsoPontos, 1f, Time.unscaledDeltaTime * 10f);
        pulsoVidas = Mathf.Lerp(pulsoVidas, 1f, Time.unscaledDeltaTime * 10f);
        if (textoPontos != null) textoPontos.transform.localScale = Vector3.one * pulsoPontos;
        foreach (var icone in iconesVida) icone.transform.localScale = Vector3.one * pulsoVidas;
    }

    public void MostrarMenu(int recorde)
    {
        Ativar(menu: true);
        textoRecordeMenu.text = recorde.ToString();
    }

    public void MostrarJogo() => Ativar(hud: true);

    public void MostrarPausa(bool pausado) => Ativar(hud: true, pausa: pausado);

    public void MostrarGameOver(int pontos, int recorde, bool novoRecorde, int moedas, int distancia)
    {
        Ativar(gameOver: true);
        textoPontosFinal.text = pontos.ToString();
        textoRecordeFinal.text = "RECORDE: " + recorde;
        textoMoedasFinal.text = "MOEDAS: " + moedas;
        textoDistanciaFinal.text = "DISTÂNCIA: " + distancia + " m";
        textoNovoRecorde.SetActive(novoRecorde);
    }

    public void AtualizarHUD(int pontos, int recorde, int vidas)
    {
        textoPontos.text = pontos.ToString();
        textoRecordeHud.text = "RECORDE " + recorde;
        for (int i = 0; i < iconesVida.Length; i++)
            iconesVida[i].color = i < vidas ? corVidaCheia : corVidaVazia;
    }

    /// <summary>Mostra no HUD o escudo e quantos segundos faltam (0 = sem escudo).</summary>
    public void AtualizarEscudo(float segundosRestantes)
    {
        bool ativo = segundosRestantes > 0f;
        if (indicadorEscudo.activeSelf != ativo) indicadorEscudo.SetActive(ativo);
        if (ativo) textoEscudo.text = "ESCUDO " + Mathf.CeilToInt(segundosRestantes) + "s";
    }

    public void PulsarPontos() => pulsoPontos = 1.35f;
    public void PulsarVidas() => pulsoVidas = 1.5f;

    void Ativar(bool hud = false, bool menu = false, bool pausa = false, bool gameOver = false)
    {
        this.hud.SetActive(hud);
        painelMenu.SetActive(menu);
        painelPausa.SetActive(pausa);
        painelGameOver.SetActive(gameOver);
        if (!hud) indicadorEscudo.SetActive(false);

        // Tira o foco dos botões para a tecla ESPAÇO não "clicar" neles sem querer
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
