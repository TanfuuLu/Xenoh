using Mediator;

namespace Xenoh.Application.Features.Plans.Queries.ExportPlan;

public sealed record ExportPlanCsvQuery(Guid PlanId) : IRequest<PlanCsvExportResult>;
