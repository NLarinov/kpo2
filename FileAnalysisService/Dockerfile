FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileAnalysisService/FileAnalysisService.csproj", "FileAnalysisService/"]
COPY ["Shared.Models/Shared.Models.csproj", "Shared.Models/"]
RUN dotnet restore "FileAnalysisService/FileAnalysisService.csproj"
COPY . .
WORKDIR "/src/FileAnalysisService"
RUN dotnet build "FileAnalysisService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileAnalysisService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileAnalysisService.dll"]

