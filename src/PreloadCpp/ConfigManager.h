#pragma once

#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <windows.h>
#include "logger.h"

namespace fs = std::filesystem;

class ConfigManager
{
private:
    std::string m_jsonContent;

    // 获取本体 EXE 所在的绝对路径目录
    std::wstring GetExeDirectory()
    {
        wchar_t path[MAX_PATH];
        // 传入 NULL 获取当前进程 (.exe) 的路径
        if (GetModuleFileNameW(NULL, path, MAX_PATH) == 0)
        {
            return L"";
        }
        fs::path exePath(path);
        return exePath.parent_path().wstring();
    }

    std::string ReadJsonFile()
    {
        // 构建绝对路径：EXE目录 / config / BedrockBoot2 / config.json
        fs::path configPath = GetExeDirectory();
        if (configPath.empty()) return "";

        configPath /= "config";
        configPath /= "BedrockBoot2";
        configPath /= "config.json";

        std::string fullPathStr = configPath.string();

        if (!fs::exists(configPath))
        {
            Logger::Error("Config file missing: " + fullPathStr);
            return "";
        }

        std::ifstream file(configPath);
        if (!file.is_open())
        {
            Logger::Error("Access denied or failed to open: " + fullPathStr);
            return "";
        }

        std::stringstream buffer;
        buffer << file.rdbuf();
        file.close();

        return buffer.str();
    }

    // 通用修剪函数
    std::string Trim(const std::string& str, const std::string& trimChars = " \t\n\r\"")
    {
        size_t first = str.find_first_not_of(trimChars);
        if (first == std::string::npos) return "";
        size_t last = str.find_last_not_of(trimChars);
        return str.substr(first, last - first + 1);
    }

    // 简易 JSON 解析逻辑
    std::string GetJsonValue(const std::string& json, const std::string& key)
    {
        std::string searchKey = "\"" + key + "\"";
        size_t keyPos = json.find(searchKey);
        if (keyPos == std::string::npos) return "";

        size_t colonPos = json.find(':', keyPos);
        if (colonPos == std::string::npos) return "";

        size_t start = colonPos + 1;
        // 跳过空白
        while (start < json.length() && isspace(static_cast<unsigned char>(json[start])))
            start++;

        size_t end = start;
        bool inString = false;
        int braceCount = 0;
        int bracketCount = 0;

        while (end < json.length())
        {
            char c = json[end];
            if (c == '\"') inString = !inString;
            if (!inString)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                else if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
                else if ((c == ',' || c == '}' || c == ']') && braceCount <= 0 && bracketCount <= 0) break;
            }
            end++;
        }
        return json.substr(start, end - start);
    }

public:
    ConfigManager()
    {
        m_jsonContent = ReadJsonFile();
    }

    bool GetBoolConfig(const std::string& key)
    {
        std::string section = GetJsonValue(m_jsonContent, "config");
        if (section.empty()) return false;

        std::string val = Trim(GetJsonValue(section, key));
        // 转换为小写判断
        for (auto& c : val) c = static_cast<char>(tolower(c));
        return (val == "true" || val == "1");
    }

    std::string GetStringConfig(const std::string& key)
    {
        std::string section = GetJsonValue(m_jsonContent, "config");
        if (section.empty()) return "";

        return Trim(GetJsonValue(section, key));
    }

    bool IsConfigValid() const
    {
        return !m_jsonContent.empty();
    }
};