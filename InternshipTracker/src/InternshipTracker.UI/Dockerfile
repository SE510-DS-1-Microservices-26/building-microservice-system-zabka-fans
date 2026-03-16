FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InternshipTracker.UI/InternshipTracker.UI.csproj", "src/InternshipTracker.UI/"]
COPY ["src/InternshipTracker.Application/InternshipTracker.Application.csproj", "src/InternshipTracker.Application/"]
COPY ["src/InternshipTracker.Domain/InternshipTracker.Domain.csproj", "src/InternshipTracker.Domain/"]
RUN dotnet restore "src/InternshipTracker.UI/InternshipTracker.UI.csproj"
COPY . .
WORKDIR "/src/src/InternshipTracker.UI"
RUN dotnet build "./InternshipTracker.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InternshipTracker.UI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InternshipTracker.UI.dll"]
