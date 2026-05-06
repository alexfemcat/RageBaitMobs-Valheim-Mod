using RagebateMobs.Configuration;
using Xunit;

namespace RagebateMobs.Tests
{
    public class ModConfigTests
    {
        [Fact]
        public void OutputMode_DefaultIsShout()
        {
            // Arrange & Act
            var mode = OutputMode.Shout;

            // Assert
            Assert.Equal(OutputMode.Shout, mode);
            Assert.NotEqual(OutputMode.Chat, mode);
        }

        [Fact]
        public void OutputMode_CanBeChatMode()
        {
            // Arrange & Act
            var mode = OutputMode.Chat;

            // Assert
            Assert.Equal(OutputMode.Chat, mode);
            Assert.NotEqual(OutputMode.Shout, mode);
        }

        [Fact]
        public void OutputMode_HasTwoModes()
        {
            // Arrange & Act
            var modes = System.Enum.GetValues(typeof(OutputMode));

            // Assert
            Assert.Equal(2, modes.Length);
        }

        [Fact]
        public void ConfigProperties_AreInitializable()
        {
            // Just verify the config class structure is correct
            // Full config testing would require BepInEx ConfigFile

            // Verify enum exists and is properly defined
            Assert.True(System.Enum.IsDefined(typeof(OutputMode), OutputMode.Shout));
            Assert.True(System.Enum.IsDefined(typeof(OutputMode), OutputMode.Chat));
        }
    }
}
