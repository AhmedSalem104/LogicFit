# Build the tenant API. Run from the solution root:
#   docker build -f LogicFit.API/Dockerfile -t logicfit-api .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LogicFit.sln ./
COPY LogicFit.Domain/LogicFit.Domain.csproj LogicFit.Domain/
COPY LogicFit.Application/LogicFit.Application.csproj LogicFit.Application/
COPY LogicFit.Infrastructure/LogicFit.Infrastructure.csproj LogicFit.Infrastructure/
COPY LogicFit.API/LogicFit.API.csproj LogicFit.API/
RUN dotnet restore LogicFit.API/LogicFit.API.csproj

COPY . .
RUN dotnet publish LogicFit.API/LogicFit.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LogicFit.API.dll"]
