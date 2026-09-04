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

            LlenarValores(root.Element("oldValues"), resultado.OldValues, resultado.LookupOldValues);
            LlenarValores(root.Element("newValues"), resultado.NewValues, resultado.LookupNewValues);

            return resultado;
        }

        /// <summary>
        /// Para un atributo lookup, Dataverse escribe el Id en el atributo XML "value" y el
        /// nombre legible (display name) como texto del nodo (ej. &lt;ownerid value="{guid}"&gt;
        /// Juan Pérez&lt;/ownerid&gt;). Los atributos no-lookup no tienen texto de nodo (nodo
        /// autocontenido, solo "value") — por eso alcanza con chequear que haya texto y que
        /// difiera del Id para separar uno de otro sin conocer el tipo de atributo de antemano.
        /// </summary>
        private static void LlenarValores(XElement contenedor, IDictionary<string, string> destino, IDictionary<string, string> destinoLookup)
        {
            if (contenedor == null) return;

            foreach (var campo in contenedor.Elements())
            {
                var nombre = campo.Name.LocalName;
                if (string.IsNullOrWhiteSpace(nombre)) continue;

                var valorAttr = campo.Attribute("value")?.Value;
                var valor = valorAttr ?? campo.Value;
                destino[nombre] = valor;

                var textoNodo = campo.Value;
                if (valorAttr != null && !string.IsNullOrWhiteSpace(textoNodo) &&
                    !string.Equals(textoNodo, valorAttr, System.StringComparison.Ordinal))
                {
                    destinoLookup[nombre] = textoNodo;
                }
            }
        }
    }

    /// <summary>Resultado del parseo: valores anteriores y nuevos por nombre de atributo.</summary>
    public class AuditChangeData
    {
        public IDictionary<string, string> OldValues { get; } = new Dictionary<string, string>();
        public IDictionary<string, string> NewValues { get; } = new Dictionary<string, string>();

        /// <summary>Display name del valor anterior, solo para atributos lookup. Ver <see cref="AuditChangeDataParser"/>.</summary>
        public IDictionary<string, string> LookupOldValues { get; } = new Dictionary<string, string>();

        /// <summary>Display name del valor nuevo, solo para atributos lookup.</summary>
        public IDictionary<string, string> LookupNewValues { get; } = new Dictionary<string, string>();
    }
}
