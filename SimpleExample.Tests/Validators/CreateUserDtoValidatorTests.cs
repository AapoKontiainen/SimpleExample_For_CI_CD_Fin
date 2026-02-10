using FluentAssertions;
using FluentValidation;
using SimpleExample.Domain.Entities;
using SimpleExample.Application.DTOs;
using Xunit;
using FluentValidation.TestHelper;

namespace SimpleExample.Tests.Validators;

public class ValidatorTests
{
  private readonly IValidator<CreateUserDto> _validator;

  public ValidatorTests()
  {
    var v = new InlineValidator<CreateUserDto>();
    v.RuleFor(x => x.FirstName)
     .NotEmpty().WithMessage("Etunimi on pakollinen");

    _validator = v;
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

