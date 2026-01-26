using Microsoft.Extensions.Configuration;
using ClashSubManager.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Cross-platform configuration service implementation
    /// </summary>
    public class PlatformConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PlatformConfigurationService> _logger;
        private readonly IEnvironmentDetector _environmentDetector;
        private readonly IPathResolver _pathResolver;
        private readonly IConfigurationValidator _configurationValidator;
        private readonly HttpClient _httpClient;
        private readonly INodeNamingTemplateService _nodeNamingTemplateService;
        private string? _cachedDataPath;

        public PlatformConfigurationService(
            IConfiguration configuration,
            ILogger<PlatformConfigurationService> logger,
            IEnvironmentDetector environmentDetector,
            IPathResolver pathResolver,
            IConfigurationValidator configurationValidator,
            HttpClient httpClient,
            INodeNamingTemplateService nodeNamingTemplateService)
        {
            _configuration = configuration;
            _logger = logger;
            _environmentDetector = environmentDetector;
            _pathResolver = pathResolver;
            _configurationValidator = configurationValidator;
            _httpClient = httpClient;
            _nodeNamingTemplateService = nodeNamingTemplateService;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("clash-verge/v1.0.0");
        }

        /// <summary>
        /// Get data storage path
        /// </summary>
        /// <returns>Data path</returns>
        public string GetDataPath()
        {
            if (_cachedDataPath != null)
                return _cachedDataPath;

            var configuredPath = GetValue<string>("DataPath");
            if (!string.IsNullOrEmpty(configuredPath))
            {
                _cachedDataPath = _pathResolver.ResolvePath(configuredPath);
            }
            else
            {
                _cachedDataPath = _pathResolver.GetDefaultDataPath();
            }

            // Ensure data directory exists
            try
            {
                Directory.CreateDirectory(_cachedDataPath);
                _logger.LogInformation("Data path configured: {DataPath}", _cachedDataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create data directory: {DataPath}", _cachedDataPath);
                throw new InvalidOperationException($"Cannot create data directory: {_cachedDataPath}", ex);
            }

            return _cachedDataPath;
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Configuration value</returns>
        public T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                var value = _configuration.GetValue(key, defaultValue);
                object? logValue = IsSensitiveKey(key) ? "[REDACTED]" : value;
                _logger.LogDebug("Configuration value retrieved: {Key} = {Value}", key, logValue);
                return value!;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get configuration value for key: {Key}", key);
                return defaultValue!;
            }
        }

        private static bool IsSensitiveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("Key", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if key exists</returns>
        public bool HasValue(string key)
        {
            return _configuration[key] != null;
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public void ValidateConfiguration()
        {
            try
            {
                _configurationValidator.Validate(_configuration);
                _logger.LogInformation("Configuration validation passed");
            }
            catch (ConfigurationException ex)
            {
                _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", ex.ValidationErrors));
                throw;
            }
        }

        /// <summary>
        /// Get environment type
        /// </summary>
        /// <returns>Environment type</returns>
        public string GetEnvironmentType()
        {
            return _environmentDetector.GetEnvironmentType();
        }

        /// <summary>
        /// Get subscription URL template
        /// </summary>
        /// <returns>Subscription URL template</returns>
        public string GetSubscriptionUrlTemplate()
        {
            // Get from environment variable first
            var template = Environment.GetEnvironmentVariable("SUBSCRIPTION_URL_TEMPLATE");
            if (!string.IsNullOrEmpty(template))
            {
                _logger.LogDebug("Subscription URL template from environment: {Template}", template);
                return template;
            }

            // Get from configuration file
            template = GetValue<string>("SubscriptionUrlTemplate");
            if (!string.IsNullOrEmpty(template))
            {
                _logger.LogDebug("Subscription URL template from configuration: {Template}", template);
                return template;
            }

            _logger.LogWarning("Subscription URL template not configured");
            return string.Empty;
        }

        /// <summary>
        /// Get node naming template
        /// </summary>
        /// <returns>Node naming template</returns>
        public string GetNodeNamingTemplate()
        {
            return _nodeNamingTemplateService.GetNamingTemplate();
        }

        /// <summary>
        /// Generate subscription configuration
        /// </summary>
        /// <param name="template">Base template</param>
        /// <param name="subscriptionUrl">Subscription URL</param>
        /// <param name="defaultIPs">Default IP list</param>
        /// <param name="dedicatedIPs">Dedicated IP list</param>
        /// <returns>Generated YAML configuration</returns>
        public async Task<string> GenerateSubscriptionConfigAsync(
            string template, 
            string subscriptionUrl, 
            List<IPRecord> defaultIPs, 
            List<IPRecord> dedicatedIPs)
        {
            try
            {
                // Parse the base YAML template into a manipulable structure
                var yamlStream = new YamlStream();
                using (var reader = new StringReader(template))
                {
                    yamlStream.Load(reader);
                }

                var rootNode = yamlStream.Documents[0].RootNode as YamlMappingNode;
                if (rootNode == null)
                {
                    throw new InvalidOperationException("Invalid YAML template format");
                }

                // Fetch the remote subscription content from the user's subscription service
                var remoteConfig = await FetchRemoteSubscriptionAsync(subscriptionUrl);
                var remoteYaml = ParseRemoteYAML(remoteConfig);

                // Merge remote configuration into the template (template takes priority)
                MergeYAMLNodes(rootNode, remoteYaml);

                // Clean up empty proxy-groups if neither config has them
                CleanupEmptyProxyGroups(rootNode);

                // Extend proxy nodes with optimized IP addresses
                await ExtendIPAddressesAsync(rootNode, defaultIPs, dedicatedIPs);

                // Serialize the final configuration back to YAML string
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var result = serializer.Serialize(rootNode);
                
                _logger.LogInformation("Configuration generated successfully, total IPs: {TotalIPs}", defaultIPs.Count + dedicatedIPs.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating subscription configuration");
                throw;
            }
        }

        /// <summary>
        /// Fetch remote subscription content
        /// </summary>
        /// <param name="subscriptionUrl">Subscription URL</param>
        /// <returns>Subscription content</returns>
        private async Task<string> FetchRemoteSubscriptionAsync(string subscriptionUrl)
        {
            try
            {
                _logger.LogInformation("Fetching remote subscription from: {SubscriptionUrl}", subscriptionUrl);
                
                var response = await _httpClient.GetAsync(subscriptionUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Remote subscription fetched successfully, length: {Length}", content.Length);
                
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching remote subscription from: {Url}", subscriptionUrl);
                
                // Return empty content as fallback
                return string.Empty;
            }
        }

        /// <summary>
        /// Parse remote YAML configuration
        /// </summary>
        /// <param name="remoteConfig">Remote configuration content</param>
        /// <returns>Parsed YAML node</returns>
        private YamlMappingNode ParseRemoteYAML(string remoteConfig)
        {
            if (string.IsNullOrWhiteSpace(remoteConfig))
                return new YamlMappingNode();

            try
            {
                var yamlStream = new YamlStream();
                using (var reader = new StringReader(remoteConfig))
                {
                    yamlStream.Load(reader);
                }

                return yamlStream.Documents[0].RootNode as YamlMappingNode ?? new YamlMappingNode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing remote YAML configuration");
                return new YamlMappingNode();
            }
        }

        /// <summary>
        /// Merges YAML nodes from source into target with special handling for arrays
        /// Template fields take priority over remote subscription fields
        /// </summary>
        /// <param name="target">Target YAML node (template)</param>
        /// <param name="source">Source YAML node (remote subscription)</param>
        private void MergeYAMLNodes(YamlMappingNode target, YamlMappingNode source)
        {
            if (source == null)
                return;

            foreach (var entry in source.Children)
            {
                var key = entry.Key as YamlScalarNode;
                if (key == null)
                    continue;

                var keyText = key.Value?.ToString();
                if (string.IsNullOrEmpty(keyText))
                    continue;

                // If the target already has this key, decide how to merge based on type
                if (target.Children.ContainsKey(key))
                {
                    var targetValue = target.Children[key];
                    var sourceValue = entry.Value;

                    // Special handling for proxies array - merge them instead of overwriting
                    if (keyText == "proxies")
                    {
                        MergeArrayNodes(targetValue as YamlSequenceNode, sourceValue as YamlSequenceNode);
                    }
                    // For proxy-groups, use priority selection instead of merging
                    else if (keyText == "proxy-groups")
                    {
                        // Template (user config) takes priority over remote config
                        // If template has proxy-groups, keep it and ignore remote
                        // If template doesn't have proxy-groups, use remote proxy-groups
                        // If neither has proxy-groups, don't include this field
                        if (targetValue as YamlSequenceNode == null || 
                            ((targetValue as YamlSequenceNode)?.Children.Any() == false))
                        {
                            // Template doesn't have proxy-groups, use remote's
                            target.Children[key] = sourceValue;
                        }
                        // else: template has proxy-groups, keep it (do nothing)
                    }
                    else
                    {
                        // For other fields, template (target) takes priority over remote (source)
                        // So we keep the target value and don't overwrite it
                    }
                }
                else
                {
                    // New field from source - add it to target
                    // Special case for proxy-groups: only add if target doesn't already have it
                    if (keyText == "proxy-groups")
                    {
                        // This means target doesn't have proxy-groups, so we can safely add from source
                        target.Children[key] = entry.Value;
                    }
                    else
                    {
                        target.Children[key] = entry.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Clean up empty proxy-groups node
        /// </summary>
        /// <param name="rootNode">Root YAML node</param>
        private void CleanupEmptyProxyGroups(YamlMappingNode rootNode)
        {
            try
            {
                var proxyGroupsNode = rootNode.Children.FirstOrDefault(c => 
                    (c.Key as YamlScalarNode)?.Value == "proxy-groups");

                if (proxyGroupsNode.Value != null)
                {
                    var proxyGroups = proxyGroupsNode.Value as YamlSequenceNode;
                    
                    // If proxy-groups is empty or null, remove it entirely
                    if (proxyGroups == null || !proxyGroups.Children.Any())
                    {
                        rootNode.Children.Remove(proxyGroupsNode.Key);
                        _logger.LogInformation("Removed empty proxy-groups from configuration");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up empty proxy-groups");
            }
        }

        /// <summary>
        /// Merge array nodes
        /// </summary>
        /// <param name="target">Target array</param>
        /// <param name="source">Source array</param>
        private void MergeArrayNodes(YamlSequenceNode? target, YamlSequenceNode? source)
        {
            if (source == null)
                return;

            if (target == null)
            {
                // If target array doesn't exist, use source array directly
                target = source;
                return;
            }

            // Merge array content, avoid duplicates
            foreach (var item in source.Children)
            {
                if (!target.Children.Contains(item))
                {
                    target.Children.Add(item);
                }
            }
        }

        /// <summary>
        /// Extend IP addresses to proxy configuration
        /// </summary>
        /// <param name="rootNode">Root YAML node</param>
        /// <param name="defaultIPs">Default IP list</param>
        /// <param name="dedicatedIPs">Dedicated IP list</param>
        private async Task ExtendIPAddressesAsync(YamlMappingNode rootNode, List<IPRecord> defaultIPs, List<IPRecord> dedicatedIPs)
        {
            try
            {
                var proxiesNode = rootNode.Children.FirstOrDefault(c => 
                    (c.Key as YamlScalarNode)?.Value == "proxies").Value as YamlSequenceNode;

                if (proxiesNode == null || !proxiesNode.Children.Any())
                    return;

                var allIPs = dedicatedIPs.Any() ? dedicatedIPs : defaultIPs;
                if (!allIPs.Any())
                    return;

                var newProxies = new YamlSequenceNode();
                
                foreach (var proxyNode in proxiesNode.Children)
                {
                    if (proxyNode is YamlMappingNode proxyMapping)
                    {
                        var serverNode = proxyMapping.Children.FirstOrDefault(c => 
                            (c.Key as YamlScalarNode)?.Value == "server");

                        if (serverNode.Value is YamlScalarNode serverScalar)
                        {
                            var originalServer = serverScalar.Value;
                            
                            // Check if the original server is an IP address
                            bool isOriginalServerIP = System.Net.IPAddress.TryParse(originalServer, out _);
                            
                            if (isOriginalServerIP)
                            {
                                // If the original server is an IP address, replace it with Cloudflare IPs
                                // Create a new proxy configuration for each IP address
                                for (int index = 0; index < allIPs.Count; index++)
                                {
                                    var ip = allIPs[index];
                                    var newProxy = CloneProxyNode(proxyMapping);
                                    var newServerNode = newProxy.Children.FirstOrDefault(c => 
                                        (c.Key as YamlScalarNode)?.Value == "server");
                                    
                                    if (newServerNode.Value is YamlScalarNode newServerScalar)
                                    {
                                        newServerScalar.Value = ip.IPAddress;
                                        
                                        // Update port
                                        var portNode = newProxy.Children.FirstOrDefault(c => 
                                            (c.Key as YamlScalarNode)?.Value == "port");
                                        if (portNode.Value is YamlScalarNode portScalar)
                                        {
                                            portScalar.Value = ip.Port.ToString();
                                        }
                                        
                                        // Generate new node name
                                        var nameNode = newProxy.Children.FirstOrDefault(c => 
                                            (c.Key as YamlScalarNode)?.Value == "name");
                                        if (nameNode.Value is YamlScalarNode nameScalar)
                                        {
                                            var originalName = nameScalar.Value;
                                            
                                            // Use new naming template system
                                            var variables = _nodeNamingTemplateService.ExtractVariables(proxyMapping, index, ip.IPAddress);
                                            var context = new NodeNamingContext
                                            {
                                                OriginalName = originalName ?? string.Empty,
                                                Index = index + 1,
                                                Server = ip.IPAddress,
                                                ServerName = originalServer ?? string.Empty,
                                                Port = ip.Port,
                                                CustomProperties = variables!
                                            };
                                            
                                            var namingTemplate = GetNodeNamingTemplate();
                                            var newName = _nodeNamingTemplateService.ProcessTemplate(namingTemplate, context);
                                            
                                            // If template processing fails, use original name as fallback
                                            if (string.IsNullOrEmpty(newName))
                                            {
                                                newName = $"{originalName}-Node-{index + 1}";
                                            }
                                            
                                            nameScalar.Value = newName;
                                        }
                                    }
                                    
                                    newProxies.Add(newProxy);
                                }
                            }
                            else
                            {
                                // If the original server is not an IP address (e.g., domain name), keep the original node
                                newProxies.Add(proxyMapping);
                            }
                        }
                        else
                        {
                            // If there is no server node, keep the original node
                            newProxies.Add(proxyMapping);
                        }
                    }
                }

                // Replace original proxies node
                var proxiesKey = rootNode.Children.FirstOrDefault(c => 
                    (c.Key as YamlScalarNode)?.Value == "proxies").Key;
                rootNode.Children[proxiesKey] = newProxies;

                _logger.LogInformation("Extended proxies with {AllIPsCount} IP addresses", allIPs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending IP addresses");
            }
        }

        /// <summary>
        /// Clone proxy node
        /// </summary>
        /// <param name="original">Original node</param>
        /// <returns>Cloned node</returns>
        private YamlMappingNode CloneProxyNode(YamlMappingNode original)
        {
            var clone = new YamlMappingNode();
            foreach (var child in original.Children)
            {
                // Deep copy: create new node objects instead of copying references
                var clonedKey = CloneYamlNode(child.Key);
                var clonedValue = CloneYamlNode(child.Value);
                clone.Children.Add(clonedKey, clonedValue);
            }
            return clone;
        }

        /// <summary>
        /// Clone YAML node
        /// </summary>
        /// <param name="node">Original node</param>
        /// <returns>Cloned node</returns>
        private YamlNode CloneYamlNode(YamlNode node)
        {
            return node switch
            {
                YamlScalarNode scalar => new YamlScalarNode(scalar.Value),
                YamlMappingNode mapping => CloneProxyNode(mapping),
                YamlSequenceNode sequence => CloneSequenceNode(sequence),
                _ => node
            };
        }

        /// <summary>
        /// Clone sequence node
        /// </summary>
        /// <param name="original">Original sequence node</param>
        /// <returns>Cloned sequence node</returns>
        private YamlSequenceNode CloneSequenceNode(YamlSequenceNode original)
        {
            var clone = new YamlSequenceNode();
            foreach (var child in original.Children)
            {
                clone.Children.Add(CloneYamlNode(child));
            }
            return clone;
        }
    }
}
