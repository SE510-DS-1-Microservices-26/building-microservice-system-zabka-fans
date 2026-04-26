FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/NotificationService/NotificationService.Api/NotificationService.Api.csproj", "src/NotificationService/NotificationService.Api/"]
COPY ["src/NotificationService/NotificationService.Application/NotificationService.Application.csproj", "src/NotificationService/NotificationService.Application/"]
COPY ["src/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj", "src/NotificationService/NotificationService.Infrastructure/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
RUN dotnet restore "src/NotificationService/NotificationService.Api/NotificationService.Api.csproj"
COPY . .
RUN dotnet publish "src/NotificationService/NotificationService.Api/NotificationService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]

