//#R System.Data.dll

using MyMis.Connector.Contracts;
using MyMis.Connector.ScriptAdapter.Helpers;
using MyMis.Connector.PlatformApi;
using MyMis.Connector.PlatformApi.Models;
using System.Data.SqlClient;
using System.Data;

/*
Developed by: NumbersBelieve
Function: Used to perform queries directly on an SQL database. This script receives a SQL query string from the system, and returns the result of the SQL query.
Parameters: The external service must have "SqlServer" and "SqlDb" attributes containing the values to pass to DataSource and InitialCatalog.
*/
namespace myMIS
{
    public class Script
    {
        public ScriptResponse Execute(ContextDataObject context, string query)
        {
            //BUILD CONNECTION
            var connectionString = new SqlConnectionStringBuilder();
            connectionString.DataSource = context.Parameters["SqlServer"].ToString();
            connectionString.UserID = context.Username;
            connectionString.Password = context.Password;
            connectionString.InitialCatalog = context.Parameters["SqlDb"].ToString();
            var connection = new SqlConnection(connectionString.ToString());

			//EXECUTE QUERY
            DataTable table = new DataTable();

            using (SqlCommand cmd = new SqlCommand(
        query, new SqlConnection(connectionString.ToString())))
            {
                cmd.Connection.Open();
                table.Load(cmd.ExecuteReader());
                cmd.Connection.Close();
            }

            string[] headers = new string[table.Columns.Count];
            object[,] data = new object[table.Rows.Count, table.Columns.Count];

            for (short i = 0; i < table.Columns.Count; i++)
            {
                headers[i] = table.Columns[i].ColumnName;
            }
            for (short j = 0; j < table.Rows.Count; j++)
            {
                for (short k = 0; k < table.Columns.Count; k++)
                {
                    data[j, k] = table.Rows[j][k];
                }
            }

			//RETURN OBJECT
            QueryResult response = new QueryResult()
            {
                Headers = headers,
                Data = data
            };
            
            return new ScriptResponse
            {
                Object = response
            };
        }
    }
}