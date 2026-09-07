using UnityEngine;

public abstract class AtaqueBossBase : MonoBehaviour
{
    public abstract bool EmExecucao { get; }
    public abstract bool EmPreparacao { get; }
    public abstract bool Inicializar();
    public abstract bool TentarIniciar(Transform alvo);
    public abstract void Atualizar(float deltaTime);
    public abstract void Cancelar();
}
