# OpenBase SQL Server CLI 🚀

[![NuGet Version](https://img.shields.io/nuget/v/w3ti.OpenBaseSqlServer.Cli.svg)](https://www.nuget.org/packages/w3ti.OpenBaseSqlServer.Cli/)

A interface de linha de comando oficial para o ecossistema **OpenBase**. Projetada para acelerar a criação de projetos baseados em **Clean Architecture**, **.NET 10** e **SQL Server**.

---

## 🛠️ Instalação

A OpenBase CLI é distribuída como uma ferramenta global do .NET. Para instalar, execute:

```bash
dotnet tool install -g w3ti.OpenBaseSqlServer.Cli

🚀 Como usar
1. Preparar o ambiente

Instale os templates oficiais de arquitetura necessários para a CLI:
Bash

openbase install

2. Criar um novo projeto

Gere uma solução completa com API, Infraestrutura e suporte a SQL Server:
Bash

openbase new --type api --template sqlserver --name MeuProjeto

3. Verificar o ambiente

Consulte as informações do Sistema Operacional e as versões do .NET e Angular instaladas:
Bash

openbase version

📋 Comandos Disponíveis
Comando Descrição Exemplo
install Instala ou atualiza os templates NuGet necessários.openbase install
new Cria um novo projeto a partir dos templates.openbase new --name X
update Sincroniza a CLI e os templates com a última versão.openbase update
version Exibe o SO, Arquitetura e versões do ecossistema.openbase version
help Guia completo de argumentos e flags.openbase help
💻 Requisitos

    SDK .NET 10 ou superior.

    Node.js & Angular CLI (Opcional, para projetos com Front-end).

    SQL Server (Local ou via Docker).

🛡️ Segurança e Compatibilidade

Esta ferramenta foi desenvolvida com foco em segurança e é monitorada pelo SonarCloud.

    Multiplataforma: Suporte nativo para Windows, macOS (Intel/Apple Silicon) e Linux (Fedora/Ubuntu).

    Resiliência: Detecta automaticamente instalações globais e gerenciadas via NVM (Node Version Manager).

    Segurança: Execução de processos protegida contra injeção de comandos (S4036 compliance).

📄 Licença

Distribuído sob a licença MIT. Veja LICENSE.txt para mais informações.

Desenvolvido com ❤️ por w3ti.
