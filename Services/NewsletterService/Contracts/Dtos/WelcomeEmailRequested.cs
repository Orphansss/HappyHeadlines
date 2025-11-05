namespace NewsletterService.Contracts.Dtos;

public record WelcomeEmailRequested(
    string Email,
    string Subject,
    string Body
);
