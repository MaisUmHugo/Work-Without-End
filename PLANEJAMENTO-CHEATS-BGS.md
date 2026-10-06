# Controle de cheats — planejamento aprovado

Status: planejado; ainda não implementar.

Criar um CheatManager simples em um objeto geral ativo da cena. Os comandos continuam nos scripts dos respectivos sistemas, consultando a permissão central antes de executar. Não desativar managers de gameplay para desligar cheats.

## Configuração no Inspector

- Ativar Cheats: interruptor geral, desligado por padrão.
- Permitir Cheats na Build: autorização geral para builds, desligada por padrão.
- Cada comando tem permissões independentes para Editor e Build.
- Uma build normal pode executar somente os comandos autorizados individualmente quando os dois interruptores gerais estiverem ligados. Development Build também segue a seleção para Build.
- Sem CheatManager ativo, todos os cheats ficam bloqueados.
- Manter as opções locais existentes como restrições adicionais.

## Comandos a organizar

| Grupo | Comando | Atalho atual |
| --- | --- | --- |
| Hordas | Avançar uma horda | N |
| Hordas | Avançar dez hordas | M |
| Hordas | Dificuldade máxima | Shift + T |
| Vida | Perder vida | Shift + P |
| Vida | Ganhar vida | Shift + V |
| Necromante | Projétil comum | Shift + 1 |
| Necromante | Tiro carregado | Shift + 2 |
| Necromante | Sequência rápida | Shift + 3 |
| Necromante | Invocação | Shift + 4 |
| Necromante | Sombrio isolado | Shift + 5 |
| Cenário | Cidade | Shift + 6 |
| Cenário | Floresta Morta | Shift + 7 |
| Encontros | Primeira aparição | Shift + 8 |
| Encontros | Reaparição | Shift + 9 |

## Integração e validação futuras

Revisar HordaManager, VidaManager, CheatAtaquesNecromante e ProgressaoNecromante. Os atalhos de hordas e vida atualmente não têm proteção para builds normais. Os do Necromante precisam deixar de depender exclusivamente de UNITY_EDITOR ou DEVELOPMENT_BUILD para permitir a seleção em builds normais.

Aplicar a mesma permissão às entradas de teste por teclado, métodos e menus de contexto. Conferir também as alterações temporárias de HUD existentes no componente de cheats, mantendo as regras de HUD do jogo independentes. Pedidos de teste pendentes devem conferir a permissão novamente antes da execução.

Pular cutscene é um controle normal do jogo e permanece disponível. Logs de erro e avisos não são cheats.

Validar permissões por comando no Editor e em builds, ausência ou desativação do manager, e continuidade de vida, hordas, encontros automáticos, pontuação e HUD com todos os cheats desligados. Para a BGS, desligar Ativar Cheats; builds específicas de teste podem habilitar somente os comandos desejados.
