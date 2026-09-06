using UnityEngine;

public abstract class ComportamentoBossBase : MonoBehaviour
{
    public abstract EstadoBoss EstadoAtual { get; }
    public abstract bool Inicializar(MovimentoBossBase movimento);
    public abstract void Iniciar();
    public abstract void Atualizar(float deltaTime);
    public abstract void Cancelar();
}
