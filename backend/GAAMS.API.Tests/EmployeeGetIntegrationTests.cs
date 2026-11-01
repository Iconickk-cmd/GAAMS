
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GAAMS.API.Tests;

public class EmployeeGetIntegrationTests
{
    [Fact]
    public async Task GetEmployeeById_WithExistingId_ReturnsEmployee()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var departmentId = await CreateDepartmentAsync(client);

        var employeeNumber = $"GAAMS-GET-{Guid.NewGuid():N}";
        var nationalId = $"N-{Guid.NewGuid():N}".Substring(0, 20);

        var createResponse = await client.PostAsJsonAsync(
            "/api/employees",
            CreateEmployeeRequest(employeeNumber, nationalId, departmentId));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdEmployee =
            await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();



        Assert.NotNull(createdEmployee);

        var getResponse = await client.GetAsync(
            $"/api/employees/{createdEmployee.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var employee =
            await getResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        Assert.NotNull(employee);
        Assert.Equal(createdEmployee.Id, employee.Id);
        Assert.Equal(employeeNumber, employee.EmployeeNumber);
        Assert.Equal("Integration", employee.FirstName);
        Assert.Equal("Test", employee.LastName);
        Assert.Equal(departmentId, employee.DepartmentId);
    }

    [Fact]
    public async Task GetEmployeeById_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new GAAMSTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/employees/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Create department failed: {(int)response.StatusCode}");
            Console.WriteLine(errorBody);

            throw new Exception(
                $"Create department failed with status {(int)response.StatusCode}: {errorBody}");
        }

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
        public string EmployeeNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
    }
}
