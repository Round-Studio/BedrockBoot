#pragma once

#include <string>
#include <filesystem>
#include <fstream>
#include <sstream>
#include "logger.h"

namespace fs = std::filesystem;

class ConfigManager
{
private:
	static constexpr const char* CONFIG_PATH = "config/BedrockBoot2/config.json";
	std::string m_jsonContent;

	std::string ReadJsonFile(const std::string& filePath)
	{
		if (!fs::exists(filePath))
		{
			Logger::Error("Config file not found: " + filePath);
			return "";
		}

		std::ifstream file(filePath);
		if (!file.is_open())
		{
			Logger::Error("Failed to open config file: " + filePath);
			return "";
		}

		std::stringstream buffer;
		buffer << file.rdbuf();
		file.close();
		return buffer.str();
	}

	std::string TrimString(const std::string& str)
	{
		size_t first = str.find_first_not_of(" \t\n\r");
		if (first == std::string::npos) return "";
		size_t last = str.find_last_not_of(" \t\n\r");
		return str.substr(first, last - first + 1);
	}

	std::string GetJsonValue(const std::string& json, const std::string& key)
	{
		std::string searchKey = "\"" + key + "\"";
		size_t keyPos = json.find(searchKey);
		if (keyPos == std::string::npos)
		{
			return "";
		}

		size_t colonPos = json.find(':', keyPos);
		if (colonPos == std::string::npos)
		{
			return "";
		}

		size_t start = colonPos + 1;
		while (start < json.length() && (json[start] == ' ' || json[start] == '\t' || json[start] == '\n' || json[start] == '\r'))
		{
			start++;
		}

		if (start >= json.length())
		{
			return "";
		}

		size_t end = start;
		int braceCount = 0;
		int bracketCount = 0;
		bool inString = false;
		bool escaped = false;

		while (end < json.length())
		{
			char c = json[end];

			if (escaped)
			{
				escaped = false;
				end++;
				continue;
			}

			if (c == '\\')
			{
				escaped = true;
				end++;
				continue;
			}

			if (c == '"' && !escaped)
			{
				inString = !inString;
			}
			else if (!inString)
			{
				if (c == '{') braceCount++;
				else if (c == '}') braceCount--;
				else if (c == '[') bracketCount++;
				else if (c == ']') bracketCount--;
				else if ((c == ',' || c == '}' || c == ']') && braceCount == 0 && bracketCount == 0)
				{
					break;
				}
			}

			end++;
		}

		return TrimString(json.substr(start, end - start));
	}

	bool StringToBool(const std::string& value)
	{
		std::string trimmed = TrimString(value);
		return trimmed == "true" || trimmed == "True" || trimmed == "TRUE" || trimmed == "1";
	}

public:
	ConfigManager()
	{
		m_jsonContent = ReadJsonFile(CONFIG_PATH);
	}

	bool GetBoolConfig(const std::string& key)
	{
		std::string configSection = GetJsonValue(m_jsonContent, "config");
		if (configSection.empty())
		{
			Logger::Error("Could not find 'config' section in JSON");
			return false;
		}

		std::string value = GetJsonValue(configSection, key);
		if (value.empty())
		{
			Logger::Error("Could not find key '" + key + "' in config section");
			return false;
		}

		return StringToBool(value);
	}

	std::string GetStringConfig(const std::string& key)
	{
		std::string configSection = GetJsonValue(m_jsonContent, "config");
		if (configSection.empty())
		{
			Logger::Error("Could not find 'config' section in JSON");
			return "";
		}

		std::string value = GetJsonValue(configSection, key);
		return TrimString(value, "\"");
	}

	bool IsConfigValid() const
	{
		return !m_jsonContent.empty();
	}

private:
	std::string TrimString(const std::string& str, const std::string& trimChars)
	{
		size_t first = str.find_first_not_of(trimChars);
		if (first == std::string::npos) return "";
		size_t last = str.find_last_not_of(trimChars);
		return str.substr(first, last - first + 1);
	}
};
