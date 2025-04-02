FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
# COPY *.sln .
# COPY */*.csproj ./csproj-files/
# RUN dotnet restore

# copy everything else and build app
COPY . .
ARG PROJECT_NAME
ENV PROJECT_NAME ${PROJECT_NAME}
RUN dotnet restore
RUN dotnet build -c release
RUN dotnet test -v normal --filter FullyQualifiedName!~Signify.eGFR.System.Tests
RUN dotnet publish ${PROJECT_NAME}/${PROJECT_NAME}.csproj -c release -o /app -r linux-x64 --self-contained false

USER app
# final stage/image
FROM signifyhealth.azurecr.io/dotnet/sh-runtime:8.0
WORKDIR /app
COPY --from=build /app ./

# The ENTRYPOINT needs to be done in exec mode, which unfortunately disallows args
# so you have to put in the actual project name.  The dotnet new template process will automatically
# replace the template name with the name you supply but unfortunately does so in lower case.
# You will need to verify that the project name in the following line is properly cased.
# Ex. If you run dotnet new {templatename} -n Signify.MyProject, the following line will look like
# ENTRYPOINT ["dotnet", "signify.myproject.dll"] 
# and you will need to manually fix the casing so it says
# ENTRYPOINT ["dotnet", "Signify.MyProject.dll"]
ENTRYPOINT ["dotnet", "Signify.eGFR.Svc.dll"]