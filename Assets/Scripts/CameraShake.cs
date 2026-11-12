using UnityEngine;

/// <summary>
/// Faz a câmera tremer (feedback de impacto).
/// </summary>
public class CameraShake : MonoBehaviour
{
    Vector3 posicaoBase;
    float intensidade;
    float duracao;
    float restante;

    void Awake()
    {
        posicaoBase = transform.localPosition;
    }

    public void Tremer(float novaIntensidade, float novaDuracao)
    {
        intensidade = Mathf.Max(novaIntensidade, restante > 0f ? intensidade : 0f);
        duracao = novaDuracao;
        restante = novaDuracao;
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f) return; // não treme com o jogo pausado

        if (restante > 0f)
        {
            restante -= Time.unscaledDeltaTime;
            float forca = intensidade * Mathf.Clamp01(restante / duracao);
            transform.localPosition = posicaoBase + (Vector3)(Random.insideUnitCircle * forca);
        }
        else
        {
            transform.localPosition = posicaoBase;
        }
    }
}
