namespace Xenoh.Domain.Enums;

public enum CompetitionDiscipline { Powerlifting = 0, Bodybuilding = 1 }
public enum CompetitionEventStatus { Draft = 0, Published = 1, RegistrationClosed = 2, InProgress = 3, Completed = 4, Cancelled = 5 }
public enum OrganizerProfileStatus { Pending = 0, Approved = 1, Rejected = 2, Suspended = 3 }

[Flags]
public enum CompetitionStaffPermission
{
    None = 0,
    EditEvent = 1,
    PublishEvent = 2,
    ManageCategories = 4,
    ManageRegistrations = 8,
    ReviewPayments = 16,
    ManageResults = 32,
    ManageStaff = 64,
    All = EditEvent | PublishEvent | ManageCategories | ManageRegistrations | ReviewPayments | ManageResults | ManageStaff
}

public enum CompetitionRegistrationStatus { Submitted = 0, Waitlisted = 1, Approved = 2, Rejected = 3, Withdrawn = 4 }
public enum CompetitionPaymentStatus { NotRequired = 0, AwaitingReceipt = 1, UnderReview = 2, Paid = 3, ReceiptRejected = 4 }
public enum CompetitionReceiptStatus { UnderReview = 0, Accepted = 1, Rejected = 2 }
public enum CompetitionResultState { Finished = 0, Disqualified = 1, DidNotFinish = 2 }
public enum PowerliftingScoringFormula { Total = 0, IpfGlPoints = 1, Dots = 2, Wilks = 3 }

