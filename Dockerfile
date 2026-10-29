# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura só com os arquivos de projeto: esta camada fica em cache enquanto as dependências não mudarem
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/CartaoCredito.Api/CartaoCredito.Api.csproj src/CartaoCredito.Api/
COPY src/CartaoCredito.Application/CartaoCredito.Application.csproj src/CartaoCredito.Application/
COPY src/CartaoCredito.Domain/CartaoCredito.Domain.csproj src/CartaoCredito.Domain/
COPY src/CartaoCredito.Infrastructure/CartaoCredito.Infrastructure.csproj src/CartaoCredito.Infrastructure/
RUN dotnet restore src/CartaoCredito.Api/CartaoCredito.Api.csproj

COPY src/ src/
RUN dotnet publish src/CartaoCredito.Api/CartaoCredito.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Usuário sem privilégios já existente na imagem oficial
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "CartaoCredito.Api.dll"]
