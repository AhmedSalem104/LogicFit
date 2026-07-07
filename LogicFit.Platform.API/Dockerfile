# Build the platform API. Run from the solution root:
#   docker build -f LogicFit.Platform.API/Dockerfile -t logicfit-platform-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LogicFit.sln ./
COPY LogicFit.Domain/LogicFit.Domain.csproj LogicFit.Domain/
COPY LogicFit.Application/LogicFit.Application.csproj LogicFit.Application/
COPY LogicFit.Infrastructure/LogicFit.Infrastructure.csproj LogicFit.Infrastructure/
COPY LogicFit.Platform.API/LogicFit.Platform.API.csproj LogicFit.Platform.API/
RUN dotnet restore LogicFit.Platform.API/LogicFit.Platform.API.csproj

COPY . .
RUN dotnet publish LogicFit.Platform.API/LogicFit.Platform.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LogicFit.Platform.API.dll"]
