# Atualização para Lumina Player 1.3.0

1. Extraia o ZIP completo.
2. No GitHub, abra **Add file → Upload files**.
3. Envie todos os itens que estão dentro de `LuminaPlayer-Windows`, incluindo `.github`, `design`, `installer`, `LuminaPlayer`, `DESIGN-SPEC.md`, `LuminaPlayer.sln`, `publish.ps1`, `README.md` e este arquivo.
4. Preserve os caminhos: `LuminaPlayer/...`, `.github/...` e `installer/...`. Não crie uma pasta externa duplicada `LuminaPlayer-Windows/LuminaPlayer/...`.
5. Faça Commit diretamente na branch `main`.
6. Abra **Actions → Build Windows installer → Run workflow**.
7. Aguarde o indicador verde e baixe `LuminaPlayer-Setup-win-x64` em Artifacts.
8. Desinstale a versão anterior pelo Windows e instale a nova versão.
9. Teste: adicionar uma pasta que contenha arquivos MP3/WAV, clicar uma vez num cartão para tocar, testar play/pause, próxima/anterior, seek e volume.

Este ZIP contém código-fonte, não um instalador pronto. O GitHub Actions compila o executável e cria o instalador em um runner Windows. A compilação e os testes reais de áudio precisam passar antes de considerar a versão estável.
