#pragma once

#include <windows.h>
#include <dwmapi.h>

#pragma comment(lib, "dwmapi.lib")

// DWM缩略图属性常量（与 C# 参考 DwmThumbnailAPI.cs 保持一致）
#ifndef DWM_TNP_RECTDESTINATION
#define DWM_TNP_RECTDESTINATION      0x1   // 目标矩形
#define DWM_TNP_RECTSOURCE           0x2   // 源矩形
#define DWM_TNP_OPACITY              0x4   // 透明度
#define DWM_TNP_VISIBLE              0x8   // 可见性
#define DWM_TNP_SOURCECLIENTAREAONLY 0x10  // 仅客户区
#endif


// API函数类型定义
typedef HRESULT (WINAPI *DwmRegisterThumbnailFunc)(HWND, HWND, PHTHUMBNAIL);
typedef HRESULT (WINAPI *DwmUnregisterThumbnailFunc)(HTHUMBNAIL);
typedef HRESULT (WINAPI *DwmUpdateThumbnailPropertiesFunc)(HTHUMBNAIL, const DWM_THUMBNAIL_PROPERTIES*);
typedef HRESULT (WINAPI *DwmIsCompositionEnabledFunc)(BOOL*);

class DwmThumbnailControl {
private:
    HWND m_hwndParent;                  // 父窗口句柄
    HWND m_hwndChild;                   // 子窗口句柄（用于承载缩略图）
    HWND m_hwndSource;                  // 源窗口句柄
    HTHUMBNAIL m_hThumbnail;            // 缩略图句柄
    bool m_bThumbRegistered;            // 缩略图是否已注册
    bool m_bClientAreaOnly;             // 是否仅捕获客户区
    int m_x, m_y;                       // 控件位置
    int m_width, m_height;              // 控件大小
    
    // DWM API函数指针
    static DwmRegisterThumbnailFunc pDwmRegisterThumbnail;
    static DwmUnregisterThumbnailFunc pDwmUnregisterThumbnail;
    static DwmUpdateThumbnailPropertiesFunc pDwmUpdateThumbnailProperties;
    static DwmIsCompositionEnabledFunc pDwmIsCompositionEnabled;
    static bool s_bFunctionsLoaded;
    
    // 加载DWM函数
    static bool LoadDwmFunctions();
    
    // 窗口过程
    static LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

public:
    DwmThumbnailControl();
    ~DwmThumbnailControl();
    
    // 创建控件
    bool Create(HWND hwndParent, int x, int y, int width, int height, HINSTANCE hInstance = nullptr);
    
    // 设置源窗口，返回 HRESULT（S_OK=成功，否则为错误码）
    HRESULT SetWindowSource(HWND hwndSource);
    
    // 设置是否仅捕获客户区
    void SetClientAreaOnly(bool clientAreaOnly);
    
    // 获取源窗口
    HWND GetWindowSource() const { return m_hwndSource; }
    
    // 获取是否仅捕获客户区
    bool GetClientAreaOnly() const { return m_bClientAreaOnly; }
    
    // 更新缩略图
    void UpdateThumbnail();
    
    // 设置位置和大小
    void SetBounds(int x, int y, int width, int height);
    
    // 获取控件句柄
    HWND GetHwnd() const { return m_hwndChild; }
    
    // 显示/隐藏
    void Show(bool bShow = true);
    
    // 注册窗口类
    static bool RegisterControlClass(HINSTANCE hInstance);
    static void UnregisterControlClass(HINSTANCE hInstance);
    
    // 检查DWM合成是否启用
    static bool IsCompositionEnabled();
};