namespace MangaStudio.Core.Enums;

public enum DuplicateAction
{
    Skip,      // Skip chapters whose output folder already has files
    Overwrite  // Delete existing output and reprocess
}