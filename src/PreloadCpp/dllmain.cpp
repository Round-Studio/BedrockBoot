#include "pch.h"
#include <shellapi.h>
#include <algorithm>
#include <iostream>
#include <stdio.h>
#include <fstream>
#include <vector>
#include <filesystem>
#include "detours.h"
#include "redirctor.h"
#include "logger.h"
#include "ConfigManager.h"
#pragma comment(lib, "detours.lib")
fs::path g_logicalBaseDir;
HANDLE g_localDataHandle = INVALID_HANDLE_VALUE;
std::mutex g_handleMutex;
bool g_hooksInstalled = false;
ConfigManager g_configManager;
bool isOutFileHook = g_configManager.GetBoolConfig("isDetailedLog");

std::string bbFolder = "config/BedrockBoot2/isolation";

NtCreateFile_t OriginalNtCreateFile = nullptr;
NtOpenFile_t OriginalNtOpenFile = nullptr;
NtQueryAttributesFile_t OriginalNtQueryAttributesFile = nullptr;
NtQueryFullAttributesFile_t OriginalNtQueryFullAttributesFile = nullptr;
NtSetInformationFile_t OriginalNtSetInformationFile = nullptr;
NtDeleteFile_t OriginalNtDeleteFile = nullptr;
NtQueryDirectoryFile_t OriginalNtQueryDirectoryFile = nullptr;
NtCreateSection_t OriginalNtCreateSection = nullptr;

