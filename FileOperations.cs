using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
/* Example usage:

USE [test]
GO

DECLARE @arguments nvarchar(max) = 'test.dbo.OrderDetails out "C:\temp\OrderDetails.bcp" -T -c -C ACP'
DECLARE @output_msg nvarchar(max)
DECLARE @error_msg nvarchar(max)
DECLARE @return_val int

EXECUTE [dbo].[RunBCP] 
   @arguments
  ,@output_msg OUTPUT
  ,@error_msg OUTPUT
  ,@return_val OUTPUT

SELECT
 @output_msg
,@error_msg 
,@return_val
GO
*/
public partial class StoredProcedures
{
    private static string EndsWithSeparator(string absolutePath)
    {
        return absolutePath?.TrimEnd('\\') + "\\";
    }

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void RunBCP (SqlString arguments, out SqlString output_msg, out SqlString error_msg, out SqlInt32 return_val)
    {
        output_msg = string.Empty;
        error_msg = string.Empty;
        try
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bcp",
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                output_msg += proc.StandardOutput.ReadLine();
            }
            return_val = proc.ExitCode;
        }
        catch (Exception e)
        {
            error_msg = e.Message;
            return_val = 1;
        }
    }

    /* Example usage:

    USE [test]
    GO
    DECLARE @sourcePath nvarchar(max) = 'C:\IncomingData\FileToMove.txt'
    DECLARE @targetPath nvarchar(max) = 'C:\OutgoingData'
    DECLARE @overwrite bit = 1
    DECLARE @output_msg nvarchar(max)
    DECLARE @error_msg nvarchar(max)
    DECLARE @return_val int

    EXECUTE [dbo].[MoveFile] 
         @sourcePath
        ,@targetPath
        ,@overwrite
        ,@output_msg OUTPUT
        ,@error_msg OUTPUT
        ,@return_val OUTPUT

    SELECT
     @output_msg
    ,@error_msg 
    ,@return_val

    GO
    */
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void MoveFile(SqlString sourcePath, SqlString targetPath, SqlBoolean overwrite, out SqlString output_msg, out SqlString error_msg, out SqlInt32 return_val)
    {
        output_msg = string.Empty;
        error_msg = string.Empty;

        string[] allowedPaths = new string[] {
            /*
             ** Add allowed paths here **
             * Example:
             @"\\FileSrv\SharedPath\", @"\\sql-srv\IncomingData\", @"C:\IncomingData\", @"C:\OutgoingData\"
             * Leave empty to allow all folder paths
            */
        };

        bool sourceOkay = false;
        bool targetOkay = false;

        if (allowedPaths.Length == 0)
        {
            sourceOkay = true;
            targetOkay = true;
        }
        else
        {
            foreach (var allowedPath in allowedPaths)
            {
                if (sourcePath.Value.StartsWith(allowedPath))
                {
                    sourceOkay = true;
                }
                if (targetPath.Value.StartsWith(allowedPath))
                {
                    targetOkay = true;
                }
            }
        }

        if (!sourceOkay)
        {
            error_msg = "Source path is not allowed: " + sourcePath.Value;
            return_val = -1;
            return;
        }

        if (!targetOkay)
        {
            error_msg = "Target path is not allowed: " + targetPath.Value;
            return_val = -1;
            return;
        }

        if (!File.Exists(sourcePath.Value))
        {
            error_msg = "Source file does not exist: " + sourcePath.Value;
            return_val = -1;
            return;
        }

        string targetFilePath = targetPath.Value;

        if (Directory.Exists(targetFilePath))
        {
            targetFilePath = Path.Combine(targetFilePath, Path.GetFileName(sourcePath.Value));
        }

        if (File.Exists(targetFilePath))
        {
            if (overwrite.Value)
            {
                try
                {
                    File.Delete(targetFilePath);
                }
                catch (Exception e)
                {
                    error_msg = "Error while trying to delete existing target file: " + e.Message;
                    return_val = -1;
                    return;
                }
            }
            else
            {
                // target file already exists and overwrite is false
                error_msg = "Target file already exists: " + targetFilePath;
                return_val = -1;
                return;
            }
        }

        // move sourcePath file to targetPath
        try
        {
            System.IO.File.Move(sourcePath.Value, targetFilePath);
            output_msg = "File moved successfully from " + sourcePath.Value + " to " + targetFilePath;
            return_val = 0;
        }
        catch (Exception e)
        {
            error_msg = e.Message;
            return_val = -1;
        }
    }
}
