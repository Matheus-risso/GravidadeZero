using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Ferramenta de editor: monta automaticamente sprites, prefabs, a cena do jogo e as configurações do projeto.
/// Menu: Gravidade Zero > Montar cena do jogo.
/// </summary>
public static class MontarJogo
{
    const string PastaSprites = "Assets/Sprites";
    const string PastaPrefabs = "Assets/Prefabs";
    const string PastaCenas = "Assets/Scenes";
    const string CaminhoCena = PastaCenas + "/Jogo.unity";

    static readonly Color CorFundo = new Color(0.04f, 0.03f, 0.12f);
    static readonly Color CorJogador = new Color(0.2f, 0.95f, 1f);
    static readonly Color CorObstaculo = new Color(1f, 0.2f, 0.7f);
    static readonly Color CorMoeda = new Color(1f, 0.85f, 0.2f);
    static readonly Color CorVida = new Color(1f, 0.3f, 0.35f);
    static readonly Color CorEscudo = new Color(0.35f, 1f, 0.55f);
    static readonly Color CorCampo = new Color(0.35f, 0.65f, 1f, 0.6f);
    static readonly Color CorChao = new Color(0.45f, 0.25f, 0.85f);
    static readonly Color CorCartao = new Color(0.05f, 0.04f, 0.15f, 0.92f);
    static readonly Color CorTexto = Color.white;
    static readonly Color CorTextoSuave = new Color(1f, 1f, 1f, 0.6f);
    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

    static Font fonte;
    static Sprite spriteQuadrado, spriteCirculo, spriteBloco, spriteMoldura, spriteCoracao, spriteEscudo, spriteAnel;

    [MenuItem("Gravidade Zero/1. Montar cena do jogo")]
    public static void MontarTudo()
    {
        if (!Application.isBatchMode && File.Exists(CaminhoCena) &&
            !EditorUtility.DisplayDialog("Gravidade Zero",
                "A cena Jogo.unity já existe e será recriada do zero. Continuar?", "Recriar", "Cancelar"))
            return;

        fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CriarPasta(PastaSprites);
        CriarPasta(PastaPrefabs);
        CriarPasta(PastaCenas);
        CriarSprites();

        var prefabObstaculo = CriarPrefabObstaculo();
        var prefabMoeda = CriarPrefabMoeda();
        var prefabZona = CriarPrefabZona();
        var prefabVida = CriarPrefabPowerUp("Vida", spriteCoracao, CorVida, PowerUp.Tipo.Vida);
        var prefabEscudo = CriarPrefabPowerUp("Escudo", spriteEscudo, CorEscudo, PowerUp.Tipo.Escudo);

        CriarCena(prefabObstaculo, prefabMoeda, prefabZona, prefabVida, prefabEscudo);
        ConfigurarProjeto();

        Debug.Log("[Gravidade Zero] Cena montada em " + CaminhoCena);
    }

    [MenuItem("Gravidade Zero/2. Gerar build para Windows")]
    public static void GerarBuild()
    {
        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CaminhoCena },
            locationPathName = "Build/GravidadeZero.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var relatorio = BuildPipeline.BuildPlayer(opcoes);
        bool sucesso = relatorio.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        Debug.Log("[Gravidade Zero] Build: " + relatorio.summary.result + " -> " + Path.GetFullPath("Build"));

