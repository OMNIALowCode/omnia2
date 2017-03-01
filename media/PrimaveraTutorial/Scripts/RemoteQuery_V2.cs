//#R Interop.ErpBS900.dll    
//#R Interop.StdBE900.dll
//#R System.Data.dll

using System;
using MyMis.Scripting.Core.Contracts;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Dynamic;
using System.Text;
using System.Data;
using Interop.ErpBS900;
using Interop.StdBE900;

/*
Developed by: NumbersBelieve
Function: The standard file used to obtain queries to the Primavera ERP. This script receives a SQL query string from the system, as well as all the information necessary to authenticate itself with a specific company in the ERP, and returns the result of the SQL query.

Everything but the executeQuery method is generic and can be used to query other external systems.

Parameters: N/A
*/

namespace myMIS
{
    public class Script
    {
        const string WILDCARD_MULTIPLE = "*";
        const string WILDCARD_SINGLE = "?";

        public ScriptResponse Execute(ContextData context, Entity document, Dictionary<string, object> parameters)
        {
            string where = document.Where;

            bool whereAllowWildCards = true;
            if (!string.IsNullOrEmpty(where) && where.Contains("@_QUERYKEYPARAMETER_@"))
                whereAllowWildCards = false;// Isn't a list but a foreign entity lookup


            string queryStr = string.Format("SELECT * FROM ({0}) DEFINED_QUERY ", document.Query);

            //Apply key parameter to where
            if (!string.IsNullOrEmpty(where))
            {
                where = where.Replace("@_QUERYKEYPARAMETER_@", document.QueryKeyParameter);

                where = processWhereStatement(where, whereAllowWildCards);

            }

            //Get date filter
            string dateFilter = applyDateFilter(document.DateParameter, document.BeginYear, document.BeginMonth, document.BeginDay, document.EndYear, document.EndMonth, document.EndDay);

            //Apply security to where clause
            applySecurityFiltersToQuery(document.SecurityFilters, ref where);


            if (!string.IsNullOrEmpty(where))
            {
                queryStr += String.Format(" WHERE {0}", where);

                if (!string.IsNullOrEmpty(dateFilter))
                    queryStr += String.Format(" AND {0}", dateFilter);
            }
            else if (!string.IsNullOrEmpty(dateFilter))
            {
                queryStr += String.Format(" WHERE {0}", dateFilter);
            }


            queryStr = applyPaggingAndSelectToQuery(queryStr, document.QueryKeyParameter, document.Select, document.OrderBy, document.Take, document.Page);


            return executeQuery(context, document, queryStr);
        }


        private string applyPaggingAndSelectToQuery(string query, string queryKeyParameter, string select, string orderBy, short? take, short? page)
        {
            const string QUERY_PAGGING_STRUCTURE =
                "SELECT @MYMIS_SELECT_COLUMNS@ FROM " +
                "(SELECT ROW_NUMBER() OVER(ORDER BY @MYMIS_QUERYKEYPARAMETER@) AS _MYMIS_ROW_NUMBER_, INNER_TBL.* FROM ( " +
                "@MYMIS_QUERY@ " +
                ") AS INNER_TBL " +
                ") AS TBL " +
                "WHERE _MYMIS_ROW_NUMBER_ BETWEEN ((@MYMIS_PAGE@ - 1) * @MYMIS_ROWS@ + 1) AND (@MYMIS_PAGE@ * @MYMIS_ROWS@) ";

            if (!take.HasValue || !page.HasValue)
            {
                //TODO: Process Take to do a Top "n" records
                query = applyOrderByToQuery(query, queryKeyParameter, orderBy);
                return query;
            }


            var queryWithPagging = QUERY_PAGGING_STRUCTURE
                .Replace("@MYMIS_QUERY@", query)
                .Replace("@MYMIS_QUERYKEYPARAMETER@", string.IsNullOrEmpty(orderBy) ? queryKeyParameter : orderBy)
                .Replace("@MYMIS_PAGE@", page.Value.ToString())
                .Replace("@MYMIS_ROWS@", take.Value.ToString())
                .Replace("@MYMIS_SELECT_COLUMNS@", string.IsNullOrEmpty(select) ? "*" : select);


            return queryWithPagging;



        }

        private string applyOrderByToQuery(string query, string queryKeyParameter, string orderBy)
        {
            if (string.IsNullOrEmpty(orderBy))
            {
                query += string.Format("ORDER BY {0}", queryKeyParameter);
            }
            else
            {
                query += string.Format("ORDER BY {0}", orderBy);
            }

            return query;

        }


