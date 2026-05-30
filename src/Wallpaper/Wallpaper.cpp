#include <windows.h>
#include <dwmapi.h>
#include <iostream>
#include <string>

#pragma comment(lib, "dwmapi.lib")
#pragma comment(lib, "user32.lib")

// DWM缩略图属性常量
#ifndef DWM_TNP_RECTDESTINATION
#define DWM_TNP_RECTDESTINATION      0x1
#define DWM_TNP_RECTSOURCE           0x2
#define DWM_TNP_OPACITY              0x4
#define DWM_TNP_VISIBLE              0x8
#define DWM_TNP_SOURCECLIENTAREAONLY 0x10
#endif

// ============================================================
//  DWM 缩略图全局状态
// ============================================================
static HWND      g_hwndMain       = nullptr;
static HTHUMBNAIL g_hThumbnail    = nullptr;
static HWND      g_hwndSource     = nullptr;
static bool      g_bThumbRegistered = false;

// ============================================================
//  EnumChildWindows 回调：在窗口中搜索 SHELLDLL_DefView
// ============================================================
static BOOL CALLBACK FindShellDefViewProc(HWND hwnd, LPARAM lParam)
{
    WCHAR className[256] = {};
    GetClassNameW(hwnd, className, 256);
    if (wcscmp(className, L"SHELLDLL_DefView") == 0)
    {
        HWND* pResult = reinterpret_cast<HWND*>(lParam);
        *pResult = hwnd;
        return FALSE; // 找到，停止
    }
    return TRUE;
}

// ============================================================
//  EnumWindows 回调：枚举所有 WorkerW 并在其中找 SHELLDLL_DefView
// ============================================================
struct WorkerWEnumData
{
    HWND hwndWithIcons;  // 含 SHELLDLL_DefView 的 WorkerW
    HWND hwndFirst;      // 碰到的第一个 WorkerW
};

static BOOL CALLBACK EnumWorkerWProc(HWND hwnd, LPARAM lParam)
{
    WCHAR className[256] = {};
    GetClassNameW(hwnd, className, 256);
    if (wcscmp(className, L"WorkerW") != 0)
        return TRUE; // 非 WorkerW，继续

    auto* data = reinterpret_cast<WorkerWEnumData*>(lParam);
    if (!data->hwndFirst)
        data->hwndFirst = hwnd;

    // 递归搜索子窗口，找 SHELLDLL_DefView
    HWND hwndDefView = nullptr;
    EnumChildWindows(hwnd, FindShellDefViewProc, reinterpret_cast<LPARAM>(&hwndDefView));
    if (hwndDefView)
    {
        data->hwndWithIcons = hwnd;
        std::wcout << L"  Found SHELLDLL_DefView in WorkerW 0x" 
                    << std::hex << (uintptr_t)hwnd << std::dec << std::endl;
        return FALSE; // 找到了，停止枚举
    }

    return TRUE;
}

// ============================================================
//  查找桌面图标窗口（壁纸 + 桌面图标的那一层）
// ============================================================
HWND FindDesktopIconWindow()
{
    // 方案A: 在 Progman 的子窗口中直接找 SHELLDLL_DefView
    HWND hwndProgman = FindWindowW(L"Progman", nullptr);
    if (hwndProgman)
    {
        HWND hwndDefView = nullptr;
        EnumChildWindows(hwndProgman, FindShellDefViewProc, reinterpret_cast<LPARAM>(&hwndDefView));
        if (hwndDefView)
        {
            std::wcout << L"SHELLDLL_DefView found directly in Progman 0x" 
                        << std::hex << (uintptr_t)hwndProgman << std::dec << std::endl;
            return hwndProgman;
        }
    }

    // 方案B: 发送 0x052C 让 Progman 生成 WorkerW，再枚举
    if (hwndProgman)
    {
        std::wcout << L"Progman: 0x" << std::hex << (uintptr_t)hwndProgman << std::dec << std::endl;
        DWORD_PTR result = 0;
        SendMessageTimeoutW(hwndProgman, 0x052C, 0, 0, SMTO_NORMAL, 2000, &result);
        std::cout << "Sent 0x052C to Progman" << std::endl;
    }

    // 等消息队列处理完
    MSG dummy;
    for (int i = 0; i < 20; i++)
    {
        PeekMessageW(&dummy, nullptr, 0, 0, PM_REMOVE);
        Sleep(5);
    }

    // 枚举所有顶层 WorkerW，找含 SHELLDLL_DefView 的那个
    std::cout << "Enumerating WorkerW..." << std::endl;
    WorkerWEnumData data = {};
    EnumWindows(EnumWorkerWProc, reinterpret_cast<LPARAM>(&data));

    if (data.hwndWithIcons)
    {
        std::wcout << L"Icon-layer WorkerW: 0x" << std::hex 
                    << (uintptr_t)data.hwndWithIcons << std::dec << std::endl;
        return data.hwndWithIcons;
    }

    if (data.hwndFirst)
    {
        std::wcout << L"No icon WorkerW, trying first WorkerW: 0x" 
                    << std::hex << (uintptr_t)data.hwndFirst << std::dec << std::endl;
        return data.hwndFirst;
    }

    if (hwndProgman)
    {
        std::cout << "Falling back to Progman" << std::endl;
        return hwndProgman;
    }

    return nullptr;
}

