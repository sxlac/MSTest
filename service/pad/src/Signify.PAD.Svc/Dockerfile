FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy code into container, built, test, publish

COPY . .
RUN dotnet restore
RUN dotnet build -c release
RUN dotnet test -v normal --filter FullyQualifiedName!~Signify.PAD.Svc.System.Tests
RUN dotnet publish Signify.PAD.Svc/Signify.PAD.Svc.csproj -c release -o /app -r linux-x64 --self-contained false

USER app

# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Signify.PAD.Svc.dll"]
