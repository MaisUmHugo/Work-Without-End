using System;

public interface IControladorEncontroBoss
{
    ResultadoEncontroBoss ResultadoAtual { get; }
    bool Encerrado { get; }
    bool Concluido { get; }

    event Action<ResultadoEncontroBoss> EncerramentoIniciado;
    event Action<ResultadoEncontroBoss> EncontroEncerrado;
}
