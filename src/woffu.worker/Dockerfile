#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/woffu.worker/woffu.worker.csproj", "src/woffu.worker/"]
RUN dotnet restore "src/woffu.worker/woffu.worker.csproj"
COPY . .
WORKDIR "/src/src/woffu.worker"
RUN dotnet build "woffu.worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "woffu.worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "woffu.worker.dll"]