        private void applySecurityFiltersToQuery(string[] securityFilters, ref string whereStatement)
        {

            string securityFilter = string.Empty;


            foreach (var rolePrivilege in securityFilters)
            {
                string roleFilterSentence = string.Empty;

                string filter = rolePrivilege;

                //Process Query Filter
                if (!string.IsNullOrEmpty(filter))
                {

                    string filterSentence = filter;

                    var filterCollection = Regex.Split(filter, "\\||&");

                    foreach (var filterClause in filterCollection)
                    {
                        string filterAux = filterClause;

                        //var filterDecomposed = Regex.Split(filterAux.Replace("(", "").Replace(")", ""), "(>=|<=|<>|=|<|>)"); //GF: 2014.06.23
                        var filterDecomposed = Regex.Split(filterAux, "(>=|<=|<>|=|<|>)");

                        var filterField = filterDecomposed.First();

                        if (filterDecomposed.Count() >= 3 && filterDecomposed[1].Equals("<>") && string.IsNullOrEmpty(filterDecomposed[2]))
                        {
                            filterAux = filterAux.Replace(filterDecomposed[1] + filterDecomposed[2], " is not null");
                        }
                        else if (filterDecomposed.Count() >= 3 && filterDecomposed[1].Equals("=") && string.IsNullOrEmpty(filterDecomposed[2]))
                        {
                            filterAux = filterAux.Replace(filterDecomposed[1] + filterDecomposed[2], " is null");
                        }
                        else if (filterDecomposed.Count() >= 3 && filterDecomposed[2].Equals("TRUE"))
                        {
                            filterAux = filterAux.Replace(string.Format("{0}{1}", filterDecomposed[1], "TRUE"), string.Format("{0}{1}", filterDecomposed[1], "1"));
                        }
                        else if (filterDecomposed.Count() >= 3 && filterDecomposed[2].Equals("FALSE"))
                        {
                            filterAux = filterAux.Replace(string.Format("{0}{1}", filterDecomposed[1], "FALSE"), string.Format("{0}{1}", filterDecomposed[1], "0"));
                        }


                        if (filterDecomposed.Count() >= 3 && filterDecomposed[0].Equals(string.Format("{0}", "TRUE")))
                        {
                            filterAux = filterAux.Replace(string.Format("{0}{1}", "TRUE", filterDecomposed[1]), string.Format("{0}{1}", "1", filterDecomposed[1]));
                        }
                        else if (filterDecomposed.Count() >= 3 && filterDecomposed[0].Equals(string.Format("{0}", "FALSE")))
                        {
                            filterAux = filterAux.Replace(string.Format("{0}{1}", "FALSE", filterDecomposed[1]), string.Format("{0}{1}", "0", filterDecomposed[1]));
                        }


                        filterSentence = filterSentence.Replace(filterClause, string.Format("({0})", filterAux));

                    }

                    filterSentence = filterSentence.Replace(@"|", " OR ").Replace(@"&", " AND "); //Change to SQL Operators

                    if (!String.IsNullOrEmpty(roleFilterSentence)) roleFilterSentence += " AND ";

                    roleFilterSentence = string.Format("({0} ({1}))", roleFilterSentence, filterSentence);
                }

                if (!string.IsNullOrEmpty(roleFilterSentence))
                {
                    if (!string.IsNullOrEmpty(securityFilter)) securityFilter += " OR ";
                    securityFilter += roleFilterSentence;
                }


            }

            if (!string.IsNullOrEmpty(securityFilter))
            {
                //Add to where statement
                if (!String.IsNullOrEmpty(whereStatement))
                {
                    whereStatement += " AND ";
                }

                whereStatement += string.Format("({0})", securityFilter);
            }


        }

        private string processWhereStatement(string where, bool whereAllowWildCards)
        {


            if (string.IsNullOrEmpty(where))
                return where;


            string[] parts = conditionSplit(where);

            foreach (var clause in parts)
            {
                string composedWhere = string.Empty;

                if (!whereAllowWildCards || (!clause.Contains(WILDCARD_MULTIPLE) && !clause.Contains(WILDCARD_SINGLE)))
                {

                    composedWhere += clause;

                }
                else
                {

                    var clauseParts = Regex.Split(clause, "(=|<|>|<=|>=)").ToArray();

                    //Validate if have any of wild cards and replace it
                    if (clauseParts.Length >= 3 && clauseParts[1].Equals("=") && (clauseParts[2].ToString().Contains(WILDCARD_MULTIPLE) || clauseParts[2].ToString().Contains(WILDCARD_SINGLE)))
                    {
                        composedWhere += string.Format("{0} LIKE {1}", clauseParts[0], clauseParts[2].ToString().Replace(WILDCARD_MULTIPLE, "%").Replace(WILDCARD_SINGLE, "_"));
                    }
                    else
                    {
                        composedWhere += clause;
                    }
                }


                where = where.Replace(clause, composedWhere);


            }

            where = conditionReplace(where); //Change to SQL Operators

            return where;

        }

