FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebBankAPI/WebBankAPI.csproj", "WebBankAPI/"]
COPY ["WebBankAPI.Infrastructure/WebBankAPI.Infrastructure.csproj", "WebBankAPI.Infrastructure/"]
COPY ["WebBankAPI.Domain/WebBankAPI.Domain.csproj", "WebBankAPI.Domain/"]
COPY ["WebBankAPI.Application/WebBankAPI.Application.csproj", "WebBankAPI.Application/"]
COPY ["WebBankAPI.API/WebBankAPI.API.csproj", "WebBankAPI.API/"]
RUN dotnet restore "WebBankAPI/WebBankAPI.csproj"
COPY . .
WORKDIR "/src/WebBankAPI"
RUN dotnet build "WebBankAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebBankAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebBankAPI.dll"]
