using Google.Cloud.Firestore;
using WebApplication2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication2.Services
{
    public class EmployeeService
    {
        private readonly FirestoreService _firestoreService;

        public EmployeeService(FirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _firestoreService.GetAllEmployeesAsync();
        }

        public async Task<Employee> GetEmployeeByIdAsync(string employeeId)
        {
            return await _firestoreService.GetEmployeeByIdAsync(employeeId);
        }

        public async Task<List<Skill>> GetEmployeeSkillsAsync(string employeeId)
        {
            return await _firestoreService.GetEmployeeSkillsAsync(employeeId);
        }

        public async Task<List<Qualification>> GetEmployeeQualificationsAsync(string employeeId)
        {
            return await _firestoreService.GetEmployeeQualificationsAsync(employeeId);
        }

        public async Task<List<Training>> GetEmployeeTrainingsAsync(string employeeId)
        {
            return await _firestoreService.GetEmployeeTrainingsAsync(employeeId);
        }

        public async Task<string> DebugEmployeeData(string employeeId = null)
        {
            return await _firestoreService.DebugEmployeeData(employeeId);
        }
    }
}