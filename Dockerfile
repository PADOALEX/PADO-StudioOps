FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/PADO.StudioOps.Api/PADO.StudioOps.Api.csproj src/PADO.StudioOps.Api/
RUN dotnet restore src/PADO.StudioOps.Api/PADO.StudioOps.Api.csproj
COPY src/PADO.StudioOps.Api/ src/PADO.StudioOps.Api/
RUN dotnet publish src/PADO.StudioOps.Api/PADO.StudioOps.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PADO.StudioOps.Api.dll"]
