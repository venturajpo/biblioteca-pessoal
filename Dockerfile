# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# API — ASP.NET Core 10 (build em múltiplos estágios)
# Contexto de build: raiz do repositório.
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /origem

# Os .csproj são copiados primeiro para que o restore fique em cache
# e só seja refeito quando alguma dependência mudar.
COPY BibliotecaPessoal.sln ./
COPY src/BibliotecaPessoal.Domain/*.csproj         src/BibliotecaPessoal.Domain/
COPY src/BibliotecaPessoal.Application/*.csproj    src/BibliotecaPessoal.Application/
COPY src/BibliotecaPessoal.Infrastructure/*.csproj src/BibliotecaPessoal.Infrastructure/
COPY src/BibliotecaPessoal.Api/*.csproj            src/BibliotecaPessoal.Api/
COPY tests/BibliotecaPessoal.UnitTests/*.csproj    tests/BibliotecaPessoal.UnitTests/
RUN dotnet restore BibliotecaPessoal.sln

COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/BibliotecaPessoal.Api/BibliotecaPessoal.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /publicacao

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /publicacao ./

# Usuário sem privilégios já definido nas imagens oficiais do .NET.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BibliotecaPessoal.Api.dll"]
