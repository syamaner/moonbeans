namespace AspireRagDemo.API.Ingestion;

public enum DocumentType
{
    /// <summary>
    /// GitHub repository scraped and formatted in a single file.
    /// The logical files are seperated as below with the file names.
    /// ================================================
    /// File: README.md
    /// ================================================
    /// </summary>
    GitIngest,
    /// <summary>
    /// Generic text file. Could be a single file.
    /// </summary>
    Text
}