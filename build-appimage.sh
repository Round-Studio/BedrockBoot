#!/bin/bash

# ======================================================
# 使用 DotnetPackaging.Tool 构建 .NET AppImage
# 用法: ./build-appimage.sh <项目路径或发布目录> [应用名称] [图标路径] [-version <版本号>]
# 示例: ./build-appimage.sh ./src/MyApp.csproj "我的应用" ./icon.png -version 1.2.3
#       ./build-appimage.sh ./publish/linux-x64 "我的应用" -version 2.0.0
# ======================================================

set -e

# 颜色输出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
print_step() { echo -e "${BLUE}==>${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }

# 默认值
RUNTIME="linux-x64"
CONFIGURATION="Release"
OUTPUT_DIR="./artifacts"
PUBLISH_DIR="./publish/${RUNTIME}/${CONFIGURATION}"
VERSION=""

# 解析参数
INPUT_PATH=""
APP_NAME=""
ICON_PATH=""

while [[ $# -gt 0 ]]; do
    case $1 in
        -version)
            VERSION="$2"
            shift 2
            ;;
        *)
            if [ -z "${INPUT_PATH}" ]; then
                INPUT_PATH="$1"
            elif [ -z "${APP_NAME}" ]; then
                APP_NAME="$1"
            elif [ -z "${ICON_PATH}" ]; then
                ICON_PATH="$1"
            else
                print_error "未知参数: $1"
            fi
            shift
            ;;
    esac
done

# 参数检查
if [ -z "${INPUT_PATH}" ]; then
    echo "用法: $0 <项目路径或发布目录> [应用名称] [图标路径] [-version <版本号>]"
    echo "示例: $0 ./src/MyApp.csproj"
    echo "      $0 ./src/MyApp.csproj '我的应用' ./icon.png"
    echo "      $0 ./src/MyApp.csproj '我的应用' -version 1.2.3"
    echo "      $0 ./publish/linux-x64 '我的应用' -version 2.0.0"
    exit 1
fi

# 创建输出目录
mkdir -p "${OUTPUT_DIR}"

# ======================================================
# 步骤 1: 安装 DotnetPackaging.Tool
# ======================================================
install_tool() {
    print_step "[1/4] 安装 DotnetPackaging.Tool..."
    
    if ! command -v dotnetpackager &> /dev/null; then
        dotnet tool install --global DotnetPackaging.Tool
        print_info "工具安装完成"
    else
        print_info "工具已安装，尝试更新到最新版本..."
        dotnet tool update --global DotnetPackaging.Tool 2>/dev/null || true
    fi
}

# ======================================================
# 步骤 2: 发布 .NET 项目（如果输入是 .csproj）
# ======================================================
publish_if_needed() {
    if [[ "${INPUT_PATH}" == *.csproj ]]; then
        print_step "[2/4] 发布 .NET 项目..."
        
        # 获取项目目录和名称
        PROJECT_DIR=$(dirname "${INPUT_PATH}")
        PROJECT_NAME=$(basename "${INPUT_PATH}" .csproj)
        
        # 如果未指定应用名称，使用项目名称
        if [ -z "${APP_NAME}" ]; then
            APP_NAME="${PROJECT_NAME}"
        fi
        
        # 构建发布命令
        PUBLISH_CMD="dotnet publish \"${INPUT_PATH}\" \
            -c ${CONFIGURATION} \
            -r ${RUNTIME} \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:DebugType=none \
            -o \"${PUBLISH_DIR}\""
        
        # 如果指定了版本，传递给发布过程
        if [ -n "${VERSION}" ]; then
            PUBLISH_CMD="${PUBLISH_CMD} -p:Version=${VERSION}"
            print_info "设置版本号: ${VERSION}"
        fi
        
        eval ${PUBLISH_CMD}
        
        PUBLISH_DIR_ABS=$(realpath "${PUBLISH_DIR}")
        print_info "发布完成: ${PUBLISH_DIR_ABS}"
    else
        # 输入是目录，直接使用
        PUBLISH_DIR_ABS=$(realpath "${INPUT_PATH}")
        print_step "[2/4] 使用已有发布目录: ${PUBLISH_DIR_ABS}"
        
        # 如果未指定应用名称，尝试从目录名获取
        if [ -z "${APP_NAME}" ]; then
            APP_NAME=$(basename "${PUBLISH_DIR_ABS}")
        fi
    fi
}

# ======================================================
# 步骤 3: 打包为 AppImage
# ======================================================
package_appimage() {
    print_step "[3/4] 打包为 AppImage..."
    
    # 构建文件名（包含版本号）
    APP_NAME_SAFE="${APP_NAME// /_}"
    OUTPUT_APPIMAGE="${OUTPUT_DIR}/${APP_NAME_SAFE}-x86_64-linux.AppImage"
    
    # 构建命令参数
    CMD="dotnetpackager appimage \
        --directory \"${PUBLISH_DIR_ABS}\" \
        --output \"${OUTPUT_APPIMAGE}\" \
        --application-name \"${APP_NAME}\" \
        --summary \"${APP_NAME} Application\""
    
    # 添加版本号参数
    if [ -n "${VERSION}" ]; then
        CMD="${CMD} --version \"${VERSION}\""
        print_info "设置 AppImage 版本: ${VERSION}"
    fi
    
    # 添加图标参数
    if [ -n "${ICON_PATH}" ] && [ -f "${ICON_PATH}" ]; then
        CMD="${CMD} --icon \"${ICON_PATH}\""
        MIME=$(file --mime-type -b "${ICON_PATH}")
        if [[ "${MIME}" != "image/png" ]]; then
            print_error "不受支持的图标格式"
        fi
        cp "${ICON_PATH}" "${PUBLISH_DIR_ABS}/bedrockboot.png"
        print_info "使用自定义图标: ${ICON_PATH}"
    fi
    
    # 可选：添加更多元数据（取消注释即可使用）
    # CMD="${CMD} --comment \"应用详细描述\""
    # CMD="${CMD} --homepage \"https://example.com\""
    
    eval ${CMD}
    
    if [ $? -eq 0 ]; then
        print_info "AppImage 生成成功: ${OUTPUT_APPIMAGE}"
    else
        print_error "打包失败"
    fi
}

# ======================================================
# 步骤 4: 设置权限并显示结果
# ======================================================
finalize() {
    print_step "[4/4] 完成..."
    
    chmod +x "${OUTPUT_APPIMAGE}"
    
    echo ""
    echo "=========================================="
    echo -e "${GREEN}✅ AppImage 构建完成！${NC}"
    echo "=========================================="
    echo "📦 输出文件: ${OUTPUT_APPIMAGE}"
    echo "📏 文件大小: $(du -h ${OUTPUT_APPIMAGE} | cut -f1)"
    if [ -n "${VERSION}" ]; then
        echo "🏷️  版本号: ${VERSION}"
    fi
    echo ""
    echo "🚀 测试运行:"
    echo "   ${OUTPUT_APPIMAGE}"
    echo "=========================================="
}

# ======================================================
# 主函数
# ======================================================
main() {
    echo "=========================================="
    echo "   .NET AppImage Builder (DotnetPackaging)"
    echo "=========================================="
    
    if [ -n "${VERSION}" ]; then
        print_info "目标版本: ${VERSION}"
    fi
    
    install_tool
    publish_if_needed
    package_appimage
    finalize
}

main
