//#R Interop.ErpBS900.dll
//#R Interop.StdBE900.dll
//#R Interop.IGcpBS900.dll
//#R Interop.GcpBE900.dll

using System;
using Interop.ErpBS900;
using Interop.StdBE900;
using Interop.GcpBE900;
using Interop.IGcpBS900;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MyMis.Scripting.Core.Contracts;

/*
Developed by: NumbersBelieve
Function: A sample integration in the Primavera ERP. When an interaction is saved or approved in the platform, integrates a "Compras" document of type "ECF".
Parameters: An interaction with a "Supplier" attribute in its header and whose lines are commitments named "GoodsPurchaseRequest". 
*/
namespace myMIS
{
    public class Script
    {
        public ScriptResponse Execute(ContextData context, Entity document, Dictionary<string, object> parameters)
        {
            /* **************************************** */
            /* **************************************** */
            /*          ADD YOUR CODE HERE              */
            ErpBS bsERP = new ErpBS();
            var externalSystem = context.ExternalSystems.FirstOrDefault().Value;
            try
            {
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
                    bsERP.AbreEmpresaTrabalho(tipoPlataforma, (string)externalSystem.Parameters["Company"], (string)externalSystem.Parameters["Username"], (string)externalSystem.Parameters["Password"]);
                }
                catch (Exception e)
                {
                    throw new Exception("Erro a abrir a empresa no ERP: " + e.Message);
                }
                GcpBEDocumentoCompra purchaseOrder = new GcpBEDocumentoCompra();
                purchaseOrder.set_Tipodoc("ECF");
                purchaseOrder.set_Serie("A");
                purchaseOrder.set_TipoEntidade("F");
                purchaseOrder.set_Entidade(document.Attributes.Supplier);
                purchaseOrder.set_NumDocExterno("0");
                purchaseOrder.set_Observacoes("Documento gerado no portal OMNIA: Pedido de Encomenda " + document.NumberSerieCode + "/" + document.Number);
                purchaseOrder.set_DataCarga(document.DateCreated.ToShortDateString());
                purchaseOrder.set_DataDescarga(document.DateCreated.ToShortDateString());

                bsERP.Comercial.Compras.PreencheDadosRelacionados(purchaseOrder);
                foreach (var line in document.Commitments.GoodsPurchaseRequest)
                {
                    bsERP.Comercial.Compras.AdicionaLinha(purchaseOrder, line.Resource, line.Quantity, "A1", "", line.Amount);

                }

                bsERP.Comercial.Compras.Actualiza(purchaseOrder);

                bsERP.FechaEmpresaTrabalho();

                return new ScriptResponse
                {
                    Message = "Integrado documento " + purchaseOrder.get_Tipodoc() + " " + purchaseOrder.get_Serie() + "/" + purchaseOrder.get_NumDoc()
                };

            }
            catch (Exception ex)
            {
                bsERP.FechaEmpresaTrabalho();

                throw ex;

            }
        }

    }
}