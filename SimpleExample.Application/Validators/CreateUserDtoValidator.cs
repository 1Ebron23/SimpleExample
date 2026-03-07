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
                .NotEmpty().WithMessage("Etunimi ei voi olla tyhjä.")
                .MinimumLength(3).WithMessage("Etunimen tulee olla vähintään 3 merkkiä pitkä.")
                .MaximumLength(100).WithMessage("Etunimi voi olla enintään 100 merkkiä pitkä.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Sukunimi ei voi olla tyhjä.")
                .MinimumLength(3).WithMessage("Sukunimen tulee olla vähintään 3 merkkiä pitkä.")
                .MaximumLength(100).WithMessage("Sukunimi voi olla enintään 100 merkkiä pitkä.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Sähköposti ei voi olla tyhjä.")
                .EmailAddress().WithMessage("Sähköpostin tulee olla kelvollinen.")
                .MaximumLength(255).WithMessage("Sähköposti voi olla enintään 255 merkkiä pitkä.");
        }
    }

}
