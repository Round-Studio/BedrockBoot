
#include "pch.h"
#include <shellapi.h>
#include <algorithm>
#include <iostream>
#include <windows.h>
#include <stdio.h>
#include <fstream>
#include <vector>
#include <filesystem>

#include "detours.h"
#include "redirctor.h"
#pragma comment(lib, "detours.lib")
fs::path g_logicalBaseDir;
HANDLE g_localDataHandle = INVALID_HANDLE_VALUE;
std::mutex g_handleMutex;
bool g_hooksInstalled = false;

NtCreateFile_t OriginalNtCreateFile = nullptr;
NtOpenFile_t OriginalNtOpenFile = nullptr;
NtQueryAttributesFile_t OriginalNtQueryAttributesFile = nullptr;
NtQueryFullAttributesFile_t OriginalNtQueryFullAttributesFile = nullptr;
NtSetInformationFile_t OriginalNtSetInformationFile = nullptr;
NtDeleteFile_t OriginalNtDeleteFile = nullptr;

std::wstring GetRedirectedRelativePath(const std::wstring& originalPath)
{
	const std::vector<std::wstring> keywords = {
		L"AppData\\Roaming\\Minecraft Bedrock",
		L"AppData\\Local\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe",
		L"AppData\\Local\\Packages\\Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
		L"AppData\\Roaming\\Minecraft Bedrock Preview"
	};


	std::wstring matchedKeyword;
	size_t pos = std::wstring::npos;

	for (const auto& keyword : keywords)
	{
		size_t foundPos = originalPath.find(keyword);
		if (foundPos != std::wstring::npos)
		{
			pos = foundPos;
			matchedKeyword = keyword;
			break; 
		}
	}

	if (pos == std::wstring::npos)
	{
		return L"";
	}

	std::wstring relativePart = originalPath.substr(pos + matchedKeyword.length());

	while (!relativePart.empty() &&
		(relativePart[0] == L'\\' || relativePart[0] == L'/'))
	{
		relativePart.erase(0, 1);
	}

	if (relativePart.empty())
	{
		return L"";
	}
	for (wchar_t& c : relativePart)
	{
		if (c == L'/') c = L'\\';
	}

	fs::path fullTarget = g_logicalBaseDir / relativePart;
	fs::path parentDir = fullTarget.parent_path();

	if (!parentDir.empty() && !fs::exists(parentDir))
	{
		try
		{
			fs::create_directories(parentDir);
		}
		catch (...)
		{
		}
	}

	return relativePart;
}

void InitializeBaseDir()
{
	wchar_t modulePath[MAX_PATH];
	GetModuleFileNameW(nullptr, modulePath, MAX_PATH);
	fs::path exePath = modulePath;
	g_logicalBaseDir = exePath.parent_path() / "Minecraft Bedrock";

	if (!fs::exists(g_logicalBaseDir))
	{
		try
		{
			fs::create_directories(g_logicalBaseDir);
		}
		catch (const std::exception& e)
		{
		}
	}
}

HANDLE GetLocalDataRoot()
{
	std::lock_guard<std::mutex> lock(g_handleMutex);

	if (g_localDataHandle != INVALID_HANDLE_VALUE)
	{
		return g_localDataHandle;
	}

	InitializeBaseDir();

	std::wstring dirPath = g_logicalBaseDir.wstring();
	g_localDataHandle = CreateFileW(
		dirPath.c_str(),
		FILE_LIST_DIRECTORY,
		FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
		nullptr,
		OPEN_EXISTING,
		FILE_FLAG_BACKUP_SEMANTICS,
		nullptr
	);

	if (g_localDataHandle == INVALID_HANDLE_VALUE)
	{
		DWORD error = GetLastError();
	}
	else
	{
	}

	return g_localDataHandle;
}


