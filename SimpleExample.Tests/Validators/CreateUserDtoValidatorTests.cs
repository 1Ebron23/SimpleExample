using SimpleExample.Application.DTOs;
using SimpleExample.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleExample.Tests.Validators
{

    public class CreateUserDtoValidatorTests
    {
        private readonly CreateUserDtoValidator _validator;

        public CreateUserDtoValidatorTests()
        {
            _validator = new CreateUserDtoValidator();
        }

        [Fact]
        public void Should_Have_Error_When_FirstName_Is_Empty()
        {
            CreateUserDto dto = new CreateUserDto { FirstName = "", LastName = "Meikäläinen", Email = "test@test.com" };
            var result = _validator.TestValidate(dto);

            // VÄÄRÄ ODOTUS - testi epäonnistuu!
            result.ShouldNotHaveAnyValidationErrors();
        }
    }


}
