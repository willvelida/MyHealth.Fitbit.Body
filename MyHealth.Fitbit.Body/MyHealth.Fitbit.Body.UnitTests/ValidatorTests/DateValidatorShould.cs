using MyHealth.Fitbit.Body.Validators;
using Xunit;

namespace MyHealth.Fitbit.Body.UnitTests.ValidatorTests
{
    public class DateValidatorShould
    {
        private DateValidator _sut;

        public DateValidatorShould()
        {
            _sut = new DateValidator();
        }

        [Fact]
        public void ReturnFalseIfDateIsNotInValidFormat()
        {
            // Arrange
            string testWeightDate = "100/12/2021";

            // Act
            var response = _sut.IsDateValid(testWeightDate);

            // Assert
            Assert.False(response);
        }

        [Fact]
        public void ReturnTrueIfDateIsInValidFormat()
        {
            // Arrange
            string testWeightDate = "2020-12-31";

            // Act
            var response = _sut.IsDateValid(testWeightDate);

            // Assert
            Assert.True(response);
        }
    }
}
