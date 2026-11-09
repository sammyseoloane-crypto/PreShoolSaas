using System.Text;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Preschool;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Infrastructure.Audit;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Preschool;
using BabyCareApp.Infrastructure.Subscriptions;
using BabyCareApp.Infrastructure.Tenancy;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Preschool;

public class PreschoolProfileServiceTests
{
    private static (PreschoolProfileService Service, ApplicationDbContext Db, Guid BranchId, FakeFileStorageService FileStorage)
        CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new FakeTenantContext();
        var currentUser = new FakeCurrentUserContext { UserId = Guid.NewGuid() };
        var db = new ApplicationDbContext(options, tenantContext);
        db.Database.EnsureCreated();

        var auditLogger = new AuditLogger(db);
        var subscriptionService = new SubscriptionService(db, tenantContext, currentUser, auditLogger);
        var tenantService = new TenantService(db, tenantContext, auditLogger, subscriptionService);
        var fileStorage = new FakeFileStorageService();
        var service = new PreschoolProfileService(db, tenantContext, tenantService, fileStorage, auditLogger);

        var tenant = tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Profile Test School" })
            .GetAwaiter().GetResult();
        tenantContext.TenantId = tenant.Id;
        var branchId = tenantService.ListBranchesAsync().GetAwaiter().GetResult().Single().Id;

