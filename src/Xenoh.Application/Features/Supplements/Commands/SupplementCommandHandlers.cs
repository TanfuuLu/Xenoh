using Mediator;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Supplements.Commands;

public sealed class CreateSupplementRegimenHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<CreateSupplementRegimenCommand, SupplementRegimenResponse>
{
    public async ValueTask<SupplementRegimenResponse> Handle(
        CreateSupplementRegimenCommand command,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            command.UserId,
            coachClientRepository,
            cancellationToken);
        var request = command.Request;
        var startDate = request.StartDate ?? today;

        if (startDate < today)
            throw new InvalidOperationException("A new supplement regimen cannot start in the past.");

        SupplementRules.ValidateRegimen(
            request.Name,
            request.Brand,
            request.Form,
            request.Instructions,
            request.Notes,
            request.DoseSlots);

        var regimen = new SupplementRegimen
        {
            UserId = userId,
            CreatedByUserId = currentUser.UserId
        };
        SupplementRules.ApplyMetadata(
            regimen,
            request.Name,
            request.Brand,
            request.Form,
            request.Instructions,
            request.Notes);
        regimen.ScheduleVersions.Add(
            SupplementRules.BuildVersion(startDate, currentUser.UserId, request.DoseSlots));

        supplementRepository.AddRegimen(regimen);
        await supplementRepository.SaveChangesAsync(cancellationToken);

        return SupplementMapper.ToResponse(regimen, today);
    }
}

public sealed class UpdateSupplementRegimenHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateSupplementRegimenCommand, SupplementRegimenResponse>
{
    public async ValueTask<SupplementRegimenResponse> Handle(
        UpdateSupplementRegimenCommand command,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            command.UserId,
            coachClientRepository,
            cancellationToken);
        var regimen = await supplementRepository.GetRegimenForUpdateAsync(
            command.RegimenId,
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Supplement regimen was not found.");

        if (regimen.IsArchived)
            throw new SupplementConflictException("Archived supplement regimens cannot be edited.");

        var request = command.Request;
        if (request.EffectiveFrom <= today)
            throw new InvalidOperationException(
                "Schedule changes must take effect no earlier than tomorrow.");

        SupplementRules.ValidateRegimen(
            request.Name,
            request.Brand,
            request.Form,
            request.Instructions,
            request.Notes,
            request.DoseSlots);
        SupplementRules.ApplyMetadata(
            regimen,
            request.Name,
            request.Brand,
            request.Form,
            request.Instructions,
            request.Notes);

        foreach (var futureVersion in regimen.ScheduleVersions
                     .Where(x => x.EffectiveFrom > today)
                     .ToList())
        {
            regimen.ScheduleVersions.Remove(futureVersion);
            supplementRepository.RemoveScheduleVersion(futureVersion);
        }

        var currentVersion = regimen.ScheduleVersions
            .Where(x => x.EffectiveFrom <= today)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefault();
        if (currentVersion is not null)
            currentVersion.EffectiveTo = request.EffectiveFrom.AddDays(-1);

        var newVersion = SupplementRules.BuildVersion(
            request.EffectiveFrom,
            currentUser.UserId,
            request.DoseSlots);
        newVersion.RegimenId = regimen.Id;
        regimen.ScheduleVersions.Add(newVersion);
        supplementRepository.AddScheduleVersion(newVersion);

        await supplementRepository.SaveChangesAsync(cancellationToken);
        return SupplementMapper.ToResponse(regimen, today);
    }
}

public sealed class ArchiveSupplementRegimenHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<ArchiveSupplementRegimenCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        ArchiveSupplementRegimenCommand command,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            command.UserId,
            coachClientRepository,
            cancellationToken);
        var regimen = await supplementRepository.GetRegimenForUpdateAsync(
            command.RegimenId,
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Supplement regimen was not found.");

        if (regimen.IsArchived)
            return Unit.Value;

        foreach (var futureVersion in regimen.ScheduleVersions
                     .Where(x => x.EffectiveFrom > today)
                     .ToList())
        {
            regimen.ScheduleVersions.Remove(futureVersion);
            supplementRepository.RemoveScheduleVersion(futureVersion);
        }

        foreach (var version in regimen.ScheduleVersions.Where(x =>
                     x.EffectiveFrom <= today &&
                     (x.EffectiveTo is null || x.EffectiveTo > today)))
        {
            version.EffectiveTo = today;
        }

        regimen.IsArchived = true;
        regimen.ArchivedAt = DateTime.UtcNow;
        regimen.UpdatedAt = DateTime.UtcNow;
        await supplementRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class DeleteSupplementRegimenHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<DeleteSupplementRegimenCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteSupplementRegimenCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            command.UserId,
            coachClientRepository,
            cancellationToken);
        var regimen = await supplementRepository.GetRegimenForUpdateAsync(
            command.RegimenId,
            userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Supplement regimen was not found.");

        // Archived regimens are the common case here — removing one is how a user
        // clears out something they never should have created, so there is no guard.
        await supplementRepository.RemoveRegimenAsync(regimen, cancellationToken);
        await supplementRepository.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public sealed class RecordSupplementDoseHandler(
    ISupplementRepository supplementRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<RecordSupplementDoseCommand, SupplementDailyDoseResponse>
{
    public async ValueTask<SupplementDailyDoseResponse> Handle(
        RecordSupplementDoseCommand command,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (command.Date > today)
            throw new InvalidOperationException("A future supplement dose cannot be recorded.");

        var slot = await supplementRepository.GetDoseSlotAsync(
            command.DoseSlotId,
            currentUser.UserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Supplement dose was not found.");

        if (!slot.ScheduleVersion.IsEffectiveOn(command.Date) || !slot.OccursOn(command.Date))
            throw new InvalidOperationException("This dose is not scheduled for the selected date.");

        var now = DateTime.UtcNow;
        var recordedAt = command.Request.RecordedAt?.ToUniversalTime() ?? now;
        if (recordedAt > now.AddMinutes(5))
            throw new InvalidOperationException("Recorded time cannot be in the future.");
        if (command.Request.Note?.Trim().Length > 300)
            throw new InvalidOperationException("Dose note cannot exceed 300 characters.");

        var intake = await supplementRepository.GetIntakeForUpdateAsync(
            slot.Id,
            command.Date,
            currentUser.UserId,
            cancellationToken);
        if (intake is null)
        {
            intake = new SupplementIntakeLog
            {
                UserId = currentUser.UserId,
                DoseSlotId = slot.Id,
                ScheduledDate = command.Date
            };
            supplementRepository.AddIntake(intake);
        }

        intake.Status = command.Request.Status;
        intake.RecordedAt = recordedAt;
        intake.Note = string.IsNullOrWhiteSpace(command.Request.Note)
            ? null
            : command.Request.Note.Trim();
        intake.UpdatedAt = now;

        await supplementRepository.SaveChangesAsync(cancellationToken);
        return SupplementOccurrenceFactory.ToDailyDose(
            slot,
            intake,
            command.Date,
            today);
    }
}

public sealed class ResetSupplementDoseHandler(
    ISupplementRepository supplementRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<ResetSupplementDoseCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        ResetSupplementDoseCommand command,
        CancellationToken cancellationToken)
    {
        var slot = await supplementRepository.GetDoseSlotAsync(
            command.DoseSlotId,
            currentUser.UserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Supplement dose was not found.");

        if (!slot.ScheduleVersion.IsEffectiveOn(command.Date) || !slot.OccursOn(command.Date))
            throw new InvalidOperationException("This dose is not scheduled for the selected date.");

        var intake = await supplementRepository.GetIntakeForUpdateAsync(
            command.DoseSlotId,
            command.Date,
            currentUser.UserId,
            cancellationToken);
        if (intake is not null)
        {
            supplementRepository.RemoveIntake(intake);
            await supplementRepository.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
