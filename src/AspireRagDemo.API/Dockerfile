FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AspireRagDemo.API/AspireRagDemo.API.csproj", "AspireRagDemo.API/"]
RUN dotnet restore "AspireRagDemo.API/AspireRagDemo.API.csproj"
COPY . .
WORKDIR "/src/AspireRagDemo.API"
RUN dotnet build "AspireRagDemo.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AspireRagDemo.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AspireRagDemo.API.dll"]
