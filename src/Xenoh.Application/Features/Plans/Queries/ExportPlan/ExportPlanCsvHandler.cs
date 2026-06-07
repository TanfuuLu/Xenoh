using ClosedXML.Excel;
using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Plans.Queries.ExportPlan;

public sealed class ExportPlanCsvHandler(
    IPlanRepository planRepo,
    ICurrentUserService currentUser
) : IRequestHandler<ExportPlanCsvQuery, PlanCsvExportResult>
{
    private const int WeekCols = 5;   // Exercise | Sets | Reps | Weight | Notes
    private const int SepCols = 1;   // blank gap column between weeks

    private static readonly string[] DataHeaders =
    [
        "Exercise", "Planned Sets", "Planned Reps", "Planned Weight (kg)", "Notes"
    ];

    // Column widths (characters) for each position within a week block
    private static readonly double[] ColWidths = [28, 11, 11, 18, 22];

    public async ValueTask<PlanCsvExportResult> Handle(
        ExportPlanCsvQuery request, CancellationToken cancellationToken)
    {
        PlanExportData data = await planRepo.GetForExportAsync(
            request.PlanId, currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found or access denied.");

        byte[] bytes = BuildXlsx(data);
        string fileName = $"{SanitizeFileName(data.Name)}-{data.StartDate:yyyy-MM-dd}.xlsx";

        return new PlanCsvExportResult(bytes, fileName);
    }

    private static byte[] BuildXlsx(PlanExportData data)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet ws = workbook.Worksheets.Add("Plan");

        int numWeeks = data.Weeks.Count;
        int totalCols = numWeeks * WeekCols + Math.Max(0, numWeeks - 1) * SepCols;

        // 1-based starting column for week wi
        int WeekStartCol(int wi) => wi * (WeekCols + SepCols) + 1;

        int row = 1;

        // ── Title ─────────────────────────────────────────────────────────────
        int titleCol = (totalCols / 2) + 1; // roughly centred
        IXLCell titleCell = ws.Cell(row, titleCol);
        titleCell.Value = data.Name;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        row += 2; // blank row after title

        // ── Week headers ──────────────────────────────────────────────────────
        for (int wi = 0; wi < numWeeks; wi++)
        {
            WeekExportData week = data.Weeks[wi];
            int startCol = WeekStartCol(wi);
            IXLCell cell = ws.Cell(row, startCol);

            cell.Value = $"WEEK {week.WeekNumber}  —  {week.Name}  " +
                         $"({week.StartDate:dd/MM/yyyy} – {week.EndDate:dd/MM/yyyy})";
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D6DCE4");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(row, startCol, row, startCol + WeekCols - 1).Merge();
        }
        row++;

        // ── Column headers ────────────────────────────────────────────────────
        for (int wi = 0; wi < numWeeks; wi++)
        {
            int startCol = WeekStartCol(wi);
            for (int ci = 0; ci < DataHeaders.Length; ci++)
            {
                IXLCell cell = ws.Cell(row, startCol + ci);
                cell.Value = DataHeaders[ci];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2F7");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
        row++;

        // ── Day groups (aligned by day index across all weeks) ─────────────────
        int maxDays = data.Weeks.Count == 0 ? 0 : data.Weeks.Max(w => w.Days.Count);

        for (int di = 0; di < maxDays; di++)
        {
            bool anyExercises = data.Weeks.Any(w =>
                di < w.Days.Count && w.Days[di].Exercises.Count > 0);
            if (!anyExercises) continue;

            // Day header row
            for (int wi = 0; wi < numWeeks; wi++)
            {
                if (di >= data.Weeks[wi].Days.Count) continue;
                DayExportData day = data.Weeks[wi].Days[di];
                if (day.Exercises.Count == 0) continue;

                int startCol = WeekStartCol(wi);
                IXLCell cell = ws.Cell(row, startCol);
                cell.Value = $"{day.DayOfWeek}  —  {day.Date:dd/MM/yyyy}";
                cell.Style.Font.Bold = true;
                cell.Style.Font.Italic = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF3FB");

                ws.Range(row, startCol, row, startCol + WeekCols - 1).Merge();
            }
            row++;

            // Exercise rows — padded to the max count across weeks for this day
            int maxEx = data.Weeks.Max(w => di < w.Days.Count ? w.Days[di].Exercises.Count : 0);

            for (int ei = 0; ei < maxEx; ei++)
            {
                for (int wi = 0; wi < numWeeks; wi++)
                {
                    if (di >= data.Weeks[wi].Days.Count) continue;
                    DayExportData day = data.Weeks[wi].Days[di];
                    if (ei >= day.Exercises.Count) continue;

                    ExerciseExportData ex = day.Exercises[ei];
                    int startCol = WeekStartCol(wi);

                    ws.Cell(row, startCol + 0).Value = ex.Name;
                    ws.Cell(row, startCol + 1).Value = ex.PlannedSets;
                    ws.Cell(row, startCol + 2).Value = ex.PlannedReps;
                    if (ex.PlannedWeight.HasValue)
                        ws.Cell(row, startCol + 3).Value = (double)ex.PlannedWeight.Value;
                    if (!string.IsNullOrEmpty(ex.Notes))
                        ws.Cell(row, startCol + 4).Value = ex.Notes;
                }
                row++;
            }

            // Blank separator row after each day
            row++;
        }

        // ── Column widths ──────────────────────────────────────────────────────
        for (int wi = 0; wi < numWeeks; wi++)
        {
            int startCol = WeekStartCol(wi);
            for (int ci = 0; ci < WeekCols; ci++)
                ws.Column(startCol + ci).Width = ColWidths[ci];

            // Separator column — narrow
            if (wi < numWeeks - 1)
                ws.Column(startCol + WeekCols).Width = 3;
        }

        // ── Freeze top rows (title + blank + week header + col header = 4 rows) ─
        ws.SheetView.FreezeRows(4);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == ' ' ? c : '_'))
              .Trim()
              .Replace(' ', '-');
}
