#pragma once

#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <windows.h>
#include <unordered_map>
#include <cctype>
#include <algorithm>
#include "logger.h"

namespace fs = std::filesystem;

class ConfigManager
{
private:
    std::unordered_map<std::string, std::string> m_configValues;
    bool m_isValid;

    std::wstring GetExeDirectory()
    {
        wchar_t path[MAX_PATH];
        if (GetModuleFileNameW(NULL, path, MAX_PATH) == 0)
        {
            return L"";
        }
        fs::path exePath(path);
        return exePath.parent_path().wstring();
    }

    std::string ReadJsonFile()
    {
        fs::path configPath = GetExeDirectory();
        if (configPath.empty()) return "";

        configPath /= "config";
        configPath /= "BedrockBoot2";
        configPath /= "config.json";

        if (!fs::exists(configPath))
        {
            Logger::Error("Config file missing: " + configPath.string());
            return "";
        }

        std::ifstream file(configPath);
        if (!file.is_open())
        {
            Logger::Error("Failed to open config file: " + configPath.string());
            return "";
        }

        std::stringstream buffer;
        buffer << file.rdbuf();
        file.close();

        return buffer.str();
    }

    std::string Trim(const std::string& str)
    {
        size_t first = str.find_first_not_of(" \t\n\r");
        if (first == std::string::npos) return "";
        size_t last = str.find_last_not_of(" \t\n\r");
        return str.substr(first, last - first + 1);
    }

    std::string ExtractValue(const std::string& json, size_t startPos)
    {
        size_t pos = startPos;
        size_t endPos;
        char c;

        while (pos < json.length())
        {
            c = json[pos];
            if (!(c == ' ' || c == '\t' || c == '\n' || c == '\r')) break;
            pos++;
        }

        if (pos >= json.length()) return "";

        if (json[pos] == '"')
        {
            pos++;
            endPos = pos;
            while (endPos < json.length())
            {
                if (json[endPos] == '"')
                {
                    if (endPos == pos || json[endPos - 1] != '\\')
                    {
                        break;
                    }
                }
                endPos++;
            }
            return json.substr(pos, endPos - pos);
        }

        endPos = pos;
        while (endPos < json.length())
        {
            c = json[endPos];
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == ',' || c == '}')
            {
                break;
            }
            endPos++;
        }

        return json.substr(pos, endPos - pos);
    }

    void ParseJson(const std::string& json)
    {
        size_t configPos;
        size_t colonPos;
        size_t objStart;
        size_t objEnd;
        int braceCount;
        size_t currentPos;
        size_t keyStart;
        size_t keyEnd;
        size_t valueColonPos;
        size_t commaPos;
        std::string key;
        std::string value;

        m_isValid = false;

        configPos = json.find("\"config\"");
        if (configPos == std::string::npos)
        {
            Logger::Error("JSON missing config key");
            return;
        }

        colonPos = json.find(':', configPos);
        if (colonPos == std::string::npos)
        {
            Logger::Error("Invalid JSON: missing colon");
            return;
        }

        objStart = json.find('{', colonPos);
        if (objStart == std::string::npos)
        {
            Logger::Error("Invalid JSON: config not an object");
            return;
        }

        braceCount = 1;
        objEnd = objStart + 1;
        while (objEnd < json.length() && braceCount > 0)
        {
            if (json[objEnd] == '{') braceCount++;
            else if (json[objEnd] == '}') braceCount--;
            objEnd++;
        }

        if (braceCount != 0)
        {
            Logger::Error("Invalid JSON: unmatched braces");
            return;
        }

        currentPos = objStart + 1;
        while (currentPos < objEnd)
        {
            keyStart = json.find('"', currentPos);
            if (keyStart == std::string::npos || keyStart >= objEnd) break;

            keyEnd = json.find('"', keyStart + 1);
            if (keyEnd == std::string::npos || keyEnd >= objEnd) break;

            key = json.substr(keyStart + 1, keyEnd - keyStart - 1);

            valueColonPos = json.find(':', keyEnd);
            if (valueColonPos == std::string::npos || valueColonPos >= objEnd) break;

            value = ExtractValue(json, valueColonPos + 1);

            m_configValues[key] = value;

            commaPos = json.find(',', valueColonPos);
            if (commaPos == std::string::npos || commaPos >= objEnd) break;
            currentPos = commaPos + 1;
        }

        m_isValid = true;
        Logger::Info("Config loaded, items: " + std::to_string(m_configValues.size()));
    }

public:
    ConfigManager() : m_isValid(false)
    {
        std::string jsonContent = ReadJsonFile();
        if (!jsonContent.empty())
        {
            ParseJson(jsonContent);
        }

        if (!m_isValid)
        {
            Logger::Warning("Using default config values");
            m_configValues["isConsole"] = "true";
            m_configValues["isVersionIsolated"] = "true";
            m_configValues["isDetailedLog"] = "false";
            m_isValid = true;
        }
    }

    bool GetBoolConfig(const std::string& key)
    {
        std::unordered_map<std::string, std::string>::iterator it;
        std::string value;

        it = m_configValues.find(key);
        if (it == m_configValues.end())
        {
            Logger::Warning("Config key not found: " + key);
            return false;
        }

        value = it->second;
        std::transform(value.begin(), value.end(), value.begin(), ::tolower);

        return (value == "true" || value == "1");
    }

    std::string GetStringConfig(const std::string& key)
    {
        std::unordered_map<std::string, std::string>::iterator it;

        it = m_configValues.find(key);
        if (it == m_configValues.end())
        {
            Logger::Warning("Config key not found: " + key);
            return "";
        }

        return it->second;
    }

    bool IsConfigValid() const
    {
        return m_isValid;
    }
};