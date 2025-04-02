FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy code into container, built, test, publish
COPY . .
RUN dotnet restore
RUN dotnet build -c release
RUN dotnet test -v normal --filter FullyQualifiedName!~Signify.FOBT.Svc.System.Tests
RUN dotnet publish Signify.FOBT.Svc/Signify.FOBT.Svc.csproj -c release -o /app -r linux-x64 --self-contained false


# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:8.0
WORKDIR /app
COPY --from=build /app ./

USER app

ENTRYPOINT ["dotnet", "Signify.FOBT.Svc.dll"]