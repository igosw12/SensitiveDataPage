using AuditLogApp.Data;
using AuditLogApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AuditLogApp.Pages;

public class IndexModel : PageModel
{
    private readonly AuditLogDbContext _db;
    private const int PageSize = 25;

    public IndexModel(AuditLogDbContext db)
    {
        _db = db;
    }

    public List<AuditLog> AuditLogs { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterAction { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterEmail { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterEntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public List<string> AvailableActions { get; set; } = new();
    public List<string> AvailableEntityTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        AvailableActions = await _db.AuditLogs
            .Where(a => a.Action != null)
            .Select(a => a.Action!)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        AvailableEntityTypes = await _db.AuditLogs
            .Where(a => a.EntityType != null)
            .Select(a => a.EntityType!)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(FilterAction))
            query = query.Where(a => a.Action == FilterAction);

        if (!string.IsNullOrWhiteSpace(FilterEntityType))
            query = query.Where(a => a.EntityType == FilterEntityType);

        if (!string.IsNullOrWhiteSpace(FilterEmail))
            query = query.Where(a => a.User != null && a.User.Email != null && a.User.Email.Contains(FilterEmail));

        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

        AuditLogs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