bool ApplyRedirection(POBJECT_ATTRIBUTES objectAttributes, RedirectContext& context, bool& isRedirected)
{
	isRedirected = false;

	if (!objectAttributes || !objectAttributes->ObjectName || !objectAttributes->ObjectName->Buffer)
	{
		return false;
	}
	std::wstring originalPath(
		objectAttributes->ObjectName->Buffer,
		objectAttributes->ObjectName->Length / sizeof(wchar_t)
	);

	std::wstring relativePath = GetRedirectedRelativePath(originalPath);

	if (!relativePath.empty())
	{
		HANDLE rootHandle = GetLocalDataRoot();
		if (rootHandle != INVALID_HANDLE_VALUE)
		{
			isRedirected = true;
			context.wideBuffer.assign(relativePath.begin(), relativePath.end());
			context.wideBuffer.push_back(L'\0');

			context.unicodeString.Length = static_cast<USHORT>(relativePath.length() * sizeof(wchar_t));
			context.unicodeString.MaximumLength = static_cast<USHORT>(context.wideBuffer.size() * sizeof(wchar_t));
			context.unicodeString.Buffer = context.wideBuffer.data();

			context.objectAttributes = *objectAttributes;
			context.objectAttributes.Attributes = 0x00000040; 
			context.objectAttributes.ObjectName = &context.unicodeString;
			context.objectAttributes.RootDirectory = rootHandle;
			context.objectAttributes.SecurityDescriptor = nullptr;

			return true;
		}
	}

	return false;
}

bool IsDirectory(const std::wstring& relativePath)
{
	if (relativePath.empty())
	{
		return true;
	}

	fs::path fullPath = g_logicalBaseDir / relativePath;
	return fs::is_directory(fullPath);
}


NTSTATUS NTAPI HookedNtCreateFile(
	PHANDLE FileHandle,
	ACCESS_MASK DesiredAccess,
	POBJECT_ATTRIBUTES ObjectAttributes,
	PIO_STATUS_BLOCK IoStatusBlock,
	PLARGE_INTEGER AllocationSize,
	ULONG FileAttributes,
	ULONG ShareAccess,
	ULONG CreateDisposition,
	ULONG CreateOptions,
	PVOID EaBuffer,
	ULONG EaLength
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	if (ApplyRedirection(ObjectAttributes, context, isRedirected))
	{
		actualAttributes = &context.objectAttributes;

		if (isRedirected)
		{
			bool isDir = IsDirectory(context.wideBuffer.data());
			if (isDir)
			{
				CreateOptions &= ~0x00000040; 
				CreateOptions |= 0x00000001;
			}
		}
	}

	return OriginalNtCreateFile(
		FileHandle, DesiredAccess, actualAttributes, IoStatusBlock,
		AllocationSize, FileAttributes, ShareAccess, CreateDisposition,
		CreateOptions, EaBuffer, EaLength
	);
}

NTSTATUS NTAPI HookedNtOpenFile(
	PHANDLE FileHandle,
	ACCESS_MASK DesiredAccess,
	POBJECT_ATTRIBUTES ObjectAttributes,
	PIO_STATUS_BLOCK IoStatusBlock,
	ULONG ShareAccess,
	ULONG OpenOptions
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	if (ApplyRedirection(ObjectAttributes, context, isRedirected))
	{
		actualAttributes = &context.objectAttributes;

		if (isRedirected)
		{
			bool isDir = IsDirectory(context.wideBuffer.data());
			if (isDir)
			{
				OpenOptions &= ~0x00000040;
				OpenOptions |= 0x00000001;
			}
		}
	}

	return OriginalNtOpenFile(
		FileHandle, DesiredAccess, actualAttributes,
		IoStatusBlock, ShareAccess, OpenOptions
	);
}

NTSTATUS NTAPI HookedNtQueryAttributesFile(
	POBJECT_ATTRIBUTES ObjectAttributes,
	PVOID FileInformation
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	ApplyRedirection(ObjectAttributes, context, isRedirected);
	if (isRedirected)
	{
		actualAttributes = &context.objectAttributes;
	}

	return OriginalNtQueryAttributesFile(actualAttributes, FileInformation);
}

