# UI — preparação para o Filipe

Os controles são apenas uma tela de consulta por enquanto. Não há botões para remapear teclas. A sensibilidade continua ajustável nas opções e no pause.

## Painel de controles

Edite `Assets/Prefabs/UI/PainelControles.prefab`. Ele é compartilhado pelo menu e pelo pause: altere o prefab para atualizar ambos.

- `Fundo`: imagem do painel; pode receber a arte final.
- `Titulo`: título da tela.
- `TecladoMouse` e `Controle`: cada grupo contém quatro linhas de ação e indicação do botão/tecla.
- Em cada linha, `Acao` identifica a função e `Binding` mostra o controle correspondente.
- `Dica`: informação sobre sensibilidade.
- `BotaoVoltar`: botão funcional com navegação e destaque da seleção.

Os botões Controles e Voltar usam o mesmo fundo amarelo dos botões do menu (`InGame_Buttons-sheet_0`). Preserve o Button, o FeedbackSelecaoUI e os eventos ao trocar a imagem.

O acesso fica em **Opções → Controles** e **Pause → Controles**. `MenuController` e `PauseController` possuem a referência `_painelControles`; os eventos de abrir/voltar já estão conectados em cada prefab. Preserve essas referências e os eventos ao trocar a arte.

As linhas de volume e sensibilidade conservam 128 unidades entre os centros; a opção de parallax, o botão Controles e os botões finais seguem o mesmo intervalo. Os sliders continuam com 480 × 96 unidades. Amplie apenas a altura do Content se adicionar opções.

## HUD do boss

Edite `Assets/Prefabs/UI/HUDBoss.prefab`. O layout anterior foi restaurado, com o nome acima da barra. O texto da fase e o valor numérico de vida ficam desativados; o jogador acompanha a vida pelo preenchimento da barra.

Preserve as referências de `HUDBoss` a nome, fase e preenchimento, mesmo com os textos ocultos. Os fades do encontro usam o grupo `PainelBoss`: mantenha os elementos do HUD dentro dele.

## Atalhos para conferir a UI durante a partida

No componente `ProgressaoNecromante`, marque `Ativar Cheats`. Os atalhos funcionam no Editor e em Development Build, respeitando pause e game over.

- **Shift + F1**: apresenta o reencontro na Floresta e inicia a fase 3 com sua vida própria cheia.
- **Shift + F2**: apresenta o primeiro encontro, esgota sua vida, mostra a fuga e faz a transição real para a Floresta.
- **Shift + F3**: apresenta o reencontro e esgota os dois trechos de vida para mostrar a morte definitiva e o retorno da HUD comum.
- **Shift + 8 / 9**: primeiro encontro / reencontro, sem derrotar automaticamente.

Os novos testes também estão no menu de contexto do componente. Eles usam os eventos normais de vida, animação, HUD e pontuação; não apagam o registro da partida. Para uma nova partida, reinicie o Play Mode.

## Zumbi Sombrio

O tamanho considera os pixels visíveis da arte, pois o sprite do Sombrio tem mais espaço transparente que o sprite do zumbi comum. Alterar apenas a dimensão do quadro da spritesheet pode mudar essa comparação.

A primeira entrega aumenta o visual em 20% (`Escala Por Entrega = 1.2`), mantendo o collider e a lane. Ao completar as duas entregas, o visual volta ao tamanho original para o joinha.

Após a segunda entrega, o Sombrio conclui o joinha, pisca e fica transparente como os demais entregáveis, mantendo o último frame da sua própria arte. Ele continua se deslocando pela esquerda na altura em que recebeu a entrega e é removido ao sair da câmera; existe também um limite de tempo de limpeza no Inspector. O clipe `ZumbiSombrioTransparente` evita trocar o Sombrio pelo sprite do zumbi comum.

No Animator exclusivo do Sombrio, `RecebeuEntrega` e `Transparente` são parâmetros bool. Preserve seus tipos e as transições: a entrega pode terminar tanto durante a caminhada quanto durante a corrida, e o estado transparente segura o joinha sem reiniciar a animação.

## Rebind em uma etapa futura

A base consultada está em `D:/Documentos/6th-Semester-Project/Assets/Game/Scripts/Input/Rebinding` e em `Assets/Game/Scripts/UI/ControlsSettingsUI.cs` daquele projeto. Ela permite usar o componente de rebind da Unity, salvar alterações, cancelar a captura e tratar conflitos.

Antes de adaptar, precisamos usar as ações de `INPUTS/Gameplay`, separar corretamente teclado/mouse e controle e aplicar os bindings salvos às instâncias de INPUTS criadas por movimentação, mira, entrega, menu e pause. Alterar apenas o InputActionReference do painel não atualiza essas instâncias automaticamente. O catálogo de ícones e os painéis de confirmação do outro projeto são dependências de UI que precisam ser adaptadas; não é necessário trazer os sistemas de multiplayer.
