FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/SFManagement.Domain/SFManagement.Domain.csproj src/SFManagement.Domain/
COPY src/SFManagement.Application/SFManagement.Application.csproj src/SFManagement.Application/
COPY src/SFManagement.Infrastructure/SFManagement.Infrastructure.csproj src/SFManagement.Infrastructure/
COPY src/SFManagement.Web/SFManagement.Web.csproj src/SFManagement.Web/
COPY SFManagement.sln .
RUN dotnet restore src/SFManagement.Web/SFManagement.Web.csproj

COPY . .
RUN dotnet publish src/SFManagement.Web/SFManagement.Web.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SFManagement.Web.dll"]
