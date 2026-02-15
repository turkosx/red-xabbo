<p align="center">
  <img src="ext/screenshot.png" alt="redxabbo screenshot" />
</p>

<h1 align="center">redxabbo</h1>

<p align="center">
  Extensao para G-Earth focada em produtividade, praticidade e qualidade de uso no Habbo.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-ff3b30?style=flat-square&logo=dotnet&logoColor=ffffff&labelColor=0b0f14" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-12-ff3b30?style=flat-square&logo=csharp&logoColor=ffffff&labelColor=0b0f14" alt="C#" />
  <img src="https://img.shields.io/badge/Avalonia-UI-ff3b30?style=flat-square&labelColor=0b0f14" alt="Avalonia UI" />
  <img src="https://img.shields.io/badge/G--Earth-Extension-ff3b30?style=flat-square&labelColor=0b0f14" alt="G-Earth Extension" />
  <img src="https://img.shields.io/badge/Platforms-Windows%20%7C%20Linux%20%7C%20macOS-ff3b30?style=flat-square&labelColor=0b0f14" alt="Platforms" />
  <img src="https://img.shields.io/badge/Habbo-Flash%20%7C%20Origins-ff3b30?style=flat-square&labelColor=0b0f14" alt="Habbo Clients" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-ff3b30?style=flat-square&labelColor=0b0f14" alt="License MIT" /></a>
</p>

<p align="center">
  <a href="https://github.com/turkosx/red-xabbo">Repositorio</a> |
  <a href="https://github.com/turkosx/red-xabbo/releases">Releases</a> |
  <a href="https://github.com/turkosx/red-xabbo/issues">Suporte / Issues</a> |
  <a href="LICENSE">Licenca</a>
</p>

---

## Visao geral
O `redxabbo` e uma extensao para usar junto com o `G-Earth`.

Ela oferece uma interface simples com ferramentas de uso diario, como:
- controles de sala e chat
- utilitarios de visualizacao
- busca de usuario em quartos (`Search/Buscar`)
- suporte de idioma (`PT-BR` e `EN`)

Compatibilidade:
- Sistemas: Windows, Linux, macOS
- Clientes Habbo: Flash e Origins
- Runtime: .NET 8

---

## Requisitos
Para usar:
1. G-Earth instalado e funcionando.
2. `.NET 8 Runtime` instalado:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

---

## Instalacao (usuario final)
### Opcao 1: pelo release oficial
1. Abra:
https://github.com/turkosx/red-xabbo/releases
2. Baixe a versao mais recente.
3. Importe/instale no G-Earth.
4. Inicie a extensao `redxabbo`.

### Opcao 2: pacote pronto
Se voce recebeu `extension.zip` e `extension.json`:
1. Abra o G-Earth.
2. Importe o pacote da extensao.
3. Inicie o `redxabbo`.

---

## Primeiro uso
1. Abra o G-Earth.
2. Conecte no Habbo pelo G-Earth.
3. Inicie o `redxabbo`.
4. Use as abas da interface.

Abas mais usadas:
- `General`: configuracoes rapidas, ferramentas e busca
- `Room`: informacoes e operacoes de quarto
- `Chat`: monitoramento de mensagens
- `Friends`, `Inventory`, `Wardrobe`, `Game data`: utilitarios extras
- `Settings`: ajustes gerais e idioma

---

## Busca de usuario (Search / Buscar)
Para buscar um usuario:
1. Abra `General > Buscar`.
2. Digite o nome do usuario.
3. Clique em `Buscar`.
4. Acompanhe o `Status`.
5. Clique em `Parar` se quiser interromper.

Quando encontra, a extensao retorna ao quarto encontrado e recarrega o ambiente.

---

## Idioma
O redxabbo suporta `PT-BR` e `EN`.

Voce pode alterar idioma pelo botao de idioma na interface principal ou pelas configuracoes.

---

## Problemas comuns
### A extensao nao abre
- Verifique o `.NET 8 Runtime`.
- Feche e abra novamente o G-Earth.

### A extensao nao conecta
- Confirme que o Habbo foi aberto pelo G-Earth.
- Verifique se o G-Earth esta conectado.

### Busca nao encontra o usuario
- O usuario pode nao estar em quarto aberto no momento.
- Tente novamente apos alguns segundos.
- Confira o nome exatamente como aparece no Habbo.

---

## Suporte
- Repositorio:
https://github.com/turkosx/red-xabbo
- Reportar bugs / pedir melhorias:
https://github.com/turkosx/red-xabbo/issues

---

## Para desenvolvedores (opcional)
```bash
git clone https://github.com/turkosx/red-xabbo.git
cd red-xabbo
git submodule update --init --recursive
dotnet restore
dotnet run --project src/Xabbo.Avalonia
```

---

## Licenca
Este projeto usa a licenca MIT.
Veja o arquivo `LICENSE`.
