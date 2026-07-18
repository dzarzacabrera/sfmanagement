FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Install Node.js for Tailwind CSS build (wwwroot/css/tailwind.css is gitignored)
RUN apt-get update && apt-get install -y --no-install-recommends nodejs npm && rm -rf /var/lib/apt/lists/*

COPY . .

# Build Tailwind CSS (package.json postinstall runs build:css)
RUN cd src/SFManagement.Web && npm install

RUN dotnet restore SFManagement.sln

RUN dotnet publish src/SFManagement.Web/SFManagement.Web.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SFManagement.Web.dll"]
