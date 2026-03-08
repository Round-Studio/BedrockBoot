#pragma once

#include <iostream>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <windows.h>
#include <string>
#include <queue>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <atomic>

enum class LogLevel {
    INFO,
    WARNING,
    ERR,
    SUCCESS
};

struct LogTask {
    LogLevel level;
    std::string message;
    std::string timestamp;
    std::string context;
};

class Logger {
public:
    static HANDLE hConsole;

private:
    static const WORD INFO_COLOR = FOREGROUND_GREEN | FOREGROUND_INTENSITY;
    static const WORD WARNING_COLOR = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY;
    static const WORD ERROR_COLOR = FOREGROUND_RED | FOREGROUND_INTENSITY;
    static const WORD SUCCESS_COLOR = FOREGROUND_GREEN | FOREGROUND_INTENSITY;
    static const WORD DEFAULT_COLOR = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
    static const WORD CYAN_COLOR = FOREGROUND_BLUE | FOREGROUND_GREEN | FOREGROUND_INTENSITY;

    // 异步相关成员
    static std::queue<LogTask> logQueue;
    static std::mutex queueMutex;
    static std::condition_variable cv;
    static std::thread workerThread;
    static std::atomic<bool> shouldStop;

    static std::string GetTimestamp() {
        auto now = std::chrono::system_clock::now();
        auto time = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()) % 1000;

        std::stringstream ss;
        struct tm tm_info;
        localtime_s(&tm_info, &time);
        ss << std::put_time(&tm_info, "%H:%M:%S") << "." << std::setfill('0') << std::setw(3) << ms.count();
        return ss.str();
    }

    static const char* GetLevelString(LogLevel level) {
        switch (level) {
        case LogLevel::INFO: return "INFO";
        case LogLevel::WARNING: return "WARN";
        case LogLevel::ERR: return "ERROR";
        case LogLevel::SUCCESS: return "SUCCESS";
        default: return "LOG";
        }
    }

    // 后台渲染线程主循环
    static void ProcessLogs() {
        while (true) {
            std::queue<LogTask> localQueue;
            {
                std::unique_lock<std::mutex> lock(queueMutex);
                cv.wait(lock, [] { return !logQueue.empty() || shouldStop; });

                if (shouldStop && logQueue.empty()) break;

                // 批量交换队列，极大减少锁占用时间
                std::swap(localQueue, logQueue);
            }

            // 在锁外进行 I/O，不阻塞 Hook 线程
            while (!localQueue.empty()) {
                const auto& task = localQueue.front();
                Render(task);
                localQueue.pop();
            }
        }
    }

    static void Render(const LogTask& task) {
        if (hConsole == INVALID_HANDLE_VALUE) return;

        // 1. 时间戳 (默认色)
        std::cout << task.timestamp << " ";

        // 2. 级别标签 (根据级别变色)
        if (task.level == LogLevel::INFO)
            SetConsoleTextAttribute(hConsole, CYAN_COLOR);
        else if (task.level == LogLevel::SUCCESS)
            SetConsoleTextAttribute(hConsole, INFO_COLOR);
        else if (task.level == LogLevel::WARNING)
            SetConsoleTextAttribute(hConsole, WARNING_COLOR);
        else if (task.level == LogLevel::ERR)
            SetConsoleTextAttribute(hConsole, ERROR_COLOR);

        std::cout << GetLevelString(task.level);

        // 3. 恢复默认并输出内容
        SetConsoleTextAttribute(hConsole, DEFAULT_COLOR);
        std::cout << " [" << task.context << "] " << task.message << "\n"; // 使用 \n 代替 std::endl 提高性能
    }

public:
    static void Initialize() {
        if (hConsole != INVALID_HANDLE_VALUE) return; // 防止重复初始化

        hConsole = GetStdHandle(STD_OUTPUT_HANDLE);

        // 提升标准流性能
        std::ios_base::sync_with_stdio(false);
        std::cin.tie(NULL);

        shouldStop = false;
        workerThread = std::thread(ProcessLogs);

        Logger::Log(LogLevel::INFO, "Logger Initialize.", "Logger");
    }

    static void Shutdown() {
        shouldStop = true;
        cv.notify_all();
        if (workerThread.joinable()) workerThread.join();
    }

    static void Log(LogLevel level, const std::string& message, const std::string& context = "BedrockBoot") {
        // 仅仅将数据打包进队列，耗时极短
        LogTask task{ level, message, GetTimestamp(), context };
        {
            std::lock_guard<std::mutex> lock(queueMutex);
            logQueue.push(std::move(task));
        }
        cv.notify_one();
    }

    static void Info(const std::string& msg) { Log(LogLevel::INFO, msg); }
    static void Warning(const std::string& msg) { Log(LogLevel::WARNING, msg); }
    static void Error(const std::string& msg) { Log(LogLevel::ERR, msg); }
    static void Success(const std::string& msg) { Log(LogLevel::SUCCESS, msg); }
};

// 静态成员初始化
HANDLE Logger::hConsole = INVALID_HANDLE_VALUE;
std::queue<LogTask> Logger::logQueue;
std::mutex Logger::queueMutex;
std::condition_variable Logger::cv;
std::thread Logger::workerThread;
std::atomic<bool> Logger::shouldStop{ false };