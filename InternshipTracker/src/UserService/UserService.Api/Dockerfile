FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/UserService/", "src/UserService/"]
RUN dotnet restore "src/UserService/UserService.Api/UserService.Api.csproj"
RUN dotnet publish "./UserService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.Api.dll"]
