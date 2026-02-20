FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY monitoring-server ./monitoring-server
RUN dotnet restore "monitoring-server/src/WinBus.Monitoring.Api/WinBus.Monitoring.Api.csproj"
RUN dotnet publish "monitoring-server/src/WinBus.Monitoring.Api/WinBus.Monitoring.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WinBus.Monitoring.Api.dll"]
