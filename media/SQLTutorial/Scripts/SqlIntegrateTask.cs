//#R System.Data.dll
//#R System.dll

using MyMis.Connector.Contracts;
using MyMis.Connector.ScriptAdapter.Helpers;
using MyMis.Connector.PlatformApi;
using MyMis.Connector.PlatformApi.Models;
using System.Data.SqlClient;
using System.Data;
using System;

/*
Developed by: NumbersBelieve
Function: Used to integrate a document into an SQL Database.
Parameters: The document needs to have a Project attribute representing the external entity in the table Project, a Description attribute with text, and a Completed True/False attribute.
*/
namespace myMIS
{
    public class Script
    {
        public ScriptResponse Execute(ContextDataObject context, Entity document)
        {
            //BUILD CONNECTION
            var connectionString = new SqlConnectionStringBuilder();
            connectionString.DataSource = context.Parameters["SqlServer"].ToString();
            connectionString.UserID = context.Username;
            connectionString.Password = context.Password;
            connectionString.InitialCatalog = context.Parameters["SqlDb"].ToString();
            var connection = new SqlConnection(connectionString.ToString());

            //EXECUTE QUERY
            var sqlString = string.Format(@"
DECLARE @Project INT

SELECT @Project = ID
FROM [dbo].[Project]
WHERE [Code] = '{0}'

INSERT INTO [dbo].[Task]
([ProjectID],[Description],[Completed])
VALUES 
(@Project,'{1}',{2})
", document.Attributes.Project, document.Attributes.Description, Convert.ToInt16(document.Attributes.Completed));

            DataTable table = new DataTable();

            using (SqlCommand cmd = new SqlCommand(
                sqlString, new SqlConnection(connectionString.ToString())))
            {
                cmd.Connection.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    throw new Exception("No data integrated");
                cmd.Connection.Close();
            }

            var message = "Integration Successful!";

            return new ScriptResponse
            {
                Message = message
            };
        }
    }
}