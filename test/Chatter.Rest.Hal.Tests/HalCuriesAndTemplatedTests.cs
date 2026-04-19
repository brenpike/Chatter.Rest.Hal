using System.Linq;
using FluentAssertions;
using Xunit;

namespace Chatter.Rest.Hal.Tests
{
    public class HalCuriesAndTemplatedTests
    {
        [Fact]
        public void Curies_Are_Parsed_As_Array_Of_LinkObjects()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var curies = res!.Links.SingleOrDefault(l => l.Rel == "curies");
            curies.Should().NotBeNull();
            curies!.LinkObjects.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Templated_Link_Has_Templated_True_If_Provided()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var itemLink = res!.Links.SingleOrDefault(l => l.Rel == "ex:item");
            itemLink.Should().NotBeNull();

            var lo = itemLink!.LinkObjects.SingleOrDefault();
            lo.Should().NotBeNull();

            lo!.Templated.Should().BeTrue();
            lo.Href.Should().Contain("{id}");
        }

        [Fact]
        public void Templated_Href_Does_Not_Automatically_Expand()
        {
            var res = TestHelpers.LoadResourceFromFixture("curies_and_templated.json");

            res.Should().NotBeNull();

            var itemLink = res!.Links.SingleOrDefault(l => l.Rel == "ex:item");
            itemLink.Should().NotBeNull();

            var lo = itemLink!.LinkObjects.SingleOrDefault();
            lo.Should().NotBeNull();

            // The SDK should not expand templates during deserialization; the href should retain template variables
            lo!.Href.Should().Contain("{id}");
        }
    }
}
