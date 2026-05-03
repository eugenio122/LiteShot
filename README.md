# LiteShot :pen:

O **LiteShot** é uma ferramenta de captura de tela leve, offline e focada em produtividade. Criado inicialmente como um projeto de estudo e como resposta a fricções reais no fluxo de trabalho de QA causadas por ferramentas pesadas ou lentas, o seu objetivo sempre foi garantir velocidade, baixo consumo de recursos e anotações imediatas. Desenvolvido em C# (.NET 10) e Windows Forms, ele substitui a tela de captura padrão por um overlay interativo que permite desenhar, anotar e salvar suas capturas rapidamente.

<br>

## 🔒 Segurança e Privacidade

> [!IMPORTANT]
> **O LiteShot é uma ferramenta 100% offline.**
> 
> * Não realiza chamadas de rede.
> * Não possui telemetria.
> * Não coleta métricas de uso.
> * Não envia dados para a nuvem.
> * Não possui mecanismo de auto-update.
> 
> Todo o processamento ocorre localmente, em sua maioria por ações explícitas do usuário. O código é *open source* para permitir auditoria e total transparência.

<br>

## 🎯 Non-Goals (O que NÃO fazemos)

> [!WARNING]
> **O LiteShot não tem como objetivo:**
> 
> * Substituir ferramentas corporativas oficiais.
> * Sincronizar ou fazer upload de dados na nuvem.
> * Rodar como serviço pesado em background.
> * Coletar informações de usuários (telemetria zero).
> 
> *O foco é estritamente a produtividade local e a captura rápida de evidências.*

<br>

## 📥 Download

Você não precisa baixar o código-fonte para usar! Baixe a versão pronta para uso diretamente na página de Releases do GitHub:

👉 [**Baixar LiteShot (Versão Mais Recente)**](https://github.com/eugenio122/LiteShot/releases/latest)

<br>

## ✨ Funcionalidades

* **100% Offline e Seguro:** Sem uploads indesejados para a nuvem. Suas imagens ficam apenas no seu computador ou na sua área de transferência.

* **Ferramentas de Anotação:** Caneta, Marcador, Linha, Seta, Formas, Texto e Seletor de Cores.

* **Precisão e Preview:** Ajuste a espessura do traço pixel a pixel com `Ctrl +` e `Ctrl -`, visualizando exatamente o tamanho através de um preview visual no cursor (estilo MS Paint) antes mesmo de desenhar.

* **Borracha Mágica (Desfazer/Refazer):** Errou o traço? Pressione `Ctrl + Z` para desfazer ou `Ctrl + Y` para refazer a anotação, ou utilize os botões dedicados na barra de ferramentas.

* **Seleção Inteligente & Memória:** \* Atalho `Ctrl + A` ou botão de expansão para capturar o monitor inteiro rapidamente, com foco imediato do teclado.
  * Opções para lembrar o tamanho e a posição exata da sua última captura e da barra de ferramentas.

* **Interface Modular e Compacta:** Arraste a barra de ferramentas para qualquer canto da tela clicando em qualquer espaço livre nela, ou mude seu layout para **Vertical** nas opções.

* **Atalhos Globais e Estabilidade:** Escolha o seu próprio atalho que funciona mesmo com o app minimizado. Construído com bloqueio nativo contra vazamento de memória (Event Leaking) para garantir total leveza e estabilidade durante todo o expediente de trabalho.

* **Multilíngue (i18n):** Suporte nativo para Português, Inglês, Espanhol, Francês, Alemão e Italiano.

* **Portátil e Limpo:** Pode ser executado a partir de um único arquivo `.exe` sem necessidade de instalação. As notificações de sucesso não poluem o menu `Alt+Tab` do Windows.

<br>

## 🚀 Como usar

1. Abra o LiteShot. Ele ficará oculto na sua Bandeja do Sistema (System Tray) com um ícone de pincel.
2. Pressione a tecla `PrintScreen` (ou o atalho que configurou).
3. Selecione a área da tela que deseja capturar.
4. Use a barra de ferramentas (arrastável) para desenhar ou anotar.
5. Copie (`Ctrl + C`) ou Salve (`Ctrl + S`) a imagem diretamente!

<br>

## 🛠️ Como compilar (Para Desenvolvedores)

Este projeto usa o .NET 10. Você pode compilá-lo para gerar o executável portátil da seguinte forma:

**Build Portátil (Arquivo único, roda em qualquer PC):**

```bash
dotnet publish -c Release -r win-x64 --self-contained true
