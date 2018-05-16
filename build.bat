dotnet restore src/TempsApi
dotnet build src/TempsApi

dotnet restore tests/TempsApi.Tests
dotnet build tests/TempsApi.Tests
dotnet test tests/TempsApi.Tests
