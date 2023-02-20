#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BaseImage=mcr.microsoft.com/dotnet/aspnet:6.0-cbl-mariner2.0
ARG BuildImage=mcr.microsoft.com/dotnet/sdk:6.0-cbl-mariner2.0

FROM ${BaseImage} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM ${BuildImage} AS build
WORKDIR /src
COPY ["WeatherService.csproj", ""]
RUN dotnet restore "./WeatherService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "WeatherService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WeatherService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherService.dll"]