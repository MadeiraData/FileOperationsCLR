using System;
using System.Data.SqlTypes;
using System.Diagnostics;
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
}
