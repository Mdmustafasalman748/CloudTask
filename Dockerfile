# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/CloudTask.Api/CloudTask.Api.csproj src/CloudTask.Api/
COPY src/CloudTask.Domain/CloudTask.Domain.csproj src/CloudTask.Domain/
COPY src/CloudTask.Infrastructure/CloudTask.Infrastructure.csproj src/CloudTask.Infrastructure/
RUN dotnet restore src/CloudTask.Api/CloudTask.Api.csproj
COPY src/ src/
RUN dotnet publish src/CloudTask.Api/CloudTask.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloudTask.Api.dll"]
