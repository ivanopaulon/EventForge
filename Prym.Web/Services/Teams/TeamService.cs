using Microsoft.AspNetCore.Components.Forms;
using Prym.DTOs.Common;
using Prym.DTOs.Teams;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Prym.Web.Services.Teams;

/// <summary>
/// Implementation of team management client service using HTTP client.
/// </summary>
public class TeamService(
    IHttpClientService httpClientService,
    IHttpClientFactory httpClientFactory,
    IAuthService authService,
    ILogger<TeamService> logger) : ITeamService
{
    private const string TeamsBaseUrl = "api/v1/teams";
    private const string DocumentsBaseUrl = "api/v1/documentreferences";

    #region Team CRUD

    public async Task<PagedResult<TeamDto>?> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PagedResult<TeamDto>>(
                $"{TeamsBaseUrl}?page={page}&pageSize={pageSize}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving teams");
            return null;
        }
    }

    public async Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TeamDto>($"{TeamsBaseUrl}/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving team {TeamId}", id);
            return null;
        }
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TeamDetailDto>($"{TeamsBaseUrl}/{id}/details", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving team detail {TeamId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<TeamDto>?> GetTeamsByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<TeamDto>>($"{TeamsBaseUrl}/by-event/{eventId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving teams for event {EventId}", eventId);
            return null;
        }
    }

    public async Task<TeamDto?> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateTeamDto, TeamDto>(TeamsBaseUrl, dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating team");
            return null;
        }
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateTeamDto, TeamDto>($"{TeamsBaseUrl}/{id}", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating team {TeamId}", id);
            return null;
        }
    }

    public async Task DeleteTeamAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{TeamsBaseUrl}/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting team {TeamId}", id);
            throw;
        }
    }

    #endregion

    #region Team Member CRUD

    public async Task<IEnumerable<TeamMemberDto>?> GetTeamMembersAsync(Guid teamId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<TeamMemberDto>>(
                $"{TeamsBaseUrl}/{teamId}/members", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving team members for team {TeamId}", teamId);
            return null;
        }
    }

    public async Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TeamMemberDto>($"{TeamsBaseUrl}/members/{memberId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving team member {MemberId}", memberId);
            return null;
        }
    }

    public async Task<TeamMemberDto?> CreateTeamMemberAsync(CreateTeamMemberDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateTeamMemberDto, TeamMemberDto>(
                $"{TeamsBaseUrl}/members", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating team member");
            return null;
        }
    }

    public async Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid memberId, UpdateTeamMemberDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateTeamMemberDto, TeamMemberDto>(
                $"{TeamsBaseUrl}/members/{memberId}", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating team member {MemberId}", memberId);
            return null;
        }
    }

    public async Task DeleteTeamMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{TeamsBaseUrl}/members/{memberId}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting team member {MemberId}", memberId);
            throw;
        }
    }

    #endregion

    #region Membership Card CRUD

    public async Task<IEnumerable<MembershipCardDto>?> GetMembershipCardsAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<MembershipCardDto>>(
                $"{TeamsBaseUrl}/members/{memberId}/membership-cards", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving membership cards for member {MemberId}", memberId);
            return null;
        }
    }

    public async Task<MembershipCardDto?> GetMembershipCardByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<MembershipCardDto>($"{TeamsBaseUrl}/membership-cards/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving membership card {CardId}", id);
            return null;
        }
    }

    public async Task<MembershipCardDto?> CreateMembershipCardAsync(CreateMembershipCardDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateMembershipCardDto, MembershipCardDto>(
                $"{TeamsBaseUrl}/membership-cards", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating membership card");
            return null;
        }
    }

    public async Task<MembershipCardDto?> UpdateMembershipCardAsync(Guid id, UpdateMembershipCardDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateMembershipCardDto, MembershipCardDto>(
                $"{TeamsBaseUrl}/membership-cards/{id}", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating membership card {CardId}", id);
            return null;
        }
    }

    public async Task DeleteMembershipCardAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{TeamsBaseUrl}/membership-cards/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting membership card {CardId}", id);
            throw;
        }
    }

    #endregion

    #region Insurance Policy CRUD

    public async Task<IEnumerable<InsurancePolicyDto>?> GetInsurancePoliciesAsync(Guid memberId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<InsurancePolicyDto>>(
                $"{TeamsBaseUrl}/members/{memberId}/insurance-policies", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving insurance policies for member {MemberId}", memberId);
            return null;
        }
    }

    public async Task<InsurancePolicyDto?> GetInsurancePolicyByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<InsurancePolicyDto>($"{TeamsBaseUrl}/insurance-policies/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving insurance policy {PolicyId}", id);
            return null;
        }
    }

    public async Task<InsurancePolicyDto?> CreateInsurancePolicyAsync(CreateInsurancePolicyDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateInsurancePolicyDto, InsurancePolicyDto>(
                $"{TeamsBaseUrl}/insurance-policies", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating insurance policy");
            return null;
        }
    }

    public async Task<InsurancePolicyDto?> UpdateInsurancePolicyAsync(Guid id, UpdateInsurancePolicyDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateInsurancePolicyDto, InsurancePolicyDto>(
                $"{TeamsBaseUrl}/insurance-policies/{id}", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating insurance policy {PolicyId}", id);
            return null;
        }
    }

    public async Task DeleteInsurancePolicyAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{TeamsBaseUrl}/insurance-policies/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting insurance policy {PolicyId}", id);
            throw;
        }
    }

    #endregion

    #region Document References

    public async Task<IEnumerable<DocumentReferenceDto>?> GetDocumentsByOwnerAsync(Guid ownerId, string ownerType, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<DocumentReferenceDto>>(
                $"{DocumentsBaseUrl}/owner/{ownerId}?ownerType={Uri.EscapeDataString(ownerType)}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error retrieving documents for owner {OwnerId}", ownerId);
            return null;
        }
    }

    public async Task<DocumentReferenceDto?> CreateDocumentReferenceAsync(CreateDocumentReferenceDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateDocumentReferenceDto, DocumentReferenceDto>(
                DocumentsBaseUrl, dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error creating document reference");
            return null;
        }
    }

    public async Task<DocumentReferenceDto?> UpdateDocumentReferenceAsync(Guid id, UpdateDocumentReferenceDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateDocumentReferenceDto, DocumentReferenceDto>(
                $"{DocumentsBaseUrl}/{id}", dto, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error updating document reference {DocId}", id);
            return null;
        }
    }

    public async Task<DocumentReferenceDto?> UploadDocumentAsync(
        IBrowserFile file,
        Guid ownerId,
        string ownerType,
        DocumentReferenceType type,
        DocumentReferenceSubType subType = DocumentReferenceSubType.None,
        DateTime? expiry = null,
        string? title = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        try
        {
            const long maxFileSize = 20 * 1024 * 1024; // 20MB

            var httpClient = httpClientFactory.CreateClient("ApiClient");

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.Name);
            content.Add(new StringContent(ownerId.ToString()), "ownerId");
            content.Add(new StringContent(ownerType), "ownerType");
            content.Add(new StringContent(((int)type).ToString()), "type");
            content.Add(new StringContent(((int)subType).ToString()), "subType");
            if (expiry.HasValue)
            {
                content.Add(new StringContent(expiry.Value.ToString("O")), "expiry");
            }
            if (!string.IsNullOrWhiteSpace(title))
            {
                content.Add(new StringContent(title), "title");
            }
            if (!string.IsNullOrWhiteSpace(notes))
            {
                content.Add(new StringContent(notes), "notes");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{DocumentsBaseUrl}/upload")
            {
                Content = content
            };

            var token = await authService.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<DocumentReferenceDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            logger.LogError("Failed to upload document. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading document for owner {OwnerId}", ownerId);
            return null;
        }
    }

    public async Task DeleteDocumentReferenceAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{DocumentsBaseUrl}/{id}", ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error deleting document reference {DocId}", id);
            throw;
        }
    }

    #endregion

    #region Fiscal Code Conflicts

    public async Task<List<TeamMemberDto>> GetFiscalCodeConflictsAsync(string fiscalCode, Guid excludeMemberId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<List<TeamMemberDto>>(
                $"{TeamsBaseUrl}/members/by-fiscal-code/{Uri.EscapeDataString(fiscalCode)}/conflicts?excludeMemberId={excludeMemberId}", ct);
            return result ?? new List<TeamMemberDto>();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error checking fiscal code conflicts for {FiscalCode}", fiscalCode);
            return new List<TeamMemberDto>();
        }
    }

    #endregion
}