NTSTATUS NTAPI HookedNtQueryFullAttributesFile(
	POBJECT_ATTRIBUTES ObjectAttributes,
	PVOID FileInformation
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	ApplyRedirection(ObjectAttributes, context, isRedirected);
	if (isRedirected)
	{
		actualAttributes = &context.objectAttributes;
	}

	return OriginalNtQueryFullAttributesFile(actualAttributes, FileInformation);
}

NTSTATUS NTAPI HookedNtSetInformationFile(
	HANDLE FileHandle,
	PIO_STATUS_BLOCK IoStatusBlock,
	PVOID FileInformation,
	ULONG Length,
	FILE_INFORMATION_CLASS FileInformationClass
)
{
	if (FileInformationClass == FileRenameInformation || FileInformationClass == FileRenameInformationEx)
	{
		PFILE_RENAME_INFORMATION renameInfo = reinterpret_cast<PFILE_RENAME_INFORMATION>(FileInformation);

		if (renameInfo && renameInfo->FileNameLength > 0)
		{
			std::wstring originalPath(
				renameInfo->FileName,
				renameInfo->FileNameLength / sizeof(wchar_t)
			);

			std::wstring relativePath = GetRedirectedRelativePath(originalPath);

			if (!relativePath.empty())
			{
				HANDLE rootHandle = GetLocalDataRoot();
				if (rootHandle != INVALID_HANDLE_VALUE)
				{
					size_t newSize = sizeof(FILE_RENAME_INFORMATION) +
						(relativePath.length() * sizeof(wchar_t));

					std::vector<BYTE> newBuffer(newSize);
					PFILE_RENAME_INFORMATION newInfo =
						reinterpret_cast<PFILE_RENAME_INFORMATION>(newBuffer.data());

					newInfo->ReplaceIfExists = renameInfo->ReplaceIfExists;
					newInfo->RootDirectory = rootHandle;
					newInfo->FileNameLength = static_cast<ULONG>(relativePath.length() * sizeof(wchar_t));

					memcpy_s(
						newInfo->FileName,
						newInfo->FileNameLength,
						relativePath.c_str(),
						newInfo->FileNameLength
					);

					return OriginalNtSetInformationFile(
						FileHandle, IoStatusBlock, newBuffer.data(),
						static_cast<ULONG>(newSize), FileInformationClass
					);
				}
			}
		}
	}

	return OriginalNtSetInformationFile(
		FileHandle, IoStatusBlock, FileInformation, Length, FileInformationClass
	);
}

NTSTATUS NTAPI HookedNtDeleteFile(
	POBJECT_ATTRIBUTES ObjectAttributes
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	ApplyRedirection(ObjectAttributes, context, isRedirected);
	if (isRedirected)
	{
		actualAttributes = &context.objectAttributes;
	}

	return OriginalNtDeleteFile(actualAttributes);
}

namespace fs = std::filesystem;
typedef BOOL (WINAPI*DLL_MAIN_PROC)(
	HINSTANCE hinstDLL,
	DWORD fdwReason,
	LPVOID lpvReserved
);

extern "C" __declspec(dllexport) void Load()
{
	std::cout << "BedrockBoot Injecting!" << std::endl;
}

