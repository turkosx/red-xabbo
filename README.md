<p align="center">
  <img src="ext/screenshot.png" alt="redxabbo screenshot" />
</p>

<h1 align="center">redxabbo</h1>

<p align="center">
  Extensao para G-Earth focada em facilitar o uso do Habbo no dia a dia.
</p>

---

## O que e o redxabbo
O `redxabbo` e uma extensao para usar junto com o `G-Earth`.

Ela adiciona uma interface simples com ferramentas de qualidade de vida, como:
- controles de sala e chat
- utilitarios de visualizacao
- busca de usuario em quartos (`Search/Buscar`)
- suporte a idioma (`PT-BR` e `EN`)

Compatibilidade:
- Sistemas: Windows, Linux, macOS
- Clientes Habbo: Flash e Origins
- Runtime: .NET 8

---

## Requisitos
Antes de usar:
1. Instale o `G-Earth`.
2. Instale o `.NET 8 Runtime`:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

---

## Instalacao (usuario final)
### Opcao 1: Releases do projeto
1. Acesse:
https://github.com/turkosx/red-xabbo/releases
2. Baixe os arquivos da versao mais recente.
3. Importe a extensao no G-Earth e abra normalmente.

### Opcao 2: Extensao ja empacotada
Se voce recebeu `extension.zip` e `extension.json`:
1. Abra o G-Earth.
2. Instale/import o pacote da extensao.
3. Inicie a extensao `redxabbo`.

---

## Como usar
1. Abra o G-Earth.
2. Conecte no Habbo pelo G-Earth.
3. Inicie o `redxabbo`.
4. Use as abas da interface conforme sua necessidade.

Abas principais:
- `General`: configuracoes rapidas, ferramentas e busca
- `Room`: informacoes e operacoes de quarto
- `Chat`: monitoramento de mensagens
- `Friends`, `Inventory`, `Wardrobe`, `Game data`: utilitarios extras
- `Settings`: ajustes gerais e idioma

---

## Busca de usuario (Search / Buscar)
Voce pode buscar um usuario em quartos de duas formas.

### Pela interface
1. Va em `General > Buscar`.
2. Digite o nome do usuario.
3. Clique em `Buscar`.
4. Acompanhe o `Status`.
5. Para interromper, clique em `Parar`.

Quando encontrar, a extensao volta para o quarto e recarrega o ambiente.

### Pelo chat (atalho)
- `:search --user NomeDoUsuario`
- `:search --stop`

---

## Trocar idioma
O redxabbo suporta `PT-BR` e `EN`.

Voce pode trocar idioma:
- no botao de idioma na interface principal, ou
- em `Settings` (quando disponivel na sua versao)

---

## Problemas comuns
### A extensao nao abre
- Verifique se o `.NET 8 Runtime` esta instalado.
- Reinicie G-Earth e redxabbo.

### A extensao nao conecta
- Confirme que o Habbo foi aberto pelo G-Earth.
- Verifique se o G-Earth esta ativo e conectado.

### Busca nao encontra o usuario
- O usuario pode nao estar em quarto aberto naquele momento.
- Tente novamente apos alguns segundos.
- Use o nome exatamente como aparece no Habbo.

---

## Suporte
- Repositorio:
https://github.com/turkosx/red-xabbo
- Reportar problema:
https://github.com/turkosx/red-xabbo/issues

---

## Para desenvolvedores (opcional)
Se voce for compilar localmente:

```bash
git clone https://github.com/turkosx/red-xabbo.git
cd red-xabbo
git submodule update --init --recursive
dotnet restore
dotnet run --project src/Xabbo.Avalonia
```

---

## Licenca
Projeto sob licenca MIT.
Consulte `LICENSE`.