        private string[] conditionSplit(string where)
        {
            List<string> parts = new List<string>();

            string textToCheck = where;
            bool isValue = false;
            int nextSplitIndex = 0;
            for (int i = 0; i < where.Count(); i++)
            {
                char token = where[i];
                if (char.Equals(token, '"') || char.Equals(token, '\''))
                {
                    isValue = !isValue;
                }

                if ((char.Equals(token, '&') || char.Equals(token, '|')) && !isValue)
                {
                    parts.Add(textToCheck.Substring(nextSplitIndex, i - nextSplitIndex));
                    nextSplitIndex = i + 1;
                }
            }

            parts.Add(textToCheck.Substring(nextSplitIndex, where.Count() - nextSplitIndex));

            return parts.ToArray();
        }

        private string conditionReplace(string where)
        {
            List<string> parts = new List<string>();

            StringBuilder textToCheck = new StringBuilder(where);
            bool isValue = false;
            int addedChars = 0;

            for (int i = 0; i < where.Count(); i++)
            {
                char token = where[i];

                switch (token)
                {
                    case '&':
                        if (!isValue)
                        {
                            textToCheck.Replace(token.ToString(), " AND ", i + addedChars, 1);
                            addedChars += 4;
                        }

                        break;
                    case '|':
                        if (!isValue)
                        {
                            textToCheck.Replace(token.ToString(), " OR ", i + addedChars, 1);
                            addedChars += 3;
                        }
                        break;
                    case '"':
                    case '\'':
                        isValue = !isValue;
                        break;
                }
            }

            return textToCheck.ToString();
        }

        private string applyDateFilter(string dateField, int? beginYear, int? beginMonth, int? beginDay, int? endYear, int? endMonth, int? endDay)
        {
            string whereStatement = "";
            //Apply date filter
            if (beginDay.HasValue && beginMonth.HasValue && beginYear.HasValue && !String.IsNullOrEmpty(dateField))
            {
                if (!string.IsNullOrEmpty(whereStatement)) whereStatement += " AND ";
                whereStatement += String.Format("{3} >= '{0:0000}-{1:00}-{2:00}'", beginYear.Value, beginMonth.Value, beginDay.Value, dateField);
            }
            else if (!beginDay.HasValue && beginMonth.HasValue && beginYear.HasValue && !String.IsNullOrEmpty(dateField))
            {
                var bg = new DateTime((int)beginYear, (int)beginMonth, 1);
                if (!string.IsNullOrEmpty(whereStatement)) whereStatement += " AND ";
                whereStatement += String.Format("{3} >= '{0:0000}-{1:00}-{2:00}'", bg.Year, bg.Month, bg.Day, dateField);
            }

            if (endDay.HasValue && endMonth.HasValue && endYear.HasValue && !String.IsNullOrEmpty(dateField))
            {
                DateTime endDate = new DateTime(endYear.Value, endMonth.Value, endDay.Value);
                endDate = endDate.AddDays(1);

                if (!string.IsNullOrEmpty(whereStatement)) whereStatement += " AND ";
                whereStatement += String.Format("{3} < '{0:0000}-{1:00}-{2:00}'", endDate.Year, endDate.Month, endDate.Day, dateField);
            }
            else if (!endDay.HasValue && endMonth.HasValue && endYear.HasValue && !String.IsNullOrEmpty(dateField))
            {
                var ed = new DateTime((int)endYear, (int)endMonth, 1);
                ed = ed.AddMonths(1);
                if (!string.IsNullOrEmpty(whereStatement)) whereStatement += " AND ";
                whereStatement += String.Format("{3} < '{0:0000}-{1:00}-{2:00}'", ed.Year, ed.Month, ed.Day, dateField);
            }

            return whereStatement;
        }

        private ScriptResponse executeQuery(ContextData context, Entity document, string query)
        {
            ErpBS bsERP = new ErpBS();

            if (context.ExternalSystems == null || context.ExternalSystems.Count == 0)
            {
                throw new Exception("External System em falta");
            }

            var externalSystem = context.ExternalSystems.FirstOrDefault().Value;

            if (!externalSystem.Parameters.ContainsKey("TipoPlataforma"))
            {
                throw new Exception("TipoPlataforma inválido");
            }

            EnumTipoPlataforma tipoPlataforma;
            if (!Enum.TryParse<EnumTipoPlataforma>((string)externalSystem.Parameters["TipoPlataforma"], out tipoPlataforma))
            {
                throw new Exception("TipoPlataforma inválido");
            }

            try
            {
                bsERP.AbreEmpresaTrabalho(tipoPlataforma, externalSystem.Code, (string)externalSystem.Parameters["Username"], (string)externalSystem.Parameters["Password"]);
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
                Result = response
            };
        }


    }
}
