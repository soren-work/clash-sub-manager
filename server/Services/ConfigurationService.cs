using ClashSubManager.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.RepresentationModel;
using System.Diagnostics;

namespace ClashSubManager.Services
{
    /// <summary>
    /// Configuration processing service
    /// </summary>
    public class ConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly HttpClient _httpClient;

        public ConfigurationService(ILogger<ConfigurationService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("clash-verge/v1.0.0");
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
                // Parse base template
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

                // Fetch remote subscription content
                var remoteConfig = await FetchRemoteSubscriptionAsync(subscriptionUrl);
                var remoteYaml = ParseRemoteYAML(remoteConfig);

                // Merge remote configuration into template
                MergeYAMLNodes(rootNode, remoteYaml);

                // Extend IP addresses
                await ExtendIPAddressesAsync(rootNode, defaultIPs, dedicatedIPs);

                // Serialize final configuration
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
                _logger.LogError("Error generating configuration: {Message}", ex.Message);
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
                // Add flag parameter to ensure subscription server correctly identifies
                var requestUrl = subscriptionUrl;
                if (requestUrl.Contains('?'))
                {
                    requestUrl += "&flag=clash";
                }
                else
                {
                    requestUrl += "?flag=clash";
                }
                
                _logger.LogInformation("Fetching remote subscription from: {SubscriptionUrl}", requestUrl);
                
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Remote subscription fetched successfully, length: {Length}", content.Length);
                
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching remote subscription from: {Url}", subscriptionUrl);
                _logger.LogError("Error fetching remote subscription: {Message}", ex.Message);
                
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
                _logger.LogError("Error parsing remote YAML: {Message}", ex.Message);
                return new YamlMappingNode();
            }
        }

        /// <summary>
        /// Merge YAML nodes
        /// </summary>
        /// <param name="target">Target node</param>
        /// <param name="source">Source node</param>
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

                // If target node already contains this key, decide how to merge based on type
                if (target.Children.ContainsKey(key))
                {
                    var targetValue = target.Children[key];
                    var sourceValue = entry.Value;

                    // Special handling for proxies and proxy-groups arrays
                    if (keyText == "proxies" || keyText == "proxy-groups")
                    {
                        MergeArrayNodes(targetValue as YamlSequenceNode, sourceValue as YamlSequenceNode);
                    }
                    else
                    {
                        // Overwrite other fields directly
                        target.Children[key] = sourceValue;
                    }
                }
                else
                {
                    // Add new fields directly
                    target.Children[key] = entry.Value;
                }
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

            // Merge array elements (deduplicate)
            var existingItems = new HashSet<string>();
            foreach (var item in target)
            {
                existingItems.Add(item.ToString());
            }

            foreach (var item in source)
            {
                var itemText = item.ToString();
                if (!existingItems.Contains(itemText))
                {
                    target.Add(item);
                    existingItems.Add(itemText);
                }
            }
        }

        /// <summary>
        /// Extend IP addresses
        /// </summary>
        /// <param name="rootNode">Root node</param>
        /// <param name="defaultIPs">Default IP list</param>
        /// <param name="dedicatedIPs">Dedicated IP list</param>
        private async Task ExtendIPAddressesAsync(
            YamlMappingNode rootNode, 
            List<IPRecord> defaultIPs, 
            List<IPRecord> dedicatedIPs)
        {
            try
            {
                // Get all IP records (prioritize dedicated IPs, supplement with default IPs)
                var allIPs = dedicatedIPs.Any() ? dedicatedIPs : defaultIPs;
                
                if (!allIPs.Any())
                {
                    _logger.LogWarning("No IP records available for extension");
                    return;
                }

                // Get proxies node
                if (!rootNode.Children.ContainsKey(new YamlScalarNode("proxies")))
                {
                    rootNode.Children["proxies"] = new YamlSequenceNode();
                }

                var proxiesNode = rootNode.Children["proxies"] as YamlSequenceNode;
                if (proxiesNode == null)
                    return;

                // Extend IP addresses for each proxy node
                var extendedProxies = new List<YamlNode>();
                var existingNames = new HashSet<string>();

                // Collect existing proxy names
                foreach (var proxy in proxiesNode)
                {
                    if (proxy is YamlMappingNode proxyMapping)
                    {
                        var nameNode = proxyMapping.Children.FirstOrDefault(c => 
                            c.Key is YamlScalarNode key && key.Value?.ToString() == "name").Value;
                        
                        if (nameNode != null)
                        {
                            existingNames.Add(nameNode.ToString());
                        }
                    }
                }

                // Create proxy nodes for each IP address
                foreach (var ip in allIPs.OrderByDescending(x => x.PacketLoss).ThenBy(x => x.Latency))
                {
                    foreach (var proxy in proxiesNode)
                    {
                        if (proxy is YamlMappingNode proxyMapping)
                        {
                            // Clone proxy node
                            var newProxy = CloneProxyNode(proxyMapping, ip, existingNames);
                            if (newProxy != null)
                            {
                                extendedProxies.Add(newProxy);
                            }
                        }
                    }
                }

                // Replace proxies node
                rootNode.Children["proxies"] = new YamlSequenceNode(extendedProxies);
                
                _logger.LogInformation("Extended proxies with {AllIPsCount} IP addresses, total proxies: {TotalProxies}", allIPs.Count, extendedProxies.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending IP addresses");
                _logger.LogError("Error extending IPs: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Clone proxy node and update IP address
        /// </summary>
        /// <param name="originalProxy">Original proxy node</param>
        /// <param name="ipRecord">IP record</param>
        /// <param name="existingNames">Existing names set</param>
        /// <returns>New proxy node</returns>
        private YamlMappingNode? CloneProxyNode(YamlMappingNode originalProxy, IPRecord ipRecord, HashSet<string> existingNames)
        {
            try
            {
                var newProxy = new YamlMappingNode();
                
                foreach (var entry in originalProxy.Children)
                {
                    var key = entry.Key as YamlScalarNode;
                    if (key == null)
                        continue;

                    var keyText = key.Value?.ToString();
                    if (string.IsNullOrEmpty(keyText))
                        continue;

                    var value = entry.Value;

                    // Special handling for server field
                    if (keyText == "server")
                    {
                        newProxy.Children[key] = new YamlScalarNode(ipRecord.IPAddress);
                    }
                    // Special handling for port field
                    else if (keyText == "port")
                    {
                        newProxy.Children[key] = new YamlScalarNode(ipRecord.Port.ToString());
                    }
                    // Special handling for name field, add IP suffix to avoid duplicates
                    else if (keyText == "name")
                    {
                        var originalName = value?.ToString() ?? "proxy";
                        var newName = $"{originalName}-{ipRecord.IPAddress}";
                        
                        // Ensure name uniqueness
                        var counter = 1;
                        while (existingNames.Contains(newName))
                        {
                            newName = $"{originalName}-{ipRecord.IPAddress}-{counter}";
                            counter++;
                        }
                        
                        existingNames.Add(newName);
                        newProxy.Children[key] = new YamlScalarNode(newName);
                    }
                    else
                    {
                        // Copy other fields directly
                        newProxy.Children[key] = value;
                    }
                }

                return newProxy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning proxy node");
                return null;
            }
        }
    }
}