        return (service, db, branchId, fileStorage);
    }

    private static List<OperatingHourEntry> FullWeek(TimeSpan open, TimeSpan close) =>
        Enum.GetValues<DayOfWeek>().Select(d => new OperatingHourEntry(d, open, close, false)).ToList();

    [Fact]
    public async Task GetOperatingHours_InitiallyEmpty()
    {
        var (service, _, branchId, _) = CreateService();

        var hours = await service.GetOperatingHoursAsync(branchId);

        Assert.Empty(hours);
    }

    [Fact]
    public async Task SetOperatingHours_ValidFullWeek_Succeeds()
    {
        var (service, _, branchId, _) = CreateService();
        var week = FullWeek(TimeSpan.FromHours(7), TimeSpan.FromHours(17));

        var result = await service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest { Days = week });

        Assert.Equal(7, result.Count);
        Assert.All(result, e => Assert.False(e.IsClosed));
    }

    [Fact]
    public async Task SetOperatingHours_MissingADay_ThrowsValidation()
    {
        var (service, _, branchId, _) = CreateService();
        var incomplete = FullWeek(TimeSpan.FromHours(7), TimeSpan.FromHours(17)).Take(6).ToList();

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest { Days = incomplete }));
    }

    [Fact]
    public async Task SetOperatingHours_DuplicateDay_ThrowsValidation()
    {
        var (service, _, branchId, _) = CreateService();
        var days = FullWeek(TimeSpan.FromHours(7), TimeSpan.FromHours(17));
        days[6] = days[0]; // duplicate Sunday/Monday entry instead of covering all 7

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest { Days = days }));
    }

    [Fact]
    public async Task SetOperatingHours_OpenAfterClose_ThrowsValidation()
    {
        var (service, _, branchId, _) = CreateService();
        var days = FullWeek(TimeSpan.FromHours(17), TimeSpan.FromHours(7)); // open after close

        await Assert.ThrowsAsync<ValidationException>(
            () => service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest { Days = days }));
    }

    [Fact]
    public async Task SetOperatingHours_CalledTwice_ReplacesPreviousWeek()
    {
        var (service, db, branchId, _) = CreateService();
        await service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest
        {
            Days = FullWeek(TimeSpan.FromHours(7), TimeSpan.FromHours(17)),
        });

        await service.SetOperatingHoursAsync(branchId, new UpdateOperatingHoursRequest
        {
            Days = FullWeek(TimeSpan.FromHours(8), TimeSpan.FromHours(13)),
        });

        var rows = await db.BranchOperatingHours.Where(h => h.BranchId == branchId).ToListAsync();
        Assert.Equal(7, rows.Count); // not 14 — the first week's rows were replaced, not appended
        Assert.All(rows, h => Assert.Equal(TimeSpan.FromHours(8), h.OpenTime));
    }

    [Fact]
    public async Task AddFacility_ThenList_ReturnsIt()
    {
        var (service, _, branchId, _) = CreateService();

        await service.AddFacilityAsync(branchId, new AddFacilityRequest { Name = "Outdoor Playground" });
        var facilities = await service.ListFacilitiesAsync(branchId);

        var facility = Assert.Single(facilities);
        Assert.Equal("Outdoor Playground", facility.Name);
    }

    [Fact]
    public async Task RemoveFacility_RemovesIt()
    {
        var (service, _, branchId, _) = CreateService();
        var facility = await service.AddFacilityAsync(branchId, new AddFacilityRequest { Name = "CCTV" });

        await service.RemoveFacilityAsync(branchId, facility.Id);

        Assert.Empty(await service.ListFacilitiesAsync(branchId));
    }

    [Fact]
    public async Task RemoveFacility_Unknown_ThrowsNotFound()
    {
        var (service, _, branchId, _) = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveFacilityAsync(branchId, Guid.NewGuid()));
    }

    [Fact]
    public async Task OperatingHours_OnBranchFromAnotherTenant_ThrowsNotFound()
    {
        // Both tenants must live in the SAME database for this to actually exercise
        // cross-tenant isolation, rather than just "an id from an unrelated empty database".
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new FakeTenantContext();
        var currentUser = new FakeCurrentUserContext { UserId = Guid.NewGuid() };
        var db = new ApplicationDbContext(options, tenantContext);
        db.Database.EnsureCreated();
        var auditLogger = new AuditLogger(db);
        var subscriptionService = new SubscriptionService(db, tenantContext, currentUser, auditLogger);
        var tenantService = new TenantService(db, tenantContext, auditLogger, subscriptionService);
        var service = new PreschoolProfileService(db, tenantContext, tenantService, new FakeFileStorageService(), auditLogger);

        var tenantA = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant A" });
        var tenantB = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant B" });

        tenantContext.TenantId = tenantA.Id;
        var tenantABranchId = (await tenantService.ListBranchesAsync()).Single().Id;

        tenantContext.TenantId = tenantB.Id;
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetOperatingHoursAsync(tenantABranchId));
    }

    [Fact]
    public async Task UploadDocument_StoresMetadata_AndUsesFileStorage()
    {
        var (service, _, branchId, fileStorage) = CreateService();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf bytes"));

        var document = await service.UploadDocumentAsync(
            branchId, "SafetyComplianceCertificate", "safety.pdf", "application/pdf", content, Guid.NewGuid());

        Assert.Equal("safety.pdf", document.FileName);
        Assert.Equal(branchId, document.BranchId);
        Assert.Single(fileStorage.UploadedKeys);
    }

    [Fact]
    public async Task UploadDocument_TooLarge_ThrowsValidation()
    {
        var (service, _, branchId, _) = CreateService();
        using var content = new MemoryStream(new byte[11 * 1024 * 1024]); // 11 MB, over the 10 MB cap

        await Assert.ThrowsAsync<ValidationException>(() => service.UploadDocumentAsync(
            branchId, "Other", "big.pdf", "application/pdf", content, Guid.NewGuid()));
    }

    [Fact]
    public async Task UploadDocument_TenantWide_HasNullBranchId()
    {
        var (service, _, _, _) = CreateService();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("registration cert"));

        var document = await service.UploadDocumentAsync(
            branchId: null, "RegistrationCertificate", "cert.pdf", "application/pdf", content, Guid.NewGuid());

        Assert.Null(document.BranchId);
    }

    [Fact]
    public async Task ListDocuments_ReturnsBothTenantWideAndBranchSpecific()
    {
        var (service, _, branchId, _) = CreateService();
        using (var content1 = new MemoryStream(Encoding.UTF8.GetBytes("a")))
        {
            await service.UploadDocumentAsync(null, "RegistrationCertificate", "a.pdf", "application/pdf", content1, Guid.NewGuid());
        }
        using (var content2 = new MemoryStream(Encoding.UTF8.GetBytes("b")))
        {
            await service.UploadDocumentAsync(branchId, "SafetyCert", "b.pdf", "application/pdf", content2, Guid.NewGuid());
        }

        var documents = await service.ListDocumentsAsync();

        Assert.Equal(2, documents.Count);
    }

    [Fact]
    public async Task GetDocumentDownloadUrl_ReturnsSignedUrl()
    {
        var (service, _, branchId, _) = CreateService();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("doc"));
        var document = await service.UploadDocumentAsync(
            branchId, "Other", "doc.pdf", "application/pdf", content, Guid.NewGuid());

        var url = await service.GetDocumentDownloadUrlAsync(document.Id);

        Assert.Contains("signed=1", url);
    }

    [Fact]
    public async Task DeleteDocument_RemovesRow_AndCallsStorageDelete()
    {
        var (service, _, branchId, fileStorage) = CreateService();
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("doc"));
        var document = await service.UploadDocumentAsync(
            branchId, "Other", "doc.pdf", "application/pdf", content, Guid.NewGuid());

        await service.DeleteDocumentAsync(document.Id);

        Assert.Empty(await service.ListDocumentsAsync());
        Assert.Single(fileStorage.DeletedKeys);
    }

    [Fact]
    public async Task DeleteDocument_Unknown_ThrowsNotFound()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteDocumentAsync(Guid.NewGuid()));
    }
}
