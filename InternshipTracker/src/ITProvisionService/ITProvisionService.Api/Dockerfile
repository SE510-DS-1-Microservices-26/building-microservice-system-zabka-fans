FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ITProvisionService/ITProvisionService.Api/ITProvisionService.Api.csproj", "src/ITProvisionService/ITProvisionService.Api/"]
COPY ["src/ITProvisionService/ITProvisionService.Infrastructure/ITProvisionService.Infrastructure.csproj", "src/ITProvisionService/ITProvisionService.Infrastructure/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
RUN dotnet restore "src/ITProvisionService/ITProvisionService.Api/ITProvisionService.Api.csproj"
COPY . .
RUN dotnet publish "src/ITProvisionService/ITProvisionService.Api/ITProvisionService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ITProvisionService.Api.dll"]

