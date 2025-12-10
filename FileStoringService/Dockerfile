FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileStoringService/FileStoringService.csproj", "FileStoringService/"]
COPY ["Shared.Models/Shared.Models.csproj", "Shared.Models/"]
RUN dotnet restore "FileStoringService/FileStoringService.csproj"
COPY . .
WORKDIR "/src/FileStoringService"
RUN dotnet build "FileStoringService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStoringService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStoringService.dll"]

