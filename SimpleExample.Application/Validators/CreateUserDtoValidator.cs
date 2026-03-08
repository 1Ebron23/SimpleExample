using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using SimpleExample.Application.DTOs;


namespace SimpleExample.Application.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Etunimi on pakollinen")
                .MinimumLength(3).WithMessage("Etunimen tulee olla vahintaan 3 merkkia pitka.")
                .MaximumLength(100).WithMessage("Etunimi voi olla enintaan 100 merkkia pitka.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Sukunimi  on pakollinen")
                .MinimumLength(3).WithMessage("Sukunimen tulee olla vahintaan 3 merkkia pitka.")
                .MaximumLength(100).WithMessage("Sukunimi voi olla enintaan 100 merkkia pitka.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Sahkoposti  on pakollinen.")
                .EmailAddress().WithMessage("Sahkopostin tulee olla kelvollinen.")
                .MaximumLength(255).WithMessage("Sahkoposti voi olla enintaan 255 merkkia pitka.");
        }
    }

}
