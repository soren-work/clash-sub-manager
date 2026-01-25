using Microsoft.Extensions.Configuration;
using ClashSubManager.Models;
using YamlDotNet.RepresentationModel;
using System.Text.RegularExpressions;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Node naming template service implementation
    /// </summary>
    public class NodeNamingTemplateService : INodeNamingTemplateService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NodeNamingTemplateService> _logger;
        private readonly Dictionary<string, Regex> _compiledPatterns = new();

        public NodeNamingTemplateService(
            IConfiguration configuration,
            ILogger<NodeNamingTemplateService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Pre-compile regex patterns for performance
            _compiledPatterns["variable"] = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);
            _compiledPatterns["validVariable"] = new Regex(@"^[a-zA-Z][a-zA-Z0-9_]*(?:\.[a-zA-Z][a-zA-Z0-9_]*)*$", RegexOptions.Compiled);
        }

        /// <summary>
        /// Processes template and replaces variables
        /// </summary>
        public string ProcessTemplate(string template, NodeNamingContext context)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            try
            {
                var variables = GetVariables(context);
                var result = template;

                // Replace all variables
                var matches = _compiledPatterns["variable"].Matches(template);
                foreach (Match match in matches.Reverse())
                {
                    var variableName = match.Groups[1].Value;
                    if (variables.TryGetValue(variableName, out var value))
                    {
                        var stringValue = value?.ToString() ?? string.Empty;
                        result = result.Remove(match.Index, match.Length)
                                     .Insert(match.Index, stringValue);
                    }
                    else
                    {
                        _logger.LogWarning("Variable '{VariableName}' not found in context", variableName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing template '{Template}'", template);
                return context.OriginalName; // Return original name as fallback
            }
        }

        /// <summary>
        /// Gets available template variables
        /// </summary>
        public Dictionary<string, object> GetVariables(NodeNamingContext context)
        {
            var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Basic variables
            variables["name"] = context.OriginalName;
            variables["index"] = context.Index;
            variables["network"] = context.Network;
            variables["port"] = context.Port;
            variables["server"] = context.Server;
            variables["servername"] = context.ServerName;
            variables["type"] = context.Type;
            variables["uuid"] = context.Uuid;

            // Multi-level variables - proxy.*
            variables["proxy.name"] = context.OriginalName;
            variables["proxy.type"] = context.Type;
            variables["proxy.port"] = context.Port;
            variables["proxy.server"] = context.Server;
            variables["proxy.servername"] = context.ServerName;
            variables["proxy.uuid"] = context.Uuid;
            variables["proxy.network"] = context.Network;

            // Multi-level variables - node.*
            variables["node.index"] = context.Index;

            // Custom properties
            foreach (var kvp in context.CustomProperties)
            {
                variables[kvp.Key] = kvp.Value;
                variables[$"custom.{kvp.Key}"] = kvp.Value;
            }

            return variables;
        }

        /// <summary>
        /// Validates template syntax
        /// </summary>
        public bool ValidateTemplate(string template, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(template))
            {
                errorMessage = "Template cannot be null or empty";
                return false;
            }

            try
            {
                // Check if braces are balanced
                var openBraces = template.Count(c => c == '{');
                var closeBraces = template.Count(c => c == '}');

                if (openBraces != closeBraces)
                {
                    errorMessage = "Unbalanced braces in template";
                    return false;
                }

                // Validate variable names
                var matches = _compiledPatterns["variable"].Matches(template);
                foreach (Match match in matches)
                {
                    var variableName = match.Groups[1].Value;
                    if (!_compiledPatterns["validVariable"].IsMatch(variableName))
                    {
                        errorMessage = $"Invalid variable name: {variableName}";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Template validation error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Extracts variables from proxy node
        /// </summary>
        public Dictionary<string, object> ExtractVariables(YamlMappingNode proxyNode, int index, string newServer)
        {
            var variables = new Dictionary<string, object>();

            try
            {
                // Extract basic variables
                variables["name"] = ExtractStringValue(proxyNode, "name") ?? string.Empty;
                variables["index"] = index + 1;
                variables["server"] = newServer;

                // Extract protocol-specific variables
                var type = ExtractStringValue(proxyNode, "type");
                var network = ExtractStringValue(proxyNode, "network");
                
                variables["type"] = type?.ToLower() ?? string.Empty;        // Service type: vless, vmess
                variables["network"] = network?.ToLower() ?? string.Empty;  // Network type: ws, grpc

                variables["port"] = ExtractIntValue(proxyNode, "port");
                variables["servername"] = ExtractStringValue(proxyNode, "server") ?? string.Empty;
                variables["uuid"] = ExtractStringValue(proxyNode, "uuid") ?? string.Empty;

                // Create multi-level variables
                CreateNestedVariables(variables);

                _logger.LogDebug("Extracted {Count} variables from proxy node", variables.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting variables from proxy node");
            }

            return variables;
        }

        /// <summary>
        /// Creates multi-level variables
        /// </summary>
        private void CreateNestedVariables(Dictionary<string, object> variables)
        {
            try
            {
                // Create proxy.* variables
                var proxyKeys = new[] { "name", "type", "port", "server", "servername", "uuid", "network" };
                foreach (var key in proxyKeys)
                {
                    if (variables.TryGetValue(key, out var value))
                    {
                        variables[$"proxy.{key}"] = value;
                    }
                }

                // Create node.* variables
                if (variables.TryGetValue("index", out var indexValue))
                {
                    variables["node.index"] = indexValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating nested variables");
            }
        }

        /// <summary>
        /// Extracts string value
        /// </summary>
        private string? ExtractStringValue(YamlMappingNode node, string key)
        {
            var child = node.Children.FirstOrDefault(c => 
                (c.Key as YamlScalarNode)?.Value == key);

            if (child.Value is YamlScalarNode scalar)
            {
                return scalar.Value;
            }

            return null;
        }

        /// <summary>
        /// Extracts integer value
        /// </summary>
        private int ExtractIntValue(YamlMappingNode node, string key)
        {
            var stringValue = ExtractStringValue(node, key);
            if (int.TryParse(stringValue, out var intValue))
            {
                return intValue;
            }

            return 0;
        }

        /// <summary>
        /// Gets naming template (supports backward compatibility)
        /// </summary>
        public string GetNamingTemplate()
        {
            // Check new template configuration
            var template = _configuration["NodeNamingTemplate"];
            if (!string.IsNullOrEmpty(template))
            {
                return template;
            }

            // Migrate from old UseIpInNodeName setting
            var useIpFormatStr = _configuration["UseIpInNodeName"];
            var useIpFormat = bool.TryParse(useIpFormatStr, out var result) && result;
            return useIpFormat 
                ? "{name}-{server}" 
                : "{name}-Node-{index}";
        }
    }
}
