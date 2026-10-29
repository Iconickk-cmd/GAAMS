
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GAAMS.API.Tests;

public class EmployeeDuplicateIntegrationTests
{
    [Fact]
    public async Task CreateEmployee_WithDuplicateEmployeeNumber_ReturnsConflict()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var firstEmployee = CreateEmployeeRequest(
            "GAAMS-TEST-001",
            "National-TEST-001",
            departmentId);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/employees",
            firstEmployee);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateEmployee = CreateEmployeeRequest(
            "GAAMS-TEST-001",
            "National-TEST-002",
            departmentId);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/employees",
            duplicateEmployee);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_WithDuplicateNationalId_ReturnsConflict()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var firstEmployee = CreateEmployeeRequest(
            "GAAMS-TEST-002",
            "National-TEST-003",
            departmentId);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/employees",
            firstEmployee);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateEmployee = CreateEmployeeRequest(
            "GAAMS-TEST-003",
            "National-TEST-003",
            departmentId);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/employees",
            duplicateEmployee);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    private static async Task<int> CreateDepartmentAsync(HttpClient client)
    {
        var department = new
        {
            name = "Integration Test Department",
            code = $"TEST-{Guid.NewGuid():N}",
            description = "Department created for integration testing",
            isActive = true
        };

        var response = await client.PostAsJsonAsync(
            "/api/departments",
            department);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DepartmentResponse>();

        Assert.NotNull(result);

        return result.Id;
    }

    private static object CreateEmployeeRequest(
        string employeeNumber,
        string nationalId,
        int departmentId)
    {
        return new
        {
            employeeNumber,
            firstName = "Integration",
            lastName = "Test",
            nationalId,
            email = (string?)null,
            phoneNumber = (string?)null,
            dateOfBirth = "1990-05-10",
            dateOfEmployment = "2020-01-15",
            jobTitle = "Integration Test Officer",
            departmentId
        };
    }

    private sealed class DepartmentResponse
    {
        public int Id { get; set; }
    }
}
