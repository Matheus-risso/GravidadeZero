using UnityEngine;

/// <summary>
/// Move o objeto para a esquerda na velocidade do mundo (via Transform) e o destrói quando sai da tela.
/// </summary>
public class MovimentoMundo : MonoBehaviour
{
    public float largura = 1f;
    public float xRemocao = -14f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        transform.Translate(Vector3.left * gm.Velocidade * Time.deltaTime, Space.World);

        if (transform.position.x + largura < xRemocao)
            Destroy(gameObject);
    }
}
