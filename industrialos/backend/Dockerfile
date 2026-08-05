# build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/IndustrialOS.Api/IndustrialOS.Api.csproj
RUN dotnet publish src/IndustrialOS.Api/IndustrialOS.Api.csproj -c Release -o /app

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IndustrialOS.Api.dll"]
