using core;
using FluentAssertions;
using System;
using Xunit;

namespace woffu.core.tests
{
    public class time_should
    {
        [Theory]
        [InlineData("09:00:00", null, "09:00:00", "10:00:00")]
        [InlineData(null, "10:00:00", "09:00:00", "10:00:00")]
        public void throw_argumentnullexception(string startTime, string endTime, string trueStartTime, string trueEndTime)
        {
            Action sut = () => Time.Create(startTime, endTime, trueStartTime, trueEndTime);
            sut.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void create()
        {
            var sut = Time.Create("09:00:00", "10:00:00", null, null);
            sut.StartTime.Should().Be(9);
            sut.EndTime.Should().Be(10);
            sut.TrueStartTime.Should().BeNull();
            sut.TrueEndTime.Should().BeNull();
        }
    }
}