// ============================================================
//  更新缩略图显示属性
// ============================================================
void UpdateThumbnail()
{
    if (!g_bThumbRegistered || !g_hThumbnail || !g_hwndMain)
        return;

    RECT rc;
    GetClientRect(g_hwndMain, &rc);

    // rcDestination 是相对于目标窗口客户区的坐标，填满整个客户区即可
    DWM_THUMBNAIL_PROPERTIES props = {};
    props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY;
    props.fVisible = TRUE;
    props.fSourceClientAreaOnly = FALSE;
    props.opacity = 255;
    props.rcDestination.left   = 0;
    props.rcDestination.top    = 0;
    props.rcDestination.right  = rc.right - rc.left;
    props.rcDestination.bottom = rc.bottom - rc.top;

    DwmUpdateThumbnailProperties(g_hThumbnail, &props);
}

// ============================================================
//  窗口过程
// ============================================================
LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_CREATE:
    {
        std::cout << "WM_CREATE" << std::endl;

        // 直接在主窗口上注册 DWM 缩略图（不用子控件）
        HWND hwndDesktop = FindDesktopIconWindow();
        if (hwndDesktop)
        {
            HRESULT hr = DwmRegisterThumbnail(hwnd, hwndDesktop, &g_hThumbnail);
            if (SUCCEEDED(hr))
            {
                g_hwndSource = hwndDesktop;
                g_bThumbRegistered = true;
                UpdateThumbnail();
                std::cout << "SUCCESS: Desktop thumbnail registered!" << std::endl;
            }
            else
            {
                std::cerr << "FAILED: DwmRegisterThumbnail HRESULT=0x" 
                          << std::hex << hr << std::dec << std::endl;
            }
        }
        else
        {
            std::cerr << "No desktop window found." << std::endl;
        }
        break;
    }

    case WM_SIZE:
    {
        UpdateThumbnail();
        break;
    }

    case WM_MOVE:
    {
        UpdateThumbnail();
        break;
    }

    case WM_DESTROY:
    {
        if (g_bThumbRegistered && g_hThumbnail)
        {
            DwmUnregisterThumbnail(g_hThumbnail);
            g_hThumbnail = nullptr;
            g_bThumbRegistered = false;
        }
        PostQuitMessage(0);
        break;
    }

    default:
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
    return 0;
}

// ============================================================
//  main
// ============================================================
int main()
{
    // DPI 感知
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

    // 检查 DWM
    BOOL dwmEnabled = FALSE;
    DwmIsCompositionEnabled(&dwmEnabled);
    if (!dwmEnabled)
    {
        std::cerr << "DWM composition not enabled!" << std::endl;
        return 1;
    }

    HINSTANCE hInstance = GetModuleHandle(nullptr);

    WNDCLASSEXW wc = {};
    wc.cbSize        = sizeof(WNDCLASSEXW);
    wc.lpfnWndProc   = WndProc;
    wc.hInstance     = hInstance;
    wc.lpszClassName = L"DesktopThumbWnd";
    wc.hCursor       = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    RegisterClassExW(&wc);

    g_hwndMain = CreateWindowExW(
        0, L"DesktopThumbWnd", L"Desktop Thumbnail",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 960, 640,
        nullptr, nullptr, hInstance, nullptr);

    if (!g_hwndMain) return 1;

    ShowWindow(g_hwndMain, SW_SHOW);
    UpdateWindow(g_hwndMain);

    MSG msg;
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return (int)msg.wParam;
}
