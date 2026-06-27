#pragma once

#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <windows.h>
#include <unordered_map>
#include <cctype>
#include <algorithm>
#include <vector>
#include "logger.h"

namespace fs = std::filesystem;

class ConfigManager
{
private:
    std::unordered_map<std::string, std::unordered_map<std::string, std::string>> m_allData;
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
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == ',' || c == '}' || c == ']')
            {
                break;
            }
            endPos++;
        }

        return json.substr(pos, endPos - pos);
    }

    std::unordered_map<std::string, std::string> ParseObject(const std::string& json, size_t startPos, size_t endPos)
    {
        std::unordered_map<std::string, std::string> result;
        size_t currentPos = startPos;

        while (currentPos < endPos)
        {
            size_t keyStart = json.find('"', currentPos);
            if (keyStart == std::string::npos || keyStart >= endPos) break;

            size_t keyEnd = json.find('"', keyStart + 1);
            if (keyEnd == std::string::npos || keyEnd >= endPos) break;

            std::string key = json.substr(keyStart + 1, keyEnd - keyStart - 1);

            size_t colonPos = json.find(':', keyEnd);
            if (colonPos == std::string::npos || colonPos >= endPos) break;

            std::string value = ExtractValue(json, colonPos + 1);

            result[key] = value;

            size_t commaPos = json.find(',', colonPos);
            if (commaPos == std::string::npos || commaPos >= endPos) break;
            currentPos = commaPos + 1;
        }

        return result;
    }

    size_t FindObjectEnd(const std::string& json, size_t startPos)
    {
        int braceCount = 1;
        size_t pos = startPos + 1;

        while (pos < json.length() && braceCount > 0)
        {
            if (json[pos] == '{') braceCount++;
            else if (json[pos] == '}') braceCount--;
            pos++;
        }

        return (braceCount == 0) ? pos : std::string::npos;
    }

    void ParseFullJson(const std::string& json)
    {
        m_allData.clear();
        m_isValid = false;

        size_t pos = 0;
        size_t jsonLen = json.length();

        while (pos < jsonLen)
        {
            size_t keyStart = json.find('"', pos);
            if (keyStart == std::string::npos) break;

            size_t keyEnd = json.find('"', keyStart + 1);
            if (keyEnd == std::string::npos) break;

            std::string objectName = json.substr(keyStart + 1, keyEnd - keyStart - 1);

            size_t colonPos = json.find(':', keyEnd);
            if (colonPos == std::string::npos) break;

            size_t objStart = json.find('{', colonPos);
            if (objStart == std::string::npos)
            {
                pos = keyEnd + 1;
                continue;
            }

            size_t objEnd = FindObjectEnd(json, objStart);
            if (objEnd == std::string::npos)
            {
                Logger::Warning("Unmatched braces for object: " + objectName);
                pos = objStart + 1;
                continue;
            }

            m_allData[objectName] = ParseObject(json, objStart + 1, objEnd - 1);

            pos = objEnd;

            size_t commaPos = json.find(',', pos);
            if (commaPos != std::string::npos && commaPos < jsonLen)
            {
                pos = commaPos + 1;
            }
            else
            {
                break;
            }
        }

        m_isValid = !m_allData.empty();

        if (m_isValid)
        {
            Logger::Info("Config loaded, objects: " + std::to_string(m_allData.size()));
            for (const auto& obj : m_allData)
            {
                Logger::Info("  - " + obj.first + ": " + std::to_string(obj.second.size()) + " keys");
            }
        }
        else
        {
            Logger::Warning("Failed to parse JSON, using defaults");
            SetDefaultValues();
        }
    }

    void SetDefaultValues()
    {
        m_allData.clear();
        m_allData["config"]["isConsole"] = "true";
        m_allData["config"]["isVersionIsolated"] = "true";
        m_allData["config"]["isDetailedLog"] = "false";
        m_isValid = true;
    }

public:
    ConfigManager()
    {
        std::string jsonContent = ReadJsonFile();
        if (!jsonContent.empty())
        {
            ParseFullJson(jsonContent);
        }
        else
        {
            SetDefaultValues();
        }
    }

    std::string GetValue(const std::string& objectName, const std::string& key)
    {
        auto objIt = m_allData.find(objectName);
        if (objIt == m_allData.end())
        {
            Logger::Warning("Object not found: " + objectName);
            return "";
        }

        auto keyIt = objIt->second.find(key);
        if (keyIt == objIt->second.end())
        {
            Logger::Warning("Key not found: " + objectName + "." + key);
            return "";
        }

        return keyIt->second;
    }

    std::string GetStringConfig(const std::string& key)
    {
        return GetValue("config", key);
    }

    bool GetBoolConfig(const std::string& key)
    {
        std::string value = GetValue("config", key);
        if (value.empty()) return false;

        std::transform(value.begin(), value.end(), value.begin(), ::tolower);
        return (value == "true" || value == "1");
    }

    std::string GetInfo(const std::string& key)
    {
        return GetValue("info", key);
    }

    std::string GetPlayerData(const std::string& key)
    {
        return GetValue("playerData", key);
    }

    std::string GetGameStatus(const std::string& key)
    {
        return GetValue("gameStatus", key);
    }

    int GetInfoInt(const std::string& key)
    {
        std::string value = GetValue("info", key);
        if (value.empty()) return 0;
        return std::stoi(value);
    }

    int GetPlayerDataInt(const std::string& key)
    {
        std::string value = GetValue("playerData", key);
        if (value.empty()) return 0;
        return std::stoi(value);
    }

    bool GetGameStatusBool(const std::string& key)
    {
        std::string value = GetValue("gameStatus", key);
        if (value.empty()) return false;

        std::transform(value.begin(), value.end(), value.begin(), ::tolower);
        return (value == "true" || value == "1");
    }

    bool HasObject(const std::string& objectName) const
    {
        return m_allData.find(objectName) != m_allData.end();
    }

    bool HasKey(const std::string& objectName, const std::string& key) const
    {
        auto objIt = m_allData.find(objectName);
        if (objIt == m_allData.end()) return false;
        return objIt->second.find(key) != objIt->second.end();
    }

    std::vector<std::string> GetObjectNames() const
    {
        std::vector<std::string> names;
        for (const auto& obj : m_allData)
        {
            names.push_back(obj.first);
        }
        return names;
    }

    std::vector<std::string> GetKeys(const std::string& objectName) const
    {
        std::vector<std::string> keys;
        auto objIt = m_allData.find(objectName);
        if (objIt == m_allData.end()) return keys;

        for (const auto& pair : objIt->second)
        {
            keys.push_back(pair.first);
        }
        return keys;
    }

    bool IsConfigValid() const
    {
        return m_isValid;
    }

    void PrintAllData() const
    {
        Logger::Info("========== Config Data ==========");
        for (const auto& obj : m_allData)
        {
            Logger::Info("[" + obj.first + "]");
            for (const auto& pair : obj.second)
            {
                Logger::Info("  " + pair.first + " = " + pair.second);
            }
        }
        Logger::Info("=================================");
    }
};