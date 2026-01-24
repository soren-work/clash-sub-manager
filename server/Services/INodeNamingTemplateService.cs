using ClashSubManager.Models;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Node naming template service interface
    /// </summary>
    public interface INodeNamingTemplateService
    {
        /// <summary>
        /// Process template and replace variables
        /// </summary>
        /// <param name="template">Template string</param>
        /// <param name="context">Node naming context</param>
        /// <returns>Processed node name</returns>
        string ProcessTemplate(string template, NodeNamingContext context);

        /// <summary>
        /// Get available template variables
        /// </summary>
        /// <param name="context">Node naming context</param>
        /// <returns>Variable dictionary</returns>
        Dictionary<string, object> GetVariables(NodeNamingContext context);

        /// <summary>
        /// Validate template syntax
        /// </summary>
        /// <param name="template">Template string</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Whether validation passed</returns>
        bool ValidateTemplate(string template, out string errorMessage);

        /// <summary>
        /// Extract variables from proxy node
        /// </summary>
        /// <param name="proxyNode">Proxy node</param>
        /// <param name="index">Node index</param>
        /// <param name="newServer">New server address</param>
        /// <returns>Variable dictionary</returns>
        Dictionary<string, object> ExtractVariables(YamlDotNet.RepresentationModel.YamlMappingNode proxyNode, int index, string newServer);

        /// <summary>
        /// Get naming template (with backward compatibility support)
        /// </summary>
        /// <returns>Naming template</returns>
        string GetNamingTemplate();
    }
}
