#pragma once
#define WIN32_LEAN_AND_MEAN
#define _CRT_SECURE_NO_WARNINGS

#include <windows.h>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>
#include <mutex>

namespace fs = std::filesystem;

#ifndef _UNICODE_STRING_DEFINED
typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
    PWSTR Buffer;
} UNICODE_STRING, * PUNICODE_STRING;
#define _UNICODE_STRING_DEFINED
#endif

#ifndef _OBJECT_ATTRIBUTES_DEFINED
typedef struct _OBJECT_ATTRIBUTES {
    ULONG Length;
    HANDLE RootDirectory;
    PUNICODE_STRING ObjectName;
    ULONG Attributes;
    PVOID SecurityDescriptor;
    PVOID SecurityQualityOfService;
} OBJECT_ATTRIBUTES, * POBJECT_ATTRIBUTES;
#define _OBJECT_ATTRIBUTES_DEFINED
#endif

#ifndef _IO_STATUS_BLOCK_DEFINED
typedef struct _IO_STATUS_BLOCK {
    union {
        LONG Status;
        PVOID Pointer;
    };
    ULONG_PTR Information;
} IO_STATUS_BLOCK, * PIO_STATUS_BLOCK;
#define _IO_STATUS_BLOCK_DEFINED
#endif

#ifndef _FILE_RENAME_INFORMATION_DEFINED
typedef struct _FILE_RENAME_INFORMATION {
    union {
        BOOLEAN ReplaceIfExists;
        ULONG Flags;
    };
    HANDLE RootDirectory;
    ULONG FileNameLength;
    WCHAR FileName[1];
} FILE_RENAME_INFORMATION, * PFILE_RENAME_INFORMATION;
#define _FILE_RENAME_INFORMATION_DEFINED
#endif

typedef LONG NTSTATUS;
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)

#ifndef _FILE_INFORMATION_CLASS_DEFINED
typedef enum _FILE_INFORMATION_CLASS {
    FileDirectoryInformation = 1,
    FileFullDirectoryInformation,
    FileBothDirectoryInformation,
    FileBasicInformation,
    FileStandardInformation,
    FileInternalInformation,
    FileEaInformation,
    FileAccessInformation,
    FileNameInformation,
    FileRenameInformation,
    FileLinkInformation,
    FileNamesInformation,
    FileDispositionInformation,
    FilePositionInformation,
    FileFullEaInformation,
    FileModeInformation,
    FileAlignmentInformation,
    FileAllInformation,
    FileAllocationInformation,
    FileEndOfFileInformation,
    FileAlternateNameInformation,
    FileStreamInformation,
    FilePipeInformation,
    FilePipeLocalInformation,
    FilePipeRemoteInformation,
    FileMailslotQueryInformation,
    FileMailslotSetInformation,
    FileCompressionInformation,
    FileObjectIdInformation,
    FileCompletionInformation,
    FileMoveClusterInformation,
    FileQuotaInformation,
    FileReparsePointInformation,
    FileNetworkOpenInformation,
    FileAttributeTagInformation,
    FileTrackingInformation,
    FileIdBothDirectoryInformation,
    FileIdFullDirectoryInformation,
    FileValidDataLengthInformation,
    FileShortNameInformation,
    FileIoCompletionNotificationInformation,
    FileIoStatusBlockRangeInformation,
    FileIoPriorityHintInformation,
    FileSfioReserveInformation,
    FileSfioVolumeInformation,
    FileHardLinkInformation,
    FileProcessIdsUsingFileInformation,
    FileNormalizedNameInformation,
    FileNetworkPhysicalNameInformation,
    FileIdGlobalTxDirectoryInformation,
    FileIsRemoteDeviceInformation,
    FileUnusedInformation,
    FileNumaNodeInformation,
    FileStandardLinkInformation,
    FileRemoteProtocolInformation,
    FileRenameInformationBypassAccessCheck,
    FileLinkInformationBypassAccessCheck,
    FileVolumeNameInformation,
    FileIdInformation,
    FileIdExtdDirectoryInformation,
    FileReplaceCompletionInformation,
    FileHardLinkFullIdInformation,
    FileIdExtdBothDirectoryInformation,
    FileDispositionInformationEx,
    FileRenameInformationEx,
    FileRenameInformationExBypassAccessCheck,
    FileDesiredStorageClassInformation,
    FileStatInformation,
    FileMemoryPartitionInformation,
    FileStatLxInformation,
    FileCaseSensitiveInformation,
    FileLinkInformationEx,
    FileLinkInformationExBypassAccessCheck,
    FileStorageReserveIdInformation,
    FileCaseSensitiveInformationForceAccessCheck,
    FileMaximumInformation
} FILE_INFORMATION_CLASS;
#define _FILE_INFORMATION_CLASS_DEFINED
#endif

