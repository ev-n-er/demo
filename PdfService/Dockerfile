FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN mkdir -p Upload && chown $APP_UID Upload
USER $APP_UID
EXPOSE 8080
EXPOSE 8081


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PdfService/PdfService.csproj", "PdfService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "./PdfService/PdfService.csproj"
COPY . .
WORKDIR "/src/PdfService"
RUN dotnet build "./PdfService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PdfService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PdfService.dll"]