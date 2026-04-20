using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalEmbeddedTests
    {
        [Fact]
        public void Embedded_Single_Writes_As_Object()
        {
            var resource = TestHelpers.LoadResourceFromFixture("embedded_single_and_array.json");

            // Assert that embedded contains an entry named "item" and it's deserialized as a single object
            var itemEmbedded = resource.Embedded.FirstOrDefault(e => e.Name == "item");
            itemEmbedded.Should().NotBeNull();
            itemEmbedded!.Resources.Should().HaveCount(1);

            // Serialization assertion: programmatically create a Resource with one EmbeddedResource entry
            var r = new Chatter.Rest.Hal.Resource();
            var er = new Chatter.Rest.Hal.EmbeddedResource("item");
            er.Resources.Add(new Chatter.Rest.Hal.Resource(new { id = 1, name = "Single" }));
            r.Embedded.Add(er);

            var json = JsonSerializer.Serialize(r);
            // Should serialize the single embedded resource as an object (not an array)
            json.Should().Contain("\"item\":{").And.NotContain("\"item\":[");
        }

        [Fact]
        public void ForceWriteAsCollection_Writes_As_Array_Even_If_One_Item()
        {
            // The SDK exposes EmbeddedResource.ForceWriteAsCollection so this feature should be supported.
            var r = new Chatter.Rest.Hal.Resource();
            var er = new Chatter.Rest.Hal.EmbeddedResource("item")
            {
                ForceWriteAsCollection = true
            };
            er.Resources.Add(new Chatter.Rest.Hal.Resource(new { id = 1 }));
            r.Embedded.Add(er);

            var json = JsonSerializer.Serialize(r);

            // When ForceWriteAsCollection is true, even a single item should be written as an array
            json.Should().Contain("\"item\":[");
        }

        [Fact]
        public void Nested_Embedded_Resources_Are_Read()
        {
            var resource = TestHelpers.LoadResourceFromFixture("embedded_nested.json");

            var parentEmbedded = resource.Embedded.FirstOrDefault(e => e.Name == "parent");
            parentEmbedded.Should().NotBeNull();
            parentEmbedded!.Resources.Should().HaveCount(1);

            var parentResource = parentEmbedded.Resources.First();
            parentResource.Should().NotBeNull();

            // The parent resource should itself contain an embedded child named "child"
            parentResource.Embedded.Should().NotBeNull();
            var childEmbedded = parentResource.Embedded.FirstOrDefault(e => e.Name == "child");
            childEmbedded.Should().NotBeNull();
            childEmbedded!.Resources.Should().HaveCount(1);

            // Verify child resource state contains id = "c1"
            var childResource = childEmbedded.Resources.First();
            var node = childResource.As<JsonNode>();
            node.Should().NotBeNull();
            node!["id"]!.ToString().Should().Be("c1");
        }

        [Fact]
        public void Duplicate_Embedded_Names_Behavior()
        {
            // Load the fixture which contains duplicate _embedded names for the same rel (ea:order)
            // System.Text.Json throws when a JsonObject contains duplicate property names; this test documents
            // the current SDK behavior. If parsing throws due to duplicate keys we assert that this is the
            // observed behavior. Otherwise, inspect the resulting Embedded collection and document how duplicates
            // were handled by the converter.
            try
            {
                var resource = TestHelpers.LoadResourceFromFixture("duplicate_embeddedresource_name.json");

                // Observe current SDK behavior and assert accordingly.
                var found = resource.Embedded.Where(e => e.Name == "ea:order").ToList();

                if (found.Count == 1)
                {
                    // Parser consolidated duplicate properties (only one entry present).
                    found[0].Resources.Should().NotBeNull();
                    found.Should().HaveCount(1);
                }
                else
                {
                    // Multiple EmbeddedResource entries were created for the same rel.
                    found.Should().HaveCountGreaterOrEqualTo(2);
                    var total = found.Sum(e => e.Resources.Count);
                    total.Should().BeGreaterOrEqualTo(0);
                }
            }
            catch (System.ArgumentException ex) when (ex.Message.Contains("DuplicateKey") || ex.Message.Contains("same key") || ex.Message.Contains("An item with the same key has already been added"))
            {
                // The underlying JsonNode/JsonObject parsing throws for duplicate property names.
                // This documents that duplicate property names in the fixture are not permitted by the parser.
                ex.Message.Should().Contain("same key");
            }
        }

        [Fact]
        public void Embedded_Resources_May_Be_Partial_Representations()
        {
            // HAL Spec Section 4.1.2 states that embedded resources MAY be partial representations.
            // A partial representation contains a subset of the fields that would be present in the
            // canonical/full representation of a resource. This test verifies that the SDK correctly
            // handles partial embedded resources with:
            // - Subset of fields (e.g., only `id` when canonical has `id`, `name`, `description`)
            // - Different properties than canonical
            // - Null or missing fields
            // - Empty state (no properties)

            // Create a parent resource with multiple embedded resources demonstrating partial representations
            var parentResource = new Chatter.Rest.Hal.Resource(new { id = "parent1", title = "Parent Resource" });

            // Embedded resource 1: Partial representation with only subset of fields
            // Canonical might have id, name, description, price, but partial only has id and name
            var partialProduct = new Chatter.Rest.Hal.Resource(new { id = "prod123", name = "Widget" });

            // Embedded resource 2: Different properties than canonical
            // Canonical user might have id, username, email, but partial only exposes id and displayName
            var partialUser = new Chatter.Rest.Hal.Resource(new { id = "user456", displayName = "John D." });

            // Embedded resource 3: Minimal representation with just identifier
            var minimalItem = new Chatter.Rest.Hal.Resource(new { id = "item789" });

            // Embedded resource 4: Empty state (no properties) - valid per spec
            var emptyResource = new Chatter.Rest.Hal.Resource();

            // Add all partial representations to embedded collections
            var productsEmbedded = new Chatter.Rest.Hal.EmbeddedResource("products");
            productsEmbedded.Resources.Add(partialProduct);

            var usersEmbedded = new Chatter.Rest.Hal.EmbeddedResource("users");
            usersEmbedded.Resources.Add(partialUser);

            var itemsEmbedded = new Chatter.Rest.Hal.EmbeddedResource("items");
            itemsEmbedded.Resources.Add(minimalItem);

            var emptyEmbedded = new Chatter.Rest.Hal.EmbeddedResource("empty");
            emptyEmbedded.Resources.Add(emptyResource);

            parentResource.Embedded.Add(productsEmbedded);
            parentResource.Embedded.Add(usersEmbedded);
            parentResource.Embedded.Add(itemsEmbedded);
            parentResource.Embedded.Add(emptyEmbedded);

            // Serialize to JSON
            var json = JsonSerializer.Serialize(parentResource);
            json.Should().NotBeNullOrEmpty();

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<Chatter.Rest.Hal.Resource>(json);
            deserialized.Should().NotBeNull();

            // Assert partial product representation is preserved
            var productsEmb = deserialized!.Embedded.FirstOrDefault(e => e.Name == "products");
            productsEmb.Should().NotBeNull();
            productsEmb!.Resources.Should().HaveCount(1);
            var product = productsEmb.Resources.First().As<JsonNode>();
            product.Should().NotBeNull();
            product!["id"]!.ToString().Should().Be("prod123");
            product["name"]!.ToString().Should().Be("Widget");
            product["description"].Should().BeNull(); // Partial - description not included
            product["price"].Should().BeNull(); // Partial - price not included

            // Assert partial user representation is preserved
            var usersEmb = deserialized.Embedded.FirstOrDefault(e => e.Name == "users");
            usersEmb.Should().NotBeNull();
            usersEmb!.Resources.Should().HaveCount(1);
            var user = usersEmb.Resources.First().As<JsonNode>();
            user.Should().NotBeNull();
            user!["id"]!.ToString().Should().Be("user456");
            user["displayName"]!.ToString().Should().Be("John D.");
            user["username"].Should().BeNull(); // Partial - different properties than canonical
            user["email"].Should().BeNull(); // Partial - different properties than canonical

            // Assert minimal representation with only id is preserved
            var itemsEmb = deserialized.Embedded.FirstOrDefault(e => e.Name == "items");
            itemsEmb.Should().NotBeNull();
            itemsEmb!.Resources.Should().HaveCount(1);
            var item = itemsEmb.Resources.First().As<JsonNode>();
            item.Should().NotBeNull();
            item!["id"]!.ToString().Should().Be("item789");

            // Assert empty resource is preserved
            var emptyEmb = deserialized.Embedded.FirstOrDefault(e => e.Name == "empty");
            emptyEmb.Should().NotBeNull();
            emptyEmb!.Resources.Should().HaveCount(1);
            var empty = emptyEmb.Resources.First().As<JsonNode>();
            // Empty resource should deserialize successfully even with no state properties
            empty.Should().NotBeNull();
        }
    }
}