typedef NTSTATUS(NTAPI* NtCreateFile_t)(
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
    );

typedef NTSTATUS(NTAPI* NtOpenFile_t)(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK IoStatusBlock,
    ULONG ShareAccess,
    ULONG OpenOptions
    );

typedef NTSTATUS(NTAPI* NtQueryAttributesFile_t)(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
    );

typedef NTSTATUS(NTAPI* NtQueryFullAttributesFile_t)(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
    );

typedef NTSTATUS(NTAPI* NtSetInformationFile_t)(
    HANDLE FileHandle,
    PIO_STATUS_BLOCK IoStatusBlock,
    PVOID FileInformation,
    ULONG Length,
    FILE_INFORMATION_CLASS FileInformationClass
    );

typedef NTSTATUS(NTAPI* NtDeleteFile_t)(
    POBJECT_ATTRIBUTES ObjectAttributes
    );

typedef VOID(NTAPI* PIO_APC_ROUTINE)(
    PVOID ApcContext,
    PIO_STATUS_BLOCK IoStatusBlock,
    ULONG Reserved
    );

typedef NTSTATUS(NTAPI* NtQueryDirectoryFile_t)(
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
    );

typedef NTSTATUS(NTAPI* NtCreateSection_t)(
    PHANDLE SectionHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PLARGE_INTEGER MaximumSize,
    ULONG PageProtection,
    ULONG AllocationAttributes,
    HANDLE FileHandle
    );

struct Config {
    bool enable_debug_console = false;
    bool enable_redirection = false;
    std::wstring base_directory = L"Minecraft Bedrock";
};

struct RedirectContext {
    std::vector<wchar_t> wideBuffer;
    UNICODE_STRING unicodeString;
    OBJECT_ATTRIBUTES objectAttributes;

    RedirectContext() {
        memset(&unicodeString, 0, sizeof(unicodeString));
        memset(&objectAttributes, 0, sizeof(objectAttributes));
    }
};

extern fs::path g_logicalBaseDir;
extern HANDLE g_localDataHandle;
extern std::mutex g_handleMutex;
extern bool g_hooksInstalled;

extern NtCreateFile_t OriginalNtCreateFile;
extern NtOpenFile_t OriginalNtOpenFile;
extern NtQueryAttributesFile_t OriginalNtQueryAttributesFile;
extern NtQueryFullAttributesFile_t OriginalNtQueryFullAttributesFile;
extern NtSetInformationFile_t OriginalNtSetInformationFile;
extern NtDeleteFile_t OriginalNtDeleteFile;
extern NtQueryDirectoryFile_t OriginalNtQueryDirectoryFile;
extern NtCreateSection_t OriginalNtCreateSection;

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
);

NTSTATUS NTAPI HookedNtOpenFile(
    PHANDLE FileHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK IoStatusBlock,
    ULONG ShareAccess,
    ULONG OpenOptions
);

NTSTATUS NTAPI HookedNtQueryAttributesFile(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
);

NTSTATUS NTAPI HookedNtQueryFullAttributesFile(
    POBJECT_ATTRIBUTES ObjectAttributes,
    PVOID FileInformation
);

NTSTATUS NTAPI HookedNtSetInformationFile(
    HANDLE FileHandle,
    PIO_STATUS_BLOCK IoStatusBlock,
    PVOID FileInformation,
    ULONG Length,
    FILE_INFORMATION_CLASS FileInformationClass
);

NTSTATUS NTAPI HookedNtDeleteFile(
    POBJECT_ATTRIBUTES ObjectAttributes
);

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
);

NTSTATUS NTAPI HookedNtCreateSection(
    PHANDLE SectionHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PLARGE_INTEGER MaximumSize,
    ULONG PageProtection,
    ULONG AllocationAttributes,
    HANDLE FileHandle
);