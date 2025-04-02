

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy code into container, built, test, publish


COPY . .
#ARG PROJECT_NAME
#ENV PROJECT_NAME ${PROJECT_NAME}
RUN dotnet restore
RUN dotnet build -c release
RUN dotnet test -v normal --filter FullyQualifiedName!~Signify.HBA1CPOC.System.Tests
RUN dotnet publish Signify.HBA1CPOC.Svc/Signify.HBA1CPOC.Svc.csproj -c Release -o /app -r linux-x64 --self-contained false

USER app

# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:8.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Signify.HBA1CPOC.Svc.dll"]
