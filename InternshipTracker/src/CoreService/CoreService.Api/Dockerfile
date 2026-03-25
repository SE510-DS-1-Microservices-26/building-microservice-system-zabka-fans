FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CoreService/", "src/CoreService/"]
RUN dotnet restore "src/CoreService/CoreService.Api/CoreService.Api.csproj"
RUN dotnet publish "./CoreService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CoreService.Api.dll"]