# WhatsBot (WASender) — Guia do Projeto

Guia de como compilar, atualizar e usar o sistema. Tudo em português, direto ao ponto.

---

## 1. Visão geral: como o sistema funciona

O WhatsBot é um aplicativo Windows (C#/.NET WinForms) que automatiza o envio de mensagens
pelo WhatsApp Web usando um navegador interno (WebView2).

```
CÓDIGO-FONTE          COMPILAÇÃO           CÓPIAS PRONTAS PARA USAR
CodeHere\WASender  →  bin\Debug\WASender.exe  →  Portables (2 pastas)
(C# .cs)                 (pasta completa)  →  App instalado (Program Files)
```

**Ponto mais importante:** as 3 versões (bin\Debug, Portables e instalado) compartilham
**os mesmos dados**, que ficam em:

```
C:\ProgramData\WaSender\
├── GeneralSettings.txt   → configurações do sistema
├── Profiles\            → sessões do WhatsApp (QR code fica salvo aqui)
├── SysFiles\
│   ├── chatTemplate.csv  → código JavaScript injetado no WhatsApp Web (a "mágica" do envio)
│   └── lidmap.json       → cache LID (número → LID do WhatsApp)
├── temp\                 → relatórios HTML das campanhas
└── ProcessLogger\        → logs de diagnóstico (só gravam quando a janela da campanha fecha)
```

Ou seja: **não importa por onde você abre o app** — a conta, a sessão do WhatsApp
(sem precisar escanear QR de novo), as campanhas e configurações são sempre as mesmas.

---

## 2. Os arquivos .bat (o que cada um faz)

### Dentro do projeto (`E:\01 - Negocios\WaSender\WhatsBot\`)

#### `COMPILAR-E-ATUALIZAR-PORTABLE.bat` — COMPILAR (uso diário)

**Para que serve:** gera uma versão nova do executável a partir do código-fonte e
atualiza **o Portable do próprio projeto** (`AppHere\Portable`).

**O que ele faz, em ordem:**
1. Verifica se o app está fechado (o arquivo `.exe` fica travado enquanto o app roda)
2. Compila o projeto (Rebuild completo, demora 1–2 min)
3. Copia os arquivos novos para `AppHere\Portable\` (exe, dll, csv, languages)
4. Atualiza o `chatTemplate.csv` que roda em `C:\ProgramData\WaSender\SysFiles\`

**Como usar:** clique duplo. Não precisa de administrador.

**Depois de rodar:** abra o app pelo `AppHere\Portable\WASender.exe` do projeto.

#### `ATUALIZAR-APP-como-administrador.bat` — ATUALIZAR O APP INSTALADO (ocasional)

**Para que serve:** copia a versão compilada para o app instalado
(`C:\Program Files (x86)\DigitPrime\WhatsBotSetUp\`) — aquele que tem atalho no Menu Iniciar.

**Por que precisa de administrador:** a pasta `Program Files` é protegida pelo Windows.
Só um administrador pode escrever nela.

**O que copia:** `WASender.exe`, `ProjectCommon.dll`, `chatTemplate.csv` e a pasta `languages\`
(traduções). Salva backups `.bak` dos arquivos antigos antes.

**Como usar:** clique DIREITO → **"Executar como administrador"** → aceitar o UAC (Sim).

**Você não precisa dele se usar só os Portables.** Como os dados são compartilhados,
o app instalado é só mais um lugar para abrir o mesmo sistema.

### Fora do projeto (`E:\01 - Negocios\WaSender\`)

#### `ATUALIZAR-PORTABLE-EXTERNO.bat` — sincroniza o Portable da pasta de cima

**Para que serve:** copia o build recente do projeto para o Portable externo
(`E:\01 - Negocios\WaSender\AppHere\Portable`). Este .bat **não compila** — ele usa o
build que já existe (gerado pelo .bat de compilar do projeto).

**Importante ao mover o projeto:** a primeira linha configurável dele é
`set "PROJETO=E:\01 - Negocios\WaSender\WhatsBot"`. Se você mover a pasta do projeto,
ajuste esse caminho dentro do .bat externo.

---

## 2.1 Projeto autocontido (pronto para mover / Git)

Tudo o que o projeto `WhatsBot\` precisa está **dentro dele**:

- `CodeHere\` — código-fonte (solução Visual Studio)
- `AppHere\Portable\` — cópia executável do projeto
- Os 2 .bat internos usam **caminhos relativos** à própria pasta — continuam
  funcionando se você mover `WhatsBot\` para qualquer lugar (ou clonar do Git)

**Ao mover para outra pasta / repositório Git, ajuste apenas:**
1. O .bat externo (`ATUALIZAR-PORTABLE-EXTERNO.bat`, na pasta de cima) — variável `PROJETO`
2. A variável `MSBUILD` nos .bats, se o Visual Studio estiver em outro caminho

**Não fazem parte do projeto** (ficam fora dele):
- `E:\01 - Negocios\WaSender\AppHere\Portable\` — cópia externa (atualizada pelo .bat externo)
- `C:\Program Files (x86)\DigitPrime\WhatsBotSetUp\` — app instalado (atualizado pelo .bat de admin)
- `C:\ProgramData\WaSender\` — dados de runtime (conta, sessão, campanhas) — **nunca versionar no Git**

---

## 3. Como compilar (as 3 formas)

### Forma A — .bat com um clique (recomendada)

Clique duplo em `COMPILAR-E-ATUALIZAR-PORTABLE.bat`. Pronto.

### Forma B — Visual Studio

1. Abra `E:\01 - Negocios\WaSender\WhatsBot\CodeHere\WASender.sln` no Visual Studio 2022
2. Menu **Compilar → Recompilar Solução** (ou `Ctrl+Shift+B`)
3. O executável sai em `CodeHere\WASender\bin\Debug\WASender.exe`
4. Para usar: rode o .bat (Forma A) **ou** copie manualmente os arquivos
   (`WASender.exe`, `ProjectCommon.dll`, `chatTemplate.csv`, pasta `languages\`)
   para um dos Portables

### Forma C — Linha de comando (é isso que a IA usa)

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
  "E:\01 - Negocios\WaSender\WhatsBot\CodeHere\WASender\WASender.csproj" ^
  -t:Rebuild -p:Configuration=Debug -verbosity:minimal -nologo
```

O resultado é o mesmo das outras formas.

---

## 4. Dúvidas comuns e problemas

### "Compilei, mas o app não mudou nada"
O build incremental do Visual Studio às vezes falha em detectar mudanças. Solução:
use **Recompilar** (Rebuild) em vez de Compilar, ou apague a pasta
`CodeHere\WASender\obj\Debug` e compile de novo. O .bat já usa Rebuild automático.

### "Deu erro ao copiar o arquivo"
O app está aberto (o Windows trava o `.exe` em uso). Feche o WASender e rode o .bat de novo.

### "O app instalado está diferente do Portable"
O app instalado só atualiza com o `ATUALIZAR-APP-como-administrador.bat`. Se estiver
desatualizado, use os Portables ou rode o .bat como administrador.

### "Perdi a sessão / pediu QR de novo?"
A sessão fica em `C:\ProgramData\WaSender\Profiles\`. Ela é perdida se: o WhatsApp
desvincular o aparelho, você limpar o cache/perfil, ou o número ficar muito tempo sem
conexão. Basta escanear o QR de novo — os dados de campanha e o cache LID continuam salvos.

### "Apaguei/editei algo e o envio parou de funcionar"
Os arquivos críticos do envio são: `WASender.exe`, `ProjectCommon.dll` e o
`chatTemplate.csv`. Se qualquer um deles for substituído por uma versão antiga
(por exemplo, rodando o instalador original do fornecedor), o envio quebra.
NUNCA rode o instalador original — ele reverte tudo para a versão antiga do fornecedor.

---

## 5. Variáveis de mensagem (tags)

| Tag | O que faz |
|---|---|
| `[NAME]` ou `{{name}}` | Nome do contato (coluna "Name" da planilha) |
| `[SPOILER]` | Botão "Ler mais" na mensagem |
| `{{NomeDaColuna}}` | Valor da coluna da planilha para aquele contato (ex.: `{{saudacao}}`) |
| `{{sys.date}}` | Data e hora do envio |
| `{{ RANDOM }}` | Número aleatório (10000–50000) |
| `{{ KEY :a\|b\|c }}` | Sorteia uma das opções em cada envio |

O nome da tag `{{ }}` deve ser **idêntico ao título da coluna da planilha**
(maiúsculas/minúsculas não importam, mas erros de digitação sim — "saudacao" ≠ "suadacao").
Só funcionam as colunas a partir da 3ª da planilha; a 2ª coluna (nome) é usada com `[NAME]`.
No app, o botão de variáveis (na tela de mensagem) lista todas e tem explicação com exemplos.

---

## 6. Resumo rápido (cola)

- **Alterei o código / pedi correção à IA** → clique duplo em `COMPILAR-E-ATUALIZAR-PORTABLE.bat` (só o Portable do projeto + ProgramData)
- **Quero usar o app** → abra `AppHere\Portable\WASender.exe` (dentro do projeto)
- **Quero atualizar o Portable da pasta de cima** → rode o `ATUALIZAR-PORTABLE-EXTERNO.bat` (na pasta de cima, depois de compilar)
- **Quero atualizar o app instalado (Menu Iniciar)** → clique direito em `ATUALIZAR-APP-como-administrador.bat` → Executar como administrador
- **Dados/sessão/campanhas** → sempre em `C:\ProgramData\WaSender` (compartilhado entre todas as versões)
- **Vou mover o projeto para outra pasta / Git** → pode mover a pasta `WhatsBot\` inteira; os .bat internos são relativos. Só ajuste o `PROJETO=` no .bat externo e o `MSBUILD=` se necessário
- **NUNCA rode o instalador original** → ele reverte as correções
