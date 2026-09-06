using UnityEngine;

public abstract class MovimentoBossBase : MonoBehaviour
{
    public abstract bool EmMovimento { get; }
    public abstract bool Inicializar();
    public abstract void MoverPara(Vector2 destino);
    public abstract void Atualizar(float deltaTime);
    public abstract void Pausar();
    public abstract void Cancelar();
}
