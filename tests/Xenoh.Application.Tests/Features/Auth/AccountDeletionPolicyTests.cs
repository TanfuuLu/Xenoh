using FluentAssertions;
using Xenoh.Application.Features.Auth.Commands.AccountDeletion;
using Xenoh.Domain.Entities;
using Xunit;

namespace Xenoh.Application.Tests.Features.Auth;

public sealed class AccountDeletionPolicyTests
{
    [Fact]
    public void AnonymizeCompetitionRegistration_RemovesDirectIdentifiersAndUserLink()
    {
        var registration = new CompetitionRegistration
        {
            UserId = Guid.NewGuid(),
            AthleteName = "Test Athlete",
            ContactEmail = "athlete@example.com",
            ContactPhone = "0900000000",
            ContactFacebook = "https://facebook.com/test",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Sex = "Female",
            DeclaredWeightKg = 60,
            DeclaredHeightCm = 165
        };

        AccountDeletionPolicy.AnonymizeCompetitionRegistration(registration);

        registration.UserId.Should().BeNull();
        registration.AthleteName.Should().Be(AccountDeletionPolicy.DeletedAthleteName);
        registration.ContactEmail.Should().Be(AccountDeletionPolicy.DeletedContactEmail);
        registration.ContactPhone.Should().BeNull();
        registration.ContactFacebook.Should().BeNull();
        registration.DateOfBirth.Should().BeNull();
        registration.Sex.Should().BeNull();
        registration.DeclaredWeightKg.Should().BeNull();
        registration.DeclaredHeightCm.Should().BeNull();
    }

    [Fact]
    public void AnonymizeOwnedCompetitionEvent_RemovesOrganizerPaymentAndContactDetails()
    {
        var competition = new CompetitionEvent
        {
            OrganizerContact = "Organizer Name / 0900000000",
            BankName = "Example Bank",
            BankAccountNumber = "123456789",
            BankAccountName = "Organizer Name",
            TransferInstructions = "Send proof to organizer@example.com"
        };

        AccountDeletionPolicy.AnonymizeOwnedCompetitionEvent(competition);

        competition.OrganizerContact.Should().Be(AccountDeletionPolicy.DeletedOrganizerContact);
        competition.BankName.Should().BeNull();
        competition.BankAccountNumber.Should().BeNull();
        competition.BankAccountName.Should().BeNull();
        competition.TransferInstructions.Should().BeNull();
    }
}
