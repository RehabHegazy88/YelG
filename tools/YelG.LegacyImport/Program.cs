using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YelG.BLL.Common;
using YelG.DAL.Data;
using YelG.LegacyImport;
using YelG.Model.Audit;
using YelG.Model.Enums;
using YelG.Model.Evaluations;
using YelG.Model.Auth;

// One-time import of the old Apps Script data (agreed 2026-10-23):
//   - real data only: the answered evaluations plus empty assignments on real companies;
//   - the test company "اختبار" and the A01 = "test" answers are dropped;
//   - only the ZO Centers duplicate is merged (the "توزيع وتجارة" entry into "خدمات طبية");
//   - new accounts get one temporary password from YELG_IMPORT_PASSWORD (MustChangePassword); the existing admin keeps theirs;
//   - the old log is imported without view events (LOAD_STATE, ADMIN_LOAD_STATE, ADMIN_LIST_RESULTS).
//
// Usage: dotnet run -- <folder with the xlsx and state-*.json> [--apply]
// Without --apply nothing is written; the plan is printed. Connection string: YELG_IMPORT_CONNECTION.

const string LogPrefix = "النظام القديم: ";

// Legacy company id → the company it is merged into.
var companyAliases = new Dictionary<string, string>
{
    ["6169c34d-8118-4308-bd83-7382fb826502"] = "17a41f65-8044-4020-b96a-1fa8733ac5b0"   // ZO Centers
};
var excludedCompanies = new HashSet<string> { "55aef47a-7be3-4845-b08a-9cf4dabd08a5" };   // "اختبار"
// Evaluations whose only answers were typing tests (A01 = "test"): kept as empty assignments.
var testAnswerEvaluations = new HashSet<string>
{
    "0891f8fe-0f7d-4fb2-82c4-83c586cf8823",   // Molouk, second ZO Centers copy
    "6169c34d-8118-4308-bd83-7382fb826502"    // Rehab, ZO Centers (distribution)
};
var skippedLogActions = new HashSet<string> { "LOAD_STATE", "ADMIN_LOAD_STATE", "ADMIN_LIST_RESULTS" };

var folder = args.FirstOrDefault(a => !a.StartsWith("--")) ?? throw new ArgumentException("Pass the folder with the old data.");
var apply = args.Contains("--apply");
var connection = Environment.GetEnvironmentVariable("YELG_IMPORT_CONNECTION")
    ?? throw new InvalidOperationException("Set YELG_IMPORT_CONNECTION.");

Console.OutputEncoding = System.Text.Encoding.UTF8;
var legacy = LegacySource.Load(folder);

// No interceptor: the import sets AddedDate / AddedBy (and the modified pair) from the old data
// instead of "now" and the signed-in user.
await using var db = new YelgDbContext(new DbContextOptionsBuilder<YelgDbContext>().UseSqlServer(connection).Options);
var target = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
Console.WriteLine($"Target: {target.DataSource} / {target.InitialCatalog}");
Console.WriteLine(apply ? "Mode: APPLY" : "Mode: dry run (add --apply to write)");

var version = await db.QuestionBankVersions
    .Include(v => v.Categories).ThenInclude(c => c.Questions)
    .SingleAsync(v => v.Status == QuestionBankVersionStatus.Published);
var questionIds = version.Categories.SelectMany(c => c.Questions).ToDictionary(q => q.Code, q => q.Id);
var activities = await db.Activities.ToDictionaryAsync(a => a.ArabicName, a => a.Id);
var adminRoleId = await db.Roles.Where(r => r.Name == AppRoles.Admin).Select(r => r.Id).SingleAsync();
var userRoleId = await db.Roles.Where(r => r.Name == AppRoles.User).Select(r => r.Id).SingleAsync();
var existingUsers = await db.Users.Where(u => !u.IsBuiltIn).ToDictionaryAsync(u => u.NormalizedUserName!);
Console.WriteLine($"Question bank: v{version.VersionNo} ({questionIds.Count} questions)");

if (await db.ActivityLogs.AnyAsync(l => l.Action == "LEGACY_IMPORT"))
{
    Console.WriteLine("\nStopped: the target already has a previous import.");
    return 1;
}

