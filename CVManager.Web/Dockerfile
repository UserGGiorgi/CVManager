FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CVManager.Web/CVManager.Web.csproj", "CVManager.Web/"]
COPY ["CVManager.Core/CVManager.Core.csproj", "CVManager.Core/"]
COPY ["CVManager.Data/CVManager.Data.csproj", "CVManager.Data/"]
RUN dotnet restore "CVManager.Web/CVManager.Web.csproj"
COPY . .
WORKDIR "/src/CVManager.Web"
RUN dotnet build "CVManager.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CVManager.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CVManager.Web.dll"]