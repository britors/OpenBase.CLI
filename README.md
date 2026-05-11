# OpenBase CLI

A interface de linha de comando oficial para o ecossistema **OpenBase**.

---

## Instalação

Distribuída como ferramenta global do .NET:

```bash
dotnet tool install -g w3ti.OpenBase.Cli
```

Para atualizar:

```bash
dotnet tool update -g w3ti.OpenBase.Cli
```

---

## Como usar

### 1. Instalar os templates

```bash
openbase install
```

### 2. Criar um novo projeto

```bash
# SQL Server
openbase new --type api --template sqlserver --name MeuProjeto

# PostgreSQL
openbase new --type api --template pgsql --name MeuProjeto
```

O assistente irá solicitar as informações de configuração do projeto:

```
Configuração do projeto

Licença do MediatR (deixe em branco se não tiver): <sua-licença>
Licença do AutoMapper (deixe em branco se não tiver): <sua-licença>
Servidor do banco de dados [.]: .
Usuário do banco de dados:
Senha do banco de dados:
```

As informações são gravadas automaticamente nos arquivos `appsettings.json` e `appsettings.Development.json` do projeto gerado.

### 3. Gerar scaffold de uma entidade

Dentro da raiz do projeto criado:

```bash
openbase scaffold --entity Produto
```

O comando detecta automaticamente se o projeto usa **SQL Server** ou **PostgreSQL** e abre um assistente interativo para definir as propriedades da entidade:

```
Propriedades da entidade
Banco: SqlServer | Tipos disponíveis: int, long, short, string, bool, decimal, ...

Prop 1 — Nome (PascalCase): Nome
  Tipo: string
  Not null (obrigatório)? [s/n] (s): s
  + Nome (string)

Adicionar outra propriedade? [s/n] (n): s

Prop 2 — Nome (PascalCase): Preco
  Tipo: decimal
  Not null (obrigatório)? [s/n] (s): s
  + Preco (decimal)

Adicionar outra propriedade? [s/n] (n): n

┌────────────┬─────────┬─────┬──────────┐
│ Propriedade│ Tipo    │ PK  │ Not Null │
├────────────┼─────────┼─────┼──────────┤
│ Id         │ int     │ Sim │ Sim      │
│ Nome       │ string  │ Não │ Sim      │
│ Preco      │ decimal │ Não │ Sim      │
└────────────┴─────────┴─────┴──────────┘
```

Ao final, são gerados **47 arquivos** cobrindo todas as camadas da Clean Architecture e o `DbSet` da entidade é **inserido automaticamente** no `OneBaseDataBaseContext`:

| Camada         | O que é gerado                                               |
|----------------|--------------------------------------------------------------|
| Domain         | Entity, IRepository, IDomainService, DomainService           |
| Application    | DTOs, Commands/Queries, Handlers, Validators, Mapper, Service|
| Infrastructure | EF Core Configuration, Repository                            |
| Presentation   | Controller com endpoints CRUD completos                       |
| Tests          | Testes unitários para handlers, validators e services        |

#### Tipos de propriedades disponíveis

| Tipo            | SQL Server | PostgreSQL |
|-----------------|:----------:|:----------:|
| `int`           | ✓          | ✓          |
| `long`          | ✓          | ✓          |
| `short`         | ✓          | ✓          |
| `string`        | ✓          | ✓          |
| `bool`          | ✓          | ✓          |
| `decimal`       | ✓          | ✓          |
| `float`         | ✓          | ✓          |
| `double`        | ✓          | ✓          |
| `DateTime`      | ✓          | ✓          |
| `DateOnly`      | ✓          | ✓          |
| `TimeOnly`      | ✓          | ✓          |
| `DateTimeOffset`| ✓          | ✓          |
| `Guid`          | ✓          | ✓          |
| `byte[]`        | ✓          | ✓          |
| `JsonDocument`  |            | ✓          |

#### Regras geradas automaticamente nos Validators

- `string` required → `NotEmpty().MinimumLength(1).MaximumLength(255)`
- `Guid` required → `NotEmpty()`
- Campos string no Update → regra com `.When(x => !string.IsNullOrWhiteSpace(x.Prop))`

#### Próximos passos após o scaffold

O `DbSet` é injetado automaticamente no `OneBaseDataBaseContext.cs`. Basta executar as migrations:

```bash
dotnet ef migrations add AddProduto
dotnet ef database update
```

---

## Comandos disponíveis

| Comando                  | Descrição                                              | Exemplo                                                        |
|--------------------------|--------------------------------------------------------|----------------------------------------------------------------|
| `install`                | Instala os templates NuGet necessários                 | `openbase install`                                             |
| `new`                    | Cria um novo projeto a partir dos templates            | `openbase new --type api --template sqlserver --name X`        |
| `scaffold`               | Gera todas as camadas para uma entidade (interativo)   | `openbase scaffold --entity Produto`                           |
| `update`                 | Atualiza a CLI e os templates para a última versão     | `openbase update`                                              |
| `history`                | Exibe o histórico de atualizações por componente       | `openbase history --type cli`                                  |
| `version show`           | Exibe as versões da CLI e do template instalados       | `openbase version show`                                        |
| `version restore`        | Restaura um componente para uma versão específica      | `openbase version restore 10.5.9 --type cli`                   |
| `help`                   | Guia completo de argumentos e flags                    | `openbase help`                                                |

### Histórico de atualizações

```bash
# Exibir histórico completo
openbase history

# Filtrar por componente
openbase history --type cli
openbase history --type sqlserver
openbase history --type postgres
```

### Restaurar versão

Restaura um componente para uma versão específica. Útil para reverter uma atualização problemática.

```bash
# Restaurar a CLI para uma versão anterior
openbase version restore 10.5.9 --type cli

# Restaurar um template
openbase version restore 2.0.0 --type sqlserver
openbase version restore 1.5.3 --type postgres
```

O argumento `--type` é obrigatório e aceita:

| Valor       | Componente                              |
|-------------|----------------------------------------|
| `cli`       | OpenBase CLI (`w3ti.OpenBase.CLI`)     |
| `sqlserver` | Template SQL Server                    |
| `postgres`  | Template PostgreSQL                    |

---

## Requisitos

- .NET SDK 10 ou superior

---

## Segurança e compatibilidade

- **Multiplataforma**: Windows, macOS (Intel/Apple Silicon) e Linux
- **Segurança**: Execução de processos protegida contra injeção de comandos (S4036 compliance)
- Monitorado pelo **SonarCloud**

---

## Licença

Distribuído sob a licença MIT. Veja `LICENSE.txt` para mais informações.

Desenvolvido por Rodrigo Brito <rodrigo@w3ti.com.br>.
