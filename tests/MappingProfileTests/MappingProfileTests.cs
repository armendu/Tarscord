using AutoMapper;
using Tarscord.Core.Helpers;
using Xunit;

namespace Tarscord.Core.Tests.MappingProfileTests;

[Trait("Category", "Unit")]
public class MappingProfileTests
{
    [Fact]
    public void CoreMappingProfile_ShouldSucceed()
    {
        var config = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); });

        config.AssertConfigurationIsValid();
    }
}