// ---- users ----------------------------------------------------------------------------------
var problems = new List<string>();
var users = new Dictionary<string, User>();      // legacy key → account
var newUsers = new List<(User User, bool IsAdmin)>();
var allowed = new Regex("^[A-Za-z0-9._@+-]+$");
foreach (var u in legacy.Users)
{
    var userName = Regex.Replace(u.UserName, @"\s+", ".");   // "Ahmed Abdalla" → "Ahmed.Abdalla" (Identity allows no spaces)
    if (!allowed.IsMatch(userName)) { problems.Add($"User name not allowed: {u.UserName}"); continue; }
    if (existingUsers.TryGetValue(userName.ToUpperInvariant(), out var existing))
    {
        users[u.Key] = existing;
        Console.WriteLine($"  user {userName,-16} exists → reused, password unchanged");
        continue;
    }
    var account = new User
    {
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        DisplayName = u.DisplayName,
        IsActive = u.IsActive,
        AddedDate = u.CreatedAt.UtcDateTime,
        LastModifiedDate = u.CreatedAt.UtcDateTime,
        MustChangePassword = true,   // temporary password: replaced at first sign-in
        SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        LockoutEnabled = true
    };
    users[u.Key] = account;
    newUsers.Add((account, u.IsAdmin));
    Console.WriteLine($"  user {userName,-16} new ({(u.IsAdmin ? "Admin" : "User")}{(u.IsActive ? "" : ", inactive")}) — {u.DisplayName}");
}
var admin = users.TryGetValue("admin", out var a0) && a0.Id > 0
    ? a0 : throw new InvalidOperationException("The target needs its admin account before the import.");

// With --apply the accounts are written first (inside the transaction) so every row below gets real user ids.
await using var transaction = apply ? await db.Database.BeginTransactionAsync() : null;
if (apply && newUsers.Count > 0)
{
    var password = Environment.GetEnvironmentVariable("YELG_IMPORT_PASSWORD");
    if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Set YELG_IMPORT_PASSWORD for the new accounts.");
    var hasher = new PasswordHasher<User>();
    foreach (var (user, _) in newUsers)
    {
        // Hashed directly: the temporary password chosen for the test environment is shorter than
        // the 8-character rule UserManager enforces. MustChangePassword makes each user replace it.
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
    }
    await db.SaveChangesAsync();
    foreach (var (user, isAdmin) in newUsers)
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = isAdmin ? adminRoleId : userRoleId });
    await db.SaveChangesAsync();
}

// ---- companies and evaluations --------------------------------------------------------------
var sheetCompanies = legacy.Companies.ToDictionary(c => c.Id);
var companies = new Dictionary<(string Name, long ActivityId), Company>();
var evaluations = new Dictionary<(Company, string ReviewerKey), (LegacyEvaluation? Source, Evaluation Target, User Reviewer)>();

foreach (var e in legacy.Evaluations)
{
    var companyId = companyAliases.GetValueOrDefault(e.CompanyId, e.CompanyId);
    if (excludedCompanies.Contains(companyId)) continue;
    var sheet = sheetCompanies.GetValueOrDefault(companyId);
    var name = Text.Clean(sheet?.Name ?? e.CompanyName, 200);
    var activityName = sheet?.Activity ?? e.Activity;
    if (!activities.TryGetValue(activityName, out var activityId)) { problems.Add($"Unknown activity '{activityName}' for {name}"); continue; }
    if (!users.TryGetValue(e.OwnerKey, out var reviewer)) { problems.Add($"Unknown user '{e.OwnerKey}' for {name}"); continue; }

    if (!companies.TryGetValue((name, activityId), out var company))
    {
        var createdAt = (sheet?.CreatedAt ?? e.AssignedAt ?? DateTimeOffset.UtcNow).UtcDateTime;
        companies[(name, activityId)] = company = new Company
        {
            Name = name, ActivityId = activityId, IsActive = true,
            AddedDate = createdAt, LastModifiedDate = createdAt, AddedBy_PersonID = admin.Id
        };
    }

    var answers = testAnswerEvaluations.Contains(e.Id) ? new Dictionary<string, IReadOnlyList<string>>() : e.Answers;
    foreach (var code in answers.Keys.Where(c => !questionIds.ContainsKey(c)))
        problems.Add($"Question {code} not in v{version.VersionNo} ({name}, {e.OwnerKey})");

    // One evaluation per reviewer per company: keep the copy with the most answers (a submitted one wins a tie).
    if (evaluations.TryGetValue((company, e.OwnerKey), out var kept) && kept.Source is not null
        && (kept.Source.Answers.Count, kept.Source.Status == "submitted").CompareTo((answers.Count, e.Status == "submitted")) >= 0)
        continue;

    var at = e.AssignedAt?.UtcDateTime ?? company.AddedDate;
    var editedAt = answers.Count > 0 && e.EditedAt is { } edited ? edited.UtcDateTime : at;
    var assignerId = e.AssignedBy is { } by && users.TryGetValue(by, out var assigner) ? assigner.Id : (long?)null;
    if (assignerId == 0) problems.Add($"Assigner '{e.AssignedBy}' is a new account ({name}); only existing admins assign.");
    var evaluation = new Evaluation
    {
        Company = company,
        ReviewerId = reviewer.Id,
        QuestionBankVersionId = version.Id,
        AssignedById = assignerId,
        AssignedAt = e.AssignedAt,
        Note = string.IsNullOrWhiteSpace(e.Note) ? null : e.Note,
        AddedDate = at,
        AddedBy_PersonID = admin.Id,
        LastModifiedDate = editedAt,
        ModifiedBy_PersonID = answers.Count > 0 ? reviewer.Id : null
    };
    foreach (var (code, values) in answers.Where(x => questionIds.ContainsKey(x.Key)))
        evaluation.Answers.Add(new Answer
        {
            QuestionId = questionIds[code], Values = values.ToList(),
            AddedDate = editedAt, LastModifiedDate = editedAt, AddedBy_PersonID = reviewer.Id
        });
    if (e.Status == "submitted") evaluation.Submit(reviewer.Id, e.EditedAt ?? new DateTimeOffset(at, TimeSpan.Zero));
    else if (e.Status != "draft") problems.Add($"Status '{e.Status}' not handled ({name}, {e.OwnerKey})");
    evaluations[(company, e.OwnerKey)] = (e with { Answers = answers }, evaluation, reviewer);
}

