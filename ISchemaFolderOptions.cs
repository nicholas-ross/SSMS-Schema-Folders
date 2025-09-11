﻿namespace SsmsSchemaFolders
{
    public interface ISchemaFolderOptions
    {
        bool AppendDot { get; }
        bool CloneParentNode { get; }
        bool UseObjectIcon { get; }
        bool RenameNode { get; }
        int QuickSchema { get; }
        int UnresponsiveTimeout { get; }
        int UseClear { get; }
        FolderType Level1FolderType { get; }
        int Level1MinNodeCount { get; }
        string Level1Regex { get; }
        bool Level1GroupNonMatchingAsOther { get; }

        FolderType Level2FolderType { get; }
        int Level2MinNodeCount { get; }
        string Level2Regex { get; }
        bool Level2GroupNonMatchingAsOther { get; }
    }
}
