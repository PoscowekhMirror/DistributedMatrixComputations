FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Benchmarks/Benchmarks.csproj", "Benchmarks/"]
RUN dotnet restore "Benchmarks/Benchmarks.csproj"
COPY . .
WORKDIR "/src/Benchmarks"
RUN dotnet build "Benchmarks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Benchmarks.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Benchmarks.dll"]
