using FluentValidation;
using QuizApp.DTOs;

namespace QuizApp.Validators;

public class CreateQuizRequestValidator : AbstractValidator<CreateQuizRequest>
{
    public CreateQuizRequestValidator()
    {
        RuleFor(x => x.TopicName)
            .NotEmpty().WithMessage("Topic name is required.")
            .MaximumLength(200).WithMessage("Topic name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("At least one question is required.");

        RuleForEach(x => x.Questions).SetValidator(new CreateQuestionRequestValidator());
    }
}

public class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(1000).WithMessage("Question text must not exceed 1000 characters.");

        RuleFor(x => x.AnswerOptions)
            .Must(options => options.Count == 4)
            .WithMessage("Each question must have exactly 4 answer options.");

        RuleFor(x => x.AnswerOptions)
            .Must(options => options.Count(o => o.IsCorrect) == 1)
            .WithMessage("Each question must have exactly 1 correct answer option.");

        RuleForEach(x => x.AnswerOptions).SetValidator(new CreateAnswerOptionRequestValidator());
    }
}

public class CreateAnswerOptionRequestValidator : AbstractValidator<CreateAnswerOptionRequest>
{
    public CreateAnswerOptionRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Answer option text is required.")
            .MaximumLength(500).WithMessage("Answer option text must not exceed 500 characters.");
    }
}

public class UpdateQuizRequestValidator : AbstractValidator<UpdateQuizRequest>
{
    public UpdateQuizRequestValidator()
    {
        RuleFor(x => x.TopicName)
            .NotEmpty().WithMessage("Topic name is required.")
            .MaximumLength(200).WithMessage("Topic name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("At least one question is required.");

        RuleForEach(x => x.Questions).SetValidator(new CreateQuestionRequestValidator());
    }
}