int LoadPreloadDlls(HINSTANCE hinstDLL,
                    DWORD fdwReason,
                    LPVOID lpvReserved)
{
	std::string preloadDir;
	char currentDir[MAX_PATH];
	GetCurrentDirectoryA(MAX_PATH, currentDir);
	preloadDir = std::string(currentDir) + "\\preload";


	if (!fs::exists(preloadDir) || !fs::is_directory(preloadDir))
	{
		fs::create_directory("preload");
	}

	std::vector<HMODULE> loadedModules;
	int count = 0;

	std::cout << "Loading DLLs from: " << preloadDir << std::endl;

	try
	{
		for (const auto& entry : fs::directory_iterator(preloadDir))
		{
			if (entry.is_regular_file())
			{
				std::string path = entry.path().string();
				std::string ext = entry.path().extension().string();

				std::string lowerExt = ext;
				std::transform(lowerExt.begin(), lowerExt.end(), lowerExt.begin(), ::tolower);

				if (lowerExt == ".dll")
				{
					std::string filename = entry.path().filename().string();
					std::cout << "  -> " << filename << "... ";

					HMODULE hModule = LoadLibraryA(path.c_str());
					if (hModule)
					{
						FARPROC dllMain = GetProcAddress(hModule, "DllMain");

						loadedModules.push_back(hModule);
						count++;
						std::cout << "OK" << std::endl;
					}
					else
					{
						std::cout << "FAILED (Error: " << GetLastError() << ")" << std::endl;
					}
				}
			}
		}
	}
	catch (const std::exception& e)
	{
		std::cerr << "Error: " << e.what() << std::endl;
	}

	std::cout << "\nSuccessfully loaded " << count << " DLL(s)" << std::endl;
	return count;
}
void createDefaultConfig(const std::string& filename)
{
	std::ofstream config(filename);
	config << "# 游戏配置文件\n";
	config << "# 重定向功能 (0=禁用, 1=启用)\n";
	config << "redirctor = 0\n\n";
	config << "# 控制台输出 (0=禁用, 1=启用)\n";
	config << "console_open = 0\n";
	config.close();
	std::cout << "配置文件 " << filename << " 不存在，已创建默认配置" << std::endl;
}

bool getConfigValue(const std::string& filename, const std::string& key, bool defaultValue = false)
{
	if (!fs::exists(filename))
	{
		createDefaultConfig(filename);
		return defaultValue;
	}

	std::ifstream file(filename);
	if (!file.is_open())
	{
		std::cerr << "无法打开配置文件: " << filename << std::endl;
		return defaultValue;
	}

	std::string line;
	while (std::getline(file, line))
	{
		line.erase(0, line.find_first_not_of(" \t"));
		line.erase(line.find_last_not_of(" \t") + 1);

		if (line.empty() || line[0] == '#')
		{
			continue;
		}

		std::string lowerLine = line;
		std::transform(lowerLine.begin(), lowerLine.end(), lowerLine.begin(),
		               [](unsigned char c) { return std::tolower(c); });

		std::string lowerKey = key;
		std::transform(lowerKey.begin(), lowerKey.end(), lowerKey.begin(),
		               [](unsigned char c) { return std::tolower(c); });

		if (lowerLine.find(lowerKey) != std::string::npos)
		{
			lowerLine.erase(std::remove(lowerLine.begin(), lowerLine.end(), ' '), lowerLine.end());
			lowerLine.erase(std::remove(lowerLine.begin(), lowerLine.end(), '\t'), lowerLine.end());

			size_t pos = lowerLine.find(lowerKey + "=");
			if (pos != std::string::npos)
			{
				std::string value = lowerLine.substr(pos + lowerKey.length() + 1);
				file.close();

				if (value == "1")
				{
					return true;
				}
				else if (value == "0")
				{
					return false;
				}
			}
		}
	}

	file.close();
	return defaultValue;
}
bool isRedirectorEnabled()
{
	return getConfigValue("game.conf", "redirctor", false);
}
bool isConsoleOpenEnabled()
{
	return getConfigValue("game.conf", "console_open", false);
}

void setConfigValue(const std::string& filename, const std::string& key, bool value)
{
	if (!fs::exists(filename))
	{
		createDefaultConfig(filename);
	}

	std::vector<std::string> lines;
	bool keyFound = false;
	std::ifstream inFile(filename);
	std::string line;
	while (std::getline(inFile, line))
	{
		std::string lowerLine = line;
		std::transform(lowerLine.begin(), lowerLine.end(), lowerLine.begin(),
		               [](unsigned char c) { return std::tolower(c); });

		std::string lowerKey = key;
		std::transform(lowerKey.begin(), lowerKey.end(), lowerKey.begin(),
		               [](unsigned char c) { return std::tolower(c); });
		if (lowerLine.find(lowerKey) != std::string::npos &&
			lowerLine.find("=") != std::string::npos &&
			lowerLine[0] != '#')
		{
			std::string newLine = key + " = " + (value ? "1" : "0");
			lines.push_back(newLine);
			keyFound = true;
		}
		else
		{
			lines.push_back(line);
		}
	}
	inFile.close();

	if (!keyFound)
	{
		lines.push_back(key + " = " + (value ? "1" : "0"));
	}

	std::ofstream outFile(filename);
	for (const auto& l : lines)
	{
		outFile << l << "\n";
	}
	outFile.close();
}

