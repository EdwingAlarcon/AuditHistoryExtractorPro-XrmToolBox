using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AuditHistoryExtractorPro.XrmToolBox.Core.Parsing
{
    /// <summary>
    /// Parsea el XML del atributo "changedata" de la entidad "audit" a pares
    /// atributo/valor. Dataverse expone ese XML con la forma:
    /// &lt;audit&gt;
    ///   &lt;oldValues&gt;&lt;atributo value="..." /&gt;...&lt;/oldValues&gt;
    ///   &lt;newValues&gt;&lt;atributo value="..." /&gt;...&lt;/newValues&gt;
    /// &lt;/audit&gt;
    /// donde cada nodo hijo de oldValues/newValues se llama como el atributo lógico
    /// y su valor va en el atributo XML "value" (ausente cuando el valor es null).
    /// </summary>
    public static class AuditChangeDataParser
    {
        public static AuditChangeData Parse(string changeDataXml)
        {
            var resultado = new AuditChangeData();
            if (string.IsNullOrWhiteSpace(changeDataXml))
                return resultado;

            XDocument doc;
            try
            {
                doc = XDocument.Parse(changeDataXml);
            }
            catch (Exception)
            {
                return resultado;
            }

            var root = doc.Root;
            if (root == null) return resultado;

            LlenarValores(root.Element("oldValues"), resultado.OldValues);
            LlenarValores(root.Element("newValues"), resultado.NewValues);

            return resultado;
        }

        private static void LlenarValores(XElement contenedor, IDictionary<string, string> destino)
        {
            if (contenedor == null) return;

            foreach (var campo in contenedor.Elements())
            {
                var nombre = campo.Name.LocalName;
                if (string.IsNullOrWhiteSpace(nombre)) continue;

                var valor = campo.Attribute("value")?.Value ?? campo.Value;
                destino[nombre] = valor;
            }
        }
    }

    /// <summary>Resultado del parseo: valores anteriores y nuevos por nombre de atributo.</summary>
    public class AuditChangeData
    {
        public IDictionary<string, string> OldValues { get; } = new Dictionary<string, string>();
        public IDictionary<string, string> NewValues { get; } = new Dictionary<string, string>();
    }
}
