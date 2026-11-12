using UnityEngine;

/// <summary>
/// Anima um texto do título: flutua suavemente e, de tempos em tempos,
/// vira de cabeça para baixo (como a gravidade do jogo).
/// </summary>
public class TituloAnimado : MonoBehaviour
{
    public float intervaloGiro = 3f;
    public float duracaoGiro = 0.35f;
    public float alturaFlutuacao = 8f;
    public bool girar = true;

    Vector3 posicaoBase;
    float proximoGiro;
    bool invertido;
    float escalaY = 1f;

    void Awake()
    {
        posicaoBase = transform.localPosition;
        proximoGiro = Time.unscaledTime + intervaloGiro;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (girar && t >= proximoGiro)
        {
            invertido = !invertido;
            proximoGiro = t + intervaloGiro;
        }

        escalaY = Mathf.MoveTowards(escalaY, invertido ? -1f : 1f, Time.unscaledDeltaTime * 2f / duracaoGiro);
        transform.localScale = new Vector3(1f, escalaY, 1f);
        transform.localPosition = posicaoBase + Vector3.up * Mathf.Sin(t * 2f) * alturaFlutuacao;
    }
}
