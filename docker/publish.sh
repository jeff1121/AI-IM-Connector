#!/bin/bash
# =============================================================================
# AI IM Connector — 建置並推送 Docker Image 至 Azure ACR
# 使用方式：./publish.sh
# =============================================================================

set -euo pipefail

# 載入 .env 環境變數
if [ -f .env ]; then
    set -a
    source .env
    set +a
else
    echo "❌ 找不到 .env 檔案，請先建立（cp .env.example .env）"
    exit 1
fi

# 驗證必要變數
: "${ACR_REGISTRY:?請在 .env 設定 ACR_REGISTRY（例如 logicalis.azurecr.io）}"
: "${IMAGE_VERSION:?請在 .env 設定 IMAGE_VERSION（例如 1.0.0）}"

IMAGE_NAME="${ACR_REGISTRY}/ai-connector/ai-im-connector"
IMAGE_TAG="${IMAGE_NAME}:${IMAGE_VERSION}"
IMAGE_LATEST="${IMAGE_NAME}:latest"

echo "============================================="
echo "📦 建置 Docker Image"
echo "   Image: ${IMAGE_TAG}"
echo "============================================="

# 建置 Image
docker compose -f docker-compose.build.yml build

echo ""
echo "============================================="
echo "🔑 登入 Azure ACR: ${ACR_REGISTRY}"
echo "============================================="

# 登入 ACR
az acr login --name "${ACR_REGISTRY%%.*}"

echo ""
echo "============================================="
echo "🚀 推送 Image 至 ACR"
echo "============================================="

# 推送版本標籤
docker push "${IMAGE_TAG}"

# 同時標記並推送 latest
docker tag "${IMAGE_TAG}" "${IMAGE_LATEST}"
docker push "${IMAGE_LATEST}"

echo ""
echo "============================================="
echo "✅ 完成！"
echo "   ${IMAGE_TAG}"
echo "   ${IMAGE_LATEST}"
echo "============================================="
