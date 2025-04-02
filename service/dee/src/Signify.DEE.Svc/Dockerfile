FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY *.sln .
COPY */*.csproj ./csproj-files/
# RUN dotnet restore

# copy everything else and build app
COPY . .
WORKDIR /src/
#ARG PROJECT_NAME
#ENV PROJECT_NAME ${PROJECT_NAME}
RUN dotnet restore 
RUN dotnet build -c release
RUN dotnet test -v normal --filter FullyQualifiedName!~Signify.DEE.Svc.System.Tests
RUN dotnet publish Signify.DEE.Svc/Signify.DEE.Svc.csproj -c release -o /app -r linux-x64 --self-contained false

USER app
# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Signify.DEE.Svc.dll"]