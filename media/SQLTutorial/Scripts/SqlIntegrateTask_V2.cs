//#R System.Data.dll
//#R System.dll

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MyMis.Scripting.Core.Contracts;
using System.Data.SqlClient;
using System.Data;

namespace myMIS
{
    public class Script
    {
        public ScriptResponse Execute(ContextData context, Entity document, Dictionary<string, object> parameters)
        {
            /* **************************************** */
            /* **************************************** */
            /*          ADD YOUR CODE HERE              */
            //BUILD CONNECTION
            var externalSystem = context.ExternalSystems.FirstOrDefault().Value;

            var connectionString = new SqlConnectionStringBuilder();
            connectionString.DataSource = (string)externalSystem.Parameters["SqlServer"];
            connectionString.UserID = (string)externalSystem.Parameters["Username"];
            connectionString.Password = (string)externalSystem.Parameters["Password"]; ;
            connectionString.InitialCatalog = (string)externalSystem.Parameters["SqlDb"];
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