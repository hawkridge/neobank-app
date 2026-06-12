FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/NeoBank.API/NeoBank.API.csproj", "src/NeoBank.API/"]
COPY ["src/NeoBank.Application/NeoBank.Application.csproj", "src/NeoBank.Application/"]
COPY ["src/NeoBank.Domain/NeoBank.Domain.csproj", "src/NeoBank.Domain/"]
COPY ["src/NeoBank.Infrastructure/NeoBank.Infrastructure.csproj", "src/NeoBank.Infrastructure/"]
RUN dotnet restore "src/NeoBank.API/NeoBank.API.csproj"
COPY . .
RUN dotnet build "src/NeoBank.API/NeoBank.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/NeoBank.API/NeoBank.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NeoBank.API.dll"]