// The new system gives the admin an evaluation of every company.
foreach (var company in companies.Values.Where(c => !evaluations.ContainsKey((c, "admin"))))
    evaluations[(company, "admin")] = (null, new Evaluation
    {
        Company = company, ReviewerId = admin.Id, QuestionBankVersionId = version.Id,
        AddedDate = company.AddedDate, LastModifiedDate = company.AddedDate, AddedBy_PersonID = admin.Id
    }, admin);

Console.WriteLine($"\nCompanies: {companies.Count}");
foreach (var group in evaluations.Values.GroupBy(x => x.Target.Company).OrderBy(g => g.Key.Name))
{
    Console.WriteLine($"  {group.Key.Name} — {activities.First(a => a.Value == group.Key.ActivityId).Key}");
    foreach (var (_, ev, reviewer) in group.OrderByDescending(x => x.Target.Answers.Count))
        Console.WriteLine($"      {reviewer.UserName,-16} {ev.Status,-9} {ev.Answers.Count,3} answers");
}

// ---- activity log ---------------------------------------------------------------------------
var log = legacy.Log.Where(l => !skippedLogActions.Contains(l.Action)).Select(l => new ActivityLog
{
    OccurredAt = l.At,
    UserId = users.TryGetValue(l.Key, out var u) ? u.Id : null,
    UserName = l.UserName,
    Action = Cut(l.Action, 60),
    Details = Cut(LogPrefix + l.Details, 1000)
}).ToList();
Console.WriteLine($"\nActivity log: {log.Count} of {legacy.Log.Count} rows " +
    $"({string.Join(", ", log.GroupBy(l => l.Action).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"))})");

if (problems.Count > 0)
{
    Console.WriteLine("\nProblems:");
    problems.ForEach(p => Console.WriteLine("  - " + p));
    return 1;   // the transaction (if any) rolls back on dispose
}

var names = companies.Values.Select(x => x.Name).ToList();
var clash = await db.Companies.Where(c => names.Contains(c.Name)).Select(c => c.Name).ToListAsync();
if (clash.Count > 0)
{
    Console.WriteLine($"\nStopped: the target already has these companies: {string.Join("، ", clash)}.");
    return 1;
}
if (!apply) return 0;

// ---- write ----------------------------------------------------------------------------------
db.Companies.AddRange(companies.Values);
db.Evaluations.AddRange(evaluations.Values.Select(x => x.Target));
db.ActivityLogs.AddRange(log);
db.ActivityLogs.Add(new ActivityLog
{
    OccurredAt = DateTimeOffset.UtcNow, UserId = admin.Id, UserName = admin.UserName, Action = "LEGACY_IMPORT",
    Details = $"استيراد من النظام القديم: {newUsers.Count} مستخدم، {companies.Count} شركة، {evaluations.Count} تقييم، " +
              $"{evaluations.Values.Sum(x => x.Target.Answers.Count)} إجابة، {log.Count} عملية من السجل"
});
await db.SaveChangesAsync();
await transaction!.CommitAsync();
Console.WriteLine("\nImported.");
return 0;

static string Cut(string value, int max) => value.Length > max ? value[..max] : value;
