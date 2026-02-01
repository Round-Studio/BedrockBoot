using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BedrockBoot.Base.Entry.Game.Pack.ResourcePack.CurseForge;

public class CurseForgeResponse
{
    [JsonPropertyName("data")] public List<ModData> Data { get; set; }

    [JsonPropertyName("pagination")] public PaginationEntry Pagination { get; set; }

    public class Links
    {
        [JsonPropertyName("websiteUrl")] public string WebsiteUrl { get; set; }

        [JsonPropertyName("wikiUrl")] public string WikiUrl { get; set; }

        [JsonPropertyName("issuesUrl")] public string IssuesUrl { get; set; }

        [JsonPropertyName("sourceUrl")] public string SourceUrl { get; set; }
    }

    public class Category
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("gameId")] public int GameId { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("iconUrl")] public string IconUrl { get; set; }

        [JsonPropertyName("dateModified")] public DateTime DateModified { get; set; }

        [JsonPropertyName("isClass")] public bool IsClass { get; set; }

        [JsonPropertyName("classId")] public int ClassId { get; set; }

        [JsonPropertyName("parentCategoryId")] public int ParentCategoryId { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }

        [JsonPropertyName("avatarUrl")] public string AvatarUrl { get; set; }
    }

    public class Logo
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("thumbnailUrl")] public string ThumbnailUrl { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class Screenshot
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("thumbnailUrl")] public string ThumbnailUrl { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class Hash
    {
        [JsonPropertyName("value")] public string Value { get; set; }

        [JsonPropertyName("algo")] public int Algo { get; set; }
    }

    public class Dependency
    {
        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("relationType")] public int RelationType { get; set; }
    }

    public class Module
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("fingerprint")] public long Fingerprint { get; set; }
    }

    public class LatestFile
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("gameId")] public int GameId { get; set; }

        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; }

        [JsonPropertyName("displayName")] public string DisplayName { get; set; }

        [JsonPropertyName("fileName")] public string FileName { get; set; }

        [JsonPropertyName("releaseType")] public int ReleaseType { get; set; }

        [JsonPropertyName("fileStatus")] public int FileStatus { get; set; }

        [JsonPropertyName("hashes")] public List<Hash> Hashes { get; set; }

        [JsonPropertyName("fileDate")] public DateTime FileDate { get; set; }

        [JsonPropertyName("fileLength")] public long FileLength { get; set; }

        [JsonPropertyName("downloadCount")] public int DownloadCount { get; set; }

        [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }

        [JsonPropertyName("gameVersions")] public List<string> GameVersions { get; set; }

        [JsonPropertyName("sortableGameVersions")]
        public List<SortableGameVersion> SortableGameVersions { get; set; }

        [JsonPropertyName("dependencies")] public List<Dependency> Dependencies { get; set; }

        [JsonPropertyName("alternateFileId")] public int AlternateFileId { get; set; }

        [JsonPropertyName("isServerPack")] public bool IsServerPack { get; set; }

        [JsonPropertyName("fileFingerprint")] public long FileFingerprint { get; set; }

        [JsonPropertyName("modules")] public List<Module> Modules { get; set; }
    }

    public class LatestFilesIndex
    {
        [JsonPropertyName("gameVersion")] public string GameVersion { get; set; }

        [JsonPropertyName("fileId")] public int FileId { get; set; }

        [JsonPropertyName("filename")] public string Filename { get; set; }

        [JsonPropertyName("releaseType")] public int ReleaseType { get; set; }

        [JsonPropertyName("gameVersionTypeId")]
        public int GameVersionTypeId { get; set; }
    }

    public class SocialLink
    {
        [JsonPropertyName("type")] public int Type { get; set; }

        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public class ModData
    {
        [JsonPropertyName("screenshots")] public List<Screenshot> Screenshots { get; set; }

        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("gameId")] public int GameId { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("slug")] public string Slug { get; set; }

        [JsonPropertyName("links")] public Links Links { get; set; }

        [JsonPropertyName("summary")] public string Summary { get; set; }

        [JsonPropertyName("status")] public int Status { get; set; }

        [JsonPropertyName("downloadCount")] public int DownloadCount { get; set; }

        [JsonPropertyName("isFeatured")] public bool IsFeatured { get; set; }

        [JsonPropertyName("primaryCategoryId")]
        public int PrimaryCategoryId { get; set; }

        [JsonPropertyName("categories")] public List<Category> Categories { get; set; }

        [JsonPropertyName("classId")] public int ClassId { get; set; }

        [JsonPropertyName("authors")] public List<Author> Authors { get; set; }

        [JsonPropertyName("logo")] public Logo Logo { get; set; }

        [JsonPropertyName("mainFileId")] public int MainFileId { get; set; }

        [JsonPropertyName("latestFiles")] public List<LatestFile> LatestFiles { get; set; }

        [JsonPropertyName("latestFilesIndexes")]
        public List<LatestFilesIndex> LatestFilesIndexes { get; set; }

        [JsonPropertyName("latestEarlyAccessFilesIndexes")]
        public List<LatestFilesIndex> LatestEarlyAccessFilesIndexes { get; set; }

        [JsonPropertyName("dateCreated")] public DateTime DateCreated { get; set; }

        [JsonPropertyName("dateModified")] public DateTime DateModified { get; set; }

        [JsonPropertyName("dateReleased")] public DateTime DateReleased { get; set; }

        [JsonPropertyName("allowModDistribution")]
        public bool AllowModDistribution { get; set; }

        [JsonPropertyName("gamePopularityRank")]
        public int GamePopularityRank { get; set; }

        [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; }

        [JsonPropertyName("thumbsUpCount")] public int ThumbsUpCount { get; set; }

        [JsonPropertyName("socialLinks")] public List<SocialLink> SocialLinks { get; set; }

        [JsonPropertyName("featuredProjectTag")]
        public int FeaturedProjectTag { get; set; }
    }

    public class PaginationEntry
    {
        [JsonPropertyName("index")] public int Index { get; set; }

        [JsonPropertyName("pageSize")] public int PageSize { get; set; }

        [JsonPropertyName("resultCount")] public int ResultCount { get; set; }

        [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    }

    // 在 CurseForgeResponse 类所在的文件中添加这些类
    public class FileHash
    {
        [JsonPropertyName("value")] public string Value { get; set; }

        [JsonPropertyName("algo")] public int Algo { get; set; }
    }

    public class SortableGameVersion
    {
        [JsonPropertyName("gameVersionName")] public string GameVersionName { get; set; }

        [JsonPropertyName("gameVersionPadded")]
        public string GameVersionPadded { get; set; }

        [JsonPropertyName("gameVersion")] public string GameVersion { get; set; }

        [JsonPropertyName("gameVersionReleaseDate")]
        public DateTime GameVersionReleaseDate { get; set; }

        [JsonPropertyName("gameVersionTypeId")]
        public int GameVersionTypeId { get; set; }
    }

    public class SingleFileResponse
    {
        [JsonPropertyName("data")] public ModFile Data { get; set; }
    }

    public class FileDependency
    {
        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("relationType")] public int RelationType { get; set; }
    }

    public class FileModule
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("fingerprint")] public long Fingerprint { get; set; }
    }

    public class ModFile
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("gameId")] public int GameId { get; set; }

        [JsonPropertyName("modId")] public int ModId { get; set; }

        [JsonPropertyName("isAvailable")] public bool IsAvailable { get; set; }

        [JsonPropertyName("displayName")] public string DisplayName { get; set; }

        [JsonPropertyName("fileName")] public string FileName { get; set; }

        [JsonPropertyName("releaseType")] public int ReleaseType { get; set; }

        [JsonPropertyName("fileStatus")] public int FileStatus { get; set; }

        [JsonPropertyName("hashes")] public List<FileHash> Hashes { get; set; } = new();

        [JsonPropertyName("fileDate")] public DateTime FileDate { get; set; }

        [JsonPropertyName("fileLength")] public long FileLength { get; set; }

        [JsonPropertyName("downloadCount")] public int DownloadCount { get; set; }

        [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }

        [JsonPropertyName("gameVersions")] public List<string> GameVersions { get; set; } = new();

        [JsonPropertyName("sortableGameVersions")]
        public List<SortableGameVersion> SortableGameVersions { get; set; } = new();

        [JsonPropertyName("dependencies")] public List<FileDependency> Dependencies { get; set; } = new();

        [JsonPropertyName("alternateFileId")] public int AlternateFileId { get; set; }

        [JsonPropertyName("isServerPack")] public bool IsServerPack { get; set; }

        [JsonPropertyName("fileFingerprint")] public long FileFingerprint { get; set; }

        [JsonPropertyName("modules")] public List<FileModule> Modules { get; set; } = new();
    }

    // 文件列表响应类
    public class ModFilesResponse
    {
        [JsonPropertyName("data")] public List<ModFile> Data { get; set; } = new();

        [JsonPropertyName("pagination")] public PaginationEntry Pagination { get; set; } = new();
    }
    
    public class SingleModResponse
    {
        [JsonPropertyName("data")] 
        public ModData Data { get; set; }
    }
}