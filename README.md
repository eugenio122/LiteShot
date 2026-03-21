LiteShot 🪶


O LiteShot é uma ferramenta de captura de tela leve, offline e focada em produtividade. Desenvolvido em C# (.NET 10) e Windows Forms, ele substitui a tela de captura padrão por um overlay interativo que permite desenhar, anotar e salvar suas capturas rapidamente.

📥 Download

Você não precisa baixar o código-fonte para usar! Baixe a versão pronta para uso diretamente na página de Releases do GitHub:

👉 Baixar LiteShot (Versão Mais Recente) https://github.com/eugenio122/LiteShot/releases/tag/v1.2.0

✨ Funcionalidades

100% Offline e Seguro: Sem uploads indesejados para a nuvem. Suas imagens ficam apenas no seu computador ou na sua área de transferência.

Ferramentas de Anotação: Caneta, Marcador, Linha, Seta, Formas, Texto e Seletor de Cores.

Borracha Mágica (Desfazer/Refazer): Errou o traço? Pressione Ctrl + Z para desfazer ou Ctrl + Y para refazer a anotação, ou utilize os novos botões dedicados na barra de ferramentas.

Seleção Inteligente & Memória: * Atalho Ctrl + A ou botão de expansão para capturar o monitor inteiro rapidamente.

Opções para lembrar o tamanho e a posição exata da sua última captura e da barra de ferramentas.

Interface Modular e Compacta: Arraste a barra de ferramentas para qualquer canto da tela clicando em qualquer espaço livre nela, ou mude seu layout para Vertical nas opções.

Atalhos Globais: Escolha o seu próprio atalho (ex: PrintScreen, Ctrl+Shift+S) que funciona mesmo com o app minimizado na bandeja. Bloqueio nativo contra vazamento de eventos (Event Leaking).

Multilíngue (i18n): Suporte nativo para Português, Inglês, Espanhol, Francês, Alemão e Italiano.

Portátil: Pode ser executado a partir de um único arquivo .exe sem necessidade de instalação.

🚀 Como usar

Abra o LiteShot. Ele ficará oculto na sua Bandeja do Sistema (System Tray) com um ícone de pincel.

Pressione a tecla PrintScreen (ou o atalho que configurou).

Selecione a área da tela que deseja capturar.

Use a barra de ferramentas (arrastável) para desenhar ou anotar.

Copie (Ctrl + C) ou Salve (Ctrl + S) a imagem diretamente!

🛠️ Como compilar (Para Desenvolvedores)

Este projeto usa o .NET 10. Você pode compilá-lo para gerar o executável portátil da seguinte forma:

Build Portátil (Arquivo único, roda em qualquer PC):

dotnet publish -c Release -r win-x64 --self-contained true


📄 Licença

Distribuído sob a licença MIT. Veja o arquivo LICENSE para mais informações.
