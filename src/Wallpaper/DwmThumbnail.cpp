#include "DwmThumbnail.h"
#include <stdexcept>

// 静态成员初始化
DwmRegisterThumbnailFunc DwmThumbnailControl::pDwmRegisterThumbnail = nullptr;
DwmUnregisterThumbnailFunc DwmThumbnailControl::pDwmUnregisterThumbnail = nullptr;
DwmUpdateThumbnailPropertiesFunc DwmThumbnailControl::pDwmUpdateThumbnailProperties = nullptr;
DwmIsCompositionEnabledFunc DwmThumbnailControl::pDwmIsCompositionEnabled = nullptr;
bool DwmThumbnailControl::s_bFunctionsLoaded = false;

// 窗口类名
static const wchar_t* THUMBNAIL_CLASS_NAME = L"DwmThumbnailControl";
static HINSTANCE g_hInstance = nullptr;

DwmThumbnailControl::DwmThumbnailControl()
    : m_hwndParent(nullptr)
    , m_hwndChild(nullptr)
    , m_hwndSource(nullptr)
    , m_hThumbnail(nullptr)
    , m_bThumbRegistered(false)
    , m_bClientAreaOnly(false)
    , m_x(0)
    , m_y(0)
    , m_width(0)
    , m_height(0)
{
    LoadDwmFunctions();
}

DwmThumbnailControl::~DwmThumbnailControl()
{
    // 释放缩略图
    if (m_bThumbRegistered && m_hThumbnail && pDwmUnregisterThumbnail) {
        pDwmUnregisterThumbnail(m_hThumbnail);
        m_hThumbnail = nullptr;
        m_bThumbRegistered = false;
    }
    
    // 销毁窗口
    if (m_hwndChild) {
        DestroyWindow(m_hwndChild);
        m_hwndChild = nullptr;
    }
}

bool DwmThumbnailControl::LoadDwmFunctions()
{
    if (s_bFunctionsLoaded) return true;
    
    HMODULE hDwmApi = LoadLibraryW(L"dwmapi.dll");
    if (!hDwmApi) return false;
    
    pDwmRegisterThumbnail = (DwmRegisterThumbnailFunc)GetProcAddress(hDwmApi, "DwmRegisterThumbnail");
    pDwmUnregisterThumbnail = (DwmUnregisterThumbnailFunc)GetProcAddress(hDwmApi, "DwmUnregisterThumbnail");
    pDwmUpdateThumbnailProperties = (DwmUpdateThumbnailPropertiesFunc)GetProcAddress(hDwmApi, "DwmUpdateThumbnailProperties");
    pDwmIsCompositionEnabled = (DwmIsCompositionEnabledFunc)GetProcAddress(hDwmApi, "DwmIsCompositionEnabled");
    
    s_bFunctionsLoaded = (pDwmRegisterThumbnail && pDwmUnregisterThumbnail && pDwmUpdateThumbnailProperties);
    return s_bFunctionsLoaded;
}

bool DwmThumbnailControl::RegisterControlClass(HINSTANCE hInstance)
{
    if (g_hInstance) return true; // 已经注册
    
    WNDCLASSEXW wc = {};
    wc.cbSize = sizeof(WNDCLASSEXW);
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = THUMBNAIL_CLASS_NAME;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    
    if (!RegisterClassExW(&wc)) return false;
    
    g_hInstance = hInstance;
    return true;
}

void DwmThumbnailControl::UnregisterControlClass(HINSTANCE hInstance)
{
    if (g_hInstance == hInstance) {
        UnregisterClassW(THUMBNAIL_CLASS_NAME, hInstance);
        g_hInstance = nullptr;
    }
}

LRESULT CALLBACK DwmThumbnailControl::WindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
    DwmThumbnailControl* pControl = nullptr;
    
    if (msg == WM_NCCREATE) {
        CREATESTRUCT* pCreate = (CREATESTRUCT*)lParam;
        pControl = (DwmThumbnailControl*)pCreate->lpCreateParams;
        SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)pControl);
        pControl->m_hwndChild = hwnd;
    } else {
        pControl = (DwmThumbnailControl*)GetWindowLongPtr(hwnd, GWLP_USERDATA);
    }
    
    if (pControl) {
        switch (msg) {
        case WM_SIZE:
            pControl->UpdateThumbnail();
            break;
            
        case WM_MOVE:
            pControl->UpdateThumbnail();
            break;
            
        case WM_DESTROY:
            pControl->m_hwndChild = nullptr;
            break;
        }
    }
    
    return DefWindowProc(hwnd, msg, wParam, lParam);
}

