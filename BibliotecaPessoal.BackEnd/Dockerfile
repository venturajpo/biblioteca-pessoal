FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BibliotecaPessoal.BackEnd/BibliotecaPessoal.BackEnd.csproj", "BibliotecaPessoal.BackEnd/"]
RUN dotnet restore "BibliotecaPessoal.BackEnd/BibliotecaPessoal.BackEnd.csproj"
COPY . .
WORKDIR "/src/BibliotecaPessoal.BackEnd"
RUN dotnet build "./BibliotecaPessoal.BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BibliotecaPessoal.BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BibliotecaPessoal.BackEnd.dll"]
