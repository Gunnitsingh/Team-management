# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskWorkerService/*.csproj TaskWorkerService/
COPY Shared/*.csproj Shared/
COPY team-management-api/*.csproj team-management-api/
RUN dotnet restore TaskWorkerService/TaskWorkerService.csproj

COPY . .
WORKDIR /src/TaskWorkerService
RUN dotnet publish -c Release -o /app /p:ErrorOnDuplicatePublishOutputFiles=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "TaskWorkerService.dll"]
