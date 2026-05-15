FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "src/Place/Api/FoodSocial.Place.Api.csproj"
RUN dotnet publish "src/Place/Api/FoodSocial.Place.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FoodSocial.Place.Api.dll"]
