# xabbo

Extensao cross-platform para [G-Earth](https://github.com/sirjonasxx/G-Earth), focada em produtividade e qualidade de uso no Habbo.

Compatibilidade atual:
- Sistemas: Windows, Linux, macOS
- Clientes Habbo: Flash e Origins
- Runtime: .NET 8
- UI: Avalonia (EN/PT-BR)

<img src="https://raw.githubusercontent.com/xabbo/xabbo/refs/heads/main/ext/screenshot.png" width="720" alt="xabbo screenshot" />

## Requisitos

Para usar a extensao (release):
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- G-Earth instalado e funcional

Para desenvolver localmente:
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Git com suporte a submodules
- Bash/ZIP para empacotamento (opcional)

## Setup do projeto

```sh
git clone https://github.com/xabbo/xabbo
cd xabbo
git submodule update --init --recursive
dotnet restore
```

## Executar em desenvolvimento

Comando direto:

```sh
dotnet run --project src/Xabbo.Avalonia
```

Atalho com Taskfile (opcional):

```sh
task run
```

## Build e teste

Build da aplicacao:

```sh
dotnet build src/Xabbo.Avalonia/Xabbo.Avalonia.csproj
```

Build completo da solucao:

```sh
dotnet build Xabbo.sln
```

Testes:

```sh
dotnet test Xabbo.sln
```

## Empacotar extensao

Script oficial:

```sh
bash ./.scripts/pack.sh
```

Atalho:

```sh
task pack
```

Saida esperada:
- `out/extension.zip`
- `out/extension.json`
- `out/icon.png`
- `out/screenshot.png`

Observacoes sobre o empacotamento:
- O script publica multiplas plataformas (`win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`).
- O merge dos binarios macOS usa `lipo` (necessario ter a ferramenta disponivel).
- A versao e gerada via `dotnet-gitversion`.

## Scripts auxiliares

Criar novo control Avalonia:

```sh
task new-control
```

Criar nova view Avalonia:

```sh
task new-view
```

Os scripts usam `gum` para prompt interativo. Sem `gum`, rode os comandos `dotnet new` manualmente.

## Estrutura do repositorio

- `src/Xabbo`: nucleo da extensao (dominio, controllers, servicos, viewmodels)
- `src/Xabbo.Avalonia`: camada de UI (views, estilos, assets, bootstrap desktop)
- `lib/common`, `lib/messages`, `lib/gearth`, `lib/core`: submodules do framework xabbo
- `ext`: metadados e assets de distribuicao da extensao
- `.scripts`: automacao local (pack e scaffolding)

## Contribuicao

1. Crie uma branch para sua alteracao.
2. Rode `dotnet build Xabbo.sln`.
3. Rode `dotnet test Xabbo.sln`.
4. Abra um Pull Request com descricao objetiva do que mudou e como validar.

## Licenca

Distribuido sob licenca MIT. Veja [`LICENSE`](LICENSE).
