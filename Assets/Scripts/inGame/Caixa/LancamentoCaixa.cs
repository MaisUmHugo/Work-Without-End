using UnityEngine;

public class LancamentoCaixa : MonoBehaviour
{
    public CaixaTiro caixaTiro; // referência pro script do pai

    public void InstanciarCaixa()
    {
        if (caixaTiro != null)
            caixaTiro.SpawnCaixa();
    }
}