        if (Application.isBatchMode)
            EditorApplication.Exit(sucesso ? 0 : 1);
        else if (sucesso)
            EditorUtility.RevealInFinder("Build/GravidadeZero.exe");
    }

    // ================= Sprites =================

    static void CriarPasta(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        string pai = Path.GetDirectoryName(caminho).Replace('\\', '/');
        AssetDatabase.CreateFolder(pai, Path.GetFileName(caminho));
    }

    static void CriarSprites()
    {
        const int borda = 3;
        spriteQuadrado = SpritePorPixel("Quadrado", 32, (x, y, n) => 1f);
        spriteBloco = SpritePorPixel("Bloco", 32, (x, y, n) => NaBorda(x, y, n, borda) ? 1f : 0.3f, borda);
        spriteMoldura = SpritePorPixel("Moldura", 32, (x, y, n) => NaBorda(x, y, n, borda) ? 1f : 0f, borda);

        spriteCirculo = SpritePorForma("Circulo", 64, (u, v) => u * u + v * v <= 1f ? 1f : 0f);
        spriteAnel = SpritePorForma("Anel", 128, (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            return d > 1f ? 0f : d > 0.88f ? 1f : 0.12f;
        });
        spriteCoracao = SpritePorForma("Coracao", 64, (u, v) =>
        {
            float x = u * 1.25f, y = v * 1.25f + 0.15f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f ? 1f : 0f;
        });
        spriteEscudo = SpritePorForma("Escudo", 64, (u, v) =>
        {
            if (DentroEscudo(u, v, 1f)) return DentroEscudo(u, v, 0.72f) ? 0.55f : 1f;
            return 0f;
        });
    }

    static bool NaBorda(int x, int y, int n, int borda) =>
        x < borda || y < borda || x >= n - borda || y >= n - borda;

    static bool DentroEscudo(float u, float v, float escala)
    {
        u /= escala;
        v /= escala;
        if (v > 0.85f || v < -1f) return false;
        float meiaLargura = v >= 0.2f ? 0.85f : 0.85f * Mathf.Sqrt(Mathf.Max(0f, (v + 1f) / 1.2f));
        return Mathf.Abs(u) <= meiaLargura;
    }

    /// <summary>Cria um sprite definindo a transparência de cada pixel.</summary>
    static Sprite SpritePorPixel(string nome, int tamanho, Func<int, int, int, float> alfa, int bordaSliced = 0)
    {
        var textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        for (int y = 0; y < tamanho; y++)
            for (int x = 0; x < tamanho; x++)
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alfa(x, y, tamanho)));
        return SalvarSprite(nome, textura, bordaSliced);
    }

    /// <summary>Cria um sprite a partir de uma forma matemática (u, v de -1 a 1), com suavização nas bordas.</summary>
    static Sprite SpritePorForma(string nome, int tamanho, Func<float, float, float> forma)
    {
        const int amostras = 4;
        var textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        for (int y = 0; y < tamanho; y++)
            for (int x = 0; x < tamanho; x++)
            {
                float soma = 0f;
                for (int sy = 0; sy < amostras; sy++)
                    for (int sx = 0; sx < amostras; sx++)
                    {
                        float u = (x + (sx + 0.5f) / amostras) / tamanho * 2f - 1f;
                        float v = (y + (sy + 0.5f) / amostras) / tamanho * 2f - 1f;
                        soma += forma(u, v);
                    }
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, soma / (amostras * amostras)));
            }
        return SalvarSprite(nome, textura, 0);
    }

    static Sprite SalvarSprite(string nome, Texture2D textura, int bordaSliced)
    {
        string caminho = $"{PastaSprites}/{nome}.png";
        File.WriteAllBytes(caminho, textura.EncodeToPNG());
        int tamanho = textura.width;
        Object.DestroyImmediate(textura);

        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);
        var importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = tamanho; // 1 sprite = 1 unidade do mundo
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.filterMode = FilterMode.Bilinear;
        importador.wrapMode = TextureWrapMode.Clamp;
        if (bordaSliced > 0)
        {
            // Necessário para o modo "Sliced" (estica sem deformar a borda)
            var config = new TextureImporterSettings();
            importador.ReadTextureSettings(config);
            config.spriteMeshType = SpriteMeshType.FullRect;
            importador.SetTextureSettings(config);
            importador.spriteBorder = new Vector4(bordaSliced, bordaSliced, bordaSliced, bordaSliced);
        }
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }

    // ================= Prefabs =================

    static T SalvarPrefab<T>(GameObject go, string nome) where T : Component
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PastaPrefabs}/{nome}.prefab");
        Object.DestroyImmediate(go);
        return prefab.GetComponent<T>();
    }

    static Obstaculo CriarPrefabObstaculo()
    {
        var go = new GameObject("Obstaculo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteBloco;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = Vector2.one;
        sr.color = CorObstaculo;
        sr.sortingOrder = 2;
        var colisor = go.AddComponent<BoxCollider2D>();
        colisor.isTrigger = true;
        colisor.size = new Vector2(0.9f, 0.9f);
        go.AddComponent<Obstaculo>();
        return SalvarPrefab<Obstaculo>(go, "Obstaculo");
    }

    static Moeda CriarPrefabMoeda()
    {
        var go = new GameObject("Moeda");
        go.transform.localScale = Vector3.one * 0.55f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteCirculo;
        sr.color = CorMoeda;
        sr.sortingOrder = 3;
        var colisor = go.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.5f;
        go.AddComponent<Moeda>();
        return SalvarPrefab<Moeda>(go, "Moeda");
    }

    static ZonaGravidadeZero CriarPrefabZona()
    {
        var go = new GameObject("ZonaGravidadeZero");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteBloco;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = Vector2.one;
        sr.color = CorCampo;
        sr.sortingOrder = 1;
        var colisor = go.AddComponent<BoxCollider2D>();
        colisor.isTrigger = true;
        colisor.size = Vector2.one;
        go.AddComponent<ZonaGravidadeZero>();
        return SalvarPrefab<ZonaGravidadeZero>(go, "ZonaGravidadeZero");
    }

    static PowerUp CriarPrefabPowerUp(string nome, Sprite sprite, Color cor, PowerUp.Tipo tipo)
    {
        var go = new GameObject(nome);
        go.transform.localScale = Vector3.one * 0.75f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = cor;
        sr.sortingOrder = 3;

        // Brilho atrás do item para ele se destacar
        var brilho = new GameObject("Brilho");
        brilho.transform.SetParent(go.transform, false);
        brilho.transform.localScale = Vector3.one * 1.6f;
        var srBrilho = brilho.AddComponent<SpriteRenderer>();
        srBrilho.sprite = spriteCirculo;
        srBrilho.color = new Color(cor.r, cor.g, cor.b, 0.2f);
        srBrilho.sortingOrder = 2;

        var colisor = go.AddComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.6f;
        go.AddComponent<PowerUp>().tipo = tipo;
        return SalvarPrefab<PowerUp>(go, nome);
    }

    // ================= Cena =================

    static void CriarCena(Obstaculo prefabObstaculo, Moeda prefabMoeda, ZonaGravidadeZero prefabZona,
                          PowerUp prefabVida, PowerUp prefabEscudo)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Câmera ----
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraGO.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CorFundo;
        cameraGO.AddComponent<AudioListener>();
        var tremor = cameraGO.AddComponent<CameraShake>();

        // ---- Chão e teto ----
        var chao = CriarSuperficie("Chao", -4.5f);
        var teto = CriarSuperficie("Teto", 4.5f);

        // ---- Jogador ----
        var jogadorGO = new GameObject("Jogador");
        jogadorGO.transform.position = new Vector3(-5f, -3.6f, 0f);
        var corpo = jogadorGO.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 4f;
        corpo.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpo.sleepMode = RigidbodySleepMode2D.NeverSleep;
        jogadorGO.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(jogadorGO.transform, false);
        visual.transform.localScale = Vector3.one * 0.8f;
        var srJogador = visual.AddComponent<SpriteRenderer>();
        srJogador.sprite = spriteQuadrado;
        srJogador.color = CorJogador;
        srJogador.sortingOrder = 5;
        CriarOlho(visual.transform, new Vector2(0.08f, 0.15f));
        CriarOlho(visual.transform, new Vector2(0.32f, 0.15f));

        var bolha = new GameObject("BolhaEscudo");
        bolha.transform.SetParent(jogadorGO.transform, false);
        bolha.transform.localScale = Vector3.one * 1.5f;
        var srBolha = bolha.AddComponent<SpriteRenderer>();
        srBolha.sprite = spriteAnel;
        srBolha.color = new Color(CorEscudo.r, CorEscudo.g, CorEscudo.b, 0.85f);
        srBolha.sortingOrder = 7;

        var jogador = jogadorGO.AddComponent<PlayerController>();
        jogador.visual = visual.transform;
        jogador.bolhaEscudo = srBolha;
        jogador.cor = CorJogador;

        // ---- Sistemas ----
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        var spawner = new GameObject("Spawner").AddComponent<Spawner>();
        spawner.prefabObstaculo = prefabObstaculo;
        spawner.prefabMoeda = prefabMoeda;
        spawner.prefabZona = prefabZona;
        spawner.prefabVida = prefabVida;
        spawner.prefabEscudo = prefabEscudo;

        new GameObject("Audio").AddComponent<SoundManager>();
        new GameObject("Efeitos").AddComponent<Efeitos>().sprite = spriteQuadrado;
        var fundo = new GameObject("Fundo").AddComponent<Fundo>();
        fundo.sprite = spriteQuadrado;

        var ciclo = new GameObject("CicloDeCores").AddComponent<CicloDeCores>();
        ciclo.cameraPrincipal = camera;
        ciclo.chao = chao;
        ciclo.teto = teto;
        ciclo.fundo = fundo;
        ciclo.jogador = jogador;
        ciclo.paletas = CicloDeCores.PaletasPadrao();

        // ---- Interface ----
        var ui = CriarInterface(gm);
        gm.jogador = jogador;
        gm.ui = ui;
        gm.cameraShake = tremor;

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), CaminhoCena);
    }

    static SpriteRenderer CriarSuperficie(string nome, float y)
    {
        var go = new GameObject(nome);
        go.transform.position = new Vector3(0f, y, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteBloco;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(60f, 1f);
        sr.color = CorChao;
        go.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
        return sr;
    }

    static void CriarOlho(Transform pai, Vector2 posicao)
    {
        var olho = new GameObject("Olho");
        olho.transform.SetParent(pai, false);
        olho.transform.localPosition = posicao;
        olho.transform.localScale = Vector3.one * 0.18f;
        var sr = olho.AddComponent<SpriteRenderer>();
        sr.sprite = spriteQuadrado;
        sr.color = CorFundo;
        sr.sortingOrder = 6;
    }

    // ================= Interface =================

    static UIManager CriarInterface(GameManager gm)
    {
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = canvasGO.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var ui = canvasGO.AddComponent<UIManager>();
        Transform raiz = canvasGO.transform;

        CriarHUD(raiz, ui);
        CriarMenu(raiz, ui, gm);
        CriarPausa(raiz, ui, gm);
        CriarGameOver(raiz, ui, gm);

        ui.painelPausa.SetActive(false);
        ui.painelGameOver.SetActive(false);
        ui.hud.SetActive(false);
        ui.indicadorEscudo.SetActive(false);
        return ui;
    }

    static void CriarHUD(Transform raiz, UIManager ui)
    {
        var hud = Painel(raiz, "HUD", Color.clear);
        ui.hud = hud;
        ui.textoPontos = Texto(hud.transform, "Pontos", "0", 72, CorTexto, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(50f, -25f), FontStyle.Bold);
        ui.textoRecordeHud = Texto(hud.transform, "Recorde", "RECORDE 0", 30, CorTextoSuave, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(54f, -112f));

        // Corações de vida (máximo 3)
        const int vidas = 3;
        ui.iconesVida = new Image[vidas];
        for (int i = 0; i < vidas; i++)
        {
            var icone = Elemento(hud.transform, "Vida" + (i + 1), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-45f - (vidas - 1 - i) * 66f, -36f), new Vector2(58f, 58f));
            var imagem = icone.AddComponent<Image>();
            imagem.sprite = spriteCoracao;
            imagem.preserveAspect = true;
            imagem.raycastTarget = false;
            ui.iconesVida[i] = imagem;
        }

        // Indicador de escudo (aparece enquanto o jogador tem escudo)
        var indicador = Elemento(hud.transform, "IndicadorEscudo", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-45f, -108f), new Vector2(44f, 44f));
        ui.indicadorEscudo = indicador;
        var iconeEscudo = indicador.AddComponent<Image>();
        iconeEscudo.sprite = spriteEscudo;
        iconeEscudo.color = CorEscudo;
        iconeEscudo.preserveAspect = true;
        iconeEscudo.raycastTarget = false;
        indicador.AddComponent<Piscar>().alfaMinimo = 0.6f;
        var rotulo = Texto(indicador.transform, "Rotulo", "ESCUDO", 26, CorEscudo, TextAnchor.MiddleRight,
            new Vector2(0f, 0.5f), new Vector2(-12f, 0f), FontStyle.Bold);
        ((RectTransform)rotulo.transform).pivot = new Vector2(1f, 0.5f);
        ui.textoEscudo = rotulo;

        Texto(hud.transform, "Dica", "ESC: pausar", 26, new Color(1f, 1f, 1f, 0.35f), TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 20f));
    }

    static void CriarMenu(Transform raiz, UIManager ui, GameManager gm)
    {
        var menu = Painel(raiz, "PainelMenu", new Color(0f, 0f, 0f, 0.3f));
        ui.painelMenu = menu;
        var t = menu.transform;

        // ---- Título ----
        var titulo1 = Texto(t, "TituloGravidade", "GRAVIDADE", 112, CorTexto, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 390f), FontStyle.Bold);
        titulo1.gameObject.AddComponent<TituloAnimado>().girar = false;
        var titulo2 = Texto(t, "TituloZero", "ZERO", 150, CorJogador, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 265f), FontStyle.Bold);
        var brilho = titulo2.gameObject.AddComponent<Outline>();
        brilho.effectColor = new Color(CorJogador.r, CorJogador.g, CorJogador.b, 0.3f);
        brilho.effectDistance = new Vector2(6f, 6f);
        titulo2.gameObject.AddComponent<TituloAnimado>();
        Texto(t, "Subtitulo", "Inverta a gravidade. Desvie. Sobreviva.", 34, CorTextoSuave,
            TextAnchor.MiddleCenter, Centro, new Vector2(0f, 165f));

        // ---- Cartão da esquerda: jogar / sair / recorde ----
        var esquerda = Cartao(t, "CartaoJogar", new Vector2(-400f, -150f), new Vector2(620f, 520f), CorJogador);
        var e = esquerda.transform;
        Botao(e, "BotaoJogar", "JOGAR", new Vector2(0f, 150f), new Vector2(480f, 110f), gm.Jogar, true);
        Texto(e, "DicaEspaco", "ou pressione ESPAÇO", 26, CorMoeda, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 68f)).gameObject.AddComponent<Piscar>();
        Botao(e, "BotaoSair", "SAIR", new Vector2(0f, -15f), new Vector2(480f, 80f), gm.Sair, false);
        var divisoria = Elemento(e, "Divisoria", Centro, Centro, new Vector2(0f, -95f), new Vector2(480f, 3f));
        divisoria.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        Texto(e, "RotuloRecorde", "MELHOR PONTUAÇÃO", 26, CorTextoSuave, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, -140f), FontStyle.Bold);
        ui.textoRecordeMenu = Texto(e, "Recorde", "0", 72, CorMoeda, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, -200f), FontStyle.Bold);

        // ---- Cartão da direita: como jogar ----
        var direita = Cartao(t, "CartaoComoJogar", new Vector2(390f, -150f), new Vector2(740f, 520f), CorObstaculo);
        var d = direita.transform;
        Texto(d, "Titulo", "COMO JOGAR", 38, CorTexto, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 205f), FontStyle.Bold);
        float y = 128f;
        const float passo = 64f;
        LinhaLegenda(d, y, spriteQuadrado, CorJogador, new Vector2(42f, 42f), "Você: inverta a gravidade (até no ar!)");
        LinhaLegenda(d, y -= passo, spriteBloco, CorObstaculo, new Vector2(30f, 52f), "Obstáculo: encostou, perdeu 1 vida");
        LinhaLegenda(d, y -= passo, spriteBloco, new Color(0.35f, 0.65f, 1f, 0.9f), new Vector2(48f, 40f), "Campo azul: gravidade zero, flutue pelo meio");
        LinhaLegenda(d, y -= passo, spriteCirculo, CorMoeda, new Vector2(34f, 34f), "Moeda: +25 pontos");
        LinhaLegenda(d, y -= passo, spriteCoracao, CorVida, new Vector2(44f, 44f), "Coração: recupera 1 vida (máximo 3)");
        LinhaLegenda(d, y -= passo, spriteEscudo, CorEscudo, new Vector2(44f, 44f), "Escudo: 10 s ou 1 batida (quebra o obstáculo)");

        // ---- Barra de controles ----
        var barra = Elemento(t, "Controles", Centro, Centro, new Vector2(0f, -462f), new Vector2(1600f, 60f));
        var layout = barra.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        Tecla(barra.transform, "ESPAÇO");
        Tecla(barra.transform, "W");
        Tecla(barra.transform, "CLIQUE");
        RotuloControle(barra.transform, "inverter a gravidade");
        Espacador(barra.transform, 50f);
        Tecla(barra.transform, "ESC");
        RotuloControle(barra.transform, "pausar");
    }

    static void CriarPausa(Transform raiz, UIManager ui, GameManager gm)
    {
        var pausa = Painel(raiz, "PainelPausa", new Color(0f, 0f, 0f, 0.6f));
        ui.painelPausa = pausa;
        var cartao = Cartao(pausa.transform, "Cartao", Vector2.zero, new Vector2(600f, 520f), CorJogador);
        var c = cartao.transform;
        Texto(c, "Titulo", "PAUSADO", 92, CorTexto, TextAnchor.MiddleCenter, Centro, new Vector2(0f, 170f), FontStyle.Bold);
        Botao(c, "BotaoContinuar", "CONTINUAR", new Vector2(0f, 40f), new Vector2(440f, 96f), gm.Continuar, true);
        Botao(c, "BotaoMenu", "MENU", new Vector2(0f, -70f), new Vector2(440f, 80f), gm.VoltarAoMenu, false);
        Botao(c, "BotaoSair", "SAIR", new Vector2(0f, -170f), new Vector2(440f, 80f), gm.Sair, false);
    }

    static void CriarGameOver(Transform raiz, UIManager ui, GameManager gm)
    {
        var fim = Painel(raiz, "PainelGameOver", new Color(0f, 0f, 0f, 0.6f));
        ui.painelGameOver = fim;
        var cartao = Cartao(fim.transform, "Cartao", new Vector2(0f, 10f), new Vector2(720f, 720f), CorObstaculo);
        var c = cartao.transform;
        Texto(c, "Titulo", "FIM DE JOGO", 92, CorObstaculo, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 280f), FontStyle.Bold);
        Texto(c, "RotuloPontos", "PONTOS", 28, CorTextoSuave, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 190f), FontStyle.Bold);
        ui.textoPontosFinal = Texto(c, "PontosFinal", "0", 104, CorTexto, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, 120f), FontStyle.Bold);
        ui.textoMoedasFinal = Texto(c, "Moedas", "MOEDAS: 0", 30, CorMoeda, TextAnchor.MiddleCenter, Centro,
            new Vector2(-150f, 35f));
        ui.textoDistanciaFinal = Texto(c, "Distancia", "DISTÂNCIA: 0 m", 30, CorJogador, TextAnchor.MiddleCenter, Centro,
            new Vector2(150f, 35f));
        ui.textoRecordeFinal = Texto(c, "RecordeFinal", "RECORDE: 0", 32, CorTextoSuave, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, -20f));
        var novoRecorde = Texto(c, "NovoRecorde", "NOVO RECORDE!", 42, CorMoeda, TextAnchor.MiddleCenter, Centro,
            new Vector2(0f, -75f), FontStyle.Bold);
        novoRecorde.gameObject.AddComponent<Piscar>();
        ui.textoNovoRecorde = novoRecorde.gameObject;
        Botao(c, "BotaoJogarNovamente", "JOGAR NOVAMENTE", new Vector2(0f, -170f), new Vector2(520f, 100f), gm.JogarNovamente, true);
        Botao(c, "BotaoMenu", "MENU", new Vector2(0f, -280f), new Vector2(520f, 80f), gm.VoltarAoMenu, false);
        Texto(fim.transform, "Dica", "ou pressione ESPAÇO para jogar de novo", 28, CorTextoSuave,
            TextAnchor.MiddleCenter, Centro, new Vector2(0f, -400f));
    }

    // ================= Componentes de interface =================

    static GameObject Elemento(Transform pai, string nome, Vector2 ancora, Vector2 pivo, Vector2 posicao, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = pivo;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        return go;
    }

    /// <summary>Elemento que ocupa toda a área do pai.</summary>
    static GameObject Painel(Transform pai, string nome, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (cor.a > 0f)
        {
            var imagem = go.AddComponent<Image>();
            imagem.color = cor;
            imagem.raycastTarget = false;
        }
        return go;
    }

    /// <summary>Borda neon (sprite fatiado) cobrindo o pai.</summary>
    static void Moldura(Transform pai, Color cor)
    {
        var borda = Painel(pai, "Moldura", Color.clear);
        var imagem = borda.AddComponent<Image>();
        imagem.sprite = spriteMoldura;
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 1.5f;
        imagem.color = cor;
        imagem.raycastTarget = false;
    }

    /// <summary>Cartão escuro com borda neon e uma faixa colorida no topo.</summary>
    static GameObject Cartao(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color corBorda)
    {
        var go = Elemento(pai, nome, Centro, Centro, posicao, tamanho);
        var fundo = go.AddComponent<Image>();
        fundo.color = CorCartao;
        fundo.raycastTarget = false;
        Moldura(go.transform, new Color(corBorda.r, corBorda.g, corBorda.b, 0.75f));
        var faixa = Elemento(go.transform, "Faixa", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero,
            new Vector2(tamanho.x, 10f));
        var imagemFaixa = faixa.AddComponent<Image>();
        imagemFaixa.color = corBorda;
        imagemFaixa.raycastTarget = false;
        return go;
    }

    static Text Texto(Transform pai, string nome, string conteudo, int tamanho, Color cor, TextAnchor alinhamento,
        Vector2 ancora, Vector2 posicao, FontStyle estilo = FontStyle.Normal)
    {
        var go = Elemento(pai, nome, ancora, ancora, posicao, new Vector2(1600f, tamanho * 1.4f));
        var rt = (RectTransform)go.transform;
        // Alinha o pivô com o alinhamento do texto para posicionar nos cantos corretamente
        if (alinhamento == TextAnchor.UpperLeft) rt.pivot = new Vector2(0f, 1f);
        else if (alinhamento == TextAnchor.LowerCenter) rt.pivot = new Vector2(0.5f, 0f);
        else if (alinhamento == TextAnchor.MiddleLeft) rt.pivot = new Vector2(0f, 0.5f);

        var texto = go.AddComponent<Text>();
        texto.font = fonte;
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.raycastTarget = false;
        var sombra = go.AddComponent<Shadow>(); // sombra deixa o texto legível sobre qualquer fundo
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(3f, -3f);
        return texto;
    }

    static void Botao(Transform pai, string nome, string rotulo, Vector2 posicao, Vector2 tamanho,
        UnityAction acao, bool principal)
    {
        var go = Elemento(pai, nome, Centro, Centro, posicao, tamanho);
        var imagem = go.AddComponent<Image>();
        imagem.color = principal ? CorJogador : new Color(0.1f, 0.09f, 0.25f, 1f);
        if (!principal) Moldura(go.transform, new Color(CorJogador.r, CorJogador.g, CorJogador.b, 0.8f));

        var botao = go.AddComponent<Button>();
        var cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = principal ? new Color(0.85f, 1f, 1f) : new Color(1.6f, 1.6f, 1.8f);
        cores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cores.selectedColor = Color.white;
        cores.colorMultiplier = 1.5f;
        botao.colors = cores;
        botao.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(botao.onClick, acao);
        go.AddComponent<BotaoAnimado>();

        var texto = Texto(go.transform, "Texto", rotulo, Mathf.RoundToInt(tamanho.y * 0.42f),
            principal ? CorFundo : CorTexto, TextAnchor.MiddleCenter, Centro, Vector2.zero, FontStyle.Bold);
        ((RectTransform)texto.transform).sizeDelta = tamanho;
        if (principal) Object.DestroyImmediate(texto.GetComponent<Shadow>());
    }

    static void LinhaLegenda(Transform pai, float y, Sprite sprite, Color cor, Vector2 tamanhoIcone, string descricao)
    {
        var icone = Elemento(pai, "Icone", Centro, Centro, new Vector2(-305f, y), tamanhoIcone);
        var imagem = icone.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.color = cor;
        imagem.raycastTarget = false;
        if (sprite == spriteBloco)
        {
            imagem.type = Image.Type.Sliced;
            imagem.pixelsPerUnitMultiplier = 2f;
        }
        else
        {
            imagem.preserveAspect = true;
        }
        Texto(pai, "Descricao", descricao, 27, CorTexto, TextAnchor.MiddleLeft, Centro, new Vector2(-255f, y));
    }

    static void Tecla(Transform pai, string nomeTecla)
    {
        var go = new GameObject("Tecla_" + nomeTecla, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var fundo = go.AddComponent<Image>();
        fundo.color = new Color(1f, 1f, 1f, 0.1f);
        fundo.raycastTarget = false;
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        go.AddComponent<LayoutElement>().minHeight = 52f;

        var texto = new GameObject("Texto", typeof(RectTransform));
        texto.transform.SetParent(go.transform, false);
        var t = texto.AddComponent<Text>();
        t.font = fonte;
        t.text = nomeTecla;
        t.fontSize = 26;
        t.fontStyle = FontStyle.Bold;
        t.color = CorTexto;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        Moldura(go.transform, new Color(1f, 1f, 1f, 0.6f));
        // A moldura não deve participar do layout
        go.transform.Find("Moldura").gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    static void RotuloControle(Transform pai, string conteudo)
    {
        var go = new GameObject("Rotulo", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var t = go.AddComponent<Text>();
        t.font = fonte;
        t.text = conteudo;
        t.fontSize = 28;
        t.color = CorTextoSuave;
        t.alignment = TextAnchor.MiddleLeft;
        t.raycastTarget = false;
    }

    static void Espacador(Transform pai, float largura)
    {
        var go = new GameObject("Espaco", typeof(RectTransform));
        go.transform.SetParent(pai, false);
        go.AddComponent<LayoutElement>().minWidth = largura;
    }

    // ================= Projeto =================

    static void ConfigurarProjeto()
    {
        PlayerSettings.productName = "Gravidade Zero";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CaminhoCena, true) };
        AssetDatabase.SaveAssets();
    }
}
