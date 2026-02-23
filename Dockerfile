FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["StreamAxis.Api/StreamAxis.Api.csproj", "StreamAxis.Api/"]
COPY ["StreamAxis.Shared/StreamAxis.Shared.csproj", "StreamAxis.Shared/"]
RUN dotnet restore "StreamAxis.Api/StreamAxis.Api.csproj"

COPY . .
WORKDIR "/src/StreamAxis.Api"
RUN dotnet publish "StreamAxis.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StreamAxis.Api.dll"]
