# SquareGrid API

This is the repo for details the backend API infra for the square grid app.

|Pipeline|Status|
|--------|------|
|Live|[![Release (Live)](https://github.com/ourgameltd/squaregrid/actions/workflows/release-live.yml/badge.svg)](https://github.com/ourgameltd/squaregrid/actions/workflows/release-live.yml)|
|PR|[![PR](https://github.com/ourgameltd/squaregrid/actions/workflows/pr.yml/badge.svg)](https://github.com/ourgameltd/squaregrid/actions/workflows/pr.yml)|
|CodeQL|[![CodeQL](https://github.com/ourgameltd/squaregrid/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/ourgameltd/squaregrid/actions/workflows/github-code-scanning/codeql)|

## Setup

* Install Visual Studio Code
* Install dotnet 8
* VS Code should prompt you to install extensions required in ./.vscode/extensions.json
* Create local.settings.json file as below in ./src/SquareGrid.Api folder
* In vscode run Cmd + shift + P and pick: Azurite: Start
* Hit play on debugger
* Pick functions process

## Settings vars

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "BlobStorageConnection": "UseDevelopmentStorage=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

```