std::wstring GetRedirectedRelativePath(const std::wstring& originalPath)
{
    const std::vector<std::wstring> keywords = {
    	L"AppData\\Roaming\\Minecraft Bedrock",
		L"AppData\\Local\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe",
		L"AppData\\Local\\Packages\\Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
		L"AppData\\Local\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\LocalState",
    	L"AppData\\Local\\Packages\\Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe\\LocalState",
        L"AppData\\Roaming\\Minecraft Bedrock Preview"
    };

    const std::vector<std::wstring> excludedPatterns = {
        L"AC",
        L"LocalCache",
        L"SystemAppData",
        L"Settings",
        L"TempState",
        L"RoamingState"
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

    size_t firstSlashPos = relativePart.find(L'\\');
    std::wstring topLevelFolder;

    if (firstSlashPos != std::wstring::npos)
    {
        topLevelFolder = relativePart.substr(0, firstSlashPos);
    }
    else
    {
        topLevelFolder = relativePart;
    }

    std::wstring lowerTopLevel = topLevelFolder;
    std::transform(lowerTopLevel.begin(), lowerTopLevel.end(), lowerTopLevel.begin(), ::towlower);

    for (const auto& pattern : excludedPatterns)
    {
        std::wstring lowerPattern = pattern;
        std::transform(lowerPattern.begin(), lowerPattern.end(), lowerPattern.begin(), ::towlower);

        if (lowerTopLevel == lowerPattern)
        {
            return L"";
        }
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
	
	if (g_configManager.GetStringConfig("folderPolicyString") == "shares")
	{
		int versionType = g_configManager.GetInfoInt("versionType");
		if (versionType == 0 || versionType == 2)
		{
			g_logicalBaseDir = exePath.parent_path() / "Minecraft Bedrock Preview";
		}
		else if (versionType == 1)
		{
			g_logicalBaseDir = exePath.parent_path() / "Minecraft Bedrock";
		}
	}
	
	if (g_configManager.GetStringConfig("folderPolicyString") == "independence")
	{
		g_logicalBaseDir = exePath.parent_path() / bbFolder;
	}

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

std::string WStringToString(const std::wstring& wstr) {
	if (wstr.empty()) return std::string();

	int size_needed = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), NULL, 0, NULL, NULL);

	std::string strTo(size_needed, 0);
	WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &strTo[0], size_needed, NULL, NULL);

	return strTo;
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


bool ApplyRedirection(POBJECT_ATTRIBUTES objectAttributes, RedirectContext& context, bool& isRedirected, std::string opType)
{
	isRedirected = false; 
	
	if (objectAttributes && objectAttributes->ObjectName && isOutFileHook) {
		std::wstring path(objectAttributes->ObjectName->Buffer, objectAttributes->ObjectName->Length / sizeof(wchar_t));
		Logger::Info(opType + ": " + WStringToString(path));
	}

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

	ULONG originalCreateOptions = CreateOptions;

	if (ApplyRedirection(ObjectAttributes, context, isRedirected, "NtCreateFile"))
	{
		actualAttributes = &context.objectAttributes;

		if (isRedirected)
		{
			std::wstring relativePath(context.wideBuffer.data());

			if (IsDirectory(relativePath))
			{
				CreateOptions &= ~0x00000040;
				CreateOptions |= 0x00000001;
			}
			else
			{
				if (!relativePath.empty() && relativePath.back() == L'\\')
				{
					CreateOptions &= ~0x00000040;
					CreateOptions |= 0x00000001;
				}
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

	if (ApplyRedirection(ObjectAttributes, context, isRedirected, "NtOpenFile"))
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

	ApplyRedirection(ObjectAttributes, context, isRedirected,"NtQueryAttributesFile");
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

	ApplyRedirection(ObjectAttributes, context, isRedirected,"NtQueryFullAttributesFile");
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

NTSTATUS NTAPI HookedNtQueryDirectoryFile(
	HANDLE FileHandle,
	HANDLE Event,
	PIO_APC_ROUTINE ApcRoutine,
	PVOID ApcContext,
	PIO_STATUS_BLOCK IoStatusBlock,
	PVOID FileInformation,
	ULONG Length,
	FILE_INFORMATION_CLASS FileInformationClass,
	BOOLEAN ReturnSingleEntry,
	PUNICODE_STRING FileName,
	BOOLEAN RestartScan
)
{
	if (FileName && FileName->Buffer)
	{
		std::wstring queryPath(FileName->Buffer, FileName->Length / sizeof(wchar_t));
	}

	return OriginalNtQueryDirectoryFile(
		FileHandle, Event, ApcRoutine, ApcContext, IoStatusBlock,
		FileInformation, Length, FileInformationClass, ReturnSingleEntry,
		FileName, RestartScan
	);
}

NTSTATUS NTAPI HookedNtCreateSection(
	PHANDLE SectionHandle,
	ACCESS_MASK DesiredAccess,
	POBJECT_ATTRIBUTES ObjectAttributes,
	PLARGE_INTEGER MaximumSize,
	ULONG PageProtection,
	ULONG AllocationAttributes,
	HANDLE FileHandle
)
{
	RedirectContext context;
	bool isRedirected = false;

	if (ObjectAttributes && ObjectAttributes->ObjectName)
	{
		std::wstring originalPath(
			ObjectAttributes->ObjectName->Buffer,
			ObjectAttributes->ObjectName->Length / sizeof(wchar_t)
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

				OBJECT_ATTRIBUTES newAttr = *ObjectAttributes;
				newAttr.ObjectName = &context.unicodeString;
				newAttr.RootDirectory = rootHandle;

				return OriginalNtCreateSection(
					SectionHandle, DesiredAccess, &newAttr,
					MaximumSize, PageProtection, AllocationAttributes, FileHandle
				);
			}
		}
	}

	return OriginalNtCreateSection(
		SectionHandle, DesiredAccess, ObjectAttributes,
		MaximumSize, PageProtection, AllocationAttributes, FileHandle
	);
}

NTSTATUS NTAPI HookedNtDeleteFile(
	POBJECT_ATTRIBUTES ObjectAttributes
)
{
	RedirectContext context;
	bool isRedirected = false;
	POBJECT_ATTRIBUTES actualAttributes = ObjectAttributes;

	ApplyRedirection(ObjectAttributes, context, isRedirected,"NtDeleteFile");
	if (isRedirected)
	{
		actualAttributes = &context.objectAttributes;
	}

	return OriginalNtDeleteFile(actualAttributes);
}

namespace fs = std::filesystem;
typedef BOOL(WINAPI* DLL_MAIN_PROC)(
	HINSTANCE hinstDLL,
	DWORD fdwReason,
	LPVOID lpvReserved
	);

extern "C" __declspec(dllexport) void Load()
{
	Logger::Info("BedrockBoot Injecting!");
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

	Logger::Info("Loading DLLs from: " + preloadDir);

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
					Logger::Info("Loading DLL: " + filename + "...");

					HMODULE hModule = LoadLibraryA(path.c_str());
					if (hModule)
					{
						FARPROC dllMain = GetProcAddress(hModule, "DllMain");

						loadedModules.push_back(hModule);
						count++;
						Logger::Success("Success for loading DLL: " + filename);
					}
					else
					{
						Logger::Error("FAILED Error:" + GetLastError()); 
					}
				}
			}
		}
	}
	catch (const std::exception& e)
	{
		Logger::Error(std::string("Error: ") + e.what());
	}

	Logger::Success("Successfully loaded " + std::to_string(count) + " DLL(s)");
	return count;
}

