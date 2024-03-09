FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Executor/Executor.csproj", "Executor/"]
RUN dotnet restore "Executor/Executor.csproj"
COPY . .
WORKDIR "/src/Executor"
RUN dotnet build "Executor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Executor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Executor.dll"]
