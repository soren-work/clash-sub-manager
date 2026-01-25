using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClashSubManager.Services;
using ClashSubManager.Models;
using YamlDotNet.RepresentationModel;
using Xunit;
using Moq;

namespace ClashSubManager.Tests.Services
{
    /// <summary>
    /// Node naming template service tests
    /// </summary>
    public class NodeNamingTemplateServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<NodeNamingTemplateService>> _mockLogger;
        private readonly NodeNamingTemplateService _service;

        public NodeNamingTemplateServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<NodeNamingTemplateService>>();
            _service = new NodeNamingTemplateService(_mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public void ProcessTemplate_WithValidVariables_ReturnsCorrectResult()
        {
            // Arrange
            var template = "{name}-Node-{index}";
            var context = new NodeNamingContext
            {
                OriginalName = "HK-Server",
                Index = 1,
                Server = "104.16.1.1",
                Port = 443,
                Type = "VLESS"
            };

            // Act
            var result = _service.ProcessTemplate(template, context);

            // Assert
            Assert.Equal("HK-Server-Node-1", result);
        }

        [Fact]
        public void ProcessTemplate_WithMultipleVariables_ReturnsCorrectResult()
        {
            // Arrange
            var template = "{name}-{type}-{index}-{server}";
            var context = new NodeNamingContext
            {
                OriginalName = "US-Proxy",
                Index = 2,
                Server = "104.16.2.1",
                Port = 8080,
                Type = "VMess"
            };

            // Act
            var result = _service.ProcessTemplate(template, context);

            // Assert
            Assert.Equal("US-Proxy-VMess-2-104.16.2.1", result);
        }

        [Fact]
        public void ProcessTemplate_WithMultiLevelVariables_ReturnsCorrectResult()
        {
            // Arrange
            var template = "Custom-{proxy.type}-Node-{node.index}";
            var context = new NodeNamingContext
            {
                OriginalName = "Test-Server",
                Index = 3,
                Server = "104.16.3.1",
                Type = "VLESS"
            };

            // Act
            var result = _service.ProcessTemplate(template, context);

            // Assert
            Assert.Equal("Custom-VLESS-Node-3", result);
        }

        [Fact]
        public void ProcessTemplate_WithEmptyTemplate_ReturnsEmpty()
        {
            // Arrange
            var template = "";
            var context = new NodeNamingContext { OriginalName = "Test" };

            // Act
            var result = _service.ProcessTemplate(template, context);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void ProcessTemplate_WithMissingVariable_PreservesPlaceholder()
        {
            // Arrange
            var template = "{name}-{nonexistent}";
            var context = new NodeNamingContext
            {
                OriginalName = "Original",
                Index = 1
            };

            // Act
            var result = _service.ProcessTemplate(template, context);

            // Assert - Missing variables should preserve their placeholders
            Assert.Equal("Original-{nonexistent}", result);
        }

        [Fact]
        public void GetVariables_ReturnsAllExpectedVariables()
        {
            // Arrange
            var context = new NodeNamingContext
            {
                OriginalName = "Test-Server",
                Index = 5,
                Server = "104.16.5.1",
                ServerName = "example.com",
                Port = 443,
                Type = "VLESS",
                Uuid = "12345678-1234-1234-1234-123456789abc",
                Network = "vless"
            };

            // Act
            var variables = _service.GetVariables(context);

            // Assert
            Assert.Equal("Test-Server", variables["name"]);
            Assert.Equal(5, variables["index"]);
            Assert.Equal("104.16.5.1", variables["server"]);
            Assert.Equal("example.com", variables["servername"]);
            Assert.Equal(443, variables["port"]);
            Assert.Equal("VLESS", variables["type"]);
            Assert.Equal("12345678-1234-1234-1234-123456789abc", variables["uuid"]);
            Assert.Equal("vless", variables["network"]);

            // Check multi-level variables
            Assert.Equal("Test-Server", variables["proxy.name"]);
            Assert.Equal(5, variables["node.index"]);
            Assert.Equal("VLESS", variables["proxy.type"]);
        }

        [Fact]
        public void ValidateTemplate_WithValidTemplate_ReturnsTrue()
        {
            // Arrange
            var template = "{name}-Node-{index}";

            // Act
            var result = _service.ValidateTemplate(template, out var errorMessage);

            // Assert
            Assert.True(result);
            Assert.Equal("", errorMessage);
        }

        [Fact]
        public void ValidateTemplate_WithUnbalancedBraces_ReturnsFalse()
        {
            // Arrange
            var template = "{name}-Node-{index";

            // Act
            var result = _service.ValidateTemplate(template, out var errorMessage);

            // Assert
            Assert.False(result);
            Assert.Equal("Unbalanced braces in template", errorMessage);
        }

        [Fact]
        public void ValidateTemplate_WithInvalidVariableName_ReturnsFalse()
        {
            // Arrange
            var template = "{name}-Node-{123invalid}";

            // Act
            var result = _service.ValidateTemplate(template, out var errorMessage);

            // Assert
            Assert.False(result);
            Assert.Equal("Invalid variable name: 123invalid", errorMessage);
        }

        [Fact]
        public void ValidateTemplate_WithEmptyTemplate_ReturnsFalse()
        {
            // Arrange
            var template = "";

            // Act
            var result = _service.ValidateTemplate(template, out var errorMessage);

            // Assert
            Assert.False(result);
            Assert.Equal("Template cannot be null or empty", errorMessage);
        }

        [Fact]
        public void ExtractVariables_WithValidProxyNode_ReturnsCorrectVariables()
        {
            // Arrange
            var proxyNode = CreateTestProxyNode();
            var index = 2;
            var newServer = "104.16.2.1";

            // Act
            var variables = _service.ExtractVariables(proxyNode, index, newServer);

            // Assert
            Assert.Equal("Test-Proxy", variables["name"]);
            Assert.Equal(3, variables["index"]);
            Assert.Equal("104.16.2.1", variables["server"]);
            Assert.Equal("example.com", variables["servername"]);
            Assert.Equal(443, variables["port"]);
            Assert.Equal("vless", variables["type"]);
            Assert.Equal("12345678-1234-1234-1234-123456789abc", variables["uuid"]);
            Assert.Equal("", variables["network"]); // network字段不存在，应该为空
        }

        [Fact]
        public void GetNamingTemplate_WithNodeNamingTemplateConfig_ReturnsTemplate()
        {
            // Arrange
            var expectedTemplate = "{name}-Custom-{index}";
            _mockConfiguration.Setup(c => c["NodeNamingTemplate"]).Returns(expectedTemplate);

            // Act
            var result = _service.GetNamingTemplate();

            // Assert
            Assert.Equal(expectedTemplate, result);
        }

        [Fact]
        public void GetNamingTemplate_WithUseIpInNodeNameTrue_ReturnsServerTemplate()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["NodeNamingTemplate"]).Returns((string?)null);
            _mockConfiguration.Setup(c => c["UseIpInNodeName"]).Returns("true");

            // Act
            var result = _service.GetNamingTemplate();

            // Assert
            Assert.Equal("{name}-{server}", result);
        }