bool SetExeDirectoryAsWorkingDir()
{
	char exePath[MAX_PATH] = { 0 };
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

	char currentDir[MAX_PATH] = { 0 };
	DWORD dirLength = GetCurrentDirectoryA(MAX_PATH, currentDir);

	if (dirLength > 0)
	{
		return true;
	}

	return false;
}

inline void PrintBanner()
{
	std::string banner = R"(
  ____           _                 _     ____              _   
 | __ )  ___  __| |_ __ ___   ___ | | __| __ )  ___   ___ | |_ 
 |  _ \ / _ \/ _` | '__/ _ \ / _ \| |/ /|  _ \ / _ \ / _ \| __|
 | |_) |  __/ (_| | | | (_) | (_) |   < | |_) | (_) | (_) | |_ 
 |____/ \___|\__,_|_|  \___/ \___/|_|\_\|____/ \___/ \___/ \__|

)";

	std::stringstream ss(banner);
	std::string line;

	while (std::getline(ss, line))
	{
		Logger::Info(line);
	}
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
		if (g_configManager.GetBoolConfig("isConsole"))
		{
			AllocConsole();
			system("title Minecraft Bedrock Console");
			FILE* fDummy;
			freopen_s(&fDummy, "CONOUT$", "w", stdout);
			freopen_s(&fDummy, "CONOUT$", "w", stderr);
			freopen_s(&fDummy, "CONIN$", "r", stdin);
			Logger::Initialize();
			PrintBanner();

			Logger::Success("BedrockBoot is free software licensed under GPLv3");
			Logger::Success("Submit issues and submit PR: https://github.com/Round-Studio/BedrockBoot");
		}
		if (g_configManager.GetBoolConfig("isVersionIsolated"))
		{
			Logger::Info("Initializing File Hook.");
			HMODULE ntdll = GetModuleHandleW(L"ntdll.dll");
			if (!ntdll)
			{
				Logger::Error("Get ntdll pt error");
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
			OriginalNtQueryDirectoryFile = reinterpret_cast<NtQueryDirectoryFile_t>(GetProcAddress(ntdll, "NtQueryDirectoryFile"));
			OriginalNtCreateSection = reinterpret_cast<NtCreateSection_t>(GetProcAddress(ntdll, "NtCreateSection"));

			Logger::Info("NtCreateFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtCreateFile)));
			Logger::Info("NtOpenFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtOpenFile)));
			Logger::Info("NtQueryAttributesFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtQueryAttributesFile)));
			Logger::Info("NtQueryFullAttributesFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtQueryFullAttributesFile)));
			Logger::Info("NtSetInformationFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtSetInformationFile)));
			Logger::Info("NtDeleteFile addr: " + std::to_string(reinterpret_cast<unsigned long long>(OriginalNtDeleteFile)));

			DetourTransactionBegin();
			DetourUpdateThread(GetCurrentThread());

			DetourAttach(&(PVOID&)OriginalNtCreateFile, HookedNtCreateFile);
			DetourAttach(&(PVOID&)OriginalNtOpenFile, HookedNtOpenFile);
			DetourAttach(&(PVOID&)OriginalNtQueryAttributesFile, HookedNtQueryAttributesFile);
			DetourAttach(&(PVOID&)OriginalNtQueryFullAttributesFile, HookedNtQueryFullAttributesFile);
			DetourAttach(&(PVOID&)OriginalNtSetInformationFile, HookedNtSetInformationFile);
			DetourAttach(&(PVOID&)OriginalNtDeleteFile, HookedNtDeleteFile);
			DetourAttach(&(PVOID&)OriginalNtQueryDirectoryFile, HookedNtQueryDirectoryFile);
			DetourAttach(&(PVOID&)OriginalNtCreateSection, HookedNtCreateSection);

			LONG error = DetourTransactionCommit();
			if (error == NO_ERROR)
			{
				g_hooksInstalled = true;
				Logger::Success("File Redirector Hooked Successfully. Attached: 6");
			}
			else
			{
				Logger::Error("DetourTransactionCommit failed with error: " + std::to_string(error));
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
