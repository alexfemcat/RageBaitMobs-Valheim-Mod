using RagebateMobs.Services;
using Xunit;

namespace RagebateMobs.Tests
{
    public class PromptBuilderTests
    {
        [Fact]
        public void BuildInsultPrompt_SpottedPlayer_ContainsContext()
        {
            // Arrange
            string mobName = "Greydwarf";
            string playerName = "TestPlayer";

            // Act
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "spotted_player", playerName);

            // Assert
            Assert.NotNull(prompt);
            Assert.NotEmpty(prompt);
            Assert.Contains(mobName, prompt);
            Assert.Contains(playerName, prompt);
            Assert.Contains("spotted", prompt, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildInsultPrompt_TookDamage_ContainsContext()
        {
            // Arrange
            string mobName = "Troll";
            string playerName = "TestPlayer";

            // Act
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "took_damage", playerName);

            // Assert
            Assert.NotNull(prompt);
            Assert.NotEmpty(prompt);
            Assert.Contains(mobName, prompt);
            Assert.Contains(playerName, prompt);
            Assert.Contains("hit", prompt, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildInsultPrompt_ContainsInstructionForMeanness()
        {
            // Arrange
            string mobName = "Golem";

            // Act
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "spotted_player", "Player1");

            // Assert
            Assert.Contains("BRUTAL", prompt);
            Assert.Contains("toxic", prompt, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("gaming", prompt, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildInsultPrompt_IncludesGamingSlang()
        {
            // Arrange
            string mobName = "Draugr";

            // Act
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "took_damage", "Player2");

            // Assert
            // Should include references to gaming insults
            Assert.Contains("dogwater", prompt, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("skill issue", prompt, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildInsultPrompt_ConstrainedLength()
        {
            // Arrange
            string mobName = "Wraith";

            // Act
            string prompt = PromptBuilder.BuildInsultPrompt(mobName, "spotted_player", "Player3");

            // Assert
            // Prompt should exist and be reasonable length (not infinitely long)
            Assert.True(prompt.Length < 2000, "Prompt should be reasonably constrained");
            Assert.True(prompt.Length > 100, "Prompt should have meaningful content");
        }
    }
}