        [Fact]
        public void GetNamingTemplate_WithUseIpInNodeNameFalse_ReturnsIndexTemplate()
        {
            // Arrange
            _mockConfiguration.Setup(c => c["NodeNamingTemplate"]).Returns((string?)null);
            _mockConfiguration.Setup(c => c["UseIpInNodeName"]).Returns("false");

            // Act
            var result = _service.GetNamingTemplate();

            // Assert
            Assert.Equal("{name}-Node-{index}", result);
        }

        /// <summary>
        /// Creates test proxy node
        /// </summary>
        private YamlMappingNode CreateTestProxyNode()
        {
            var proxyNode = new YamlMappingNode();
            
            // Add basic properties
            proxyNode.Children.Add(new YamlScalarNode("name"), new YamlScalarNode("Test-Proxy"));
            proxyNode.Children.Add(new YamlScalarNode("type"), new YamlScalarNode("vless"));
            proxyNode.Children.Add(new YamlScalarNode("server"), new YamlScalarNode("example.com"));
            proxyNode.Children.Add(new YamlScalarNode("port"), new YamlScalarNode("443"));
            proxyNode.Children.Add(new YamlScalarNode("uuid"), new YamlScalarNode("12345678-1234-1234-1234-123456789abc"));
            
            return proxyNode;
        }
    }
}
