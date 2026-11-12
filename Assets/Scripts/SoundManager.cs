using System.Collections;
using UnityEngine;

/// <summary>
/// Gera todos os efeitos sonoros e as duas músicas (menu e jogo) por código, com síntese simples.
/// O jogo não depende de nenhum arquivo de áudio.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Range(0f, 1f)] public float volumeEfeitos = 0.5f;
    [Range(0f, 1f)] public float volumeMusicaMenu = 0.5f;
    [Range(0f, 1f)] public float volumeMusicaJogo = 0.4f;

    const int Taxa = 44100;
    const float DoisPi = Mathf.PI * 2f;
    enum Onda { Quadrada, Triangular, Seno, Ruido }

    // As músicas são geradas uma única vez e reaproveitadas quando a cena recarrega
    static float[] dadosMusicaMenu;
    static float[] dadosMusicaJogo;

    AudioSource fonteEfeitos;
    AudioSource fonteMenu;
    AudioSource fonteJogo;
    Coroutine transicaoMenu, transicaoJogo;
    AudioClip somInversao, somMoeda, somDano, somGameOver, somClique, somHover,
              somVida, somEscudo, somQuebra, somCampo, somMudancaCor;
    System.Random aleatorio = new System.Random(42);

    void Awake()
    {
        Instance = this;

        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;
        fonteMenu = CriarFonteMusica();
        fonteJogo = CriarFonteMusica();

        somInversao = CriarClipe("Inversao", Tom(320f, 640f, 0.09f, Onda.Quadrada, 0.5f));
        somMoeda = CriarClipe("Moeda", Tom(988f, 988f, 0.06f, Onda.Quadrada, 0.45f),
                                       Tom(1319f, 1319f, 0.14f, Onda.Quadrada, 0.45f));
        somDano = CriarClipe("Dano", Tom(200f, 60f, 0.3f, Onda.Ruido, 0.8f));
        somGameOver = CriarClipe("GameOver", Tom(440f, 330f, 0.18f, Onda.Quadrada, 0.5f),
                                             Tom(330f, 220f, 0.18f, Onda.Quadrada, 0.5f),
                                             Tom(220f, 70f, 0.5f, Onda.Quadrada, 0.5f));
        somClique = CriarClipe("Clique", Tom(600f, 900f, 0.05f, Onda.Quadrada, 0.35f));
        somHover = CriarClipe("Hover", Tom(1200f, 1200f, 0.03f, Onda.Seno, 0.25f));
        somVida = CriarClipe("Vida", Tom(523f, 523f, 0.07f, Onda.Quadrada, 0.4f),
                                     Tom(659f, 659f, 0.07f, Onda.Quadrada, 0.4f),
                                     Tom(784f, 784f, 0.07f, Onda.Quadrada, 0.4f),
                                     Tom(1047f, 1047f, 0.2f, Onda.Quadrada, 0.4f));
        somEscudo = CriarClipe("Escudo", Tom(200f, 900f, 0.35f, Onda.Triangular, 0.7f));
        somQuebra = CriarClipe("Quebra", Tom(800f, 100f, 0.2f, Onda.Ruido, 0.7f));
        somCampo = CriarClipe("Campo", Tom(500f, 250f, 0.3f, Onda.Seno, 0.6f));
        somMudancaCor = CriarClipe("MudancaCor", Tom(150f, 1200f, 0.6f, Onda.Triangular, 0.6f));

        if (dadosMusicaMenu == null) dadosMusicaMenu = GerarMusicaMenu();
        if (dadosMusicaJogo == null) dadosMusicaJogo = GerarMusicaJogo();
        fonteMenu.clip = ClipeDeDados("MusicaMenu", dadosMusicaMenu);
        fonteJogo.clip = ClipeDeDados("MusicaJogo", dadosMusicaJogo);
    }

    AudioSource CriarFonteMusica()
    {
        var fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.volume = 0f;
        return fonte;
    }

    // ---------- Músicas ----------

    public void TocarMusicaMenu()
    {
        Transicao(fonteMenu, volumeMusicaMenu, 1.5f);
        Transicao(fonteJogo, 0f, 0.6f);
    }

    public void TocarMusicaJogo()
    {
        fonteJogo.time = 0f;
        Transicao(fonteJogo, volumeMusicaJogo, 0.5f);
        Transicao(fonteMenu, 0f, 0.8f);
    }

    public void DefinirPausa(bool pausado)
    {
        Transicao(fonteJogo, pausado ? volumeMusicaJogo * 0.3f : volumeMusicaJogo, 0.2f);
    }

    void Transicao(AudioSource fonte, float volumeAlvo, float duracao)
    {
        if (fonte == fonteMenu)
        {
            if (transicaoMenu != null) StopCoroutine(transicaoMenu);
            transicaoMenu = StartCoroutine(RotinaTransicao(fonte, volumeAlvo, duracao));
        }
        else
        {
            if (transicaoJogo != null) StopCoroutine(transicaoJogo);
            transicaoJogo = StartCoroutine(RotinaTransicao(fonte, volumeAlvo, duracao));
        }
    }

    static IEnumerator RotinaTransicao(AudioSource fonte, float volumeAlvo, float duracao)
    {
        if (volumeAlvo > 0f && !fonte.isPlaying) fonte.Play();
        float volumeInicial = fonte.volume;
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            fonte.volume = Mathf.Lerp(volumeInicial, volumeAlvo, t / duracao);
            yield return null;
        }
        fonte.volume = volumeAlvo;
        if (volumeAlvo <= 0f) fonte.Pause();
    }

    // ---------- Efeitos ----------

    public void TocarInversao() => Tocar(somInversao, Random.Range(0.92f, 1.1f));
    public void TocarMoeda() => Tocar(somMoeda);
    public void TocarDano() => Tocar(somDano);
    public void TocarClique() => Tocar(somClique);
    public void TocarHover() => Tocar(somHover);
    public void TocarVida() => Tocar(somVida);
    public void TocarEscudo() => Tocar(somEscudo);
    public void TocarQuebra() => Tocar(somQuebra, Random.Range(0.9f, 1.1f));
    public void TocarCampo() => Tocar(somCampo);
    public void TocarMudancaCor() => Tocar(somMudancaCor);

    public void TocarGameOver()
    {
        Transicao(fonteJogo, 0f, 0.3f);
        Tocar(somGameOver);
    }

    void Tocar(AudioClip clipe, float tom = 1f)
    {
        fonteEfeitos.pitch = tom;
        fonteEfeitos.PlayOneShot(clipe, volumeEfeitos);
    }

    // ---------- Síntese dos efeitos ----------

    float[] Tom(float freqInicial, float freqFinal, float duracao, Onda onda, float volume)
    {
        int n = Mathf.CeilToInt(Taxa * duracao);
        var dados = new float[n];
        double fase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            fase += Mathf.Lerp(freqInicial, freqFinal, t) / Taxa;
            float envelope = (1f - t) * (1f - t);
            dados[i] = Amostra(onda, fase) * envelope * volume;
        }
        return dados;
    }

    float Amostra(Onda onda, double fase)
    {
        double f = fase - System.Math.Floor(fase);
        switch (onda)
        {
            case Onda.Quadrada: return f < 0.5 ? 0.5f : -0.5f;
            case Onda.Triangular: return (float)(f < 0.5 ? 4 * f - 1 : 3 - 4 * f);
            case Onda.Seno: return Mathf.Sin((float)f * DoisPi);
            default: return Ruido();
        }
    }

    float Ruido() => (float)(aleatorio.NextDouble() * 2 - 1);

    static AudioClip CriarClipe(string nome, params float[][] partes)
    {
        int total = 0;
        foreach (var p in partes) total += p.Length;
        var dados = new float[total];
        int pos = 0;
        foreach (var p in partes) { p.CopyTo(dados, pos); pos += p.Length; }
        return ClipeDeDados(nome, dados);
    }

    static AudioClip ClipeDeDados(string nome, float[] dados)
    {
        var clipe = AudioClip.Create(nome, dados.Length, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    // ---------- Música do menu: ambiente, calma (16 s em loop) ----------

    float[] GerarMusicaMenu()
    {
        // Lá menor 7 -> Fá maj7 -> Dó add9 -> Sol 6 (4 segundos cada)
        float[][] acordes =
        {
            new[] { 220.00f, 261.63f, 329.63f, 392.00f },
            new[] { 174.61f, 220.00f, 261.63f, 329.63f },
            new[] { 261.63f, 329.63f, 392.00f, 587.33f },
            new[] { 196.00f, 246.94f, 293.66f, 329.63f },
        };
        float[] baixos = { 110.00f, 87.31f, 130.81f, 98.00f };
        int[] dedilhado = { 0, 1, 2, 3, 2, 1, 3, 2 };

        const float duracaoAcorde = 4f;
        const float duracaoNota = 0.5f;
        int n = Mathf.CeilToInt(Taxa * duracaoAcorde * acordes.Length);
        var dados = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Taxa;
            int indice = Mathf.Min(acordes.Length - 1, (int)(t / duracaoAcorde));
            float tAcorde = t - indice * duracaoAcorde;
            float[] acorde = acordes[indice];

            // Pad: notas longas com entrada e saída suaves (e um leve "chorus")
            float envelopePad = Mathf.Clamp01(tAcorde / 1f) * Mathf.Clamp01((duracaoAcorde - tAcorde) / 1f);
            float pad = 0f;
            foreach (float f in acorde)
                pad += Mathf.Sin(DoisPi * f * t) + Mathf.Sin(DoisPi * f * 1.004f * t);
            pad *= 0.03f * envelopePad;

            float baixo = Mathf.Sin(DoisPi * baixos[indice] * t) * 0.12f * envelopePad;

            // Dedilhado: uma nota a cada meio segundo, uma oitava acima
            int nota = (int)(t / duracaoNota);
            float tNota = t - nota * duracaoNota;
            float freqNota = acorde[dedilhado[nota % dedilhado.Length]] * 2f;
            float dedilhadoAmostra = (Mathf.Sin(DoisPi * freqNota * t) + 0.3f * Mathf.Sin(DoisPi * freqNota * 2f * t))
                                     * Mathf.Exp(-tNota * 4f) * 0.09f;

            dados[i] = pad + baixo + dedilhadoAmostra;
        }

        AplicarEco(dados, 0.375f, 0.45f);
        Normalizar(dados, 0.8f);
        return dados;
    }

    // ---------- Música do jogo: animada, com bateria (16 s em loop, 120 BPM) ----------

    float[] GerarMusicaJogo()
    {
        float[][] acordes =
        {
            new[] { 220.00f, 261.63f, 329.63f },  // Am
            new[] { 174.61f, 220.00f, 261.63f },  // F
            new[] { 261.63f, 329.63f, 392.00f },  // C
            new[] { 196.00f, 246.94f, 293.66f },  // G
            new[] { 220.00f, 261.63f, 329.63f },  // Am
            new[] { 174.61f, 220.00f, 261.63f },  // F
            new[] { 196.00f, 246.94f, 293.66f },  // G
            new[] { 164.81f, 207.65f, 246.94f },  // E
        };
        int[] arpejoA = { 0, 1, 2, 1, 0, 1, 2, 3 };
        int[] arpejoB = { 0, 2, 3, 2, 1, 2, 3, 2 };

        const float compasso = 2f;     // 4 tempos de 0,5 s
        const float tempo = 0.5f;
        const float semicolcheia = 0.125f;
        int n = Mathf.CeilToInt(Taxa * compasso * acordes.Length);
        var dados = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Taxa;
            int indice = Mathf.Min(acordes.Length - 1, (int)(t / compasso));
            float[] acorde = acordes[indice];

            // Melodia em arpejo (a segunda metade muda o desenho)
            int nota = (int)(t / semicolcheia);
            float tNota = t - nota * semicolcheia;
            int[] arpejo = indice < 4 ? arpejoA : arpejoB;
            int grau = arpejo[nota % arpejo.Length];
            float freq = (grau == 3 ? acorde[0] * 2f : acorde[grau]) * 2f;
            float melodia = Amostra(Onda.Quadrada, freq * t) * Mathf.Exp(-tNota * 14f) * 0.16f;

            // Baixo em colcheias alternando oitavas
            int colcheia = (int)(t / (tempo / 2f));
            float tColcheia = t - colcheia * (tempo / 2f);
            float freqBaixo = acorde[0] / (colcheia % 2 == 0 ? 2f : 1f);
            float baixo = Amostra(Onda.Triangular, freqBaixo * t) * Mathf.Exp(-tColcheia * 5f) * 0.3f;

            // Bateria: bumbo em todo tempo, caixa nos tempos 2 e 4, chimbal no contratempo
            int batida = (int)(t / tempo);
            float tBatida = t - batida * tempo;
            float faseBumbo = 50f * tBatida + 100f * (1f - Mathf.Exp(-tBatida * 30f)) / 30f;
            float bumbo = Mathf.Sin(DoisPi * faseBumbo) * Mathf.Exp(-tBatida * 12f) * 0.55f;
            float caixa = batida % 2 == 1 ? Ruido() * Mathf.Exp(-tBatida * 20f) * 0.2f : 0f;
            float tChimbal = (t + tempo / 2f) % tempo;
            float chimbal = Ruido() * Mathf.Exp(-tChimbal * 70f) * 0.08f;

            dados[i] = melodia + baixo + bumbo + caixa + chimbal;
        }

        AplicarEco(dados, 0.375f, 0.2f);
        Normalizar(dados, 0.85f);
        return dados;
    }

    /// <summary>Eco circular (continua certinho quando a música volta ao início do loop).</summary>
    static void AplicarEco(float[] dados, float atrasoSegundos, float realimentacao)
    {
        int n = dados.Length;
        int atraso = Mathf.RoundToInt(atrasoSegundos * Taxa);
        var seco = (float[])dados.Clone();
        var molhado = new float[n];
        for (int volta = 0; volta < 2; volta++)
            for (int i = 0; i < n; i++)
                molhado[i] = seco[i] + molhado[(i - atraso + n) % n] * realimentacao;
        System.Array.Copy(molhado, dados, n);
    }

    static void Normalizar(float[] dados, float pico)
    {
        float maximo = 0.0001f;
        foreach (float d in dados) maximo = Mathf.Max(maximo, Mathf.Abs(d));
        float fator = pico / maximo;
        for (int i = 0; i < dados.Length; i++) dados[i] *= fator;
    }
}
