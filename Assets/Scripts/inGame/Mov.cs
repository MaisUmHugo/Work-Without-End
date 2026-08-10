using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class Mov : MonoBehaviour
{
    public LanesController.Linhas linhaAtual = LanesController.Linhas.L2; // começa no meio
    private int indiceLinha;

    private INPUTS inputs;

    [SerializeField] private bool isVerticalScrolling = true; // alterna entre vertical, horizontal
    [SerializeField] private bool podeMover = true; // flag para pausar/travar input

    // flag para evitar que o jogador fique pulando várias linhas de uma vez
    private bool inputTravado = false;
    private void Awake()
    {
        inputs = new INPUTS();
    }

    private void OnEnable()
    {
        inputs.Gameplay.Enable();

        // conecta o evento de input
        inputs.Gameplay.Move.started += OnMove;
    }

    private void OnDisable()
    {
        // desconecta o evento de input
        inputs.Gameplay.Move.started -= OnMove;
        inputs.Gameplay.Disable();
    }

    private void Start()
    {
        AudioManager.instance.TocarMusicaJogo();
        indiceLinha = (int)linhaAtual;
        AtualizarPosicao();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (!podeMover) return; // se estiver pausado/travado, ignora

        Vector2 input = ctx.ReadValue<Vector2>();

        if (isVerticalScrolling) // jogo horizontal - troca no eixo X
        {
            // só muda uma vez enquanto o botão/analógico está pressionado
            if (!inputTravado && input.x > 0.5f)
            {
                MudarLinha(1);
                inputTravado = true; // trava até soltar
            }
            else if (!inputTravado && input.x < -0.5f)
            {
                MudarLinha(-1);
                inputTravado = true; // trava até soltar
            }
            else if (Mathf.Abs(input.x) < 0.2f)
            {
                inputTravado = false; // reset quando soltar
            }
        }
        else // jogo vertical - troca no eixo Y
        {
            if (!inputTravado && input.y > 0.5f)
            {
                MudarLinha(-1);
                inputTravado = true;
            }
            else if (!inputTravado && input.y < -0.5f)
            {
                MudarLinha(1);
                inputTravado = true;
            }
            else if (Mathf.Abs(input.y) < 0.2f)
            {
                inputTravado = false;
            }
        }
    }

    private void MudarLinha(int direcao)
    {
        int novaLinha = Mathf.Clamp(indiceLinha + direcao, 0, 3); // 0–3 (L1–L4)

        if (novaLinha != indiceLinha)
        {
            indiceLinha = novaLinha;
            linhaAtual = (LanesController.Linhas)indiceLinha;
            AtualizarPosicao();
        }
    }

    private void AtualizarPosicao()
    {
        Vector3 destino = LanesController.instance.Posicao(linhaAtual);

        if (isVerticalScrolling) // horizontal
        {
            transform.position = new Vector3(destino.x, transform.position.y, transform.position.z);
        }
        else // vertical
        {
            transform.position = new Vector3(transform.position.x, destino.y, transform.position.z);
        }
    }

    // Métodos públicos para pausar e voltar
    public void PausarInput() => podeMover = false;
    public void VoltarInput() => podeMover = true;
}
