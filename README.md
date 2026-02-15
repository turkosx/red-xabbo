<p align="center">
  <img src="ext/screenshot.png" alt="redxabbo screenshot" />
</p>

<h1 align="center">redxabbo</h1>

<p align="center">
  Extensao cross-platform para G-Earth focada em produtividade e qualidade de uso no Habbo.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-ff3b30?style=flat-square&logo=dotnet&logoColor=ffffff&labelColor=0b0f14" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-12-ff3b30?style=flat-square&logo=csharp&logoColor=ffffff&labelColor=0b0f14" alt="C#" />
  <img src="https://img.shields.io/badge/Avalonia-UI-ff3b30?style=flat-square&labelColor=0b0f14" alt="Avalonia UI" />
  <img src="https://img.shields.io/badge/Xabbo-Framework-ff3b30?style=flat-square&labelColor=0b0f14" alt="Xabbo Framework" />
  <img src="https://img.shields.io/badge/G--Earth-Extension-ff3b30?style=flat-square&labelColor=0b0f14" alt="G-Earth Extension" />
  <img src="https://img.shields.io/badge/Platforms-Windows%20%7C%20Linux%20%7C%20macOS-ff3b30?style=flat-square&labelColor=0b0f14" alt="Platforms" />
  <img src="https://img.shields.io/badge/Habbo-Flash%20%7C%20Origins-ff3b30?style=flat-square&labelColor=0b0f14" alt="Habbo Clients" />
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-ff3b30?style=flat-square&labelColor=0b0f14" alt="License MIT" /></a>
</p>

<p align="center">
  <a href="https://github.com/turkosx/red-xabbo">Repositorio</a> |
  <a href="https://github.com/turkosx/red-xabbo/issues">Issues</a> |
  <a href="https://xabbo.io/api/Xabbo">Xabbo API</a> |
  <a href="https://www.habbo.com.br/api/public/api-docs/#/">Habbo API</a>
</p>

<p align="center">
  <img src="https://cdn.simpleicons.org/dotnet/ff3b30" height="20" alt=".NET" />
  <img src="https://cdn.simpleicons.org/avaloniaui/ff3b30" height="20" alt="Avalonia" />
  <img src="https://cdn.simpleicons.org/githubactions/ff3b30" height="20" alt="GitHub Actions" />
  <img src="https://cdn.simpleicons.org/linux/ff3b30" height="20" alt="Linux" />
  <img src="https://cdn.simpleicons.org/apple/ff3b30" height="20" alt="macOS" />
  <img src="https://img.shields.io/badge/Windows-ff3b30?style=flat-square&logo=windows&logoColor=ffffff&labelColor=0b0f14" height="20" alt="Windows" />
</p>

---

## Sumario
- <img src="https://cdn.simpleicons.org/github/ff3b30" height="14" alt="" /> [Visao geral](#visao-geral)
- <img src="https://cdn.simpleicons.org/dotnet/ff3b30" height="14" alt="" /> [Requisitos](#requisitos)
- <img src="https://cdn.simpleicons.org/git/ff3b30" height="14" alt="" /> [Setup rapido](#setup-rapido)
- <img src="https://cdn.simpleicons.org/dotnet/ff3b30" height="14" alt="" /> [Executar em desenvolvimento](#executar-em-desenvolvimento)
- <img src="https://cdn.simpleicons.org/githubactions/ff3b30" height="14" alt="" /> [Build e testes](#build-e-testes)
- <img src="https://cdn.simpleicons.org/gnubash/ff3b30" height="14" alt="" /> [Empacotar extensao](#empacotar-extensao)
- <img src="https://cdn.simpleicons.org/task/ff3b30" height="14" alt="" /> [Scripts do Taskfile](#scripts-do-taskfile)
- <img src="https://cdn.simpleicons.org/githubactions/ff3b30" height="14" alt="" /> [CI](#ci)
- <img src="https://cdn.simpleicons.org/github/ff3b30" height="14" alt="" /> [Estrutura do repositorio](#estrutura-do-repositorio)
- <img src="https://cdn.simpleicons.org/sentry/ff3b30" height="14" alt="" /> [Troubleshooting](#troubleshooting)
- <img src="https://cdn.simpleicons.org/bookstack/ff3b30" height="14" alt="" /> [Licenca](#licenca)

## Visao geral
- Extensao para G-Earth com foco em automacoes e operacoes de sala.
- UI desktop em Avalonia com suporte a EN/PT-BR.
- Compatibilidade com clientes Habbo Flash e Origins.
- Distribuicao multiplataforma (Windows, Linux, macOS).
- Baseada no ecossistema Xabbo (`common`, `messages`, `gearth`, `core`).

Compatibilidade atual:
- Sistemas: Windows, Linux, macOS
- Clientes Habbo: Flash e Origins
- Runtime: .NET 8
- UI: Avalonia (EN/PT-BR)

## Requisitos
Para usar a extensao (release):
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- G-Earth instalado e funcional

Para desenvolver localmente:
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Git com suporte a submodules
- Bash/ZIP para empacotamento (opcional)

## Setup rapido
```bash
git clone https://github.com/turkosx/red-xabbo.git
cd red-xabbo
git submodule update --init --recursive
dotnet restore
```

## Executar em desenvolvimento
Comando direto:
```bash
dotnet run --project src/Xabbo.Avalonia
```

Atalho com Taskfile:
```bash
task run
```

## Build e testes
Build completo da solucao:
```bash
dotnet build Xabbo.sln
```

Teste da solucao principal:
```bash
dotnet test Xabbo.sln
```

Suite de testes de base (Xabbo.Common):
```bash
dotnet test lib/common/Xabbo.Common.sln
```

## Empacotar extensao
Script oficial:
```bash
bash ./.scripts/pack.sh
```

Atalho:
```bash
task pack
```

Saida esperada:
- `out/extension.zip`
- `out/extension.json`
- `out/icon.png`
- `out/screenshot.png`

## Scripts do Taskfile
- `task run`: build + execucao local da aplicacao
- `task pack`: empacotamento da extensao
- `task new-control`: scaffolding de novo control Avalonia (interativo)
- `task new-view`: scaffolding de nova view Avalonia (interativo)

## CI
Workflow em `.github/workflows/dotnet.yml`:
- checkout com submodules
- setup .NET 8
- `dotnet restore`
- `dotnet build --no-restore`
- `dotnet test --no-build`
- `dotnet test lib/common/Xabbo.Common.sln`

## Estrutura do repositorio
- `src/Xabbo`: nucleo da extensao (dominio, controllers, services, viewmodels)
- `src/Xabbo.Avalonia`: camada de UI (views, styles, assets, bootstrap desktop)
- `lib/common`, `lib/messages`, `lib/gearth`, `lib/core`: submodules do framework Xabbo
- `ext`: metadados e assets de distribuicao
- `.scripts`: automacao local (pack e scaffolding)

## Troubleshooting
- Nao conecta ao G-Earth: valide cookie/porta e se o G-Earth esta ativo.
- Erros de build em clone novo: rode `git submodule update --init --recursive`.
- Testes aparentam nao rodar na solucao principal: rode tambem `dotnet test lib/common/Xabbo.Common.sln`.
- Falha no pack para macOS: verifique se `lipo` esta disponivel no ambiente.

## Licenca
Este projeto e distribuido sob a licenca MIT. Veja `LICENSE`.
