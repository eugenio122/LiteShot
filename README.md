LiteShot 🪶


O LiteShot é uma ferramenta de captura de tela leve, offline e focada em produtividade. Desenvolvido em C# (.NET 10) e Windows Forms, ele substitui a tela de captura padrão por um overlay interativo que permite desenhar, anotar e salvar suas capturas rapidamente.

📥 Download

Você não precisa baixar o código-fonte para usar! Baixe a versão pronta para uso diretamente na página de Releases do GitHub:

👉 Baixar LiteShot (Releases) https://github.com/eugenio122/LiteShot/releases/tag/v1.0.0

Na hora de baixar, escolha a versão que melhor te atende: 

Versão Portátil: (Recomendada) Um único arquivo .exe. Não requer instalação e já vem com tudo o que precisa embutido (não exige o .NET instalado no seu PC). É só baixar e rodar!

Versão Normal: Um arquivo menor, mas requer que você tenha o .NET 10 Desktop Runtime instalado no seu computador.

✨ Funcionalidades

100% Offline e Seguro: Sem uploads indesejados para a nuvem. Suas imagens ficam apenas no seu computador ou na sua área de transferência.

Ferramentas de Anotação: Caneta, Marcador, Linha, Seta, Formas, Texto e Seletor de Cores.

Atalhos Globais: Escolha o seu próprio atalho (ex: PrintScreen, Ctrl+Shift+S) que funciona mesmo com o app minimizado na bandeja.

Multilíngue (i18n): Suporte nativo para Português, Inglês, Espanhol, Francês, Alemão e Italiano.

Portátil: Pode ser executado a partir de um único arquivo .exe sem necessidade de instalação.

🚀 Como usar

Abra o LiteShot. Ele ficará oculto na sua Bandeja do Sistema (System Tray) com um ícone de pincel.

Pressione a tecla PrintScreen (ou o atalho que configurou).

Selecione a área da tela que deseja capturar.

Use a barra de ferramentas para desenhar ou anotar.

Copie (Ctrl+C) ou Salve a imagem diretamente!

🛠️ Como compilar (Para Desenvolvedores)

Este projeto usa o .NET 10. Você pode compilá-lo de duas formas:

1. Build Normal (Requer o .NET instalado no PC do usuário):

dotnet build -c Release


2. Build Portátil (Arquivo único, roda em qualquer PC):

dotnet publish -c Release -r win-x64 --self-contained true


📄 Licença

Distribuído sob a licença MIT. Veja o arquivo LICENSE para mais informações.
