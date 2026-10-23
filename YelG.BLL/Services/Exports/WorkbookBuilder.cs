using ClosedXML.Excel;

namespace YelG.BLL.Services.Exports
{
    /// <param name="Format">Excel number format for the column, e.g. "0.0"; null keeps the default.</param>
    /// <param name="Wrap">Long text (questions, evidence) wraps instead of widening the column.</param>
    internal sealed record Column(string Header, string? Format = null, bool Wrap = false);

    /// <summary>
    /// Right-to-left workbook with one table per sheet: styled, frozen header row and auto-filter,
    /// like the old SheetJS export but with real numbers and dates instead of text.
    /// </summary>
    internal sealed class WorkbookBuilder : IDisposable
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private const int MinWidth = 10;
        private const int MaxWidth = 60;
        private const int WrapWidth = 50;
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#E8EEF9");
        private static readonly XLColor HeaderText = XLColor.FromHtml("#0B2A5B");

        private readonly XLWorkbook _workbook = new();

        public WorkbookBuilder AddSheet(string name, IReadOnlyList<Column> columns, IEnumerable<object?[]> rows)
        {
            var sheet = _workbook.Worksheets.Add(name.Length > 31 ? name[..31] : name);
            sheet.RightToLeft = true;
            sheet.Style.Font.FontName = "Arial";

            var widths = new int[columns.Count];
            for (var c = 0; c < columns.Count; c++)
            {
                sheet.Cell(1, c + 1).Value = columns[c].Header;
                widths[c] = columns[c].Header.Length + 2;
            }

            var r = 1;
            foreach (var row in rows)
            {
                r++;
                for (var c = 0; c < columns.Count && c < row.Length; c++)
                {
                    var cell = sheet.Cell(r, c + 1);
                    cell.Value = ToCell(row[c]);
                    widths[c] = Math.Max(widths[c], Display(row[c]).Length + 2);
                }
            }

            var header = sheet.Range(1, 1, 1, columns.Count);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = HeaderText;
            header.Style.Fill.BackgroundColor = HeaderFill;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sheet.Range(1, 1, r, columns.Count).SetAutoFilter();
            sheet.SheetView.FreezeRows(1);

            for (var c = 0; c < columns.Count; c++)
            {
                var column = sheet.Column(c + 1);
                var spec = columns[c];
                if (spec.Format is not null) column.Style.NumberFormat.Format = spec.Format;
                if (spec.Wrap)
                {
                    column.Width = Math.Clamp(widths[c], MinWidth, WrapWidth);
                    if (r > 1) sheet.Range(2, c + 1, r, c + 1).Style.Alignment.WrapText = true;
                }
                else
                {
                    column.Width = Math.Clamp(widths[c], MinWidth, MaxWidth);
                }
            }
            if (r > 1) sheet.Range(2, 1, r, columns.Count).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            return this;
        }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            _workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public void Dispose() => _workbook.Dispose();

        private static XLCellValue ToCell(object? value) => value switch
        {
            null => Blank.Value,
            string s => s,
            int i => i,
            decimal d => d,
            double d => d,
            bool b => b ? "نعم" : "",
            DateTime dt => dt,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            _ => value.ToString() ?? string.Empty
        };

        private static string Display(object? value) => value switch
        {
            null => string.Empty,
            DateTime or DateOnly => "yyyy-mm-dd hh:mm",
            decimal or double or int => "000000.0",
            bool b => b ? "نعم" : "",
            _ => value.ToString() ?? string.Empty
        };
    }
}
