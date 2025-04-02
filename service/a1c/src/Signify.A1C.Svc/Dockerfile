FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

# Copy code into container, built, test, publish

COPY . .

#ARG PROJECT_NAME
#ENV PROJECT_NAME ${PROJECT_NAME}
RUN dotnet restore
RUN dotnet build -c release
RUN dotnet test -v normal
RUN dotnet publish Signify.A1C.Svc/Signify.A1C.Svc.csproj -c release -o /app -r linux-x64 --self-contained false

# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:5.0

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Signify.A1C.Svc.dll"]