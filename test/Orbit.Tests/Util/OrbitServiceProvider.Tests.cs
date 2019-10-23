﻿using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Orbit.Components;
using Orbit.Util;

using Xunit;

namespace Orbit.Tests.Util
{
    public class OrbitServiceProvider_tests
    {
        [Fact]
        public void Can_instantiate_service()
        {
            using var scope = OrbitServiceProvider.Instance.CreateScope();

            var service = scope.ServiceProvider.GetService<BatteryComponent>();
            service.Should().NotBeNull();
        }
    }
}