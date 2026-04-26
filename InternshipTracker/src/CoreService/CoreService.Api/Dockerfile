FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CoreService/CoreService.Api/CoreService.Api.csproj", "src/CoreService/CoreService.Api/"]
COPY ["src/CoreService/CoreService.Application/CoreService.Application.csproj", "src/CoreService/CoreService.Application/"]
COPY ["src/CoreService/CoreService.Domain/CoreService.Domain.csproj", "src/CoreService/CoreService.Domain/"]
COPY ["src/CoreService/CoreService.Infrastructure/CoreService.Infrastructure.csproj","src/CoreService/CoreService.Infrastructure/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
RUN dotnet restore "src/CoreService/CoreService.Api/CoreService.Api.csproj"
COPY . .
RUN dotnet publish "src/CoreService/CoreService.Api/CoreService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreService.Api.dll"]