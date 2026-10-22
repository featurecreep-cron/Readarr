BEGIN TRANSACTION;
CREATE TABLE "AuthorMetadata" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ForeignAuthorId" TEXT NOT NULL, "TitleSlug" TEXT NOT NULL, "Name" TEXT NOT NULL, "Overview" TEXT, "Disambiguation" TEXT, "Gender" TEXT, "Hometown" TEXT, "Born" DATETIME, "Died" DATETIME, "Status" INTEGER NOT NULL, "Images" TEXT NOT NULL, "Links" TEXT, "Genres" TEXT, "Ratings" TEXT, "Aliases" TEXT NOT NULL, "SortName" TEXT NOT NULL, "NameLastFirst" TEXT NOT NULL, "SortNameLastFirst" TEXT NOT NULL);
CREATE TABLE "Authors" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "CleanName" TEXT NOT NULL, "Path" TEXT NOT NULL, "Monitored" INTEGER NOT NULL, "LastInfoSync" DATETIME, "QualityProfileId" INTEGER, "Tags" TEXT, "Added" DATETIME, "AddOptions" TEXT, "MetadataProfileId" INTEGER NOT NULL, "AuthorMetadataId" INTEGER NOT NULL, "MonitorNewItems" INTEGER NOT NULL);
CREATE TABLE "Blocklist" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "SourceTitle" TEXT NOT NULL, "Quality" TEXT NOT NULL, "Date" DATETIME NOT NULL, "PublishedDate" DATETIME, "Size" INTEGER, "Protocol" INTEGER, "Indexer" TEXT, "Message" TEXT, "TorrentInfoHash" TEXT, "AuthorId" INTEGER NOT NULL, "BookIds" TEXT NOT NULL, "IndexerFlags" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "BookFiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "EditionId" INTEGER NOT NULL, "CalibreId" INTEGER NOT NULL, "Quality" TEXT NOT NULL, "Size" INTEGER NOT NULL, "SceneName" TEXT, "DateAdded" DATETIME, "ReleaseGroup" TEXT, "MediaInfo" TEXT, "Modified" DATETIME, "Path" TEXT NOT NULL, "Part" INTEGER NOT NULL, "OriginalFilePath" TEXT, "IndexerFlags" INTEGER NOT NULL DEFAULT 0, "LastRemoteSearchTime" DATETIME);
CREATE TABLE "Books" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "AuthorMetadataId" INTEGER NOT NULL, "ForeignBookId" TEXT NOT NULL, "TitleSlug" TEXT NOT NULL, "Title" TEXT NOT NULL, "ReleaseDate" DATETIME, "Links" TEXT, "Genres" TEXT, "Ratings" TEXT, "CleanTitle" TEXT NOT NULL, "Monitored" INTEGER NOT NULL, "AnyEditionOk" INTEGER NOT NULL, "LastInfoSync" DATETIME, "Added" DATETIME, "AddOptions" TEXT, "RelatedBooks" TEXT NOT NULL, "LastSearchTime" DATETIME);
CREATE TABLE "Commands" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Body" TEXT NOT NULL, "Priority" INTEGER NOT NULL, "Status" INTEGER NOT NULL, "QueuedAt" DATETIME NOT NULL, "StartedAt" DATETIME, "EndedAt" DATETIME, "Duration" TEXT, "Exception" TEXT, "Trigger" INTEGER NOT NULL, "Result" INTEGER NOT NULL DEFAULT 1);
CREATE TABLE "Config" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Key" TEXT NOT NULL, "Value" TEXT NOT NULL);
CREATE TABLE "CustomFilters" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Type" TEXT NOT NULL, "Label" TEXT NOT NULL, "Filters" TEXT NOT NULL);
CREATE TABLE "CustomFormats" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Specifications" TEXT NOT NULL DEFAULT '[]', "IncludeCustomFormatWhenRenaming" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "DelayProfiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "EnableUsenet" INTEGER NOT NULL, "EnableTorrent" INTEGER NOT NULL, "PreferredProtocol" INTEGER NOT NULL, "UsenetDelay" INTEGER NOT NULL, "TorrentDelay" INTEGER NOT NULL, "Order" INTEGER NOT NULL, "Tags" TEXT NOT NULL, "BypassIfHighestQuality" INTEGER NOT NULL DEFAULT 0, "BypassIfAboveCustomFormatScore" INTEGER NOT NULL DEFAULT 0, "MinimumCustomFormatScore" INTEGER);
INSERT INTO "DelayProfiles" VALUES(1,1,1,1,0,0,2147483647,'[]',1,0,NULL);
CREATE TABLE "DownloadClientStatus" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ProviderId" INTEGER NOT NULL, "InitialFailure" DATETIME, "MostRecentFailure" DATETIME, "EscalationLevel" INTEGER NOT NULL, "DisabledTill" DATETIME);
CREATE TABLE "DownloadClients" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Enable" INTEGER NOT NULL, "Name" TEXT NOT NULL, "Implementation" TEXT NOT NULL, "Settings" TEXT NOT NULL, "ConfigContract" TEXT NOT NULL, "Priority" INTEGER NOT NULL DEFAULT 1, "Tags" TEXT, "RemoveCompletedDownloads" INTEGER NOT NULL DEFAULT 1, "RemoveFailedDownloads" INTEGER NOT NULL DEFAULT 1);
CREATE TABLE "DownloadHistory" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "EventType" INTEGER NOT NULL, "AuthorId" INTEGER NOT NULL, "DownloadId" TEXT NOT NULL, "SourceTitle" TEXT NOT NULL, "Date" DATETIME, "Protocol" INTEGER, "IndexerId" INTEGER, "DownloadClientId" INTEGER, "Release" TEXT, "Data" TEXT);
CREATE TABLE "Editions" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "BookId" INTEGER NOT NULL, "ForeignEditionId" TEXT NOT NULL, "Isbn13" TEXT, "Asin" TEXT, "Title" TEXT NOT NULL, "TitleSlug" TEXT NOT NULL, "Language" TEXT, "Overview" TEXT, "Format" TEXT, "IsEbook" INTEGER, "Disambiguation" TEXT, "Publisher" TEXT, "PageCount" INTEGER, "ReleaseDate" DATETIME, "Images" TEXT NOT NULL, "Links" TEXT, "Ratings" TEXT, "Monitored" INTEGER NOT NULL, "ManualAdd" INTEGER NOT NULL);
CREATE TABLE "ExtraFiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "AuthorId" INTEGER NOT NULL, "BookId" INTEGER NOT NULL, "BookFileId" INTEGER NOT NULL, "RelativePath" TEXT NOT NULL, "Extension" TEXT NOT NULL, "Added" DATETIME NOT NULL, "LastUpdated" DATETIME NOT NULL);
CREATE TABLE "History" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "SourceTitle" TEXT NOT NULL, "Date" DATETIME NOT NULL, "Quality" TEXT NOT NULL, "Data" TEXT NOT NULL, "EventType" INTEGER, "DownloadId" TEXT, "AuthorId" INTEGER NOT NULL, "BookId" INTEGER NOT NULL);
CREATE TABLE "ImportListExclusions" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ForeignId" TEXT NOT NULL, "Name" TEXT NOT NULL);
CREATE TABLE "ImportListStatus" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ProviderId" INTEGER NOT NULL, "InitialFailure" DATETIME, "MostRecentFailure" DATETIME, "EscalationLevel" INTEGER NOT NULL, "DisabledTill" DATETIME, "LastInfoSync" DATETIME);
CREATE TABLE "ImportLists" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Implementation" TEXT NOT NULL, "Settings" TEXT, "ConfigContract" TEXT, "EnableAutomaticAdd" INTEGER, "RootFolderPath" TEXT NOT NULL, "ShouldMonitor" INTEGER NOT NULL, "ProfileId" INTEGER NOT NULL, "MetadataProfileId" INTEGER NOT NULL, "Tags" TEXT, "ShouldSearch" INTEGER NOT NULL DEFAULT 1, "ShouldMonitorExisting" INTEGER NOT NULL DEFAULT 0, "MonitorNewItems" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "IndexerStatus" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ProviderId" INTEGER NOT NULL, "InitialFailure" DATETIME, "MostRecentFailure" DATETIME, "EscalationLevel" INTEGER NOT NULL, "DisabledTill" DATETIME, "LastRssSyncReleaseInfo" TEXT);
CREATE TABLE "Indexers" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Implementation" TEXT NOT NULL, "Settings" TEXT, "ConfigContract" TEXT, "EnableRss" INTEGER, "EnableAutomaticSearch" INTEGER, "EnableInteractiveSearch" INTEGER NOT NULL, "Priority" INTEGER NOT NULL DEFAULT 25, "Tags" TEXT, "DownloadClientId" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "Metadata" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Enable" INTEGER NOT NULL, "Name" TEXT NOT NULL, "Implementation" TEXT NOT NULL, "Settings" TEXT NOT NULL, "ConfigContract" TEXT NOT NULL);
CREATE TABLE "MetadataFiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "AuthorId" INTEGER NOT NULL, "Consumer" TEXT NOT NULL, "Type" INTEGER NOT NULL, "RelativePath" TEXT NOT NULL, "LastUpdated" DATETIME NOT NULL, "BookId" INTEGER, "BookFileId" INTEGER, "Hash" TEXT, "Added" DATETIME, "Extension" TEXT NOT NULL);
CREATE TABLE "MetadataProfiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "MinPopularity" NUMERIC NOT NULL, "SkipMissingDate" INTEGER NOT NULL, "SkipMissingIsbn" INTEGER NOT NULL, "SkipPartsAndSets" INTEGER NOT NULL, "SkipSeriesSecondary" INTEGER NOT NULL, "AllowedLanguages" TEXT, "MinPages" INTEGER NOT NULL DEFAULT 0, "Ignored" TEXT);
INSERT INTO "MetadataProfiles" VALUES(1,'Standard',350,1,0,1,0,'eng, null',0,'[]');
INSERT INTO "MetadataProfiles" VALUES(2,'None',10000000000,0,0,0,0,NULL,0,'[]');
CREATE TABLE "NamingConfig" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ReplaceIllegalCharacters" INTEGER NOT NULL DEFAULT 1, "AuthorFolderFormat" TEXT, "RenameBooks" INTEGER, "StandardBookFormat" TEXT, "ColonReplacementFormat" INTEGER NOT NULL DEFAULT 4);
CREATE TABLE "NotificationStatus" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ProviderId" INTEGER NOT NULL, "InitialFailure" DATETIME, "MostRecentFailure" DATETIME, "EscalationLevel" INTEGER NOT NULL, "DisabledTill" DATETIME);
CREATE TABLE "Notifications" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "OnGrab" INTEGER NOT NULL, "Settings" TEXT NOT NULL, "Implementation" TEXT NOT NULL, "ConfigContract" TEXT, "OnUpgrade" INTEGER, "Tags" TEXT, "OnRename" INTEGER NOT NULL, "OnReleaseImport" INTEGER NOT NULL, "OnHealthIssue" INTEGER NOT NULL, "IncludeHealthWarnings" INTEGER NOT NULL, "OnDownloadFailure" INTEGER NOT NULL, "OnImportFailure" INTEGER NOT NULL, "OnBookRetag" INTEGER NOT NULL, "OnAuthorDelete" INTEGER NOT NULL DEFAULT 0, "OnBookDelete" INTEGER NOT NULL DEFAULT 0, "OnBookFileDelete" INTEGER NOT NULL DEFAULT 0, "OnBookFileDeleteForUpgrade" INTEGER NOT NULL DEFAULT 0, "OnApplicationUpdate" INTEGER NOT NULL DEFAULT 1, "OnAuthorAdded" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "PendingReleases" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Title" TEXT NOT NULL, "Added" DATETIME NOT NULL, "Release" TEXT NOT NULL, "AuthorId" INTEGER NOT NULL, "ParsedBookInfo" TEXT NOT NULL, "Reason" INTEGER NOT NULL);
CREATE TABLE "QualityDefinitions" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Quality" INTEGER NOT NULL, "Title" TEXT NOT NULL, "MinSize" NUMERIC, "MaxSize" NUMERIC);
INSERT INTO "QualityDefinitions" VALUES(1,0,'Unknown Text',0,350);
INSERT INTO "QualityDefinitions" VALUES(2,1,'PDF',0,350);
INSERT INTO "QualityDefinitions" VALUES(3,2,'MOBI',0,350);
INSERT INTO "QualityDefinitions" VALUES(4,3,'EPUB',0,350);
INSERT INTO "QualityDefinitions" VALUES(5,4,'AZW3',0,350);
INSERT INTO "QualityDefinitions" VALUES(6,13,'Unknown Audio',0,350);
INSERT INTO "QualityDefinitions" VALUES(7,10,'MP3',0,350);
INSERT INTO "QualityDefinitions" VALUES(8,12,'M4B',0,350);
INSERT INTO "QualityDefinitions" VALUES(9,11,'FLAC',0,NULL);
CREATE TABLE "QualityProfiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Name" TEXT NOT NULL, "Cutoff" INTEGER NOT NULL, "Items" TEXT NOT NULL, "UpgradeAllowed" INTEGER, "FormatItems" TEXT NOT NULL DEFAULT '[]', "MinFormatScore" INTEGER NOT NULL DEFAULT 0, "CutoffFormatScore" INTEGER NOT NULL DEFAULT 0);
INSERT INTO "QualityProfiles" VALUES(1,'eBook',2,'[
  {
    "quality": 0,
    "items": [],
    "allowed": false
  },
  {
    "quality": 1,
    "items": [],
    "allowed": false
  },
  {
    "quality": 2,
    "items": [],
    "allowed": true
  },
  {
    "quality": 3,
    "items": [],
    "allowed": true
  },
  {
    "quality": 4,
    "items": [],
    "allowed": true
  },
  {
    "quality": 13,
    "items": [],
    "allowed": false
  },
  {
    "quality": 10,
    "items": [],
    "allowed": false
  },
  {
    "quality": 12,
    "items": [],
    "allowed": false
  },
  {
    "quality": 11,
    "items": [],
    "allowed": false
  }
]',0,'[]',0,0);
INSERT INTO "QualityProfiles" VALUES(2,'Spoken',10,'[
  {
    "quality": 0,
    "items": [],
    "allowed": false
  },
  {
    "quality": 1,
    "items": [],
    "allowed": false
  },
  {
    "quality": 2,
    "items": [],
    "allowed": false
  },
  {
    "quality": 3,
    "items": [],
    "allowed": false
  },
  {
    "quality": 4,
    "items": [],
    "allowed": false
  },
  {
    "quality": 13,
    "items": [],
    "allowed": true
  },
  {
    "quality": 10,
    "items": [],
    "allowed": true
  },
  {
    "quality": 12,
    "items": [],
    "allowed": true
  },
  {
    "quality": 11,
    "items": [],
    "allowed": true
  }
]',0,'[]',0,0);
CREATE TABLE "ReleaseProfiles" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Required" TEXT, "Ignored" TEXT, "Tags" TEXT NOT NULL, "Enabled" INTEGER NOT NULL, "IndexerId" INTEGER NOT NULL);
CREATE TABLE "RemotePathMappings" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Host" TEXT NOT NULL, "RemotePath" TEXT NOT NULL, "LocalPath" TEXT NOT NULL);
CREATE TABLE "RootFolders" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Path" TEXT NOT NULL, "Name" TEXT, "DefaultMetadataProfileId" INTEGER NOT NULL DEFAULT 0, "DefaultQualityProfileId" INTEGER NOT NULL DEFAULT 0, "DefaultMonitorOption" INTEGER NOT NULL DEFAULT 0, "DefaultTags" TEXT, "IsCalibreLibrary" INTEGER NOT NULL, "CalibreSettings" TEXT, "DefaultNewItemMonitorOption" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE "ScheduledTasks" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "TypeName" TEXT NOT NULL, "Interval" INTEGER NOT NULL, "LastExecution" DATETIME NOT NULL, "LastStartTime" DATETIME);
INSERT INTO "ScheduledTasks" VALUES(1,'NzbDrone.Core.Download.RefreshMonitoredDownloadsCommand',1,'2026-10-22 17:27:43.480436Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(2,'NzbDrone.Core.Messaging.Commands.MessagingCleanupCommand',5,'2026-10-22 17:27:43.4863474Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(3,'NzbDrone.Core.Update.Commands.ApplicationUpdateCheckCommand',360,'2026-10-22 17:27:43.4902407Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(4,'NzbDrone.Core.HealthCheck.CheckHealthCommand',360,'2026-10-22 17:27:43.4936636Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(5,'NzbDrone.Core.Books.Commands.RefreshAuthorCommand',1440,'2026-10-22 17:27:43.4969692Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(6,'NzbDrone.Core.MediaFiles.Commands.RescanFoldersCommand',1440,'2026-10-22 17:27:43.5002654Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(7,'NzbDrone.Core.Housekeeping.HousekeepingCommand',1440,'2026-10-22 17:27:43.5034553Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(8,'NzbDrone.Core.Backup.BackupCommand',10080,'2026-10-22 17:27:43.5060349Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(9,'NzbDrone.Core.ImportLists.ImportListSyncCommand',5,'2026-10-22 17:27:43.5088134Z','0001-01-01 05:51:00Z');
INSERT INTO "ScheduledTasks" VALUES(10,'NzbDrone.Core.Indexers.RssSyncCommand',15,'2026-10-22 17:27:43.5115918Z','0001-01-01 05:51:00Z');
CREATE TABLE "Series" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "ForeignSeriesId" TEXT NOT NULL, "Title" TEXT NOT NULL, "Description" TEXT, "Numbered" INTEGER NOT NULL, "WorkCount" INTEGER NOT NULL, "PrimaryWorkCount" INTEGER NOT NULL);
CREATE TABLE "SeriesBookLink" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "SeriesId" INTEGER NOT NULL, "BookId" INTEGER NOT NULL, "Position" TEXT, "IsPrimary" INTEGER NOT NULL, "SeriesPosition" INTEGER NOT NULL DEFAULT 0, CONSTRAINT "FK_SeriesBookLink_SeriesId_Series_Id" FOREIGN KEY ("SeriesId") REFERENCES "Series" ("Id") ON DELETE CASCADE, CONSTRAINT "FK_SeriesBookLink_BookId_Books_Id" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE CASCADE);
CREATE TABLE "Tags" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Label" TEXT NOT NULL);
CREATE TABLE "Users" ("Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, "Identifier" TEXT NOT NULL, "Username" TEXT NOT NULL, "Password" TEXT NOT NULL);
CREATE TABLE "VersionInfo" ("Version" INTEGER NOT NULL, "AppliedOn" DATETIME, "Description" TEXT);
INSERT INTO "VersionInfo" VALUES(1,'2026-10-22T17:27:41','InitialSetup');
INSERT INTO "VersionInfo" VALUES(2,'2026-10-22T17:27:41','ImportListSearch');
INSERT INTO "VersionInfo" VALUES(3,'2026-10-22T17:27:41','add_priority_to_indexers');
INSERT INTO "VersionInfo" VALUES(4,'2026-10-22T17:27:41','rename_supports_on_track_retag');
INSERT INTO "VersionInfo" VALUES(5,'2026-10-22T17:27:41','add_indexer_and_enabled_to_release_profiles');
INSERT INTO "VersionInfo" VALUES(6,'2026-10-22T17:27:41','remove_chown_and_folderchmod_config');
INSERT INTO "VersionInfo" VALUES(7,'2026-10-22T17:27:41','update_notifiarr');
INSERT INTO "VersionInfo" VALUES(8,'2026-10-22T17:27:41','extend_metadata_profiles');
INSERT INTO "VersionInfo" VALUES(9,'2026-10-22T17:27:41','update_author_sort_name');
INSERT INTO "VersionInfo" VALUES(10,'2026-10-22T17:27:41','add_bookfile_part');
INSERT INTO "VersionInfo" VALUES(11,'2026-10-22T17:27:41','update_audio_qualities');
INSERT INTO "VersionInfo" VALUES(12,'2026-10-22T17:27:41','add_bookfile_part_naming_token');
INSERT INTO "VersionInfo" VALUES(13,'2026-10-22T17:27:41','update_author_sort_name_again');
INSERT INTO "VersionInfo" VALUES(14,'2026-10-22T17:27:41','rename_blacklist_to_blocklist');
INSERT INTO "VersionInfo" VALUES(15,'2026-10-22T17:27:41','FixIndexes');
INSERT INTO "VersionInfo" VALUES(16,'2026-10-22T17:27:41','AddRelatedBooks');
INSERT INTO "VersionInfo" VALUES(17,'2026-10-22T17:27:41','ImportListMonitorExisting');
INSERT INTO "VersionInfo" VALUES(18,'2026-10-22T17:27:41','AddSeriesPosition');
INSERT INTO "VersionInfo" VALUES(19,'2026-10-22T17:27:41','AddNewItemMonitorType');
INSERT INTO "VersionInfo" VALUES(20,'2026-10-22T17:27:41','AddDownloadHistory');
INSERT INTO "VersionInfo" VALUES(21,'2026-10-22T17:27:41','add_on_delete_to_notifications');
INSERT INTO "VersionInfo" VALUES(22,'2026-10-22T17:27:41','EditionMonitoredIndex');
INSERT INTO "VersionInfo" VALUES(23,'2026-10-22T17:27:42','postgres_update_timestamp_columns_to_with_timezone');
INSERT INTO "VersionInfo" VALUES(24,'2026-10-22T17:27:42','add_update_history');
INSERT INTO "VersionInfo" VALUES(25,'2026-10-22T17:27:42','add_on_update_to_notifications');
INSERT INTO "VersionInfo" VALUES(26,'2026-10-22T17:27:42','add_custom_formats');
INSERT INTO "VersionInfo" VALUES(27,'2026-10-22T17:27:42','remove_omg');
INSERT INTO "VersionInfo" VALUES(28,'2026-10-22T17:27:42','add_indexer_tags');
INSERT INTO "VersionInfo" VALUES(29,'2026-10-22T17:27:42','list_sync_time');
INSERT INTO "VersionInfo" VALUES(30,'2026-10-22T17:27:42','download_client_per_indexer');
INSERT INTO "VersionInfo" VALUES(31,'2026-10-22T17:27:42','add_colon_replacement_to_naming_config');
INSERT INTO "VersionInfo" VALUES(32,'2026-10-22T17:27:42','postgres_update_timestamp_columns_to_with_timezone_part2');
INSERT INTO "VersionInfo" VALUES(33,'2026-10-22T17:27:42','metadata_profile_ignored_to_list');
INSERT INTO "VersionInfo" VALUES(35,'2026-10-22T17:27:42','add_download_client_tags');
INSERT INTO "VersionInfo" VALUES(36,'2026-10-22T17:27:42','add_result_to_commands');
INSERT INTO "VersionInfo" VALUES(37,'2026-10-22T17:27:42','add_notification_status');
INSERT INTO "VersionInfo" VALUES(38,'2026-10-22T17:27:42','add_on_author_added_to_notifications');
INSERT INTO "VersionInfo" VALUES(39,'2026-10-22T17:27:42','book_last_searched_time');
INSERT INTO "VersionInfo" VALUES(40,'2026-10-22T17:27:42','add_indexer_flags');
INSERT INTO "VersionInfo" VALUES(41,'2026-10-22T17:27:42','bookfile_last_remote_search');
INSERT INTO "VersionInfo" VALUES(158,'2026-10-22T17:27:42','cdh_per_downloadclient');
CREATE UNIQUE INDEX "IX_Config_Key" ON "Config" ("Key" ASC);
CREATE UNIQUE INDEX "IX_RootFolders_Path" ON "RootFolders" ("Path" ASC);
CREATE UNIQUE INDEX "IX_Series_ForeignSeriesId" ON "Series" ("ForeignSeriesId" ASC);
CREATE INDEX "IX_SeriesBookLink_SeriesId" ON "SeriesBookLink" ("SeriesId" ASC);
CREATE UNIQUE INDEX "IX_Indexers_Name" ON "Indexers" ("Name" ASC);
CREATE UNIQUE INDEX "IX_QualityProfiles_Name" ON "QualityProfiles" ("Name" ASC);
CREATE UNIQUE INDEX "IX_MetadataProfiles_Name" ON "MetadataProfiles" ("Name" ASC);
CREATE UNIQUE INDEX "IX_QualityDefinitions_Quality" ON "QualityDefinitions" ("Quality" ASC);
CREATE UNIQUE INDEX "IX_QualityDefinitions_Title" ON "QualityDefinitions" ("Title" ASC);
CREATE UNIQUE INDEX "IX_Tags_Label" ON "Tags" ("Label" ASC);
CREATE UNIQUE INDEX "IX_Users_Identifier" ON "Users" ("Identifier" ASC);
CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username" ASC);
CREATE UNIQUE INDEX "IX_ImportLists_Name" ON "ImportLists" ("Name" ASC);
CREATE UNIQUE INDEX "IX_ImportListExclusions_ForeignId" ON "ImportListExclusions" ("ForeignId" ASC);
CREATE INDEX "IX_Authors_CleanName" ON "Authors" ("CleanName" ASC);
CREATE INDEX "IX_Authors_Path" ON "Authors" ("Path" ASC);
CREATE UNIQUE INDEX "IX_Authors_AuthorMetadataId" ON "Authors" ("AuthorMetadataId" ASC);
CREATE INDEX "IX_Authors_Monitored" ON "Authors" ("Monitored" ASC);
CREATE UNIQUE INDEX "IX_AuthorMetadata_ForeignAuthorId" ON "AuthorMetadata" ("ForeignAuthorId" ASC);
CREATE UNIQUE INDEX "IX_AuthorMetadata_TitleSlug" ON "AuthorMetadata" ("TitleSlug" ASC);
CREATE INDEX "IX_Books_ForeignBookId" ON "Books" ("ForeignBookId" ASC);
CREATE UNIQUE INDEX "IX_Books_TitleSlug" ON "Books" ("TitleSlug" ASC);
CREATE INDEX "IX_Books_CleanTitle" ON "Books" ("CleanTitle" ASC);
CREATE INDEX "IX_Books_AuthorMetadataId" ON "Books" ("AuthorMetadataId" ASC);
CREATE INDEX "IX_Books_AuthorMetadataId_ReleaseDate" ON "Books" ("AuthorMetadataId" ASC, "ReleaseDate" ASC);
CREATE INDEX "IX_BookFiles_EditionId" ON "BookFiles" ("EditionId" ASC);
CREATE UNIQUE INDEX "IX_BookFiles_Path" ON "BookFiles" ("Path" ASC);
CREATE UNIQUE INDEX "IX_DownloadClientStatus_ProviderId" ON "DownloadClientStatus" ("ProviderId" ASC);
CREATE UNIQUE INDEX "IX_Editions_ForeignEditionId" ON "Editions" ("ForeignEditionId" ASC);
CREATE INDEX "IX_Editions_BookId" ON "Editions" ("BookId" ASC);
CREATE INDEX "IX_Editions_Monitored" ON "Editions" ("Monitored" ASC);
CREATE INDEX "IX_History_Date" ON "History" ("Date" ASC);
CREATE INDEX "IX_History_EventType" ON "History" ("EventType" ASC);
CREATE INDEX "IX_History_BookId_Date" ON "History" ("BookId" ASC, "Date" DESC);
CREATE INDEX "IX_History_DownloadId_Date" ON "History" ("DownloadId" ASC, "Date" DESC);
CREATE UNIQUE INDEX "IX_IndexerStatus_ProviderId" ON "IndexerStatus" ("ProviderId" ASC);
CREATE UNIQUE INDEX "IX_ScheduledTasks_TypeName" ON "ScheduledTasks" ("TypeName" ASC);
CREATE UNIQUE INDEX "UC_Version" ON "VersionInfo" ("Version" ASC);
CREATE UNIQUE INDEX "IX_CustomFormats_Name" ON "CustomFormats" ("Name" ASC);
CREATE INDEX "IX_DownloadHistory_EventType" ON "DownloadHistory" ("EventType" ASC);
CREATE INDEX "IX_DownloadHistory_AuthorId" ON "DownloadHistory" ("AuthorId" ASC);
CREATE INDEX "IX_DownloadHistory_DownloadId" ON "DownloadHistory" ("DownloadId" ASC);
CREATE UNIQUE INDEX "IX_ImportListStatus_ProviderId" ON "ImportListStatus" ("ProviderId" ASC);
CREATE UNIQUE INDEX "IX_NotificationStatus_ProviderId" ON "NotificationStatus" ("ProviderId" ASC);
DELETE FROM "sqlite_sequence";
INSERT INTO "sqlite_sequence" VALUES('DelayProfiles',1);
INSERT INTO "sqlite_sequence" VALUES('Notifications',0);
INSERT INTO "sqlite_sequence" VALUES('Authors',0);
INSERT INTO "sqlite_sequence" VALUES('AuthorMetadata',0);
INSERT INTO "sqlite_sequence" VALUES('Blocklist',0);
INSERT INTO "sqlite_sequence" VALUES('Books',0);
INSERT INTO "sqlite_sequence" VALUES('BookFiles',0);
INSERT INTO "sqlite_sequence" VALUES('Commands',0);
INSERT INTO "sqlite_sequence" VALUES('DownloadClientStatus',0);
INSERT INTO "sqlite_sequence" VALUES('Editions',0);
INSERT INTO "sqlite_sequence" VALUES('ExtraFiles',0);
INSERT INTO "sqlite_sequence" VALUES('History',0);
INSERT INTO "sqlite_sequence" VALUES('IndexerStatus',0);
INSERT INTO "sqlite_sequence" VALUES('MetadataFiles',0);
INSERT INTO "sqlite_sequence" VALUES('PendingReleases',0);
INSERT INTO "sqlite_sequence" VALUES('ScheduledTasks',10);
INSERT INTO "sqlite_sequence" VALUES('ReleaseProfiles',0);
INSERT INTO "sqlite_sequence" VALUES('DownloadHistory',0);
INSERT INTO "sqlite_sequence" VALUES('ImportListStatus',0);
INSERT INTO "sqlite_sequence" VALUES('QualityDefinitions',9);
INSERT INTO "sqlite_sequence" VALUES('QualityProfiles',2);
INSERT INTO "sqlite_sequence" VALUES('MetadataProfiles',2);
COMMIT;
