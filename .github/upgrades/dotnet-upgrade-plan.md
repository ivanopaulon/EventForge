# .NET 10 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade EventForge.DTOs\EventForge.DTOs.csproj
4. Upgrade EventForge.Client\EventForge.Client.csproj
5. Upgrade EventForge.Server\EventForge.Server.csproj
6. Upgrade EventForge.Tests\EventForge.Tests.csproj


## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                                | Current Version | New Version | Description                                   |
|:------------------------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authorization                          |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per .NET 10.0      |
| Microsoft.AspNetCore.Authentication.JwtBearer               |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per .NET 10.0      |
| Microsoft.AspNetCore.Components.WebAssembly                 |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per Blazor WebAssembly su .NET 10.0 |
| Microsoft.AspNetCore.Components.WebAssembly.Authentication |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per Blazor auth su .NET 10.0 |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer       |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per DevServer      |
| Microsoft.AspNetCore.Mvc.Testing                            |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per test WebApp    |
| Microsoft.AspNetCore.SignalR.Client                         |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per SignalR client |
| Microsoft.AspNetCore.SignalR.Protocols.MessagePack          |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per MessagePack protocol |
| Microsoft.EntityFrameworkCore                               |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per EF Core        |
| Microsoft.EntityFrameworkCore.Design                        |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per tooling EF Core|
| Microsoft.EntityFrameworkCore.InMemory                      |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per test in-memory provider |
| Microsoft.EntityFrameworkCore.Sqlite                        |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per provider SQLite|
| Microsoft.EntityFrameworkCore.SqlServer                     |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per provider SQL Server|
| Microsoft.Extensions.Caching.Memory                         |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per caching        |
| Microsoft.Extensions.Caching.StackExchangeRedis             |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per Redis caching  |
| Microsoft.Extensions.Diagnostics.HealthChecks               |   9.0.10        |  10.0.0     | Aggiornamento raccomandato per health checks  |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore |   9.0.10 |  10.0.0     | Aggiornamento raccomandato per health checks EF Core |
| System.ComponentModel.Annotations                           |   5.0.0         |             | Funzionalità incluse nel framework target; rimuovere il pacchetto NuGet |
| System.Security.Cryptography.Pkcs                           |   9.0.10        |  10.0.0     | Aggiornamento raccomandato                     |
| System.Security.Cryptography.X509Certificates               |   4.3.2         |             | Funzionalità incluse nel framework target; rimuovere il pacchetto NuGet |


### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### EventForge.DTOs\EventForge.DTOs.csproj modifications

Project properties changes:
  - Target framework: nessuna modifica suggerita dall'analisi (probabile `netstandard2.1`), non aggiornare il TFM.

NuGet packages changes:
  - `System.ComponentModel.Annotations` (5.0.0) -> rimuovere; le funzionalità sono incluse nel framework di destinazione.

Other changes:
  - Nessun altro cambiamento automatizzato identificato.

#### EventForge.Client\EventForge.Client.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`.

NuGet packages changes:
  - Aggiornare i pacchetti elencati nella tabella NuGet alla versione `10.0.0`:
    - `Microsoft.AspNetCore.Components.WebAssembly`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Components.WebAssembly.Authentication`, `Microsoft.AspNetCore.SignalR.Client`, `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`.

Feature upgrades:
  - Verificare eventuali breaking changes per Blazor WebAssembly e aggiornare codice client-side se necessario.

Other changes:
  - Controllare i riferimenti a pacchetti ora inclusi nel framework e rimuoverli (vedi tabella).

#### EventForge.Server\EventForge.Server.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`.

NuGet packages changes:
  - Aggiornare i pacchetti elencati nella tabella NuGet alla versione `10.0.0`:
    - `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.SignalR.Client`, `System.Security.Cryptography.Pkcs`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `Microsoft.Extensions.Caching.StackExchangeRedis`.

Feature upgrades:
  - Verificare breaking changes EF Core 10 e aggiornare il codice di migration e configurazione DB se necessario.

Other changes:
  - Rimuovere i pacchetti ora inclusi nel framework (ad es. `System.Security.Cryptography.X509Certificates`).

#### EventForge.Tests\EventForge.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`.

NuGet packages changes:
  - Aggiornare i pacchetti di test alla versione `10.0.0`:
    - `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Caching.Memory`, `Microsoft.EntityFrameworkCore.InMemory`, `Microsoft.AspNetCore.Mvc.Testing`.

Other changes:
  - Verificare e aggiornare eventuali test che dipendono da API deprecate o comportamenti cambiati.

