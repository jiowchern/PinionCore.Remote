#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PinionCore.Consoles.Chat1.Server/PinionCore.Consoles.Chat1.Server.csproj", "PinionCore.Consoles.Chat1.Server/"]
COPY ["PinionCore.Consoles.Chat1.Common/PinionCore.Consoles.Chat1.Common.csproj", "PinionCore.Consoles.Chat1.Common/"]
COPY ["PinionCore.Consoles.Chat1/PinionCore.Consoles.Chat1.csproj", "PinionCore.Consoles.Chat1/"]
RUN dotnet restore "PinionCore.Consoles.Chat1.Server/PinionCore.Consoles.Chat1.Server.csproj"
COPY . .
WORKDIR "/src/PinionCore.Consoles.Chat1.Server"
RUN dotnet build "PinionCore.Consoles.Chat1.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PinionCore.Consoles.Chat1.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 53771
ENTRYPOINT ["dotnet", "PinionCore.Consoles.Chat1.Server.dll", "--webport", "53771", "--tcpport", "53772"]
