using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public int pontuacaoAtual { get; private set; }
    public int EntregasRealizadas { get; private set; }
    public int EntregasPerdidas { get; private set; }
    public int VezesAtingido { get; private set; }
    public int MaiorCombo { get; private set; }
    public bool PrimeiroEncontroNecromanteIniciado { get; private set; }
    public bool SegundoEncontroNecromanteIniciado { get; private set; }
    public bool NecromanteFugiu { get; private set; }
    public bool NecromanteDerrotadoDefinitivamente { get; private set; }
    public bool PartidaEncerrada { get; private set; }

    public event Action<int> OnScoreMudou;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AdicionarPontos(int valor)
    {
        if (PartidaEncerrada) return;
        pontuacaoAtual += valor;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }

    public void Resetar()
    {
        pontuacaoAtual = 0;
        EntregasRealizadas = 0;
        EntregasPerdidas = 0;
        VezesAtingido = 0;
        MaiorCombo = 0;
        PrimeiroEncontroNecromanteIniciado = false;
        SegundoEncontroNecromanteIniciado = false;
        NecromanteFugiu = false;
        NecromanteDerrotadoDefinitivamente = false;
        PartidaEncerrada = false;
        OnScoreMudou?.Invoke(pontuacaoAtual);
    }

    public void RegistrarEntrega(int comboAtual)
    {
        if (PartidaEncerrada) return;
        EntregasRealizadas++;
        MaiorCombo = Mathf.Max(MaiorCombo, comboAtual);
    }

    public void RegistrarEntregaPerdida()
    {
        if (!PartidaEncerrada) EntregasPerdidas++;
    }

    public void RegistrarDanoRecebido()
    {
        if (!PartidaEncerrada) VezesAtingido++;
    }

    public void RegistrarInicioNecromante(TipoEncontroNecromante tipo)
    {
        if (PartidaEncerrada) return;
        if (tipo == TipoEncontroNecromante.PrimeiroEncontro)
            PrimeiroEncontroNecromanteIniciado = true;
        else
            SegundoEncontroNecromanteIniciado = true;
    }

    public void RegistrarResultadoNecromante(ResultadoEncontroBoss resultado, int pontos)
    {
        if (PartidaEncerrada) return;
        if (resultado == ResultadoEncontroBoss.Fuga && !NecromanteFugiu)
            NecromanteFugiu = true;
        else if (resultado == ResultadoEncontroBoss.DerrotaDefinitiva && !NecromanteDerrotadoDefinitivamente)
            NecromanteDerrotadoDefinitivamente = true;
        else
            return;
        AdicionarPontos(Mathf.Max(0, pontos));
    }

    public void EncerrarRegistroPartida()
    {
        PartidaEncerrada = true;
    }

    public string ObterResumoPartida()
    {
        string resumo = $"Entregas realizadas: {EntregasRealizadas}\nEntregas perdidas: {EntregasPerdidas}"
            + $"\nVezes atingido: {VezesAtingido}\nMaior combo: {MaiorCombo}";
        if (NecromanteFugiu)
            resumo += "\nCidade: Necromante fugiu";
        if (NecromanteDerrotadoDefinitivamente)
            resumo += "\nFloresta Morta: Necromante derrotado definitivamente";
        return resumo;
    }
}
