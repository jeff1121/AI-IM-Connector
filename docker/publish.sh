#!/bin/bash
# =============================================================================
# AI IM Connector — 建置並推送多平台 Docker Image 至 Azure ACR
# 支援平台：linux/amd64、linux/arm64
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

IMAGE_NAME="${ACR_REGISTRY}/ai-connector/im-connector"
PLATFORMS="linux/amd64,linux/arm64"

echo "============================================="
echo "🔑 登入 Azure ACR: ${ACR_REGISTRY}"
echo "============================================="

az acr login --name "${ACR_REGISTRY%%.*}"

echo ""
echo "============================================="
echo "📦 建置並推送多平台 Docker Image"
echo "   Image: ${IMAGE_NAME}"
echo "   Tags:  ${IMAGE_VERSION}, latest"
echo "   Platforms: ${PLATFORMS}"
echo "============================================="

# 確保 buildx builder 存在
BUILDER_NAME="multiplatform"
if ! docker buildx inspect "${BUILDER_NAME}" >/dev/null 2>&1; then
    echo "🔧 建立 buildx builder: ${BUILDER_NAME}"
    docker buildx create --name "${BUILDER_NAME}" --use --driver docker-container
else
    docker buildx use "${BUILDER_NAME}"
fi

# 切換至專案根目錄（確保 buildx context 路徑正確）
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${PROJECT_ROOT}"

# 多平台建置並推送（同時標記版本號與 latest）
docker buildx build \
    --platform "${PLATFORMS}" \
    -f docker/Dockerfile \
    -t "${IMAGE_NAME}:${IMAGE_VERSION}" \
    -t "${IMAGE_NAME}:latest" \
    --push \
    .

echo ""
echo "============================================="
echo "✅ 完成！"
echo "   ${IMAGE_NAME}:${IMAGE_VERSION}"
echo "   ${IMAGE_NAME}:latest"
echo "   平台: ${PLATFORMS}"
echo "============================================="
