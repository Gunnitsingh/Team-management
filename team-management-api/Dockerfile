# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY team-management-api/*.csproj team-management-api/
COPY Shared/*.csproj Shared/
RUN dotnet restore team-management-api/team-management-api.csproj

COPY . .
WORKDIR /src/team-management-api
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .


EXPOSE 80
ENTRYPOINT ["dotnet", "team-management-api.dll"]
