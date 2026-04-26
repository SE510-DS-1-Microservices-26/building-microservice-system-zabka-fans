FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/UserService/UserService.Api/UserService.Api.csproj", "src/UserService/UserService.Api/"]
COPY ["src/UserService/UserService.Application/UserService.Application.csproj", "src/UserService/UserService.Application/"]
COPY ["src/UserService/UserService.Domain/UserService.Domain.csproj", "src/UserService/UserService.Domain/"]
COPY ["src/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj","src/UserService/UserService.Infrastructure/"]
RUN dotnet restore "src/UserService/UserService.Api/UserService.Api.csproj"
COPY . .
RUN dotnet publish "src/UserService/UserService.Api/UserService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.Api.dll"]