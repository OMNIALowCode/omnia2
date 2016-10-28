//#R Interop.ErpBS900.dll    
//#R Interop.StdBE900.dll
using System;
using Newtonsoft.Json;
using Interop.ErpBS900;
using Interop.StdBE900;
using MyMis.Connector.Contracts;
using MyMis.Connector.ScriptAdapter.Helpers;

/*
Developed by: NumbersBelieve
Function: The standard file used to obtain queries to the Primavera ERP. This script receives a SQL query string from the system, as well as all the information necessary to authenticate itself with a specific company in the ERP, and returns the result of the SQL query.
Parameters: N/A
*/
namespace myMIS
{
    public class Script
    {
        public ScriptResponse Execute(ContextDataObject context, string query)
        {

            ErpBS bsERP = new ErpBS();

            if (!context.Parameters.ContainsKey("TipoPlataforma"))
            {
                throw new Exception("TipoPlataforma inválido");
            }

            EnumTipoPlataforma tipoPlataforma;
            if (!Enum.TryParse<EnumTipoPlataforma>((string)context.Parameters["TipoPlataforma"], out tipoPlataforma))
            {
                throw new Exception("TipoPlataforma inválido");
            }

            try
            {
                bsERP.AbreEmpresaTrabalho(tipoPlataforma, context.Company, context.Username, context.Password);
            }
            catch (Exception e)
            {
                throw new Exception("Erro a abrir a empresa no ERP: " + e.Message);
            }

            StdBELista queryResults = bsERP.Consulta(query);

            int numLinhas = queryResults.NumLinhas();
            int numColunas = queryResults.NumColunas();

            string[] headers = new string[numColunas];
            for (short i = 0; i < numColunas; i++)
            {
                headers[i] = queryResults.Nome(i);
            }

            object[,] data = new object[numLinhas, numColunas];
            for (short i = 0; i < numLinhas; i++)
            {
                for (short j = 0; j < numColunas; j++)
                {
                    var nome = headers[j];
                    data[i, j] = queryResults.Valor(nome);
                }
                queryResults.Seguinte();
            }

            QueryResult response = new QueryResult()
            {
                Headers = headers,
                Data = data
            };
			
			bsERP.FechaEmpresaTrabalho();
            
			return new ScriptResponse
            {
                Object = response
            };
        }
    }
}