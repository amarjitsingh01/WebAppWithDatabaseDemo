using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace WebApp.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly WebAppContext _context;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly IConfiguration _configuration;

        public EmployeesController(WebAppContext context, BlobContainerClient blobContainerClient, IConfiguration configuration)
        {
            _context = context;
            _blobContainerClient = blobContainerClient;
            _configuration = configuration;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            return View(await _context.Employee.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employee.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Fullname,Department,Email,Phone,Address")] Employee employee,
            IFormFile file)
        {
            if (!ModelState.IsValid)
                return View(employee);

            // Save employee first
            _context.Add(employee);
            await _context.SaveChangesAsync();

            // Upload image if provided
            if (file != null && file.Length > 0)
            {
                var blobName = $"{employee.Id}_{Guid.NewGuid()}_{file.FileName}";
                var blobClient = _blobContainerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);

                employee.ImageUrl = blobClient.Uri.ToString();
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }

            // Call Logic App
            try
            {

                var logicAppUrl = _configuration["LogicApp:EmployeeCreatedUrl"];
            if (!string.IsNullOrEmpty(logicAppUrl))
            {
                using var client = new HttpClient();

                var payload = new
                {
                    FullName = employee.Fullname,
                    Email = employee.Email,
                    Department = employee.Department,
                    Phone = employee.Phone,
                    Address = employee.Address
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(logicAppUrl, content);
            }
        }
            catch
            {
                // Do not block save
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employee.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Fullname,Department,Email,Phone,Address")] Employee employee,
            IFormFile file)
        {
            var empToUpdate = await _context.Employee.FindAsync(id);
            if (empToUpdate == null) return NotFound();

            if (!ModelState.IsValid)
                return View(empToUpdate);

            // Update fields
            empToUpdate.Fullname = employee.Fullname;
            empToUpdate.Department = employee.Department;
            empToUpdate.Email = employee.Email;
            empToUpdate.Phone = employee.Phone;
            empToUpdate.Address = employee.Address;

            // Update image if provided
            if (file != null && file.Length > 0)
            {
                var blobName = $"{empToUpdate.Id}_{Guid.NewGuid()}_{file.FileName}";
                var blobClient = _blobContainerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);
                empToUpdate.ImageUrl = blobClient.Uri.ToString();
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employee.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employee.FindAsync(id);
            if (employee != null)
            {
                _context.Employee.Remove(employee);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employee.Any(e => e.Id == id);
        }
    }
}
