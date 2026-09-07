FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Focusberry.Api/Focusberry.Api.csproj Focusberry.Api/
RUN dotnet restore Focusberry.Api/Focusberry.Api.csproj
COPY . .
RUN dotnet publish Focusberry.Api/Focusberry.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Focusberry.Api.dll"]
