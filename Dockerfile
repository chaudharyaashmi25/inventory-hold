FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files for restore
COPY src/InventoryHold.WebApi/InventoryHold.WebApi.csproj src/InventoryHold.WebApi/
COPY src/InventoryHold.Contracts/InventoryHold.Contracts.csproj src/InventoryHold.Contracts/
COPY src/InventoryHold.Domain/InventoryHold.Domain.csproj src/InventoryHold.Domain/
COPY src/InventoryHold.Infrastructure/InventoryHold.Infrastructure.csproj src/InventoryHold.Infrastructure/
RUN dotnet restore src/InventoryHold.WebApi/InventoryHold.WebApi.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/InventoryHold.WebApi/InventoryHold.WebApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InventoryHold.WebApi.dll"]