bool SetExeDirectoryAsWorkingDir()
{
	char exePath[MAX_PATH] = {0};
	DWORD pathLength = GetModuleFileNameA(NULL, exePath, MAX_PATH);

	if (pathLength == 0 || pathLength == MAX_PATH)
	{
		DWORD error = GetLastError();
		return false;
	}

	std::string exePathStr(exePath);
	size_t lastSlash = exePathStr.find_last_of("\\/");

	if (lastSlash == std::string::npos)
	{
		return false;
	}

	std::string exeDir = exePathStr.substr(0, lastSlash);

	if (!SetCurrentDirectoryA(exeDir.c_str()))
	{
		DWORD error = GetLastError();
		return false;
	}

	char currentDir[MAX_PATH] = {0};
	DWORD dirLength = GetCurrentDirectoryA(MAX_PATH, currentDir);

	if (dirLength > 0)
	{
		return true;
	}

	return false;
}

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		SetExeDirectoryAsWorkingDir();
		if (isConsoleOpenEnabled())
		{
			AllocConsole();
			FILE* fDummy;
			freopen_s(&fDummy, "CONOUT$", "w", stdout);
			freopen_s(&fDummy, "CONOUT$", "w", stderr);
			freopen_s(&fDummy, "CONIN$", "r", stdin);
		}
		if (isRedirectorEnabled())
		{
			HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
			if (!ntdll)
			{
				std::cout << "Get ntll pt error" << std::endl;
				return FALSE;
			}

			OriginalNtCreateFile = reinterpret_cast<NtCreateFile_t>(
				GetProcAddress(ntdll, "NtCreateFile"));
			OriginalNtOpenFile = reinterpret_cast<NtOpenFile_t>(
				GetProcAddress(ntdll, "NtOpenFile"));
			OriginalNtQueryAttributesFile = reinterpret_cast<NtQueryAttributesFile_t>(
				GetProcAddress(ntdll, "NtQueryAttributesFile"));
			OriginalNtQueryFullAttributesFile = reinterpret_cast<NtQueryFullAttributesFile_t>(
				GetProcAddress(ntdll, "NtQueryFullAttributesFile"));
			OriginalNtSetInformationFile = reinterpret_cast<NtSetInformationFile_t>(
				GetProcAddress(ntdll, "NtSetInformationFile"));
			OriginalNtDeleteFile = reinterpret_cast<NtDeleteFile_t>(
				GetProcAddress(ntdll, "NtDeleteFile"));
			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());

			DetourAttach(&(PVOID&)OriginalNtCreateFile, HookedNtCreateFile);
			DetourAttach(&(PVOID&)OriginalNtOpenFile, HookedNtOpenFile);
			DetourAttach(&(PVOID&)OriginalNtQueryAttributesFile, HookedNtQueryAttributesFile);
			DetourAttach(&(PVOID&)OriginalNtQueryFullAttributesFile, HookedNtQueryFullAttributesFile);
			DetourAttach(&(PVOID&)OriginalNtSetInformationFile, HookedNtSetInformationFile);
			DetourAttach(&(PVOID&)OriginalNtDeleteFile, HookedNtDeleteFile);

			LONG error = DetourTransactionCommit();
			if (error == NO_ERROR)
			{
				g_hooksInstalled = true;
			}
			else
			{
			}
		}
		Load();
		LoadPreloadDlls(hModule, ul_reason_for_call, lpReserved);
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}
