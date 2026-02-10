# 建置階段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製 csproj 並還原套件（利用 Docker 快取層）
COPY src/AiImConnector/AiImConnector.csproj src/AiImConnector/
RUN dotnet restore src/AiImConnector/AiImConnector.csproj

# 複製所有原始碼並建置
COPY src/ src/
RUN dotnet publish src/AiImConnector/AiImConnector.csproj -c Release -o /app/publish --no-restore

# 執行階段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 設定非 root 使用者（安全性）
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

# 設定環境變數
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "AiImConnector.dll"]
