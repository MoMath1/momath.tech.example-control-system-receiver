using ExampleControlsSystemReceiver.ConsoleUi;
using Xunit;

namespace ExampleControlsSystemReceiver.Tests
{
    public class CalculatorTests
    {
        [Fact]
        public void Add_ShouldReturnSumOfTwoNumbers()
        {
            // Arrange
            var calculator = new Calculator();
            var a = 5;
            var b = 3;

            // Act
            var result = calculator.Add(a, b);

            // Assert
            Assert.Equal(8, result);
        }

        [Fact]
        public void Subtract_ShouldReturnDifferenceOfTwoNumbers()
        {
            // Arrange
            var calculator = new Calculator();
            var a = 5;
            var b = 3;

            // Act
            var result = calculator.Subtract(a, b);

            // Assert
            Assert.Equal(2, result);
        }
    }
}