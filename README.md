# Gravidade Zero

Jogo 2D de sobrevivência feito em **Unity 2022.3** para a disciplina de Desenvolvimento de Jogos Digitais.

O personagem corre sozinho por um corredor espacial, e o jogador só tem um poder: **inverter a gravidade**. Para sobreviver, ele escolhe entre o chão, o teto ou o meio da tela, desvia dos obstáculos e coleta moedas, corações e escudos. A velocidade aumenta com o tempo.

## Controles

| Ação | Tecla |
|---|---|
| Inverter a gravidade (funciona até no ar) | ESPAÇO, W, SETA PARA CIMA ou clique do mouse |
| Pausar / continuar | ESC ou P |
| Jogar de novo (na tela de fim de jogo) | ESPAÇO, ENTER ou R |

## Elementos do jogo

| Elemento | O que faz |
|---|---|
| Obstáculo | Tira 1 vida ao encostar. Depois do dano, o jogador fica 1,5 s invencível, piscando |
| Campo de gravidade zero (azul) | Desliga a gravidade e conduz o jogador flutuando pelo meio da tela |
| Moeda (amarela) | +25 pontos |
| Coração (vermelho) | Recupera 1 vida, até no máximo 3. Só aparece quando falta vida |
| Escudo (verde) | Dura até 10 s e aguenta 1 batida: o obstáculo se quebra e o escudo some. Pisca nos últimos 2 s |

## Ciclo de cores
A partir de **300 pontos**, o cenário troca de paleta a cada 250 pontos, num estilo parecido com o dia e a noite do jogo do dinossauro do navegador. As paletas são noite neon, dia invertido, alerta vermelho, Matrix e pôr do sol. A troca é suave, mas muda as cores do fundo, do chão, dos obstáculos e do próprio personagem, o que confunde o jogador e deixa as partidas longas mais difíceis. A lógica está em `CicloDeCores.cs`, e as paletas podem ser editadas no Inspector.

## Padrões de obstáculos
Eles vão sendo liberados conforme você avança, e todos sempre têm uma passagem possível.
- **Alto do chão:** fique no teto.
- **Alto do teto:** fique no chão.
- **Meio flutuante:** fique no chão ou no teto.
- **Duplo** (a partir de 100 m): obstáculos em cima e embaixo. Antes deles surge um campo de gravidade zero; basta inverter uma vez para entrar nele, e o campo segura o jogador no meio.
- **Zigue-zague** (a partir de 250 m): inverta no momento certo, duas vezes seguidas.

## Conceitos aplicados

| Conceito | Onde está no projeto |
|---|---|
| Controle de entradas | `PlayerController` (inverter a gravidade), `GameManager` (pausar, iniciar, reiniciar) e botões da interface |
| Movimentação | Obstáculos, itens e cenário se movem para a esquerda, e o jogador se move na vertical |
| Transform e física | **Física:** `Rigidbody2D.gravityScale` invertido (+/−), gravidade zerada nos campos e velocidade vertical limitada. **Transform:** `MovimentoMundo` usa `Transform.Translate` |
| Colisões e interações | Colisão física com chão e teto. *Triggers* para obstáculos (dano ou quebra com escudo), moedas, corações, escudos e campos de gravidade zero |
| Pontuação, vidas e derrota | Pontos por distância e moedas. 3 vidas no máximo, fim de jogo e recorde salvo com `PlayerPrefs` |
| Controle de estados | **Jogo:** Menu → Jogando → Pausado → GameOver. **Jogador:** NoChao, NoTeto, NoAr, Flutuando, Morto, mais invencibilidade temporária e escudo de 1 batida. **Cenário:** paleta de cores trocada conforme a pontuação |
| Interface e feedback | Menu com título animado, HUD (pontos, recorde, corações e indicador de escudo), tremida de câmera, *hit stop*, partículas, rastro, animação de achatar e esticar, ciclo de cores, sons e duas músicas |

Outros detalhes:
- **Dificuldade progressiva:** a velocidade aumenta com o tempo. O espaço entre os obstáculos é calculado em segundos, então sempre dá tempo de reagir.
- **Áudio gerado por código** (síntese em `SoundManager`): música ambiente no menu, música com bateria durante a partida (com transição suave entre as duas) e todos os efeitos sonoros. Não há arquivos de áudio.
- **Gráficos** com visual neon gerados pelo próprio projeto (sprites criados pela ferramenta de editor).

## Estrutura

```
Assets/
  Scenes/Jogo.unity          cena única do jogo
  Scripts/
    GameManager.cs           estados, velocidade, pontos, vidas e recorde
    PlayerController.cs      entrada, gravidade, colisões, dano, escudo e campo de gravidade zero
    Spawner.cs               gera padrões de obstáculos, moedas e itens
    MovimentoMundo.cs        move objetos via Transform e destrói os que saem da tela
    Obstaculo.cs, Moeda.cs, PowerUp.cs, ZonaGravidadeZero.cs
    CicloDeCores.cs          troca de paletas do cenário conforme a pontuação
    UIManager.cs             HUD e painéis (menu, pausa e fim de jogo)
    SoundManager.cs          músicas e efeitos sonoros gerados por código
    CameraShake.cs, Efeitos.cs, Particula.cs, Fundo.cs,
    Piscar.cs, TituloAnimado.cs, BotaoAnimado.cs        feedback visual
  Editor/MontarJogo.cs       ferramenta que monta a cena e gera a build
  Prefabs/, Sprites/
```

## Como abrir e rodar
1. Abra a pasta do projeto no **Unity Hub** (*Add → Add project from disk*) usando o Unity **2022.3**.
2. Abra a cena `Assets/Scenes/Jogo.unity` e aperte **Play**.
3. Para gerar o executável, use o menu **Gravidade Zero → 2. Gerar build para Windows**. O jogo sai em `Build/GravidadeZero.exe`.
