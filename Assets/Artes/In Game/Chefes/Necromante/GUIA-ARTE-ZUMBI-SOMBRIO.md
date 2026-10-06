# Arte do Zumbi Sombrio

O Zumbi Sombrio usa o prefab `Assets/Prefabs/Chefes/Necromante/Invocados/ZumbiSombrio.prefab`. A raiz contém a lógica, o collider e a movimentação. O filho `Visual` contém o `SpriteRenderer` e o `Animator`. Troque a arte no filho `Visual`, sem mover ou redimensionar a raiz. A cor roxa escura atual é provisória; ajuste o tint do `SpriteRenderer` quando a arte final estiver pronta. Confira o collider com a programação se a silhueta mudar.

O Animator do `Visual` usa `Animacoes/ZumbiSombrio_Arte.overrideController`, exclusivo dele. Ele herda os estados de `Zumbi.controller`, mas trocar clips nesse Override não altera o Zumbi comum. Crie os clips a partir das spritesheets e arraste-os para os campos correspondentes no Inspector. Os seis clips atuais são fallback e podem permanecer enquanto faltarem animações.

| Clip atual no Override | Arte a colocar | Quando aparece |
| --- | --- | --- |
| `AndandoZumbi` | Caminhada do Sombrio | Avanço comum |
| `CorrendoZumbi` | Corrida do Sombrio | Avanço rápido |
| `EscorregandoZumbi` | Impacto ou queda | Colisão/atordoamento |
| `PiscarZumbi` | Piscar ou reação curta | Estado visual existente |
| `RecebeuZumbi` | Joinha do Sombrio | Entrega concluída, caso a variante entregável seja adotada |
| `TransparenteZumbi` | Desaparecimento | Estado visual existente |

O parâmetro e estado `RecebeuEntrega` já existem no controller e apontam para o clip que hoje se chama `RecebeuZumbi`. Substitua esse clip no Override pela animação de joinha. Na variante atual **não entregável**, o código não aciona o joinha; ele fica pronto para uma eventual integração da variante entregável. Na build de comparação entregável, a segunda caixa aciona esse estado. Não acrescente transições nem altere scripts de combate para entregar só a arte.

Abra o prefab em Prefab Mode para trocar o sprite parado do `Visual` e ajustar **Transform > Local Scale**. A Scene View mostra o tamanho imediatamente; use a janela Animation em Preview para conferir os quadros sem Play Mode. Mantenha o visual dentro de uma lane e peça revisão do collider antes de fixar a escala final. Inclua os `.meta` dos novos sprites e clips; ao substituir arquivos existentes, preserve os `.meta` para não perder referências.
