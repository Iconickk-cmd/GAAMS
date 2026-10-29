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

        var employeeNumber = $"GAAMS-TEST-{Guid.NewGuid():N}";
        var nationalId = $"N-{Guid.NewGuid():N}".Substring(0, 20);

        var firstEmployee = CreateEmployeeRequest(
            employeeNumber,
            nationalId,
            departmentId);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/employees",
            firstEmployee);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateEmployee = CreateEmployeeRequest(
            employeeNumber,
             $"N-{Guid.NewGuid():N}".Substring(0, 20),
            departmentId);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/employees",
            duplicateEmployee);

        Console.WriteLine($"Duplicate employee response: {(int)duplicateResponse.StatusCode}");
        Console.WriteLine(await duplicateResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_WithDuplicateNationalId_ReturnsConflict()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var employeeNumber = $"GAAMS-TEST-{Guid.NewGuid():N}";
        var nationalId = $"N-{Guid.NewGuid():N}".Substring(0, 20);

        var firstEmployee = CreateEmployeeRequest(
            employeeNumber,
            nationalId,
            departmentId);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/employees",
            firstEmployee);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateEmployee = CreateEmployeeRequest(
            $"GAAMS-TEST-{Guid.NewGuid():N}",
            nationalId,
            departmentId);

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/employees",
            duplicateEmployee);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_WithValidData_ReturnsUpdatedEmployee()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var employeeNumber = $"GAAMS-UPDATE-{Guid.NewGuid():N}";
        var nationalId = $"N-{Guid.NewGuid():N}".Substring(0, 20);

        var createRequest = CreateEmployeeRequest(
            employeeNumber,
            nationalId,
            departmentId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/employees",
            createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEmployee =
            await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        Assert.NotNull(createdEmployee);

        var updateRequest = new
        {
            employeeNumber,
            firstName = "Updated",
            lastName = "Employee",
            nationalId,
            email = "updated.employee@gaams.local",
            phoneNumber = "0712345678",
            dateOfBirth = "1990-05-10",
            dateOfEmployment = "2020-01-15",
            jobTitle = "Updated Test Officer",
            departmentId,
            isActive = true
        };

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/employees/{createdEmployee.Id}",
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedEmployee =
            await updateResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        Assert.NotNull(updatedEmployee);
        Assert.Equal("Updated", updatedEmployee.FirstName);
        Assert.Equal("Employee", updatedEmployee.LastName);
        Assert.Equal("Updated Test Officer", updatedEmployee.JobTitle);
        Assert.Equal("updated.employee@gaams.local", updatedEmployee.Email);
        Assert.Equal(departmentId, updatedEmployee.DepartmentId);
        Assert.True(updatedEmployee.IsActive);
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

        var result =
            await response.Content.ReadFromJsonAsync<DepartmentResponse>();

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

    private sealed class EmployeeResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }
}