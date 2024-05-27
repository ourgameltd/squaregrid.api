## Development Pre-requisites
* Install Visual Studio 2022 (dotnet 7)
* Have IAM Contributor access to the [SquareGrid Dev/Test](https://portal.azure.com/#@michaellaw.me/resource/subscriptions/a3ac85e7-ff10-4e73-b806-3ab91af8f0c4/overview) Subscription

## New Environment Pre-requisites
When setting up a new environment there are some things which need to be configured manually prior to starting by infrastructure and cloud ops.

1. App registration for UI
   1. Dev
   2. Live
2. DNS for UI
   1. Dev - TBC
   2. Live - TBC

### Manual Config
TBC

### Devops Library settings
TBC

### Database configuration

Using our local development environment we are using a LocalDB (windows) or a docker image on mac. This enables us to run migrations etc and test that works. When the API project starts up it runs the migrations for us, we do not to do anything in the pipeline to deploy the DB.

Migrations are managed by updating the POCOs in the code and the context and running the code below to identify and track the changes.

```
dotnet ef migrations add "Migrations name"
```

You can then apply them to your local DB by running the API project or calling from the command line

```
dotnet ef database update
```