using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Auth.Commands.AccountDeletion;

public static class AccountDeletionPolicy
{
    public const string DeletedAthleteName = "Deleted athlete";
    public const string DeletedContactEmail = "deleted@deleted.xenoh.invalid";
    public const string DeletedOrganizerContact = "Deleted organizer";

    public static void AnonymizeCompetitionRegistration(CompetitionRegistration registration)
    {
        registration.UserId = null;
        registration.AthleteName = DeletedAthleteName;
        registration.ContactEmail = DeletedContactEmail;
        registration.ContactPhone = null;
        registration.ContactFacebook = null;
        registration.DateOfBirth = null;
        registration.Sex = null;
        registration.DeclaredWeightKg = null;
        registration.DeclaredHeightCm = null;
        registration.ReviewedById = null;
        registration.DecisionReason = null;
    }

    public static void AnonymizeOwnedCompetitionEvent(CompetitionEvent competition)
    {
        competition.OrganizerContact = DeletedOrganizerContact;
        competition.BankName = null;
        competition.BankAccountNumber = null;
        competition.BankAccountName = null;
        competition.TransferInstructions = null;
    }
}
