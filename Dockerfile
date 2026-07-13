FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY KafeYana.Api/KafeYana.Api/KafeYana.Api.csproj KafeYana.Api/KafeYana.Api/
COPY KafeYana.Api/KafeYana.Core/KafeYana.Domain.csproj KafeYana.Api/KafeYana.Core/
COPY KafeYana.Api/KafeYana.Domain/KafeYana.Application.csproj KafeYana.Api/KafeYana.Domain/
COPY KafeYana.Api/KafeYana.Infrastructure/KafeYana.Infrastructure.csproj KafeYana.Api/KafeYana.Infrastructure/

RUN dotnet restore KafeYana.Api/KafeYana.Api/KafeYana.Api.csproj

COPY KafeYana.Api/ KafeYana.Api/

RUN dotnet publish KafeYana.Api/KafeYana.Api/KafeYana.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:$PORT dotnet KafeYana.Api.dll"]
