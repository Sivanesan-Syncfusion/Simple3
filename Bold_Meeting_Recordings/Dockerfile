#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Bold_Meeting_Recordings/Bold_Meeting_Recordings.csproj", "Bold_Meeting_Recordings/"]
RUN dotnet restore "Bold_Meeting_Recordings/Bold_Meeting_Recordings.csproj"
COPY . .
WORKDIR "/src/Bold_Meeting_Recordings"
RUN dotnet build "Bold_Meeting_Recordings.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bold_Meeting_Recordings.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bold_Meeting_Recordings.dll"]