bool DwmThumbnailControl::Create(HWND hwndParent, int x, int y, int width, int height, HINSTANCE hInstance)
{
    if (!hInstance && !g_hInstance) return false;
    if (!hInstance) hInstance = g_hInstance;
    
    m_hwndParent = hwndParent;
    m_x = x;
    m_y = y;
    m_width = width;
    m_height = height;
    
    m_hwndChild = CreateWindowExW(
        0,
        THUMBNAIL_CLASS_NAME,
        L"",
        WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
        x, y, width, height,
        hwndParent,
        nullptr,
        hInstance,
        this
    );
    
    return m_hwndChild != nullptr;
}

HRESULT DwmThumbnailControl::SetWindowSource(HWND hwndSource)
{
    if (hwndSource == m_hwndSource) return S_OK;
    
    // 释放旧的缩略图
    if (m_bThumbRegistered && m_hThumbnail) {
        if (pDwmUnregisterThumbnail) {
            pDwmUnregisterThumbnail(m_hThumbnail);
        }
        m_hThumbnail = nullptr;
        m_bThumbRegistered = false;
    }
    
    m_hwndSource = hwndSource;
    
    // 注册新的缩略图
    if (hwndSource && m_hwndChild && pDwmRegisterThumbnail) {
        HRESULT hr = pDwmRegisterThumbnail(m_hwndChild, hwndSource, &m_hThumbnail);
        if (SUCCEEDED(hr)) {
            m_bThumbRegistered = true;
            UpdateThumbnail();
        }
        return hr;
    }
    
    return E_FAIL;
}

void DwmThumbnailControl::SetClientAreaOnly(bool clientAreaOnly)
{
    m_bClientAreaOnly = clientAreaOnly;
    UpdateThumbnail();
}

void DwmThumbnailControl::UpdateThumbnail()
{
    if (!m_bThumbRegistered || !m_hThumbnail || !m_hwndChild || !pDwmUpdateThumbnailProperties)
        return;
    
    // 获取控件的屏幕坐标（在 DPI Aware 模式下，ClientToScreen 直接返回物理像素坐标）
    RECT rcClient;
    GetClientRect(m_hwndChild, &rcClient);
    
    POINT pt = { 0, 0 };
    ClientToScreen(m_hwndChild, &pt);
    
    DWM_THUMBNAIL_PROPERTIES props = {};
    props.dwFlags = DWM_TNP_VISIBLE | DWM_TNP_RECTDESTINATION | DWM_TNP_OPACITY | DWM_TNP_SOURCECLIENTAREAONLY;
    props.fVisible = TRUE;
    props.fSourceClientAreaOnly = m_bClientAreaOnly;
    props.opacity = 255;
    props.rcDestination.left = pt.x;
    props.rcDestination.top = pt.y;
    props.rcDestination.right = pt.x + (rcClient.right - rcClient.left);
    props.rcDestination.bottom = pt.y + (rcClient.bottom - rcClient.top);
    
    pDwmUpdateThumbnailProperties(m_hThumbnail, &props);
}

void DwmThumbnailControl::SetBounds(int x, int y, int width, int height)
{
    m_x = x;
    m_y = y;
    m_width = width;
    m_height = height;
    
    if (m_hwndChild) {
        SetWindowPos(m_hwndChild, nullptr, x, y, width, height, SWP_NOZORDER);
        UpdateThumbnail();
    }
}

void DwmThumbnailControl::Show(bool bShow)
{
    if (m_hwndChild) {
        ShowWindow(m_hwndChild, bShow ? SW_SHOW : SW_HIDE);
    }
}

bool DwmThumbnailControl::IsCompositionEnabled()
{
    LoadDwmFunctions();
    if (!pDwmIsCompositionEnabled) return false;
    
    BOOL enabled = FALSE;
    HRESULT hr = pDwmIsCompositionEnabled(&enabled);
    return SUCCEEDED(hr) && enabled;
}